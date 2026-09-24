using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Suppliers
{
    public partial class SuppliersPage : Page
    {
        private readonly string connString = al_ameer.Data.DatabaseConfig.ConnectionString;

        public SuppliersPage()
        {
            InitializeComponent();
            LoadSuppliers();
        }

        public void LoadSuppliers(string filter = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    // Query reflects the removal of Email and Address
                    string query = "SELECT SupplierID, CompanyName, ContactName, Phone FROM Suppliers";

                    if (!string.IsNullOrEmpty(filter))
                    {
                        query += " WHERE CompanyName LIKE @f OR ContactName LIKE @f";
                    }

                    SqlCommand cmd = new SqlCommand(query, conn);
                    if (!string.IsNullOrEmpty(filter))
                    {
                        cmd.Parameters.AddWithValue("@f", $"%{filter}%");
                    }

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgSuppliers.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading suppliers: {ex.Message}", "Al Ameer Tires", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSupplier_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            if (!int.TryParse(button.Tag.ToString(), out int id)) return;

            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this supplier?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        string query = @"DELETE FROM Suppliers WHERE SupplierID = @id
                            AND NOT EXISTS (SELECT 1 FROM Products WHERE SupplierId=@id)
                            AND NOT EXISTS (SELECT 1 FROM PurchaseOrders WHERE SupplierId=@id)";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", id);

                        conn.Open();
                        if (cmd.ExecuteNonQuery() == 0)
                        {
                            MessageBox.Show("Supplier is referenced by products or purchases and cannot be deleted.", "Delete blocked", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        // Refresh the list after deleting
                        LoadSuppliers(txtSearch.Text);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot delete supplier: " + ex.Message,
                        "Delete Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                }
            }
        }

        private void AddSupplier_Click(object sender, RoutedEventArgs e)
        {
            AddSupplierWindow win = new AddSupplierWindow();
            win.Owner = Window.GetWindow(this);
            if (win.ShowDialog() == true) LoadSuppliers();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadSuppliers(txtSearch.Text);
        }
    }
}
