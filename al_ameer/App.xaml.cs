using System.Windows;
using al_ameer.Features.Login; // Ensure this matches your login folder
using al_ameer.Services;
using System.Windows.Controls;
using System.Globalization;

namespace al_ameer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(AppSettings.Current.Language);
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) => UiLanguage.Apply((DependencyObject)sender)));
            EventManager.RegisterClassHandler(typeof(Page), FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) => UiLanguage.Apply((DependencyObject)sender)));

            // Start with the Login Window
            LoginWindow login = new LoginWindow();
            login.Show();
        }
    }
}
