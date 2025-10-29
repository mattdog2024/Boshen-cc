using System;
using System.Runtime.InteropServices;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// Windows API 结构体定义
    /// 为透明窗口创建和管理提供所需的数据结构
    /// </summary>
    public static class WindowStructs
    {
        #region 基础结构体

        /// <summary>
        /// 表示一个点的坐标
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }

            public static POINT Empty => new POINT(0, 0);
        }

        /// <summary>
        /// 表示一个矩形的尺寸
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int CX;
            public int CY;

            public SIZE(int cx, int cy)
            {
                CX = cx;
                CY = cy;
            }

            public static SIZE Empty => new SIZE(0, 0);
        }

        /// <summary>
        /// 表示一个矩形区域
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public int Width => Right - Left;
            public int Height => Bottom - Top;

            public static RECT Empty => new RECT(0, 0, 0, 0);
        }

        #endregion

        #region 分层窗口相关结构体

        /// <summary>
        /// 分层窗口混合函数
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;

            public BLENDFUNCTION(byte blendOp, byte blendFlags, byte sourceConstantAlpha, byte alphaFormat)
            {
                BlendOp = blendOp;
                BlendFlags = blendFlags;
                SourceConstantAlpha = sourceConstantAlpha;
                AlphaFormat = alphaFormat;
            }

            /// <summary>
            /// 创建默认的混合函数
            /// </summary>
            /// <param name="alpha">透明度 (0-255)</param>
            /// <returns>混合函数结构体</returns>
            public static BLENDFUNCTION CreateDefault(byte alpha = 255)
            {
                return new BLENDFUNCTION
                {
                    BlendOp = Win32Constants.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = alpha,
                    AlphaFormat = Win32Constants.AC_SRC_ALPHA
                };
            }
        }

        /// <summary>
        /// 更新分层窗口信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct UPDATELAYEREDWINDOWINFO
        {
            public uint cbSize;
            public IntPtr hdcDst;
            public IntPtr pptDst;
            public IntPtr psize;
            public IntPtr hdcSrc;
            public IntPtr pptSrc;
            public uint crKey;
            public IntPtr pblend;
            public uint dwFlags;
            public IntPtr prcDirty;

            public UPDATELAYEREDWINDOWINFO(
                IntPtr hdcDst,
                IntPtr pptDst,
                IntPtr psize,
                IntPtr hdcSrc,
                IntPtr pptSrc,
                uint crKey,
                IntPtr pblend,
                uint dwFlags,
                IntPtr prcDirty)
            {
                this.cbSize = (uint)Marshal.SizeOf(typeof(UPDATELAYEREDWINDOWINFO));
                this.hdcDst = hdcDst;
                this.pptDst = pptDst;
                this.psize = psize;
                this.hdcSrc = hdcSrc;
                this.pptSrc = pptSrc;
                this.crKey = crKey;
                this.pblend = pblend;
                this.dwFlags = dwFlags;
                this.prcDirty = prcDirty;
            }
        }

        #endregion

        #region 窗口类和消息相关结构体

        /// <summary>
        /// 窗口类信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszClassName;
            public IntPtr hIconSm;

            public WNDCLASSEX(
                uint style,
                IntPtr lpfnWndProc,
                IntPtr hInstance,
                [MarshalAs(UnmanagedType.LPTStr)] string className)
            {
                this.cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX));
                this.style = style;
                this.lpfnWndProc = lpfnWndProc;
                this.cbClsExtra = 0;
                this.cbWndExtra = 0;
                this.hInstance = hInstance;
                this.hIcon = IntPtr.Zero;
                this.hCursor = IntPtr.Zero;
                this.hbrBackground = IntPtr.Zero;
                this.lpszMenuName = null;
                this.lpszClassName = className;
                this.hIconSm = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 窗口消息结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        #endregion

        #region 扩展结构体

        /// <summary>
        /// 窗口创建参数
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct CREATESTRUCT
        {
            public IntPtr lpCreateParams;
            public IntPtr hInstance;
            public IntPtr hMenu;
            public IntPtr hwndParent;
            public int cy;
            public int cx;
            public int y;
            public int x;
            public uint style;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszClass;
            public uint dwExStyle;
        }

        /// <summary>
        /// 窗口信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWINFO
        {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;

            public static WINDOWINFO Create()
            {
                var info = new WINDOWINFO();
                info.cbSize = (uint)Marshal.SizeOf(typeof(WINDOWINFO));
                return info;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建点结构体
        /// </summary>
        public static POINT CreatePoint(int x, int y) => new POINT(x, y);

        /// <summary>
        /// 创建尺寸结构体
        /// </summary>
        public static SIZE CreateSize(int width, int height) => new SIZE(width, height);

        /// <summary>
        /// 创建矩形结构体
        /// </summary>
        public static RECT CreateRect(int x, int y, int width, int height)
            => new RECT(x, y, x + width, y + height);

        /// <summary>
        /// 从RECT转换为POINT和SIZE
        /// </summary>
        public static (POINT point, SIZE size) RectToPointSize(RECT rect)
        {
            return (new POINT(rect.Left, rect.Top), new SIZE(rect.Width, rect.Height));
        }

        /// <summary>
        /// 检查点是否在矩形内
        /// </summary>
        public static bool PointInRect(POINT point, RECT rect)
        {
            return point.X >= rect.Left && point.X < rect.Right &&
                   point.Y >= rect.Top && point.Y < rect.Bottom;
        }

        /// <summary>
        /// 检查两个矩形是否相交
        /// </summary>
        public static bool RectIntersect(RECT rect1, RECT rect2)
        {
            return !(rect1.Right <= rect2.Left || rect1.Left >= rect2.Right ||
                     rect1.Bottom <= rect2.Top || rect1.Top >= rect2.Bottom);
        }

        #endregion
    }

    /// <summary>
    /// Windows API 常量
    /// </summary>
    public static class Win32Constants
    {
        // 混合操作常量
        public const byte AC_SRC_OVER = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;

        // 窗口类样式
        public const uint CS_HREDRAW = 0x0002;
        public const uint CS_VREDRAW = 0x0001;
        public const uint CS_OWNDC = 0x0020;
        public const uint CS_CLASSDC = 0x0040;
        public const uint CS_PARENTDC = 0x0080;
        public const uint CS_NOCLOSE = 0x0200;
        public const uint CS_SAVEBITS = 0x0800;
        public const uint CS_BYTEALIGNCLIENT = 0x1000;
        public const uint CS_BYTEALIGNWINDOW = 0x2000;
        public const uint CS_GLOBALCLASS = 0x4000;

        // 消息常量
        public const uint WM_DESTROY = 0x0002;
        public const uint WM_PAINT = 0x000F;
        public const uint WM_CLOSE = 0x0010;
        public const uint WM_ERASEBKGND = 0x0014;
        public const uint WM_SIZE = 0x0005;
        public const uint WM_MOVE = 0x0003;
        public const uint WM_ACTIVATE = 0x0006;
        public const uint WM_MOUSEACTIVATE = 0x0021;
        public const uint WM_NCHITTEST = 0x0084;
        public const uint WM_NCLBUTTONDOWN = 0x00A1;
        public const uint WM_NCMOUSEMOVE = 0x00A0;

        // 默认光标
        public const int IDC_ARROW = 32512;
        public const int IDC_IBEAM = 32513;
        public const int IDC_WAIT = 32514;
        public const int IDC_CROSS = 32515;
        public const int IDC_UPARROW = 32516;
        public const int IDC_SIZE = 32640;
        public const int IDC_ICON = 32641;
        public const int IDC_SIZENWSE = 32642;
        public const int IDC_SIZENESW = 32643;
        public const int IDC_SIZEWE = 32644;
        public const int IDC_SIZENS = 32645;
        public const int IDC_SIZEALL = 32646;
        public const int IDC_NO = 32648;
        public const int IDC_HAND = 32649;
        public const int IDC_HELP = 32651;

        // 返回值常量
        public const IntPtr HTTRANSPARENT = new IntPtr(-1);
        public const IntPtr HTCLIENT = new IntPtr(1);
        public const IntPtr HTCAPTION = new IntPtr(2);

        // 窗口状态常量
        public const uint WS_ACTIVECAPTION = 0x0001;
    }
}