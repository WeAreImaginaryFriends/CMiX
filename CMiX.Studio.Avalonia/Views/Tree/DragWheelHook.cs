// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CMiX.Studio.Avalonia.Views
{
    // Gives the mouse wheel during a drag. Windows runs its own loop for a drag and sends no wheel message to the window,
    // so a low level mouse hook reads the wheel. It exists only while a drag is over the tree. Other systems have no hook.
    public sealed class DragWheelHook : IDisposable
    {
        private const int WhMouseLl = 14;
        private const int WmMouseWheel = 0x020A;

        private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseHookData
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr module, uint thread);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? name);

        private readonly Action<int> onWheel;
        private readonly HookProc proc;
        private IntPtr hook;

        // onWheel gets the wheel steps: positive when the wheel turns away from the user (scroll up).
        public DragWheelHook(Action<int> onWheel)
        {
            this.onWheel = onWheel;
            proc = Callback;
        }

        public bool IsActive => hook != IntPtr.Zero;

        public void Start()
        {
            if (hook != IntPtr.Zero || !OperatingSystem.IsWindows())
                return;

            try
            {
                using var process = Process.GetCurrentProcess();
                hook = SetWindowsHookEx(WhMouseLl, proc, GetModuleHandle(process.MainModule?.ModuleName), 0);
            }
            catch (Exception)
            {
                hook = IntPtr.Zero;
            }
        }

        public void Stop()
        {
            if (hook == IntPtr.Zero)
                return;

            UnhookWindowsHookEx(hook);
            hook = IntPtr.Zero;
        }

        private IntPtr Callback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && wParam.ToInt32() == WmMouseWheel)
            {
                var data = Marshal.PtrToStructure<MouseHookData>(lParam);
                var steps = (short)(data.MouseData >> 16) / 120;
                if (steps != 0)
                    onWheel(steps);
            }

            return CallNextHookEx(hook, code, wParam, lParam);
        }

        public void Dispose() => Stop();
    }
}
