using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using al_ameer.Data;
using Microsoft.EntityFrameworkCore;

namespace al_ameer.Features.Dashboard
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // 1. Calculate Total Inventory Items
                    // Fix: Cast to int? during Sum to handle potential NULLs in StockQuantity column
                    int totalItems = db.Products.Sum(p => (int?)p.StockQuantity) ?? 0;
                    txtTotalItems.Text = totalItems.ToString();

                    // 2. Calculate Total Stock Value (Cost Price * Stock)
                    // Fix: Cast the multiplication result to decimal? to handle potential NULLs in CostPrice or StockQuantity
                    decimal stockValue = db.Products.Sum(p => (decimal?)(p.CostPrice * p.StockQuantity)) ?? 0;
                    txtStockValue.Text = stockValue.ToString("N0") + " LBP";

                    // 3. Low Stock Count (Items with less than 5 units)
                    // Fix: Handle null StockQuantity by treating it as 0
                    int lowStockCount = db.Products.Count(p => (p.StockQuantity ?? 0) < 5);
                    txtLowStock.Text = lowStockCount.ToString();

                    // 4. Today's Sales Total
                    DateTime today = DateTime.Today;
                    decimal todaySales = db.Sales
                        .Where(s => s.SaleDate >= today)
                        .Sum(s => (decimal?)s.GrandTotal) ?? 0;
                    txtTotalSales.Text = todaySales.ToString("N0") + " LBP";

                    // 5. Load Recent Sales into Grid
                    var recentSales = db.Sales
                        .OrderByDescending(s => s.SaleDate)
                        .Take(10)
                        .ToList();

                    dgRecentSales.ItemsSource = recentSales;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex.Message}");

                // Set defaults to avoid blank screens
                txtTotalItems.Text = "0";
                txtStockValue.Text = "0 LBP";
                txtLowStock.Text = "0";
                txtTotalSales.Text = "0 LBP";
            }
        }
    }
}