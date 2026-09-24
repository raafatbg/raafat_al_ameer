using al_ameer.Services;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Settings;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        cbLanguage.SelectedIndex = AppSettings.Current.Language == "ar" ? 1 : 0;
        txtRate.Text = AppSettings.Current.LbpPerUsd.ToString("0.##");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!InputParser.TryNonNegativeMoney(txtRate.Text, out decimal rate) || rate <= 0)
        {
            MessageBox.Show("Enter a valid positive exchange rate. / أدخل سعر صرف صحيحاً.");
            return;
        }
        try
        {
            new AppSettings
            {
                Language = ((ComboBoxItem)cbLanguage.SelectedItem).Tag.ToString()!,
                LbpPerUsd = rate
            }.Save();
            MessageBox.Show("Settings saved. Reopen the app to apply the language throughout. / تم الحفظ. أعد تشغيل البرنامج لتطبيق اللغة بالكامل.");
        }
        catch (Exception ex) { MessageBox.Show("Settings could not be saved: " + ex.Message); }
    }
}
