using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Resources;

namespace HandShaker.Detector
{
    public partial class SFUserAgreementMsg : Window
    {
        private static SFUserAgreementMsg current;

        public static void ShowAgreement()
        {
            if (current == null)
            {
                current = new SFUserAgreementMsg();
                current.Show();
            }
            else
            {
                current.WindowState = WindowState.Normal;
                current.Activate();
            }
        }

        public SFUserAgreementMsg()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            StreamResourceInfo resource = Application.GetResourceStream(
                new Uri("Resources/sfuseragreement.rtf", UriKind.Relative));
            if (resource != null)
            {
                using (resource.Stream)
                {
                    richTextBox.Selection.Load(resource.Stream, DataFormats.Rtf);
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MinBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            current = null;
            Close();
        }
    }
}
