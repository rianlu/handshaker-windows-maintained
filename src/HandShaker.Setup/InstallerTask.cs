using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace HandShakerSimpleSetup
{
    internal static class InstallerTask
    {
        private const string PayloadResourceName = "HandShaker.Payload.Setup.exe";
        private const string UninstallerResourceName = "HandShaker.Uninstaller.exe";

        public static void Install(string installPath, Action<int> reportProgress)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "HandShakerMaintained", Guid.NewGuid().ToString("N"));
            string payloadPath = Path.Combine(tempDirectory, "HandShaker.Payload.Setup.exe");
            Directory.CreateDirectory(tempDirectory);

            try
            {
                ExtractPayload(payloadPath, reportProgress);
                RunPayload(payloadPath, installPath, reportProgress);
                InstallUninstaller(installPath, reportProgress);
                reportProgress(100);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDirectory, true);
                }
                catch
                {
                }
            }
        }

        public static void RunHandShaker(string installPath)
        {
            string launcher = Path.Combine(installPath, "HandShaker.AoaLauncher.exe");
            if (!File.Exists(launcher))
            {
                throw new FileNotFoundException("HandShaker 启动程序不存在.", launcher);
            }

            Process.Start(new ProcessStartInfo(launcher) { WorkingDirectory = installPath });
        }

        private static void ExtractPayload(string destination, Action<int> reportProgress)
        {
            Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResourceName);
            if (input == null)
            {
                throw new InvalidOperationException("安装包中缺少离线Payload.");
            }

            using (input)
            using (FileStream output = File.Create(destination))
            {
                byte[] buffer = new byte[1024 * 1024];
                long copied = 0;
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                    copied += read;
                    reportProgress((int)Math.Min(20, copied * 20 / input.Length));
                }
            }
        }

        private static void RunPayload(string payloadPath, string installPath, Action<int> reportProgress)
        {
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = payloadPath,
                Arguments = "/S /D=" + installPath,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null)
            {
                throw new InvalidOperationException("无法启动离线安装引擎.");
            }

            int progress = 20;
            while (!process.WaitForExit(150))
            {
                progress = Math.Min(95, progress + 1);
                reportProgress(progress);
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("离线安装失败, 错误码: " + process.ExitCode + ".");
            }
        }

        private static void InstallUninstaller(string installPath, Action<int> reportProgress)
        {
            Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(UninstallerResourceName);
            if (input == null)
            {
                throw new InvalidOperationException("安装包中缺少维护版卸载器.");
            }

            using (input)
            using (FileStream output = File.Create(Path.Combine(installPath, "HandShakerUninst.exe")))
            {
                input.CopyTo(output);
            }
            reportProgress(99);
        }
    }
}
