using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace HandShakerUninstaller
{
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();
        private bool uninstallSucceeded;

        public MainWindow()
        {
            InitializeComponent();
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

        private void StartUninstall_Click(object sender, RoutedEventArgs e)
        {
            closeBtn.IsEnabled = false;
            startUninstallView.Visibility = Visibility.Collapsed;
            uninstallationView.Visibility = Visibility.Visible;
            worker.WorkerReportsProgress = true;
            worker.DoWork += Uninstall;
            worker.ProgressChanged += ProgressChanged;
            worker.RunWorkerCompleted += UninstallCompleted;
            worker.RunWorkerAsync(chkKeepUserFile.IsChecked == true);
        }

        private void Uninstall(object sender, DoWorkEventArgs e)
        {
            UninstallTask.Run((bool)e.Argument, delegate(int progress)
            {
                worker.ReportProgress(progress);
            });
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            progressValue.Text = e.ProgressPercentage.ToString();
        }

        private void UninstallCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            closeBtn.IsEnabled = true;
            uninstallationView.Visibility = Visibility.Collapsed;
            completionView.Visibility = Visibility.Visible;
            uninstallSucceeded = e.Error == null;
            completionMessage.Text = (string)Application.Current.Resources[
                uninstallSucceeded ? "Uninstall complete" : "Uninstall failed"];
            if (e.Error != null)
            {
                completionMessage.ToolTip = e.Error.Message;
            }
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

        protected override void OnClosed(System.EventArgs e)
        {
            if (uninstallSucceeded)
            {
                UninstallTask.ScheduleSelfDelete();
            }
            base.OnClosed(e);
        }
    }
}
