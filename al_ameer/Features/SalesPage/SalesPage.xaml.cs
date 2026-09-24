using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using al_ameer.Data;
using al_ameer.Models;
using Microsoft.EntityFrameworkCore;
using al_ameer.Services;
using Microsoft.Data.SqlClient;

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

        public void LoadSales(string filter = "")
        {
            try
            {
                using var db = new AppDbContext();
                var query = db.Sales
                    .Include(s => s.Customer)
                    .AsNoTracking()
                    .AsQueryable();
                if (!string.IsNullOrWhiteSpace(filter))
                    query = query.Where(s => (s.InvoiceNumber ?? "").Contains(filter) ||
                        (s.Customer != null && s.Customer.FullName.Contains(filter)) ||
                        (s.PaymentMethod ?? "").Contains(filter));
                var salesList = query
                    .OrderByDescending(s => s.SaleDate)
                    .Select(s => new {
                        s.SaleId,
                        s.SaleDate,
                        GrandTotal = s.GrandTotal ?? 0m,
                        Status = s.IsVoided ? "Voided" : "Active",
                        PaymentMethod = s.PaymentMethod ?? "Cash",
                        CustomerName = s.Customer != null ? s.Customer.FullName : "Walk-in"
                    })
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

            if (MessageBox.Show($"Void sale #{saleId}? Inventory will be restored and the saved receipt retained.", "Confirm Void", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    new SaleService().Void(saleId);
                    LoadSales(txtSearch.Text.Trim());
                }
                catch (Exception ex) { MessageBox.Show("Void Error: " + ex.Message); }
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
                using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
                connection.Open();
                using var command = new SqlCommand(@"SELECT COALESCE(p.ProductName,sc.ServiceName,N'Unknown item'),
                    i.ItemType,i.Quantity,i.LineTotal FROM dbo.SaleItems i
                    LEFT JOIN dbo.Products p ON p.ProductId=i.ProductId
                    LEFT JOIN dbo.ServicesCatalog sc ON sc.ServiceID=i.ServiceId
                    WHERE i.SaleId=@id ORDER BY i.SaleItemId", connection);
                command.Parameters.AddWithValue("@id", saleId);
                var lines = new System.Collections.Generic.List<string>();
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) lines.Add($"- {reader.GetString(1)}: {reader.GetString(0)} | Qty: {reader.GetInt32(2)} | {reader.GetDecimal(3):N0} LBP");
                using var receipt = new SqlCommand(@"SELECT r.ReceiptNumber,r.ReceivedLBP,r.ReceivedUSD,
                    r.LbpPerUsd,r.AppliedLBP,r.ChangeLBP,r.ChangeUSD,s.IsVoided
                    FROM dbo.SaleTenderReceipts r JOIN dbo.Sales s ON s.SaleId=r.SaleId WHERE r.SaleId=@id", connection);
                receipt.Parameters.AddWithValue("@id", saleId);
                using var saved = receipt.ExecuteReader();
                string summary = saved.Read()
                    ? $"Receipt {saved.GetString(0)}{(saved.GetBoolean(7) ? " — VOIDED" : "")}\n" +
                      $"Received: {saved.GetDecimal(1):N2} LBP + {saved.GetDecimal(2):N2} USD\n" +
                      $"Rate: {saved.GetDecimal(3):N4} LBP/USD | Applied: {saved.GetDecimal(4):N2} LBP\n" +
                      $"Change: {saved.GetDecimal(5):N2} LBP (≈ {saved.GetDecimal(6):N2} USD)\n\n"
                    : "No saved tender receipt (historical sale).\n\n";
                MessageBox.Show(summary + (lines.Count == 0 ? "No items found." : string.Join("\n", lines)), $"Sale #{saleId} Details");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadSales(txtSearch.Text.Trim());
        private void AddSale_Click(object sender, RoutedEventArgs e)
        {
            NewSaleWindow win = new NewSaleWindow { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true) LoadSales();
        }
    }
}
