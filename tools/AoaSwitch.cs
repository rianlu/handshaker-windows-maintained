using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

static class AoaSwitch
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SetupPacket { public byte RequestType, Request; public ushort Value, Index, Length; }
    [StructLayout(LayoutKind.Sequential)]
    struct DeviceInterfaceData { public int Size; public Guid ClassGuid; public int Flags; public IntPtr Reserved; }

    const uint Present = 2, DeviceInterface = 16;
    static readonly Guid AdbInterfaceClass = new Guid("f72fe0d4-cbcb-407d-8814-9ed673d0dd6b");

    [DllImport("setupapi.dll", SetLastError = true)] static extern IntPtr SetupDiGetClassDevs(ref Guid guid, IntPtr enumerator, IntPtr parent, uint flags);
    [DllImport("setupapi.dll", SetLastError = true)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info, ref Guid guid, uint index, ref DeviceInterfaceData data);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref DeviceInterfaceData data, IntPtr detail, uint size, out uint required, IntPtr info);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("winusb.dll", SetLastError = true)] static extern bool WinUsb_Initialize(SafeFileHandle file, out IntPtr handle);
    [DllImport("winusb.dll", SetLastError = true)] static extern bool WinUsb_ControlTransfer(IntPtr handle, SetupPacket packet, byte[] buffer, uint length, out uint transferred, IntPtr overlapped);
    [DllImport("winusb.dll")] static extern bool WinUsb_Free(IntPtr handle);

    static IEnumerable<string> Interfaces()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var guid = AdbInterfaceClass;
        var set = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero, Present | DeviceInterface);
        if (set == new IntPtr(-1)) yield break;
        try {
            for (uint i = 0; ; i++) {
                var data = new DeviceInterfaceData { Size = Marshal.SizeOf(typeof(DeviceInterfaceData)) };
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, i, ref data)) break;
                uint required;
                SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out required, IntPtr.Zero);
                var detail = Marshal.AllocHGlobal((int)required);
                try {
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (SetupDiGetDeviceInterfaceDetail(set, ref data, detail, required, out required, IntPtr.Zero)) {
                        var path = Marshal.PtrToStringUni(IntPtr.Add(detail, 4));
                        if (path != null && path.IndexOf("VID_18D1", StringComparison.OrdinalIgnoreCase) < 0 && seen.Add(path)) yield return path;
                    }
                } finally { Marshal.FreeHGlobal(detail); }
            }
        } finally { SetupDiDestroyDeviceInfoList(set); }
    }

    static bool Transfer(IntPtr handle, byte type, byte request, ushort index, byte[] data)
    {
        uint done;
        var packet = new SetupPacket { RequestType = type, Request = request, Index = index, Length = (ushort)data.Length };
        return WinUsb_ControlTransfer(handle, packet, data, (uint)data.Length, out done, IntPtr.Zero);
    }

    static bool Switch(string path)
    {
        using (var file = CreateFile(path, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero)) {
            IntPtr handle;
            if (file.IsInvalid || !WinUsb_Initialize(file, out handle)) return false;
            try {
                var protocol = new byte[2];
                if (!Transfer(handle, 0xC0, 51, 0, protocol)) return false;
                var version = BitConverter.ToUInt16(protocol, 0);
                if (version < 1 || version > 2) return false;
                var values = new[] { "Smartisan", "HandShaker", "HandShaker", "1.0", "http://sf.smartisan.com/sf/release/apk", "" };
                for (ushort i = 0; i < values.Length; i++) if (!Transfer(handle, 0x40, 52, i, Encoding.UTF8.GetBytes(values[i] + "\0"))) return false;
                return Transfer(handle, 0x40, 53, 0, new byte[0]);
            } finally { WinUsb_Free(handle); }
        }
    }

    static void StopAdb()
    {
        foreach (var process in Process.GetProcessesByName("adb")) try { process.Kill(); process.WaitForExit(2000); } catch { }
    }

    static void Log(string message)
    {
        try {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HandShaker");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "AoaLauncher.log"), DateTime.Now.ToString("s") + " " + message + Environment.NewLine);
        } catch { }
    }

    static bool SwitchConnected(Dictionary<string, int> attempts)
    {
        var paths = new List<string>(Interfaces());
        var candidates = paths.FindAll(path => !attempts.TryGetValue(path, out var count) || count < 3);
        var missing = new List<string>();
        foreach (var path in attempts.Keys) if (!paths.Contains(path)) missing.Add(path);
        foreach (var path in missing) attempts.Remove(path);
        if (candidates.Count == 0) return false;
        if (candidates.Exists(path => !attempts.ContainsKey(path))) StopAdb();
        var switched = false;
        foreach (var path in candidates) {
            var success = false;
            try { success = Switch(path); } catch (Exception error) { Log("switch error=" + error.Message); }
            if (success) { switched = true; attempts[path] = 3; Log("switched " + path); }
            else {
                attempts.TryGetValue(path, out var count);
                attempts[path] = count + 1;
                Log("switch failed attempt=" + attempts[path] + " " + path);
            }
        }
        return switched;
    }

    public static int Main()
    {
        using (var mutex = new Mutex(true, "HandShaker.AoaLauncher", out var owner)) {
            if (!owner) return 0;
            var root = AppDomain.CurrentDomain.BaseDirectory;
            var attempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (SwitchConnected(attempts)) Thread.Sleep(2000);
            Process.Start(new ProcessStartInfo(Path.Combine(root, "HandShakerStart.exe")) { WorkingDirectory = root });

            var started = false;
            var launchDeadline = DateTime.UtcNow.AddSeconds(30);
            for (;;) {
                // ponytail: 1s SetupAPI scan is simpler and more reliable here; use WM_DEVICECHANGE only if profiling shows measurable cost.
                Thread.Sleep(1000);
                SwitchConnected(attempts);
                var running = Process.GetProcessesByName("HandShaker").Length > 0;
                started |= running;
                if (started && !running) return 0;
                if (!started && DateTime.UtcNow >= launchDeadline) { Log("HandShaker launch timeout"); return 1; }
            }
        }
    }
}
