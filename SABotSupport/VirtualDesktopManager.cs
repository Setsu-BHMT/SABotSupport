using System;
using System.Runtime.InteropServices;

namespace SABotSupport
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
    [System.Security.SuppressUnmanagedCodeSecurity]
    public interface IVirtualDesktopManager
    {
        [PreserveSig]
        int IsWindowOnCurrentVirtualDesktop(
            [In] IntPtr TopLevelWindow,
            [Out] out int OnCurrentDesktop
            );
        [PreserveSig]
        int GetWindowDesktopId(
            [In] IntPtr TopLevelWindow,
            [Out] out Guid CurrentDesktop
            );

        [PreserveSig]
        int MoveWindowToDesktop(
            [In] IntPtr TopLevelWindow,
            [MarshalAs(UnmanagedType.LPStruct)]
            [In]Guid CurrentDesktop
            );
    }

    [ComImport, Guid("aa509086-5ca9-4c25-8f95-589d3c07b48a")]
    public class CVirtualDesktopManager
    {

    }
    public class VirtualDesktopManager
    {
        public static bool IsSupported { get; private set; } = true;    //disable functionality if we aren't on Windows 10 or later and virtual desktops aren't a thing

        public VirtualDesktopManager()
        {
            if (!IsSupported)
                return;

            try
            {
                cmanager = new CVirtualDesktopManager();
                manager = (IVirtualDesktopManager)cmanager;
            }
            catch (COMException)
            {
                IsSupported = false;
            }
        }
        ~VirtualDesktopManager()
        {
            manager = null;
            cmanager = null;
        }
        private CVirtualDesktopManager cmanager = null;
        private IVirtualDesktopManager manager;

        public bool IsWindowOnCurrentVirtualDesktop(IntPtr TopLevelWindow)
        {
            int hr;
            if ((hr = manager.IsWindowOnCurrentVirtualDesktop(TopLevelWindow, out int result)) != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            return result != 0;
        }

        public Guid GetWindowDesktopId(IntPtr TopLevelWindow)
        {
            int hr;
            if ((hr = manager.GetWindowDesktopId(TopLevelWindow, out Guid result)) != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            return result;
        }

        public void MoveWindowToDesktop(IntPtr TopLevelWindow, Guid CurrentDesktop)
        {
            int hr;
            if ((hr = manager.MoveWindowToDesktop(TopLevelWindow, CurrentDesktop)) != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}
