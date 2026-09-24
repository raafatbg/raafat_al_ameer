using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using al_ameer.Data;
using al_ameer.Models;
using al_ameer.Services;

namespace al_ameer.Features.Inventory
{
    public partial class AddProductWindow : Window
    {
        public AddProductWindow()
        {
            InitializeComponent();
            txtExchangeRate.Text = AppSettings.Current.LbpPerUsd.ToString("0.##");
            txtExchangeRate.IsReadOnly = true;
            txtExchangeRate.ToolTip = "Change the exchange rate in Settings";
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var categories = db.Categories.ToList();
                    var suppliers = db.Suppliers.ToList();

                    cbCategory.ItemsSource = categories;
                    cbSupplier.ItemsSource = suppliers;

                    if (categories.Any()) cbCategory.SelectedIndex = 0;
                    if (suppliers.Any()) cbSupplier.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading initial data: {ex.Message}");
            }
        }

        private void TireToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (pnlTireSpecs != null)
                pnlTireSpecs.Visibility = chkIsTire.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private string SelectedCurrency => (cbCurrency?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "LBP";

        private void PricingInput_Changed(object sender, RoutedEventArgs e)
        {
            if (txtConvertedPrices == null || txtCost == null || txtPrice == null || txtExchangeRate == null) return;
            if (!InputParser.TryNonNegativeMoney(txtCost.Text, out decimal cost) ||
                !InputParser.TryNonNegativeMoney(txtPrice.Text, out decimal price) ||
                !InputParser.TryNonNegativeMoney(txtExchangeRate.Text, out decimal rate) || rate <= 0)
            {
                txtConvertedPrices.Text = "Enter valid prices and an exchange rate greater than zero.";
                return;
            }
            (decimal costLbp, decimal priceLbp, decimal costUsd, decimal priceUsd) = ConvertPrices(cost, price, rate, SelectedCurrency);
            txtConvertedPrices.Text = $"LBP: Cost {costLbp:N0} · Sell {priceLbp:N0}   |   USD: Cost ${costUsd:N2} · Sell ${priceUsd:N2}";
        }

        private static (decimal CostLbp, decimal PriceLbp, decimal CostUsd, decimal PriceUsd) ConvertPrices(
            decimal cost, decimal price, decimal rate, string currency)
        {
            if (currency == "USD")
                return (decimal.Round(cost * rate, 2), decimal.Round(price * rate, 2), cost, price);
            return (cost, price, decimal.Round(cost / rate, 2), decimal.Round(price / rate, 2));
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (cbCategory.SelectedValue == null || cbSupplier.SelectedValue == null)
            {
                MessageBox.Show("Please select both a Category and a Supplier.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtProductName.Text))
            {
                MessageBox.Show("Product Name is required.");
                return;
            }

            try
            {
                if (!InputParser.TryNonNegativeMoney(txtCost.Text, out decimal rawCost) ||
                    !InputParser.TryNonNegativeMoney(txtPrice.Text, out decimal rawPrice) || rawPrice <= 0 ||
                    !InputParser.TryNonNegativeInt(txtStock.Text, out int stock) ||
                    !InputParser.TryNonNegativeMoney(txtExchangeRate.Text, out decimal exchangeRate) || exchangeRate <= 0)
                    throw new ArgumentException("Cost and stock must be non-negative, and selling price must be greater than zero.");
                string currency = SelectedCurrency;
                var converted = ConvertPrices(rawCost, rawPrice, exchangeRate, currency);
                bool tire = chkIsTire.IsChecked == true;
                int? width = null, ratio = null, diameter = null;
                if (tire)
                {
                    if (!InputParser.TryPositiveInt(txtWidth.Text, out int w) || !InputParser.TryPositiveInt(txtRatio.Text, out int r) || !InputParser.TryPositiveInt(txtDiameter.Text, out int d))
                        throw new ArgumentException("Tire width, ratio, and diameter must be positive whole numbers.");
                    width = w; ratio = r; diameter = d;
                }

                using (var db = new AppDbContext())
                {
                    var product = new Product
                    {
                        ProductName = txtProductName.Text.Trim(),
                        Barcode = string.IsNullOrWhiteSpace(txtBarcode.Text) ? null : txtBarcode.Text.Trim(),
                        CostPrice = converted.CostLbp,
                        SellingPrice = converted.PriceLbp,
                        Currency = currency,
                        CostPriceUSD = converted.CostUsd,
                        SellingPriceUSD = converted.PriceUsd,
                        StockQuantity = stock,
                        IsTire = tire,

                        // FIX: Explicitly parse strings to int? to match your SQL schema
                        TireWidth = width,
                        TireRatio = ratio,
                        TireDiameter = diameter,

                        IsActive = true,
                        CategoryId = (int)cbCategory.SelectedValue,
                        SupplierId = (int)cbSupplier.SelectedValue
                    };

                    db.Products.Add(product);
                    db.SaveChanges();

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Exception root = ex;
                while (root.InnerException != null) root = root.InnerException;
                MessageBox.Show($"Save Error: {root.Message}", "Product Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
