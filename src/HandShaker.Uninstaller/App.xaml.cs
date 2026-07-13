using System;
using System.Globalization;
using System.Windows;

namespace HandShakerUninstaller
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LoadLanguage();
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        private void LoadLanguage()
        {
            ResourceDictionary language = null;
            try
            {
                language = LoadComponent(
                    new Uri("Localizable/" + CultureInfo.CurrentCulture.Name + ".xaml", UriKind.Relative))
                    as ResourceDictionary;
            }
            catch
            {
            }

            if (language != null)
            {
                Resources.MergedDictionaries.Clear();
                Resources.MergedDictionaries.Add(language);
                Resources.MergedDictionaries.Add(
                    LoadComponent(new Uri("Themes/MainSkin.xaml", UriKind.Relative)) as ResourceDictionary);
            }
        }
    }
}
