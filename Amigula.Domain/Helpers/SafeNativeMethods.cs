using System;
using System.Runtime.InteropServices;

namespace Amigula.Domain.Helpers
{
    internal static class SafeNativeMethods
    {
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string moduleName);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule,
            [MarshalAs(UnmanagedType.LPWStr)] string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}