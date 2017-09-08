using System;
using System.Runtime.InteropServices;

namespace PersistentPlanet.Graphics.DirectX11
{
    public static class Win32
    {
        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceFrequency(out long freq);
        [DllImport("kernel32.dll")]
        public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();
    }
}