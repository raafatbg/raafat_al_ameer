using System.Windows;
using al_ameer.Features.Login; // Ensure this matches your login folder

namespace al_ameer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Start with the Login Window
            LoginWindow login = new LoginWindow();
            login.Show();
        }
    }
}