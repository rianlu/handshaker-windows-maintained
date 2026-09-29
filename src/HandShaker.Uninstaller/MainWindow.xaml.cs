using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace HandShakerUninstaller
{
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();

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
            if (e.Cancelled)
            {
                return;
            }

            UninstallTask.ScheduleSelfDelete();
            Environment.Exit(0);
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
