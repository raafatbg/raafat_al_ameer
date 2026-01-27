using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Customers
{
    public partial class CustomersPage : Page
    {
        private readonly string connString = "Server=DESKTOP-TVOR3BK;Database=al_ameer;Trusted_Connection=True;TrustServerCertificate=True;";

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
                    string query = "SELECT CustomerID, FullName, Phone, RegistrationDate FROM Customers";
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
            // Placeholder: Replace with your actual AddCustomerWindow call
            MessageBox.Show("Open Add Customer Window");
            LoadCustomers();
        }

        private void EditCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomers.SelectedItem is DataRowView row)
            {
                int id = (int)row["CustomerID"];
                MessageBox.Show($"Editing Customer ID: {id}");
                // LoadCustomers();
            }
        }

        private void DeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomers.SelectedItem is DataRowView row)
            {
                if (MessageBox.Show("Delete this customer?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Add SQL Delete logic here
                    LoadCustomers();
                }
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadCustomers(txtSearch.Text.Trim());
        }
    }
}