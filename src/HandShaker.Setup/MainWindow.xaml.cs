using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HandShaker.Detector;

namespace HandShakerSimpleSetup
{
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();
        private string selectedInstallPath;

        public MainWindow()
        {
            InitializeComponent();
            Text2.Visibility = Visibility.Hidden;
            Image2.Visibility = Visibility.Hidden;
            installPath.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "HandShaker");
            Image2.Source = new BitmapImage(new Uri(
                "Resources/" + (string)System.Windows.Application.Current.Resources["IntroduceImage"], UriKind.Relative));
        }

        private void OnHitTest(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MinBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CheckLink_Click(object sender, RoutedEventArgs e)
        {
            SFUserAgreementMsg.ShowAgreement();
        }

        private void chkAgree_Checked(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = true;
        }

        private void chkAgree_Unchecked(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
        }

        private void ChangeInstallLocationButton_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    installPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void StartInstallation_Click(object sender, RoutedEventArgs e)
        {
            selectedInstallPath = installPath.Text.TrimEnd('\\');
            if (!selectedInstallPath.EndsWith("\\HandShaker", StringComparison.OrdinalIgnoreCase))
            {
                selectedInstallPath = Path.Combine(selectedInstallPath, "HandShaker");
            }

            Text2.Visibility = Visibility.Visible;
            Image2.Visibility = Visibility.Visible;
            installationView.Visibility = Visibility.Visible;
            startInstallView.Visibility = Visibility.Collapsed;
            closeBtn.IsEnabled = false;

            worker.WorkerReportsProgress = true;
            worker.DoWork += Install;
            worker.ProgressChanged += ProgressChanged;
            worker.RunWorkerCompleted += InstallationCompleted;
            worker.RunWorkerAsync();
        }

        private void Install(object sender, DoWorkEventArgs e)
        {
            InstallerTask.Install(selectedInstallPath, delegate(int progress)
            {
                worker.ReportProgress(progress);
            });
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            progressValue.Text = e.ProgressPercentage.ToString();
        }

        private void InstallationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            closeBtn.IsEnabled = true;
            if (e.Error != null)
            {
                System.Windows.MessageBox.Show(e.Error.Message, "HandShaker", MessageBoxButton.OK, MessageBoxImage.Error);
                installationView.Visibility = Visibility.Collapsed;
                startInstallView.Visibility = Visibility.Visible;
                return;
            }

            installationProgress.Visibility = Visibility.Collapsed;
            completionMessage.Visibility = Visibility.Visible;
            completionButtons.Visibility = Visibility.Visible;
        }

        private void CloseInstallation_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenHandShaker_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InstallerTask.RunHandShaker(selectedInstallPath);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception error)
            {
                System.Windows.MessageBox.Show(error.Message, "HandShaker", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (worker.IsBusy)
            {
                e.Cancel = true;
                return;
            }
            base.OnClosing(e);
        }
    }
}
