using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Resources;
using QRCoder;

static class PatchQrResource
{
    const string ResourceName = "image/apkqrcode.png";

    static byte[] CreateQrCode(string url)
    {
        using (var generator = new QRCodeGenerator())
        using (var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M))
        using (var qrCode = new QRCode(data))
        using (var source = qrCode.GetGraphic(2))
        using (var image = new Bitmap(100, 100))
        using (var graphics = Graphics.FromImage(image))
        using (var output = new MemoryStream()) {
            graphics.Clear(Color.White);
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.DrawImageUnscaled(source, (image.Width - source.Width) / 2, (image.Height - source.Height) / 2);
            image.Save(output, ImageFormat.Png);
            return output.ToArray();
        }
    }

    public static int Main(string[] args)
    {
        if (args.Length != 2) {
            Console.Error.WriteLine("usage: PatchQrResource <resources> <url>");
            return 2;
        }

        var input = args[0];
        var output = input + ".patched";
        var replaced = false;
        var qrCode = CreateQrCode(args[1]);

        using (var reader = new ResourceReader(input))
        using (var writer = new ResourceWriter(output)) {
            foreach (DictionaryEntry entry in reader) {
                var name = (string)entry.Key;
                if (string.Equals(name, ResourceName, StringComparison.OrdinalIgnoreCase)) {
                    writer.AddResource(name, new MemoryStream(qrCode), true);
                    replaced = true;
                } else if (entry.Value is Stream) {
                    var copy = new MemoryStream();
                    ((Stream)entry.Value).CopyTo(copy);
                    copy.Position = 0;
                    writer.AddResource(name, copy, true);
                } else {
                    writer.AddResource(name, entry.Value);
                }
            }
            writer.Generate();
        }

        if (!replaced) {
            File.Delete(output);
            Console.Error.WriteLine("missing resource: " + ResourceName);
            return 1;
        }

        File.Delete(input);
        File.Move(output, input);
        Console.WriteLine("Android release QR updated: " + qrCode.Length + " bytes");
        return 0;
    }
}
