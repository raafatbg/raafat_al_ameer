using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Customers
{
    public partial class CustomersPage : Page
    {
        private readonly string connString = al_ameer.Data.DatabaseConfig.ConnectionString;

        public CustomersPage()
        {
            InitializeComponent();
            LoadCustomers();
        }

        public void LoadCustomers(string filter = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    string query = "SELECT CustomerID, FullName, Phone, RegistrationDate, Note FROM Customers";
                    if (!string.IsNullOrEmpty(filter))
                    {
                        query += " WHERE FullName LIKE @filter OR Phone LIKE @filter";
                    }
                    query += " ORDER BY FullName ASC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    if (!string.IsNullOrEmpty(filter))
                    {
                        cmd.Parameters.AddWithValue("@filter", $"%{filter}%");
                    }

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgCustomers.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddCustomerWindow { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true) LoadCustomers(txtSearch.Text.Trim());
        }

        private void EditCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomers.SelectedItem is DataRowView row)
            {
                int id = (int)row["CustomerID"];
                var window = new AddCustomerWindow(id, row["FullName"].ToString(), row["Phone"].ToString(), row["Note"].ToString())
                    { Owner = Window.GetWindow(this) };
                if (window.ShowDialog() == true) LoadCustomers(txtSearch.Text.Trim());
            }
        }

        private void DeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomers.SelectedItem is DataRowView row)
            {
                if (MessageBox.Show("Delete this customer?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var conn = new SqlConnection(connString);
                        conn.Open();
                        using var cmd = new SqlCommand("DELETE FROM Customers WHERE CustomerId=@id AND NOT EXISTS (SELECT 1 FROM Sales WHERE CustomerId=@id)", conn);
                        cmd.Parameters.AddWithValue("@id", (int)row["CustomerID"]);
                        if (cmd.ExecuteNonQuery() == 0)
                            MessageBox.Show("This customer has sales history and cannot be deleted.");
                        else LoadCustomers(txtSearch.Text.Trim());
                    }
                    catch (Exception ex) { MessageBox.Show("Delete failed: " + ex.Message); }
                }
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadCustomers(txtSearch.Text.Trim());
        }
    }
}
