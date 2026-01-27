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

namespace al_ameer.Features.Sales
{
    public partial class NewSaleWindow : Window
    {
        private readonly string connString = "Server=DESKTOP-TVOR3BK;Database=al_ameer;Trusted_Connection=True;TrustServerCertificate=True;";
        public ObservableCollection<CartItem> CartItems { get; set; } = new ObservableCollection<CartItem>();
        private int selectedCustomerId = 1; // Default: Walk-in

        public NewSaleWindow()
        {
            InitializeComponent();
            dgCart.ItemsSource = CartItems;
            txtSearch.Focus();
        }

        #region Core POS Logic

        private void ExecuteSearch(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                // Search by Barcode or Name
                string sql = "SELECT ProductID, ProductName, ISNULL(SellingPrice, 0), ISNULL(StockQuantity, 0) FROM Products WHERE Barcode = @t OR ProductName LIKE @like";
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

                            var existing = CartItems.FirstOrDefault(x => x.ProductID == id);
                            if (existing != null)
                            {
                                // Validation for quantity increase
                                if (existing.CartQuantity + 1 > stock)
                                {
                                    MessageBox.Show("Cannot add more. Limit reached for available stock.");
                                    return;
                                }
                                existing.CartQuantity++;
                            }
                            else
                            {
                                CartItems.Add(new CartItem(id, name, price, 1));
                            }

                            UpdateTotals();
                            txtSearch.Clear();
                        }
                        else { System.Media.SystemSounds.Exclamation.Play(); }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Database Error: " + ex.Message); }
            }
        }

        private void CompleteSale_Click(object sender, RoutedEventArgs e)
        {
            if (!CartItems.Any()) return;

            using SqlConnection conn = new SqlConnection(connString);
            conn.Open();

            // 1. FINAL STOCK VALIDATION BEFORE SAVING
            foreach (var item in CartItems)
            {
                using SqlCommand checkCmd = new SqlCommand("SELECT StockQuantity FROM Products WHERE ProductID = @p", conn);
                checkCmd.Parameters.AddWithValue("@p", item.ProductID);
                object result = checkCmd.ExecuteScalar();
                int currentStock = (result != null && result != DBNull.Value) ? (int)result : 0;

                if (item.CartQuantity > currentStock)
                {
                    MessageBox.Show($"Stock changed! Only {currentStock} left for {item.ProductName}. Please adjust cart.");
                    return;
                }
            }

            // 2. SAVE TRANSACTION
            using SqlTransaction trans = conn.BeginTransaction();
            try
            {
                string cleanTotalStr = lblTotal.Text.Replace(" LBP", "").Replace(",", "");
                decimal.TryParse(cleanTotalStr, out decimal total);
                string invoiceNum = "INV-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");

                // Insert Main Sale
                string saleSql = @"INSERT INTO Sales (InvoiceNumber, SaleDate, CustomerID, GrandTotal, PaymentMethod) 
                                   OUTPUT INSERTED.SaleID VALUES (@inv, GETDATE(), @c, @total, 'Cash')";
                SqlCommand cmd = new SqlCommand(saleSql, conn, trans);
                cmd.Parameters.AddWithValue("@inv", invoiceNum);
                cmd.Parameters.AddWithValue("@c", selectedCustomerId);
                cmd.Parameters.AddWithValue("@total", total);
                int saleId = (int)cmd.ExecuteScalar();

                // Insert Items and Update Stock
                foreach (var item in CartItems)
                {
                    // FIXED: LineTotal is omitted because it is a Computed Column in DB
                    string itemSql = "INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice) VALUES (@s, @p, @q, @u)";
                    SqlCommand iCmd = new SqlCommand(itemSql, conn, trans);
                    iCmd.Parameters.AddWithValue("@s", saleId);
                    iCmd.Parameters.AddWithValue("@p", item.ProductID);
                    iCmd.Parameters.AddWithValue("@q", item.CartQuantity);
                    iCmd.Parameters.AddWithValue("@u", item.SellingPrice);
                    iCmd.ExecuteNonQuery();

                    // Update Stock (Guaranteed to be >= 0 because of validation above)
                    SqlCommand uCmd = new SqlCommand("UPDATE Products SET StockQuantity = StockQuantity - @q WHERE ProductID = @p", conn, trans);
                    uCmd.Parameters.AddWithValue("@q", item.CartQuantity);
                    uCmd.Parameters.AddWithValue("@p", item.ProductID);
                    uCmd.ExecuteNonQuery();
                }

                trans.Commit();
                MessageBox.Show($"Success! Invoice: {invoiceNum}");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                MessageBox.Show("Transaction Failed: " + ex.Message);
            }
        }

        #endregion

        #region Event Handlers & UI

        private void UpdateTotals()
        {
            if (lblTotal == null || txtDiscount == null || txtTax == null) return;
            decimal subtotal = CartItems.Sum(x => x.LineTotal);
            decimal.TryParse(txtDiscount.Text, out decimal discount);
            decimal.TryParse(txtTax.Text, out decimal taxPercent);
            decimal.TryParse(txtExchangeRate.Text, out decimal rate);
            if (rate <= 0) rate = 90000;

            decimal grandTotal = (subtotal - discount) + ((subtotal - discount) * (taxPercent / 100));
            lblTotal.Text = $"{grandTotal:N0} LBP";
            lblTotalUSD.Text = $"$ {(grandTotal / rate):N2} USD";
            if (dgCart.ItemsSource != null) dgCart.Items.Refresh();
        }

        private void AddCustomer_Click(object sender, MouseButtonEventArgs e)
        {
            var win = new SelectCustomerWindow { Owner = this };
            if (win.ShowDialog() == true)
            {
                selectedCustomerId = win.SelectedID;
                lblCustomerName.Text = win.SelectedName;
            }
        }

        private void FocusBarcode_Click(object sender, RoutedEventArgs e) => txtSearch.Focus();
        private void Refresh_Click(object sender, RoutedEventArgs e) { CartItems.Clear(); UpdateTotals(); txtSearch.Focus(); }
        private void txtSearch_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) ExecuteSearch(txtSearch.Text); }
        private void UpdateCalculations_Changed(object sender, TextChangedEventArgs e) => UpdateTotals();
        private void QtyPlus_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.DataContext is CartItem i) { i.CartQuantity++; UpdateTotals(); } }
        private void QtyMinus_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.DataContext is CartItem i && i.CartQuantity > 1) { i.CartQuantity--; UpdateTotals(); } }
        private void RemoveItem_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.DataContext is CartItem i) { CartItems.Remove(i); UpdateTotals(); } }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();

        #endregion
    }

    public class CartItem : INotifyPropertyChanged
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public decimal SellingPrice { get; set; }
        private int _qty;
        public int CartQuantity { get => _qty; set { _qty = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); } }
        public decimal LineTotal => SellingPrice * CartQuantity;
        public CartItem(int id, string n, decimal p, int q) { ProductID = id; ProductName = n; SellingPrice = p; CartQuantity = q; }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}