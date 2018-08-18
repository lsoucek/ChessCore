using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ChessCoreEngine.Utils
{
    public class WindowScreenPositionManager
    {
        static Logger logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]

        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        public static bool GetWindowsCoordinates(out int x, out int y, out int width, out int height)
        {
            IntPtr ptr = GetConsoleWindow();
            if (GetWindowRect(ptr, out RECT rct))
            {
                x = rct.Left;
                y = rct.Top;
                width = rct.Right - rct.Left;
                height = rct.Bottom - rct.Top;

                return true;
            }

            x = 0;
            y = 0;
            width = 0;
            height = 0;

            logger.Error("GetWinCoordinatesOnScreen call reported error");

            return false;
        }

        public static bool SetWindowCoordinates(int x, int y, int width, int height)
        {
            IntPtr ptr = GetConsoleWindow();

            if (MoveWindow(ptr, x, y, width, height, true)) return true;

            logger.Error("MoveWindow call reported error");
            return false;

            //if (logger.IsInfoLevelLog) logger.Info($"Console position set to x={x} y={y} width={width} height={height}");
        }
    }
}
