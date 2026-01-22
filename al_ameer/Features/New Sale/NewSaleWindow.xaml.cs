using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace al_ameer.Features.Sales
{
    public partial class NewSaleWindow : Window
    {
        // Database Connection String
        private readonly string connString = "Server=DESKTOP-TVOR3BK;Database=al_ameer;Trusted_Connection=True;TrustServerCertificate=True;";

        // Observable collection for DataGrid binding
        public ObservableCollection<CartItem> CartItems { get; set; } = new ObservableCollection<CartItem>();

        private int selectedCustomerId = 1; // Default to Walk-in Customer
        private const decimal ExchangeRate = 89500; // Updated rate for Al Ameer

        public NewSaleWindow()
        {
            InitializeComponent();
            dgCart.ItemsSource = CartItems;
            UpdateTotals();
            txtSearch.Focus();
        }

        #region Search & Barcode Logic

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                ExecuteProductSearch(txtSearch.Text.Trim());
                txtSearch.Clear();
            }
        }

        private void ExecuteProductSearch(string term)
        {
            using SqlConnection conn = new SqlConnection(connString);
            // Search by exact Barcode or partial Product Name
            string sql = @"SELECT ProductID, ProductName, SellingPrice 
                           FROM Products 
                           WHERE Barcode = @t OR ProductName LIKE @like";

            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@t", term);
            cmd.Parameters.AddWithValue("@like", $"%{term}%");

            try
            {
                conn.Open();
                using SqlDataReader rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    int id = rdr.GetInt32(0);
                    string name = rdr.GetString(1);
                    decimal price = rdr.GetDecimal(2);

                    var existing = CartItems.FirstOrDefault(x => x.ProductID == id);
                    if (existing != null)
                    {
                        existing.CartQuantity++;
                    }
                    else
                    {
                        CartItems.Add(new CartItem(id, name, price, 1));
                    }
                    UpdateTotals();
                }
                else
                {
                    // Product not found
                    System.Media.SystemSounds.Exclamation.Play();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Search Error: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FocusBarcode_Click(object sender, RoutedEventArgs e) => txtSearch.Focus();

        #endregion

        #region Calculation & UI Updates

        private void UpdateTotals()
        {
            decimal totalLBP = CartItems.Sum(x => x.LineTotal);
            lblTotal.Text = $"{totalLBP:N0} LBP";
            lblTotalUSD.Text = $"≈ ${(totalLBP / ExchangeRate):N2} USD";
            lblTotalItems.Text = CartItems.Sum(x => x.CartQuantity).ToString();

            // Refresh DataGrid to update LineTotals
            dgCart.Items.Refresh();
        }

        private void QtyPlus_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is CartItem item)
            {
                item.CartQuantity++;
                UpdateTotals();
            }
        }

        private void QtyMinus_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is CartItem item && item.CartQuantity > 1)
            {
                item.CartQuantity--;
                UpdateTotals();
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is CartItem item)
            {
                CartItems.Remove(item);
                UpdateTotals();
            }
        }

        #endregion

        #region Transaction & Printing

        private void CompleteSale_Click(object sender, RoutedEventArgs e)
        {
            if (!CartItems.Any()) return;

            using SqlConnection conn = new SqlConnection(connString);
            conn.Open();
            using SqlTransaction trans = conn.BeginTransaction();

            try
            {
                // 1. Insert into Sales Table
                string saleSql = @"INSERT INTO Sales (SaleDate, CustomerID, TotalAmount, PaymentMethod) 
                                   OUTPUT INSERTED.SaleID 
                                   VALUES (GETDATE(), @cId, @total, 'Cash')";

                using SqlCommand sCmd = new SqlCommand(saleSql, conn, trans);
                sCmd.Parameters.AddWithValue("@cId", selectedCustomerId);
                sCmd.Parameters.AddWithValue("@total", CartItems.Sum(x => x.LineTotal));
                int saleId = (int)sCmd.ExecuteScalar();

                // 2. Loop Items for Details and Inventory Update
                foreach (var item in CartItems)
                {
                    // Add Detail
                    using SqlCommand dCmd = new SqlCommand("INSERT INTO SalesDetails (SaleID, ProductID, Quantity, UnitPrice) VALUES (@s, @p, @q, @u)", conn, trans);
                    dCmd.Parameters.AddWithValue("@s", saleId);
                    dCmd.Parameters.AddWithValue("@p", item.ProductID);
                    dCmd.Parameters.AddWithValue("@q", item.CartQuantity);
                    dCmd.Parameters.AddWithValue("@u", item.SellingPrice);
                    dCmd.ExecuteNonQuery();

                    // Reduce Inventory
                    using SqlCommand uCmd = new SqlCommand("UPDATE Products SET QuantityInStock = QuantityInStock - @q WHERE ProductID = @p", conn, trans);
                    uCmd.Parameters.AddWithValue("@q", item.CartQuantity);
                    uCmd.Parameters.AddWithValue("@p", item.ProductID);
                    uCmd.ExecuteNonQuery();
                }

                trans.Commit();
                MessageBox.Show($"Transaction #{saleId} Successful!", "Al Ameer Tires", MessageBoxButton.OK, MessageBoxImage.Information);

                // Auto-print receipt if needed
                GenerateReceipt();

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                MessageBox.Show($"Critical Error: {ex.Message}", "Transaction Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e) => GenerateReceipt();

        private void GenerateReceipt()
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                FlowDocument doc = new FlowDocument();
                doc.PagePadding = new Thickness(10);
                doc.ColumnWidth = pd.PrintableAreaWidth;

                Paragraph header = new Paragraph(new Run("AL AMEER TIRES")) { TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, FontSize = 18 };
                doc.Blocks.Add(header);
                doc.Blocks.Add(new Paragraph(new Run($"Date: {DateTime.Now}")) { FontSize = 10 });

                foreach (var item in CartItems)
                {
                    doc.Blocks.Add(new Paragraph(new Run($"{item.ProductName}\n{item.CartQuantity} x {item.SellingPrice:N0} = {item.LineTotal:N0} LBP")) { FontSize = 10 });
                }

                doc.Blocks.Add(new Paragraph(new Run($"TOTAL: {lblTotal.Text}")) { FontWeight = FontWeights.Bold, FontSize = 14, TextAlignment = TextAlignment.Right });

                pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Sale Receipt");
            }
        }

        #endregion

        #region Navigation & Stubs

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();

        private void AddCustomer_Click(object sender, MouseButtonEventArgs e)
        {
            // Placeholder for customer selection window
            MessageBox.Show("Opening Customer Directory...", "Al Ameer CRM");
        }

        private void SaveDraft_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Order saved as draft locally.", "Draft Saved");
        }

        #endregion
    }

    // Modern CartItem model with Property Change Notification
    public class CartItem : INotifyPropertyChanged
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal SellingPrice { get; set; }

        private int _cartQuantity;
        public int CartQuantity
        {
            get => _cartQuantity;
            set { _cartQuantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); }
        }

        public decimal LineTotal => SellingPrice * CartQuantity;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public CartItem(int id, string name, decimal price, int qty)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            ProductID = id;
            ProductName = name;
            SellingPrice = price;
            CartQuantity = qty;
        }

#pragma warning disable CS8612 // Nullability of reference types in type doesn't match implicitly implemented member.
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS8612 // Nullability of reference types in type doesn't match implicitly implemented member.
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}