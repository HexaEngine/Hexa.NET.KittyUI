namespace Hexa.NET.KittyUI.UI
{
    using Hexa.NET.DirectXTex;
    using Hexa.NET.Mathematics;
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Text;

    [SupportedOSPlatform("windows")]
    public static unsafe class Taskbar
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct AppbarData
        {
            public int CbSize;
            public IntPtr HWnd;
            public int UCallbackMessage;
            public int UEdge;
            public Rectangle Rc;
            public IntPtr LParam;
        }

        private const int AbmQueryPos = 0x00000002; // ABM_QUERYPOS
        private const int AbmGetTaskbarPos = 0x00000005;

        [DllImport("shell32.dll", EntryPoint = "SHAppBarMessage", SetLastError = true)]
        private static extern IntPtr ShAppBarMessage(int dwMessage, AppbarData* appBarData);

        public static Rectangle GetTaskbarPosition(nint hwnd)
        {
            AppbarData appbarData = new()
            {
                CbSize = sizeof(AppbarData),
                HWnd = hwnd,
                UEdge = AbmQueryPos,
            };

            ShAppBarMessage(AbmQueryPos, &appbarData);
            return appbarData.Rc;
        }

        // Define typed handles
        public struct HWND : IEquatable<HWND>
        {
            public nint Handle;

            public HWND(nint handle)
            {
                Handle = handle;
            }

            public readonly bool IsNull => Handle == 0;

            public override readonly bool Equals(object? obj)
            {
                return obj is HWND hWND && Equals(hWND);
            }

            public readonly bool Equals(HWND other)
            {
                return Handle.Equals(other.Handle);
            }

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(Handle);
            }

            public static bool operator ==(HWND left, HWND right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(HWND left, HWND right)
            {
                return !(left == right);
            }

            public static implicit operator HWND(nint handle) => new(handle);

            public static implicit operator nint(HWND handle) => handle.Handle;
        }

        // PInvoke declarations
        [DllImport("user32.dll", SetLastError = true)]
        private static extern HWND FindWindow(byte* lpClassName, byte* lpWindowName);

        private static HWND FindWindow(ReadOnlySpan<byte> lpClassName, ReadOnlySpan<byte> lpWindowName)
        {
            fixed (byte* pLpClassName = lpClassName)
            {
                fixed (byte* pLpWindowName = lpWindowName)
                {
                    return FindWindow(pLpClassName, pLpWindowName);
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern HWND FindWindowEx(HWND hwndParent, HWND hwndChildAfter, byte* lpszClass, byte* lpszWindow);

        private static HWND FindWindowEx(HWND hwndParent, HWND hwndChildAfter, ReadOnlySpan<byte> lpszClass, ReadOnlySpan<byte> lpszWindow)
        {
            fixed (byte* pLpszClass = lpszClass)
            {
                fixed (byte* pLpszWindow = lpszWindow)
                {
                    return FindWindowEx(hwndParent, hwndChildAfter, pLpszClass, pLpszWindow);
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(HWND hwnd, Rectangle* lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumChildWindows(HWND hWndParent, delegate*<HWND, void*, bool> lpEnumFunc, void* lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern HWND GetParent(HWND hWnd);

        private delegate bool EnumWindowsProc(HWND hWnd, nint lParam);

        public struct Userdata
        {
            public HWND Hwnd;
            public Rectangle Result;
        }

        public static Rectangle GetAppRect(HWND windowHandle)
        {
            HWND taskbar = FindWindow("Shell_TrayWnd"u8, null);
            if (!taskbar.IsNull)
            {
                HWND rebar = FindWindowEx(taskbar, default, "ReBarWindow32"u8, null);
                if (!rebar.IsNull)
                {
                    HWND hWndMSTaskSwWClass = FindWindowEx(rebar, default, "MSTaskSwWClass"u8, null);
                    if (!hWndMSTaskSwWClass.IsNull)
                    {
                        HWND tasklist = FindWindowEx(hWndMSTaskSwWClass, default, "MSTaskListWClass"u8, null);
                        if (!tasklist.IsNull)
                        {
                            Userdata userdata = default;
                            userdata.Hwnd = windowHandle;
                            EnumChildWindows(tasklist, &Search, &userdata);
                            return userdata.Result;
                        }
                    }
                }
            }

            return default;
        }

        private static bool Search(HWND hWnd, void* userdata)
        {
            Userdata* data = (Userdata*)userdata;
            HWND hwndOwner = GetParent(hWnd);
            if (hwndOwner.Handle == data->Hwnd.Handle)
            {
                Rectangle rect;
                if (GetWindowRect(hWnd, &rect))
                {
                    data->Result = rect;
                }
                return false; // Found the matching button, stop enumeration
            }

            return true; // Continue enumeration
        }
    }
}