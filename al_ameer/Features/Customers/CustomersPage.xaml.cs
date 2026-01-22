using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Customers
{
    public partial class CustomersPage : Page
    {
        // Your database connection string
        private readonly string connString = "Server=DESKTOP-TVOR3BK;Database=al_ameer;Trusted_Connection=True;TrustServerCertificate=True;";

        public CustomersPage()
        {
            InitializeComponent();
            LoadCustomers();
        }

        /// <summary>
        /// Fetches all customers from the database and binds them to the DataGrid
        /// </summary>
        public void LoadCustomers(string filter = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    // Search by FullName or Phone
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

        /// <summary>
        /// Logic for the "+ NEW CUSTOMER" button
        /// </summary>
        private void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            AddCustomerWindow addWin = new AddCustomerWindow();
            addWin.Owner = Window.GetWindow(this);

            // If the window returns true (Success), refresh the list
            if (addWin.ShowDialog() == true)
            {
                LoadCustomers();
            }
        }

        /// <summary>
        /// Filters the list in real-time as the user types
        /// </summary>
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadCustomers(txtSearch.Text.Trim());
        }
    }
}