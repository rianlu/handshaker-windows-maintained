using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace HandShakerUninstaller
{
    internal static class UninstallTask
    {
        private static readonly string InstallDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        private static readonly string UninstallerPath = Process.GetCurrentProcess().MainModule.FileName;

        public static void Run(bool keepUserData, Action<int> reportProgress)
        {
            StopProcesses();
            reportProgress(20);
            DeleteShortcuts();
            reportProgress(40);
            DeleteRegistryEntries();
            reportProgress(55);
            DeleteInstalledFiles();
            reportProgress(85);
            if (!keepUserData)
            {
                DeleteUserData();
            }
            DeleteDirectory(Path.Combine(Path.GetTempPath(), "HandShaker"));
            reportProgress(100);
        }

        public static void ScheduleSelfDelete()
        {
            string script = Path.Combine(Path.GetTempPath(), "HandShaker-Uninstall-" + Guid.NewGuid().ToString("N") + ".cmd");
            File.WriteAllText(script,
                "@echo off\r\n" +
                "ping 127.0.0.1 -n 3 > nul\r\n" +
                "del /f /q \"" + UninstallerPath + "\"\r\n" +
                "rmdir /s /q \"" + InstallDirectory + "\"\r\n" +
                "del /f /q \"%~f0\"\r\n",
                Encoding.Default);
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"\"" + script + "\"\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        private static void StopProcesses()
        {
            string[] names = { "adb", "HandShaker", "HandShaker.Detector", "HandShaker.AoaLauncher", "HandShakerStart", "HSUpdater" };
            foreach (string name in names)
            {
                foreach (Process process in Process.GetProcessesByName(name))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                    catch
                    {
                    }
                }
            }
            Thread.Sleep(300);
        }

        private static void DeleteShortcuts()
        {
            DeleteShortcutSet(Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolder.Programs);
            DeleteShortcutSet(Environment.SpecialFolder.CommonDesktopDirectory, Environment.SpecialFolder.CommonPrograms);
        }

        private static void DeleteShortcutSet(Environment.SpecialFolder desktop, Environment.SpecialFolder programs)
        {
            DeleteFile(Path.Combine(Environment.GetFolderPath(desktop), "HandShaker.lnk"));
            DeleteDirectory(Path.Combine(Environment.GetFolderPath(programs), "HandShaker"));
        }

        private static void DeleteRegistryEntries()
        {
            RegistryView[] views = { RegistryView.Registry32, RegistryView.Registry64 };
            foreach (RegistryView view in views)
            {
                using (RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (RegistryKey uninstall = root.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", true))
                {
                    if (uninstall != null)
                    {
                        uninstall.DeleteSubKeyTree("HandShaker", false);
                    }
                }
            }
        }

        private static void DeleteInstalledFiles()
        {
            foreach (string file in Directory.GetFiles(InstallDirectory))
            {
                if (!string.Equals(Path.GetFullPath(file), Path.GetFullPath(UninstallerPath), StringComparison.OrdinalIgnoreCase))
                {
                    DeleteFile(file);
                }
            }
            foreach (string directory in Directory.GetDirectories(InstallDirectory))
            {
                DeleteDirectory(directory);
            }
        }

        private static void DeleteUserData()
        {
            DeleteDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HandShaker"));
            DeleteDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HandShaker"));
        }

        private static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
