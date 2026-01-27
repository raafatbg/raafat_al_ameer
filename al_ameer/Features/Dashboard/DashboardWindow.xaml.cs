using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using al_ameer.Data;
using al_ameer.Features.Sales;
using Microsoft.EntityFrameworkCore;

namespace al_ameer.Features.Dashboard
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
        }

        // Auto-refresh when navigating back to the dashboard
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // 1. Total Inventory Count
                    int totalItems = db.Products.Sum(p => (int?)p.StockQuantity) ?? 0;
                    txtTotalItems.Text = totalItems.ToString("N0");

                    // 2. Stock Investment Value
                    decimal stockValue = db.Products.Sum(p => (decimal?)(p.CostPrice * p.StockQuantity)) ?? 0;
                    txtStockValue.Text = stockValue.ToString("N0") + " LBP";

                    // 3. Low Stock Items count
                    int lowStockCount = db.Products.Count(p => (p.StockQuantity ?? 0) < 5);
                    txtLowStock.Text = lowStockCount.ToString();

                    // 4. Today's Revenue Calculation
                    DateTime today = DateTime.Today;
                    decimal todaySales = db.Sales
                        .Where(s => s.SaleDate >= today)
                        .Sum(s => (decimal?)s.GrandTotal) ?? 0;

                    txtTotalSales.Text = todaySales.ToString("N0") + " LBP";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex.Message}");
            }
        }

        private void NewSale_Click(object sender, RoutedEventArgs e)
        {
            NewSaleWindow win = new NewSaleWindow { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
            {
                LoadStatistics();
            }
        }

        private void ViewInventory_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Features.Inventory.InventoryPage());
        }

        private void ViewReports_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Features.Reports.ReportsPage());
        }
    }
}