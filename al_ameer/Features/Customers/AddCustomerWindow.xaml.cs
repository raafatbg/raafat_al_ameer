using System;
using Microsoft.Data.SqlClient;
using System.Windows;

namespace al_ameer.Features.Customers
{
    public partial class AddCustomerWindow : Window
    {
        private readonly string connString = "Server=DESKTOP-TVOR3BK;Database=al_ameer;Trusted_Connection=True;TrustServerCertificate=True;";

        public AddCustomerWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Name and Phone are required.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = "INSERT INTO Customers (FullName, Phone, RegistrationDate) VALUES (@name, @phone, GETDATE())";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@name", txtFullName.Text.Trim());
                cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    this.DialogResult = true; // Close and signal success
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();
        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove(); }
    }
}