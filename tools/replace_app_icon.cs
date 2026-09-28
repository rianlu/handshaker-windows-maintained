using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr BeginUpdateResource(string file, bool deleteExisting);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool UpdateResource(IntPtr update, IntPtr type, IntPtr name, ushort language, byte[] data, uint size);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool EndUpdateResource(IntPtr update, bool discard);

    static int Main(string[] args)
    {
        string pngPath = args[0];
        string icoPath = args[1];
        string pngDir = args[2];
        using (Bitmap source = new Bitmap(pngPath))
        {
            int[] icoSizes = { 16, 24, 32, 48, 64, 128, 256 };
            var icoImages = new List<byte[]>();
            foreach (int size in icoSizes) icoImages.Add(Png(source, size, size));
            File.WriteAllBytes(icoPath, Ico(icoSizes, icoImages));
            Directory.CreateDirectory(pngDir);
            File.WriteAllBytes(Path.Combine(pngDir, "156x132.png"), IndexedPng(source, 156, 132));
            File.WriteAllBytes(Path.Combine(pngDir, "128x128.png"), IndexedPng(source, 128, 128));
            File.WriteAllBytes(Path.Combine(pngDir, "175x175.png"), IndexedPng(source, 175, 175));
            File.WriteAllBytes(Path.Combine(pngDir, "40x40.png"), IndexedPng(source, 40, 40));
            File.WriteAllBytes(Path.Combine(pngDir, "56x58.png"), IndexedPng(source, 56, 58));
            for (int i = 3; i < args.Length; i++)
            {
                if (!File.Exists(args[i])) continue;
                ReplaceExeIcon(args[i], icoSizes, icoImages);
            }
        }
        Console.WriteLine("app icon written");
        return 0;
    }

    static byte[] Png(Bitmap source, int width, int height)
    {
        using (Bitmap image = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (Graphics graphics = Graphics.FromImage(image))
        {
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            float scale = Math.Min(width / (float)source.Width, height / (float)source.Height);
            float drawWidth = source.Width * scale;
            float drawHeight = source.Height * scale;
            graphics.DrawImage(source, (width - drawWidth) / 2f, (height - drawHeight) / 2f, drawWidth, drawHeight);
            using (MemoryStream stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }

    static byte[] IndexedPng(Bitmap source, int width, int height)
    {
        using (Bitmap image = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (Graphics graphics = Graphics.FromImage(image))
        {
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            float scale = Math.Min(width / (float)source.Width, height / (float)source.Height);
            float drawWidth = source.Width * scale;
            float drawHeight = source.Height * scale;
            graphics.DrawImage(source, (width - drawWidth) / 2f, (height - drawHeight) / 2f, drawWidth, drawHeight);
            var counts = new Dictionary<int, int>();
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color color = image.GetPixel(x, y);
                int key = ((color.A >> 4) << 12) | ((color.R >> 4) << 8) | ((color.G >> 4) << 4) | (color.B >> 4);
                int count;
                counts.TryGetValue(key, out count);
                counts[key] = count + 1;
            }
            var palette = new List<int>(counts.Keys);
            palette.Sort((a, b) => counts[b].CompareTo(counts[a]));
            if (palette.Count > 256) palette = palette.GetRange(0, 256);
            byte[] indexes = new byte[width * height];
            int i = 0;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color color = image.GetPixel(x, y);
                int key = ((color.A >> 4) << 12) | ((color.R >> 4) << 8) | ((color.G >> 4) << 4) | (color.B >> 4);
                int best = 0;
                int bestScore = int.MaxValue;
                for (int p = 0; p < palette.Count; p++)
                {
                    int score = Math.Abs((palette[p] >> 12) - (key >> 12)) * 4
                        + Math.Abs(((palette[p] >> 8) & 15) - ((key >> 8) & 15))
                        + Math.Abs(((palette[p] >> 4) & 15) - ((key >> 4) & 15))
                        + Math.Abs((palette[p] & 15) - (key & 15));
                    if (score < bestScore) { bestScore = score; best = p; }
                }
                indexes[i++] = (byte)best;
            }
            return WriteIndexedPng(width, height, indexes, palette);
        }
    }

    static byte[] WriteIndexedPng(int width, int height, byte[] indexes, List<int> palette)
    {
        var png = new MemoryStream();
        png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
        byte[] header = new byte[13];
        WriteInt(header, 0, width);
        WriteInt(header, 4, height);
        header[8] = 8;
        header[9] = 3;
        Chunk(png, "IHDR", header);
        byte[] plte = new byte[palette.Count * 3];
        byte[] trns = new byte[palette.Count];
        for (int i = 0; i < palette.Count; i++)
        {
            plte[i * 3] = (byte)(((palette[i] >> 8) & 15) * 17);
            plte[i * 3 + 1] = (byte)(((palette[i] >> 4) & 15) * 17);
            plte[i * 3 + 2] = (byte)((palette[i] & 15) * 17);
            trns[i] = (byte)(((palette[i] >> 12) & 15) * 17);
        }
        Chunk(png, "PLTE", plte);
        Chunk(png, "tRNS", trns);
        byte[] raw = new byte[(width + 1) * height];
        for (int y = 0; y < height; y++)
        {
            raw[y * (width + 1)] = 0;
            Buffer.BlockCopy(indexes, y * width, raw, y * (width + 1) + 1, width);
        }
        Chunk(png, "IDAT", IonicDeflate(raw));
        Chunk(png, "IEND", new byte[0]);
        return png.ToArray();
    }

    static byte[] IonicDeflate(byte[] raw)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (var deflate = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal, true))
                deflate.Write(raw, 0, raw.Length);
            byte[] data = output.ToArray();
            var zlib = new byte[data.Length + 6];
            zlib[0] = 0x78;
            zlib[1] = 0xDA;
            Buffer.BlockCopy(data, 0, zlib, 2, data.Length);
            uint adler = Adler32(raw);
            zlib[zlib.Length - 4] = (byte)(adler >> 24);
            zlib[zlib.Length - 3] = (byte)(adler >> 16);
            zlib[zlib.Length - 2] = (byte)(adler >> 8);
            zlib[zlib.Length - 1] = (byte)adler;
            return zlib;
        }
    }

    static uint Adler32(byte[] data)
    {
        uint a = 1, b = 0;
        for (int i = 0; i < data.Length; i++)
        {
            a = (a + data[i]) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }

    static void WriteInt(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    static void Chunk(MemoryStream png, string name, byte[] data)
    {
        WriteIntTo(png, data.Length);
        byte[] tag = System.Text.Encoding.ASCII.GetBytes(name);
        png.Write(tag, 0, 4);
        png.Write(data, 0, data.Length);
        uint crc = Crc(tag, data);
        WriteIntTo(png, unchecked((int)crc));
    }

    static void WriteIntTo(MemoryStream stream, int value)
    {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    static uint Crc(byte[] tag, byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        for (int i = 0; i < tag.Length; i++) crc = Table[(crc ^ tag[i]) & 255] ^ (crc >> 8);
        for (int i = 0; i < data.Length; i++) crc = Table[(crc ^ data[i]) & 255] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFF;
    }

    static readonly uint[] Table = BuildCrc();
    static uint[] BuildCrc()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int j = 0; j < 8; j++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }

    static byte[] Ico(int[] sizes, List<byte[]> images)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)images.Count);
            int offset = 6 + 16 * images.Count;
            for (int i = 0; i < images.Count; i++)
            {
                byte side = sizes[i] >= 256 ? (byte)0 : (byte)sizes[i];
                writer.Write(side);
                writer.Write(side);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(images[i].Length);
                writer.Write(offset);
                offset += images[i].Length;
            }
            for (int i = 0; i < images.Count; i++) writer.Write(images[i]);
            return stream.ToArray();
        }
    }

    static void ReplaceExeIcon(string path, int[] sizes, List<byte[]> images)
    {
        IntPtr update = BeginUpdateResource(path, false);
        if (update == IntPtr.Zero) throw new InvalidOperationException("cannot update " + path);
        try
        {
            for (int i = 0; i < images.Count; i++)
            {
                if (!UpdateResource(update, (IntPtr)3, (IntPtr)(i + 1), 0, images[i], (uint)images[i].Length))
                    throw new InvalidOperationException("icon image " + path);
            }
            byte[] group = Group(sizes, images);
            if (!UpdateResource(update, (IntPtr)14, (IntPtr)32512, 0, group, (uint)group.Length))
                throw new InvalidOperationException("icon group " + path);
        }
        catch
        {
            EndUpdateResource(update, true);
            throw;
        }
        if (!EndUpdateResource(update, false)) throw new InvalidOperationException("save " + path);
        Console.WriteLine("exe icon " + Path.GetFileName(path));
    }

    static byte[] Group(int[] sizes, List<byte[]> images)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)images.Count);
            for (int i = 0; i < images.Count; i++)
            {
                byte side = sizes[i] >= 256 ? (byte)0 : (byte)sizes[i];
                writer.Write(side);
                writer.Write(side);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(images[i].Length);
                writer.Write((ushort)(i + 1));
            }
            return stream.ToArray();
        }
    }
}
