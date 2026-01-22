using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using al_ameer.Data;
using al_ameer.Models;
using System.Globalization;

namespace al_ameer.Features.Inventory
{
    public partial class AddProductWindow : Window
    {
        public AddProductWindow()
        {
            InitializeComponent();
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
                string cleanCost = (txtCost.Text ?? "0").Replace(" ", "").Replace(",", "");
                string cleanPrice = (txtPrice.Text ?? "0").Replace(" ", "").Replace(",", "");

                using (var db = new AppDbContext())
                {
                    decimal rawCost = decimal.Parse(cleanCost, CultureInfo.InvariantCulture);
                    decimal rawPrice = decimal.Parse(cleanPrice, CultureInfo.InvariantCulture);
                    decimal roundedPrice = Math.Round(rawPrice / 1000m) * 1000m;

                    var product = new Product
                    {
                        ProductName = txtProductName.Text.Trim(),
                        Barcode = string.IsNullOrWhiteSpace(txtBarcode.Text) ? null : txtBarcode.Text.Trim(),
                        CostPrice = rawCost,
                        SellingPrice = roundedPrice,
                        StockQuantity = int.TryParse(txtStock.Text, out int stock) ? stock : 0,
                        IsTire = chkIsTire.IsChecked ?? false,

                        // FIX: Explicitly parse strings to int? to match your SQL schema
                        TireWidth = int.TryParse(txtWidth.Text, out int width) ? width : (int?)null,
                        TireRatio = int.TryParse(txtRatio.Text, out int ratio) ? ratio : (int?)null,
                        TireDiameter = int.TryParse(txtDiameter.Text, out int diameter) ? diameter : (int?)null,

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
                MessageBox.Show($"Save Error: {ex.Message}\n\nCheck if your number formats are correct.");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}