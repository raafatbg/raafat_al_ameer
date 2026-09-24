using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using al_ameer.Data;
using al_ameer.Models;
using Microsoft.EntityFrameworkCore;
using al_ameer.Services;
using System.Windows.Controls;

namespace al_ameer.Features.Inventory
{
    public partial class EditProductWindow : Window
    {
        private int _productId;

        public EditProductWindow(Product product)
        {
            InitializeComponent();
            txtExchangeRate.Text = AppSettings.Current.LbpPerUsd.ToString("0.##");
            txtExchangeRate.IsReadOnly = true;
            txtExchangeRate.ToolTip = "Change the exchange rate in Settings";
            _productId = product.ProductId;
            LoadCategories();
            PopulateData(product);
        }

        private void LoadCategories()
        {
            using (var db = new AppDbContext())
            {
                var categories = db.Categories.ToList();
                cbCategory.ItemsSource = categories;
                cbCategory.DisplayMemberPath = "CategoryName";
                cbCategory.SelectedValuePath = "CategoryId";
            }
        }

        private void PopulateData(Product p)
        {
            txtName.Text = p.ProductName;
            txtBarcode.Text = p.Barcode;
            cbCategory.SelectedValue = p.CategoryId;
            bool usd = string.Equals(p.Currency, "USD", StringComparison.OrdinalIgnoreCase);
            cbCurrency.SelectedIndex = usd ? 1 : 0;
            txtPrice.Text = (usd && p.SellingPriceUSD > 0 ? p.SellingPriceUSD : p.SellingPrice).ToString(usd ? "N2" : "N0");

            // Fix: Convert int? to string for display, handling nulls with ?? ""
            txtStock.Text = (p.StockQuantity ?? 0).ToString();
            txtWidth.Text = p.TireWidth?.ToString() ?? "";
            txtRatio.Text = p.TireRatio?.ToString() ?? "";
            txtDiameter.Text = p.TireDiameter?.ToString() ?? "";
        }

        private string SelectedCurrency => (cbCurrency?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "LBP";

        private void PricingInput_Changed(object sender, RoutedEventArgs e)
        {
            if (txtConvertedPrice == null || txtPrice == null || txtExchangeRate == null) return;
            if (!InputParser.TryNonNegativeMoney(txtPrice.Text, out decimal price) ||
                !InputParser.TryNonNegativeMoney(txtExchangeRate.Text, out decimal rate) || rate <= 0)
            {
                txtConvertedPrice.Text = "Enter a valid price and exchange rate.";
                return;
            }
            txtConvertedPrice.Text = SelectedCurrency == "USD"
                ? $"{price * rate:N0} LBP  |  ${price:N2} USD"
                : $"{price:N0} LBP  |  ${price / rate:N2} USD";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) || cbCategory.SelectedValue is not int categoryId ||
                    !InputParser.TryNonNegativeInt(txtStock.Text, out int stock) ||
                    !InputParser.TryNonNegativeMoney(txtPrice.Text, out decimal rawPrice) || rawPrice <= 0 ||
                    !InputParser.TryNonNegativeMoney(txtExchangeRate.Text, out decimal exchangeRate) || exchangeRate <= 0)
                    throw new ArgumentException("Name and category are required; stock must be non-negative and price must be greater than zero.");
                bool hasAnyTireValue = !string.IsNullOrWhiteSpace(txtWidth.Text) || !string.IsNullOrWhiteSpace(txtRatio.Text) || !string.IsNullOrWhiteSpace(txtDiameter.Text);
                int? width = null, ratio = null, diameter = null;
                if (hasAnyTireValue)
                {
                    if (!InputParser.TryPositiveInt(txtWidth.Text, out int w) || !InputParser.TryPositiveInt(txtRatio.Text, out int r) || !InputParser.TryPositiveInt(txtDiameter.Text, out int d))
                        throw new ArgumentException("All tire dimensions must be positive whole numbers.");
                    width = w; ratio = r; diameter = d;
                }
                using (var db = new AppDbContext())
                {
                    var product = db.Products.FirstOrDefault(p => p.ProductId == _productId);
                    if (product == null) return;

                    product.ProductName = txtName.Text.Trim();
                    product.Barcode = string.IsNullOrWhiteSpace(txtBarcode.Text) ? null : txtBarcode.Text.Trim();
                    product.CategoryId = categoryId;

                    // Fix: Parse string back to int?
                    product.StockQuantity = stock;

                    // Fix: Explicitly parse Tire Specs from string to int? to match SQL INT type
                    product.TireWidth = width;
                    product.TireRatio = ratio;
                    product.TireDiameter = diameter;
                    product.IsTire = hasAnyTireValue;

                    // LBP Rounding Logic: Round to nearest 1,000
                    product.Currency = SelectedCurrency;
                    if (product.Currency == "USD")
                    {
                        product.SellingPriceUSD = rawPrice;
                        product.SellingPrice = decimal.Round(rawPrice * exchangeRate, 2);
                        product.CostPriceUSD = decimal.Round(product.CostPrice / exchangeRate, 2);
                    }
                    else
                    {
                        product.SellingPrice = rawPrice;
                        product.SellingPriceUSD = decimal.Round(rawPrice / exchangeRate, 2);
                        product.CostPriceUSD = decimal.Round(product.CostPrice / exchangeRate, 2);
                    }

                    db.SaveChanges();
                    DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving product: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }
    }
}
