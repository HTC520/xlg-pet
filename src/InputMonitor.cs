using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace StardewDesktopPet
{
    public sealed class InputMonitor : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;

        private delegate IntPtr LowLevelHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelHookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private readonly LowLevelHookProc _kbProc;
        private readonly LowLevelHookProc _mouseProc;
        private IntPtr _kbHookId = IntPtr.Zero;
        private IntPtr _mouseHookId = IntPtr.Zero;

        // Track pressed virtual keys to avoid counting OS key-repeat holds as multiple presses
        private readonly HashSet<uint> _heldKeys = new HashSet<uint>();

        private int _pendingKeys;
        private int _pendingLeftClicks;
        private int _pendingRightClicks;
        private int _pendingMiddleClicks;
        private int _lastKeySlot;

        public bool IsInstalled
        {
            get { return _kbHookId != IntPtr.Zero || _mouseHookId != IntPtr.Zero; }
        }

        public InputMonitor()
        {
            _kbProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
        }

        public void Install()
        {
            if (IsInstalled) return;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hMod = curModule != null ? GetModuleHandle(curModule.ModuleName) : IntPtr.Zero;
                _kbHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, hMod, 0);
                _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
            }
        }

        public bool ConsumePending(out int keys, out int leftClicks, out int rightClicks, out int middleClicks, out int keySlot)
        {
            keys = Interlocked.Exchange(ref _pendingKeys, 0);
            leftClicks = Interlocked.Exchange(ref _pendingLeftClicks, 0);
            rightClicks = Interlocked.Exchange(ref _pendingRightClicks, 0);
            middleClicks = Interlocked.Exchange(ref _pendingMiddleClicks, 0);
            keySlot = _lastKeySlot;
            return (keys + leftClicks + rightClicks + middleClicks) > 0;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                KBDLLHOOKSTRUCT kbd = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    if (_heldKeys.Add(kbd.vkCode))
                    {
                        Interlocked.Increment(ref _pendingKeys);
                        _lastKeySlot = (int)(kbd.vkCode % 3);
                    }
                }
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    _heldKeys.Remove(kbd.vkCode);
                }
            }
            return CallNextHookEx(_kbHookId, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_LBUTTONDOWN)
                {
                    Interlocked.Increment(ref _pendingLeftClicks);
                    _lastKeySlot = 0;
                }
                else if (msg == WM_RBUTTONDOWN)
                {
                    Interlocked.Increment(ref _pendingRightClicks);
                    _lastKeySlot = 2;
                }
                else if (msg == WM_MBUTTONDOWN)
                {
                    Interlocked.Increment(ref _pendingMiddleClicks);
                    _lastKeySlot = 1;
                }
            }
            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_kbHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_kbHookId);
                _kbHookId = IntPtr.Zero;
            }
            if (_mouseHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookId);
                _mouseHookId = IntPtr.Zero;
            }
        }
    }
}
