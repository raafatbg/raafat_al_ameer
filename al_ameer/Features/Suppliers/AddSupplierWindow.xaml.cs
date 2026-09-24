using System;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace al_ameer.Features.Suppliers
{
    public partial class AddSupplierWindow : Window
    {
        private readonly string connString = al_ameer.Data.DatabaseConfig.ConnectionString;

        public AddSupplierWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtCompanyName.Text))
            {
                MessageBox.Show("Please enter the Company Name.", "Al Ameer Tires", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                // FIXED: Removed Email and Address from the SQL INSERT statement
                string sql = @"INSERT INTO Suppliers (CompanyName, ContactName, Phone) 
                               VALUES (@name, @contact, @phone)";

                SqlCommand cmd = new SqlCommand(sql, conn);

                // Mapping only the 3 active fields
                cmd.Parameters.AddWithValue("@name", txtCompanyName.Text.Trim());
                cmd.Parameters.AddWithValue("@contact", txtContactName.Text.Trim());
                cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Signal success to SuppliersPage and close
                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Database Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
