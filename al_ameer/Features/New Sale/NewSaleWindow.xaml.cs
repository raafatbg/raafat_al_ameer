using al_ameer.Features.Customers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using al_ameer.Data;
using al_ameer.Services;
using System.Windows.Documents;
using System.Windows.Media;

namespace al_ameer.Features.Sales
{
    public partial class NewSaleWindow : Window
    {
        private readonly string connString = DatabaseConfig.ConnectionString;
        public ObservableCollection<CartItem> CartItems { get; set; } = new ObservableCollection<CartItem>();
        private int? selectedCustomerId;

        public NewSaleWindow()
        {
            InitializeComponent();
            txtExchangeRate.Text = AppSettings.Current.LbpPerUsd.ToString("0.##");
            txtExchangeRate.IsReadOnly = true;
            txtExchangeRate.ToolTip = "Change the exchange rate in Settings / الإعدادات";
            dgCart.ItemsSource = CartItems;
            LoadBrowser();
            txtSearch.Focus();
        }

        #region Core POS Logic

        private void ExecuteSearch(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                // Search by Barcode or Name
                string sql = "SELECT TOP (1) ProductID, ProductName, ISNULL(SellingPrice, 0), ISNULL(StockQuantity, 0) FROM Products WHERE ISNULL(IsActive,1)=1 AND (Barcode = @t OR ProductName LIKE @like) ORDER BY CASE WHEN Barcode=@t THEN 0 ELSE 1 END, ProductName";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@t", term.Trim());
                cmd.Parameters.AddWithValue("@like", $"%{term.Trim()}%");

                try
                {
                    conn.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            int id = rdr.GetInt32(0);
                            string name = rdr.GetString(1);
                            decimal price = rdr.GetDecimal(2);
                            int stock = rdr.GetInt32(3);

                            // STOP if already out of stock
                            if (stock <= 0)
                            {
                                MessageBox.Show($"{name} is out of stock!", "Inventory Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            AddProductToCart(id, name, price, stock);
                        }
                        else { System.Media.SystemSounds.Exclamation.Play(); }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Database Error: " + ex.Message); }
            }
        }

        private void CompleteSale_Click(object sender, RoutedEventArgs e)
        {
            if (!CartItems.Any()) { MessageBox.Show("Add at least one product."); return; }
            try
            {
                if (!InputParser.TryNonNegativeMoney(txtDiscount.Text, out decimal discount) ||
                    !InputParser.TryNonNegativeMoney(txtTax.Text, out decimal tax) || tax > 100 ||
                    !TryTenderAmount(txtTenderLBP.Text, out decimal receivedLbp) ||
                    !TryTenderAmount(txtTenderUSD.Text, out decimal receivedUsd))
                    throw new ArgumentException("Enter valid non-negative discount, tax, LBP and USD amounts (tax maximum 100%).");
                string payment = (cbPaymentMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cash";
                BillingTotals totals = BillingCalculator.Calculate(CartItems.Select(x => (x.SellingPrice, x.CartQuantity)), discount, tax);
                TenderInput input = new(receivedLbp, receivedUsd, AppSettings.Current.LbpPerUsd);
                TenderSummary tender = TenderCalculator.Calculate(totals.GrandTotal, input);
                if (tender.RemainingLBP > 0 && selectedCustomerId is null)
                    throw new ArgumentException("Select a customer for an unpaid balance.");
                if (payment == "Cash" && tender.RemainingLBP > 0)
                    throw new ArgumentException("For a partial payment, choose Credit.");
                if (payment == "Card" && tender.ChangeLBP > 0)
                    throw new ArgumentException("Card tender cannot exceed the sale total.");
                string prompt = $"Total: {totals.GrandTotal:N2} LBP\nReceived: {receivedLbp:N2} LBP + {receivedUsd:N2} USD\n" +
                    $"Rate: {input.LbpPerUsd:N2} LBP/USD\nReturn: {tender.ChangeLBP:N2} LBP (≈ ${tender.ChangeUSD:N2})\n" +
                    $"Balance due: {tender.RemainingLBP:N2} LBP\n\nSave sale and receipt?";
                if (MessageBox.Show(prompt, "Confirm payment", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
                var request = new SaleRequest(selectedCustomerId,
                    CartItems.Select(x => new SaleLine(x.ProductID, x.ProductName, x.SellingPrice, x.CartQuantity, x.ServiceID)).ToList(),
                    discount, tax, payment, tender.AppliedLBP, input);
                SaleResult result = new SaleService().Complete(request);
                if (chkPrintReceipt.IsChecked == true) PrintReceipt(result);
                MessageBox.Show($"Saved invoice {result.InvoiceNumber} and receipt {result.ReceiptNumber}.\n" +
                    $"Return: {result.Tender.ChangeLBP:N2} LBP\nBalance due: {result.RemainingBalance:N2} LBP");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Transaction Failed: " + ex.Message);
            }
        }

        #endregion

        #region Event Handlers & UI

        private void LoadBrowser()
        {
            try { tvBrowser.ItemsSource = new SaleBrowserService().Load(); }
            catch (Exception ex) { MessageBox.Show("Item browser could not load: " + ex.Message); }
        }

        private void BrowserRefresh_Click(object sender, RoutedEventArgs e) => LoadBrowser();

        private void Browser_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (tvBrowser.SelectedItem is not SaleBrowserNode item || item.Kind == "Folder") return;
            if (item.Kind == "Service") AddServiceToCart(item.Id, item.Name, item.Price);
            else AddProductToCart(item.Id, item.Name, item.Price, item.Stock);
            e.Handled = true;
        }

        private void AddProductToCart(int id, string name, decimal price, int stock)
        {
            if (stock <= 0) { MessageBox.Show($"{name} is out of stock."); return; }
            var existing = CartItems.FirstOrDefault(x => x.ServiceID is null && x.ProductID == id);
            if (existing is not null)
            {
                if (existing.CartQuantity >= stock) { MessageBox.Show($"Only {stock} in stock."); return; }
                existing.CartQuantity++;
            }
            else CartItems.Add(new CartItem(id, name, price, 1, stock));
            UpdateTotals();
            txtSearch.Clear();
        }

        private void AddServiceToCart(int id, string name, decimal price)
        {
            var existing = CartItems.FirstOrDefault(x => x.ServiceID == id);
            if (existing is not null) existing.CartQuantity++;
            else CartItems.Add(new CartItem(0, name, price, 1, int.MaxValue, id));
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            if (lblTotal == null || txtDiscount == null || txtTax == null) return;
            decimal subtotal = CartItems.Sum(x => x.LineTotal);
            InputParser.TryNonNegativeMoney(txtDiscount.Text, out decimal discount);
            InputParser.TryNonNegativeMoney(txtTax.Text, out decimal taxPercent);
            InputParser.TryNonNegativeMoney(txtExchangeRate.Text, out decimal rate);
            if (rate <= 0) rate = AppSettings.Current.LbpPerUsd;
            decimal grandTotal = 0;
            if (discount <= subtotal && taxPercent <= 100)
                grandTotal = BillingCalculator.Calculate(CartItems.Select(x => (x.SellingPrice, x.CartQuantity)), discount, taxPercent).GrandTotal;
            lblTotal.Text = $"{grandTotal:N0} LBP";
            lblTotalUSD.Text = $"$ {(grandTotal / rate):N2} USD";
            UpdateTenderPreview(grandTotal);
            if (dgCart.ItemsSource != null) dgCart.Items.Refresh();
        }

        private static bool TryTenderAmount(string? text, out decimal amount)
        {
            if (string.IsNullOrWhiteSpace(text)) { amount = 0m; return true; }
            return InputParser.TryNonNegativeMoney(text, out amount);
        }

        private void UpdateTenderPreview(decimal total)
        {
            if (txtTenderLBP == null || txtTenderUSD == null || lblChange == null || btnComplete == null) return;
            if (!TryTenderAmount(txtTenderLBP.Text, out decimal lbp) ||
                !TryTenderAmount(txtTenderUSD.Text, out decimal usd))
            {
                lblTenderTotal.Text = "Enter valid LBP and USD amounts.";
                lblChange.Text = "Return: —";
                lblRemaining.Text = "Balance due: —";
                btnComplete.IsEnabled = false;
                return;
            }
            try
            {
                TenderSummary tender = TenderCalculator.Calculate(total,
                    new TenderInput(lbp, usd, AppSettings.Current.LbpPerUsd));
                lblTenderTotal.Text = $"Received: {tender.TenderTotalLBP:N2} LBP";
                lblChange.Text = $"Return: {tender.ChangeLBP:N2} LBP (≈ ${tender.ChangeUSD:N2})";
                lblRemaining.Text = $"Balance due: {tender.RemainingLBP:N2} LBP";
                string method = (cbPaymentMethod?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cash";
                btnComplete.IsEnabled = !(method == "Card" && tender.ChangeLBP > 0);
            }
            catch (ArgumentException)
            {
                lblTenderTotal.Text = "Check payment amounts and exchange rate.";
                btnComplete.IsEnabled = false;
            }
        }

        private void PaymentMethod_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (lblTotal != null) UpdateTotals();
        }

        private void Tender_Changed(object sender, TextChangedEventArgs e) => UpdateTotals();

        private void AddCustomer_Click(object sender, MouseButtonEventArgs e)
        {
            var win = new SelectCustomerWindow { Owner = this };
            if (win.ShowDialog() == true)
            {
                selectedCustomerId = win.SelectedID;
                lblCustomerName.Text = win.SelectedName;
                UpdateTotals();
            }
        }

        private void FocusBarcode_Click(object sender, RoutedEventArgs e) => txtSearch.Focus();
        private void Refresh_Click(object sender, RoutedEventArgs e) { CartItems.Clear(); UpdateTotals(); txtSearch.Focus(); }
        private void txtSearch_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) ExecuteSearch(txtSearch.Text); }
        private void UpdateCalculations_Changed(object sender, TextChangedEventArgs e) => UpdateTotals();
        private void QtyPlus_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.DataContext is CartItem i) { if (i.ServiceID is null && i.CartQuantity >= i.AvailableStock) { MessageBox.Show($"Only {i.AvailableStock} in stock."); return; } i.CartQuantity++; UpdateTotals(); } }
        private void QtyMinus_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.DataContext is CartItem i && i.CartQuantity > 1) { i.CartQuantity--; UpdateTotals(); } }
        private void RemoveItem_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.DataContext is CartItem i) { CartItems.Remove(i); UpdateTotals(); } }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();

        private void PrintReceipt(SaleResult sale)
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;
            var doc = new FlowDocument { PagePadding = new Thickness(35), FontFamily = new FontFamily("Segoe UI") };
            doc.Blocks.Add(new Paragraph(new Run("AL AMEER TIRES")) { FontSize = 22, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
            doc.Blocks.Add(new Paragraph(new Run($"Receipt: {sale.ReceiptNumber}\nInvoice: {sale.InvoiceNumber}\nDate: {DateTime.Now:g}\nCustomer: {lblCustomerName.Text}")));
            foreach (CartItem item in CartItems) doc.Blocks.Add(new Paragraph(new Run($"{item.ItemType}: {item.ProductName}  x{item.CartQuantity}  {item.LineTotal:N2} LBP")));
            doc.Blocks.Add(new Paragraph(new Run($"Subtotal: {sale.Totals.Subtotal:N2}\nDiscount: {sale.Totals.Discount:N2}\nTax: {sale.Totals.TaxAmount:N2}\nTOTAL: {sale.Totals.GrandTotal:N2} LBP\n" +
                $"Received: {sale.Tender.ReceivedLBP:N2} LBP + {sale.Tender.ReceivedUSD:N2} USD\nRate: {sale.Tender.LbpPerUsd:N2} LBP/USD\n" +
                $"Applied: {sale.PaidAmount:N2} LBP\nReturn: {sale.Tender.ChangeLBP:N2} LBP (≈ ${sale.Tender.ChangeUSD:N2})\nBalance: {sale.RemainingBalance:N2} LBP")) { FontWeight = FontWeights.Bold });
            dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, sale.InvoiceNumber);
        }

        #endregion
    }

    public class CartItem : INotifyPropertyChanged
    {
        public int ProductID { get; set; }
        public int? ServiceID { get; }
        public string ItemType => ServiceID is null ? "Product" : "Service";
        public string ProductName { get; set; } = "";
        public decimal SellingPrice { get; set; }
        public int AvailableStock { get; }
        private int _qty;
        public int CartQuantity { get => _qty; set { _qty = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); } }
        public decimal LineTotal => SellingPrice * CartQuantity;
        public CartItem(int id, string n, decimal p, int q, int availableStock, int? serviceId = null) { ProductID = id; ServiceID = serviceId; ProductName = n; SellingPrice = p; CartQuantity = q; AvailableStock = availableStock; }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
