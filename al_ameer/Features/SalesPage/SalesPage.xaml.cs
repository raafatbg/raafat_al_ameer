using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using al_ameer.Data;
using al_ameer.Models;
using Microsoft.EntityFrameworkCore;

namespace al_ameer.Features.Sales
{
    public partial class SalesPage : Page
    {
        public SalesPage()
        {
            InitializeComponent();
            LoadSales();
        }

        public void LoadSales()
        {
            try
            {
                using var db = new AppDbContext();
                // Include Customer info if you have a relationship defined
                dgSales.ItemsSource = db.Sales
                    .OrderByDescending(s => s.SaleDate)
                    .AsNoTracking()
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sales: {ex.Message}");
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtSearch.Text.Trim().ToLower();
            using var db = new AppDbContext();

            var query = db.Sales.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                // Better search logic including payment method and ID
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                query = query.Where(s => s.SaleId.ToString().Contains(search) ||
                                       s.PaymentMethod.ToLower().Contains(search));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }

            dgSales.ItemsSource = query.OrderByDescending(s => s.SaleDate).AsNoTracking().ToList();
        }

        private void AddSale_Click(object sender, RoutedEventArgs e)
        {
            // Open the Premium Window we built instead of a Page
            NewSaleWindow win = new NewSaleWindow();
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();

            // Refresh the list after the window is closed
            LoadSales();
        }

        private void DeleteSale_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: Sale sale })
            {
                if (MessageBox.Show($"Are you sure you want to delete Sale #{sale.SaleId}?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    using var db = new AppDbContext();
                    db.Sales.Remove(sale);
                    db.SaveChanges();
                    LoadSales();
                }
            }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: Sale sale })
            {
                // Placeholder for Sale Details popup
                MessageBox.Show($"Viewing details for Sale #{sale.SaleId} (Items, Qty, etc.)");
            }
        }
    }
}