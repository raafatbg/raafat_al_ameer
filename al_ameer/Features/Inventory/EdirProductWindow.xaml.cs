using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using al_ameer.Data;
using al_ameer.Models;
using Microsoft.EntityFrameworkCore;

namespace al_ameer.Features.Inventory
{
    public partial class EditProductWindow : Window
    {
        private int _productId;

        public EditProductWindow(Product product)
        {
            InitializeComponent();
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
            txtPrice.Text = p.SellingPrice.ToString("N0"); // Show with thousands separators

            // Fix: Convert int? to string for display, handling nulls with ?? ""
            txtStock.Text = (p.StockQuantity ?? 0).ToString();
            txtWidth.Text = p.TireWidth?.ToString() ?? "";
            txtRatio.Text = p.TireRatio?.ToString() ?? "";
            txtDiameter.Text = p.TireDiameter?.ToString() ?? "";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var product = db.Products.FirstOrDefault(p => p.ProductId == _productId);
                    if (product == null) return;

                    product.ProductName = txtName.Text;
                    product.Barcode = txtBarcode.Text;
                    product.CategoryId = (int)(cbCategory.SelectedValue ?? 0);

                    // Fix: Parse string back to int?
                    product.StockQuantity = int.TryParse(txtStock.Text, out int s) ? s : (int?)null;

                    // Fix: Explicitly parse Tire Specs from string to int? to match SQL INT type
                    product.TireWidth = int.TryParse(txtWidth.Text, out int w) ? w : (int?)null;
                    product.TireRatio = int.TryParse(txtRatio.Text, out int r) ? r : (int?)null;
                    product.TireDiameter = int.TryParse(txtDiameter.Text, out int d) ? d : (int?)null;

                    // LBP Rounding Logic: Round to nearest 1,000
                    string priceText = txtPrice.Text.Replace(",", "").Replace(".", "");
                    if (decimal.TryParse(priceText, out decimal rawPrice))
                    {
                        product.SellingPrice = Math.Round(rawPrice / 1000m) * 1000m;
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