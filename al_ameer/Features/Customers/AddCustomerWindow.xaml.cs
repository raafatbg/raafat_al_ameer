using System;
using Microsoft.Data.SqlClient;
using System.Windows;

namespace al_ameer.Features.Customers
{
    public partial class AddCustomerWindow : Window
    {
        private readonly int? _customerId;

        public AddCustomerWindow(int? customerId = null, string? fullName = null, string? phone = null, string? note = null)
        {
            InitializeComponent();
            _customerId = customerId;
            txtFullName.Text = fullName ?? string.Empty;
            txtPhone.Text = phone ?? string.Empty;
            txtNote.Text = note ?? string.Empty;
            if (customerId.HasValue) Title = "Edit Customer";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Name and Phone are required.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(al_ameer.Data.DatabaseConfig.ConnectionString))
            {
                string sql = _customerId.HasValue
                    ? "UPDATE Customers SET FullName=@name, Phone=@phone, Note=@note WHERE CustomerId=@id"
                    : "INSERT INTO Customers (FullName, Phone, Note, RegistrationDate) VALUES (@name, @phone, @note, GETDATE())";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@name", txtFullName.Text.Trim());
                cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                cmd.Parameters.AddWithValue("@note", txtNote.Text.Trim());
                if (_customerId.HasValue) cmd.Parameters.AddWithValue("@id", _customerId.Value);

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
