using System;
using System.Data;
using Microsoft.Data.SqlClient; // Note: Modern WPF apps use Microsoft.Data.SqlClient
using System.Windows;
using System.Windows.Input;
using al_ameer.Features.Home;
using al_ameer.Auth;
using al_ameer.Data;

namespace al_ameer.Features.Login
{
    public partial class LoginWindow : Window
    {
        // UPDATED: Connection string points to 'al_ameer' database
        // Replace 'YOUR_SERVER_NAME' with your actual SQL Server name (e.g., . or SQLEXPRESS)
        private readonly string connectionString = DatabaseConfig.ConnectionString;

        public LoginWindow()
        {
            InitializeComponent();
        }

        // Draggable Window Logic
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Please enter both username and password.", "Al Ameer - Login", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Attempt Login
            string? authenticatedUserFullName = ValidateLogin(user, pass);

            if (authenticatedUserFullName != null)
            {
                // SUCCESS: Greet the Admin using their FullName from the DB
                MessageBox.Show($"Welcome back, {authenticatedUserFullName}!", "Login Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                // NEXT STEP: Open Dashboard
                  HomeWindow dashboard = new HomeWindow(authenticatedUserFullName);
                  dashboard.Show();

                this.Close();
            }
            else
            {
                // FAIL
                MessageBox.Show("Invalid Username or Password. Please try again.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                txtPassword.Clear();
                txtUsername.Focus();
            }
        }

        /// <summary>
        /// Validates credentials and returns the FullName if successful
        /// </summary>
        private string? ValidateLogin(string username, string password)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL query matching your new table structure
                    // We check IsActive = 1 to ensure the account isn't disabled
                    string query = "SELECT UserId, FullName, PasswordHash FROM Users WHERE Username = @user AND IsActive = 1";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", username);
                        using SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string fullName = reader.GetString(1);
                            string stored = reader.GetString(2);
                            if (!PasswordHasher.Verify(password, stored, out bool upgrade)) return null;
                            reader.Close();
                            if (upgrade)
                            {
                                using var update = new SqlCommand("UPDATE Users SET PasswordHash = @hash WHERE UserId = @id AND PasswordHash = @old", conn);
                                update.Parameters.Add("@hash", SqlDbType.NVarChar, -1).Value = PasswordHasher.Hash(password);
                                update.Parameters.Add("@old", SqlDbType.NVarChar, -1).Value = stored;
                                update.Parameters.Add("@id", SqlDbType.Int).Value = id;
                                update.ExecuteNonQuery();
                            }
                            return fullName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Connection Error: " + ex.Message, "System Error", MessageBoxButton.OK, MessageBoxImage.Stop);
            }

            return null;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
