using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml;

static class Program
{
    const string DisplayVersion = "2.6.0-r1";
    static readonly Version CurrentVersion = new Version(2, 6, 0, 1);

    [STAThread]
    static void Main(string[] args)
    {
        bool silent = false;
        if (args.Length > 0)
        {
            string[] parts = args[0].Split(';');
            if (parts.Length > 1)
                bool.TryParse(parts[1], out silent);
        }
        try
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | SecurityProtocolType.Tls;
        }
        catch (NotSupportedException)
        {
        }
        try
        {
            Check(silent);
        }
        catch (Exception error)
        {
            if (!silent)
                MessageBox.Show(error.Message, "检查更新失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    static void Check(bool silent)
    {
        string feed = ReadUpdateUrl();
        if (string.IsNullOrEmpty(feed))
            throw new InvalidOperationException("没有找到更新地址。");
        XmlDocument document = new XmlDocument();
        document.Load(feed);
        string versionText = Text(document, "version");
        string display = Text(document, "displayVersion");
        string url = Text(document, "url");
        string fileName = Text(document, "fileName");
        string md5 = Text(document, "md5");
        string description = Text(document, "description");
        Version remote;
        if (!Version.TryParse(versionText, out remote) || string.IsNullOrEmpty(url))
            throw new InvalidOperationException("无法读取更新说明。");
        if (string.IsNullOrEmpty(display))
            display = versionText;
        if (remote <= CurrentVersion)
        {
            if (!silent)
                MessageBox.Show("当前版本是 " + DisplayVersion + "，没有新版本。", "已是最新版本", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        string notes = string.IsNullOrEmpty(description) ? "可以下载安装包。" : description.Trim();
        if (!AskToDownload(display, notes))
            return;
        if (string.IsNullOrEmpty(fileName))
            fileName = Path.GetFileName(new Uri(url).AbsolutePath);
        string destination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileName);
        Download(url, destination, display);
        if (!string.IsNullOrEmpty(md5) && !string.Equals(Hash(destination), md5.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(destination);
            throw new InvalidOperationException("下载文件校验失败。");
        }
        Process.Start(destination);
        MessageBox.Show("已下载 " + display + "。\n安装包已打开，请按提示完成安装，然后重新打开 HandShaker。", "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    static bool AskToDownload(string display, string notes)
    {
        Form form = new Form();
        form.Text = "检查更新";
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ClientSize = new System.Drawing.Size(460, 220);
        Label label = new Label();
        label.Text = "发现新版本 " + display + "\n\n" + notes;
        label.Bounds = new System.Drawing.Rectangle(16, 16, 428, 140);
        Button download = new Button();
        download.Text = "下载并打开";
        download.Bounds = new System.Drawing.Rectangle(232, 172, 100, 28);
        download.DialogResult = DialogResult.OK;
        Button later = new Button();
        later.Text = "稍后";
        later.Bounds = new System.Drawing.Rectangle(344, 172, 100, 28);
        later.DialogResult = DialogResult.Cancel;
        form.Controls.Add(label);
        form.Controls.Add(download);
        form.Controls.Add(later);
        form.AcceptButton = download;
        form.CancelButton = later;
        return form.ShowDialog() == DialogResult.OK;
    }

    static string ReadUpdateUrl()
    {
        string directory = AppDomain.CurrentDomain.BaseDirectory;
        string[] names = { "HandShaker.Detector.exe.config", "HandShaker.exe.config" };
        for (int i = 0; i < names.Length; i++)
        {
            string path = Path.Combine(directory, names[i]);
            if (!File.Exists(path))
                continue;
            XmlDocument document = new XmlDocument();
            document.Load(path);
            XmlNode node = document.SelectSingleNode("//add[@key='UpdateUrl']");
            if (node != null && node.Attributes["value"] != null)
                return node.Attributes["value"].Value;
        }
        return null;
    }

    static string Text(XmlDocument document, string name)
    {
        XmlNodeList nodes = document.GetElementsByTagName(name);
        return nodes.Count == 0 || nodes[0].InnerText == null ? "" : nodes[0].InnerText.Trim();
    }

    static void Download(string url, string destination, string display)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        Form form = new Form();
        form.Text = "正在下载更新";
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ClientSize = new System.Drawing.Size(420, 110);
        Label label = new Label();
        label.Text = "正在下载 " + display + "…";
        label.AutoSize = false;
        label.Bounds = new System.Drawing.Rectangle(16, 16, 388, 36);
        ProgressBar progress = new ProgressBar();
        progress.Bounds = new System.Drawing.Rectangle(16, 60, 388, 22);
        progress.Style = ProgressBarStyle.Marquee;
        form.Controls.Add(label);
        form.Controls.Add(progress);
        WebClient client = new WebClient();
        client.Headers[HttpRequestHeader.UserAgent] = "HandShakerMaintained";
        bool finished = false;
        bool failed = false;
        Exception failure = null;
        client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs args)
        {
            if (args.TotalBytesToReceive <= 0)
                return;
            progress.Style = ProgressBarStyle.Continuous;
            progress.Value = args.ProgressPercentage;
            label.Text = "正在下载 " + display + "，已完成 " + args.ProgressPercentage + "%";
        };
        client.DownloadFileCompleted += delegate(object sender, System.ComponentModel.AsyncCompletedEventArgs args)
        {
            finished = true;
            failed = args.Error != null || args.Cancelled;
            failure = args.Error;
            form.Close();
        };
        form.Shown += delegate { client.DownloadFileAsync(new Uri(url), destination); };
        form.FormClosing += delegate(object sender, FormClosingEventArgs args)
        {
            if (!finished && client.IsBusy)
                client.CancelAsync();
        };
        form.ShowDialog();
        client.Dispose();
        if (failed)
            throw failure ?? new InvalidOperationException("下载更新失败。");
    }

    static string Hash(string path)
    {
        using (FileStream stream = File.OpenRead(path))
        using (MD5 md5 = MD5.Create())
        {
            byte[] bytes = md5.ComputeHash(stream);
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}
