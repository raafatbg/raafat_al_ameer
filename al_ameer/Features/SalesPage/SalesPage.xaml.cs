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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            LoadSales();
        }

        public void LoadSales()
        {
            try
            {
                using var db = new AppDbContext();
                var salesList = db.Sales
                    .Include(s => s.Customer)
                    .OrderByDescending(s => s.SaleDate)
                    .Select(s => new {
                        s.SaleId,
                        s.SaleDate,
                        GrandTotal = s.GrandTotal ?? 0m,
                        PaymentMethod = s.PaymentMethod ?? "Cash",
                        CustomerName = s.Customer != null ? s.Customer.FullName : "Walk-in"
                    })
                    .AsNoTracking()
                    .ToList();

                dgSales.ItemsSource = salesList;
            }
            catch (Exception ex) { MessageBox.Show($"Error loading: {ex.Message}"); }
        }

        private void DeleteSale_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgSales.SelectedItem;
            if (selectedItem == null) return;

            dynamic selectedSale = selectedItem;
            int saleId = selectedSale.SaleId;

            if (MessageBox.Show($"Delete Sale #{saleId}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new AppDbContext();
                    // Load sale and its items (SaleItems, NOT SalesDetails)
                    var sale = db.Sales.Include(s => s.SaleItems).FirstOrDefault(s => s.SaleId == saleId);

                    if (sale != null)
                    {
                        db.Sales.Remove(sale);
                        db.SaveChanges();
                        LoadSales();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Delete Error: " + ex.Message); }
            }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgSales.SelectedItem;
            if (selectedItem == null) return;

            dynamic selectedSale = selectedItem;
            int saleId = selectedSale.SaleId;

            try
            {
                using var db = new AppDbContext();
                // Query SaleItems table
                var items = db.SaleItems
                    .Include(si => si.Product)
                    .Where(si => si.SaleId == saleId)
                    .ToList();

                if (items.Any())
                {
                    string msg = string.Join("\n", items.Select(i =>
                        $"- {i.Product.ProductName} | Qty: {i.Quantity} | {i.LineTotal:N0} LBP"));
                    MessageBox.Show(msg, $"Sale #{saleId} Items");
                }
                else { MessageBox.Show("No items found."); }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) { /* Search logic */ }
        private void AddSale_Click(object sender, RoutedEventArgs e)
        {
            NewSaleWindow win = new NewSaleWindow { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true) LoadSales();
        }
    }
}