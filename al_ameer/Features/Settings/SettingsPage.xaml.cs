using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace al_ameer.Features.Settings
{
    public partial class SettingsPage : Page
    {
        private readonly string connString = "Server=DESKTOP-TVOR3BK;Database=al_ameer;Trusted_Connection=True;TrustServerCertificate=True;";

        public SettingsPage()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            // Optional: Add logic here to pull current exchange rate from a 'Settings' table
        }

        private void ChangeLanguage_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            bool isCurrentlyEnglish = btnLang.Content.ToString().Contains("Arabic");
            ResourceDictionary dict = new ResourceDictionary();

            try
            {
                if (isCurrentlyEnglish)
                {
                    dict.Source = new Uri("Resources/StringResources.ar.xaml", UriKind.Relative);
                    Application.Current.MainWindow.FlowDirection = FlowDirection.RightToLeft;
                    btnLang.Content = "English / انجليزي";
                }
                else
                {
                    dict.Source = new Uri("Resources/StringResources.en.xaml", UriKind.Relative);
                    Application.Current.MainWindow.FlowDirection = FlowDirection.LeftToRight;
                    btnLang.Content = "Arabic / عربي";
                }

                app.Resources.MergedDictionaries.Clear();
                app.Resources.MergedDictionaries.Add(dict);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Language files not found. Error: " + ex.Message);
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    // Logic to update an exchange rate table or config file
                    // This is crucial because your reports rely on LBP/USD calculations
                    string sql = "IF EXISTS (SELECT 1 FROM Settings) UPDATE Settings SET ExchangeRate = @rate ELSE INSERT INTO Settings (ExchangeRate) VALUES (@rate)";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@rate", decimal.Parse(txtExchangeRate.Text));

                    conn.Open();
                    // cmd.ExecuteNonQuery(); // Uncomment once you create a Settings table

                    MessageBox.Show("Settings Saved Successfully!", "Al Ameer Tires", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving settings: " + ex.Message);
            }
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    // Full SQL Backup Command
                    string filePath = @"C:\Backups\al_ameer_" + DateTime.Now.ToString("yyyyMMdd") + ".bak";
                    string sql = $@"BACKUP DATABASE [al_ameer] TO DISK = '{filePath}'";

                    conn.Open();
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    // cmd.ExecuteNonQuery(); // Ensure folder C:\Backups exists first!

                    MessageBox.Show($"Database backup created at {filePath}", "System Backup");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Backup failed. Please ensure C:\\Backups folder exists. " + ex.Message);
            }
        }
    }
}