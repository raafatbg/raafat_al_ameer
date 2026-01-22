using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using al_ameer.Data;
using al_ameer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace al_ameer.Features.Inventory
{
    public partial class InventoryPage : Page
    {
        public InventoryPage()
        {
            InitializeComponent();
            LoadInventory();
        }

        public void LoadInventory()
        {
            try
            {
                using var db = new AppDbContext();
                // Eager loading related Category and Supplier
                var data = db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .AsNoTracking()
                    .ToList();

                dgInventory.ItemsSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Error: {ex.Message}\nCheck if your Supplier model still has Email or Address fields.");
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtSearch.Text.Trim().ToLower();
            try
            {
                using var db = new AppDbContext();
                var data = db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .AsNoTracking()
                    .ToList();

                // Search logic updated to match actual database columns
                var filtered = data.Where(p =>
                    (p.ProductName?.ToLower() ?? "").Contains(search) ||
                    (p.Barcode?.ToLower() ?? "").Contains(search) ||
                    (p.Supplier?.CompanyName?.ToLower() ?? "").Contains(search)
                ).ToList();

                dgInventory.ItemsSource = filtered;
            }
            catch { /* Silent catch for search typing */ }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Product product)
            {
                if (MessageBox.Show($"Delete {product.ProductName}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    using var db = new AppDbContext();
                    db.Entry(product).State = EntityState.Deleted;
                    db.SaveChanges();
                    LoadInventory();
                }
            }
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (new AddProductWindow() { Owner = Window.GetWindow(this) }.ShowDialog() == true)
                LoadInventory();
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Product product)
            {
                if (new EditProductWindow(product) { Owner = Window.GetWindow(this) }.ShowDialog() == true)
                    LoadInventory();
            }
        }
    }
}