using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// Windows API P/Invoke 声明
    /// 提供透明窗口创建和管理所需的底层Windows API调用
    /// </summary>
    public static class Win32Api
    {
        #region 窗口创建和样式

        /// <summary>
        /// 创建扩展窗口
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            [MarshalAs(UnmanagedType.LPTStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPTStr)] string lpWindowName,
            uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        /// <summary>
        /// 销毁窗口
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        /// <summary>
        /// 设置窗口长整型属性
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(
            IntPtr hWnd,
            int nIndex,
            int dwNewLong);

        /// <summary>
        /// 获取窗口长整型属性
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(
            IntPtr hWnd,
            int nIndex);

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy,
            uint uFlags);

        /// <summary>
        /// 显示窗口
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(
            IntPtr hWnd,
            int nCmdShow);

        /// <summary>
        /// 更新窗口
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateWindow(IntPtr hWnd);

        /// <summary>
        /// 使窗口无效化，触发重绘
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InvalidateRect(
            IntPtr hWnd,
            IntPtr lpRect,
            [MarshalAs(UnmanagedType.Bool)] bool bErase);

        #endregion

        #region 分层窗口和透明度

        /// <summary>
        /// 设置分层窗口属性
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetLayeredWindowAttributes(
            IntPtr hWnd,
            uint crKey,
            byte bAlpha,
            uint dwFlags);

        /// <summary>
        /// 更新分层窗口
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateLayeredWindow(
            IntPtr hWnd,
            IntPtr hdcDst,
            ref POINT pptDst,
            ref SIZE psize,
            IntPtr hdcSrc,
            ref POINT pptSrc,
            uint crKey,
            ref BLENDFUNCTION pblend,
            uint dwFlags);

        /// <summary>
        /// 更新分层窗口（窗口化版本）
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateLayeredWindowIndirect(
            IntPtr hWnd,
            ref UPDATELAYEREDWINDOWINFO pULWInfo);

        #endregion

        #region 设备上下文和绘图

        /// <summary>
        /// 获取窗口设备上下文
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        /// <summary>
        /// 释放设备上下文
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        /// <summary>
        /// 创建兼容的设备上下文
        /// </summary>
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        /// <summary>
        /// 删除设备上下文
        /// </summary>
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(IntPtr hDC);

        /// <summary>
        /// 创建位图
        /// </summary>
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateBitmap(
            int nWidth,
            int nHeight,
            uint cPlanes,
            uint cBitsPerPel,
            IntPtr lpvBits);

        /// <summary>
        /// 创建兼容位图
        /// </summary>
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleBitmap(
            IntPtr hDC,
            int nWidth,
            int nHeight);

        /// <summary>
        /// 选择对象到设备上下文
        /// </summary>
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObj);

        /// <summary>
        /// 删除GDI对象
        /// </summary>
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        #endregion

        #region 窗口查找和枚举

        /// <summary>
        /// 查找窗口句柄
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(
            [MarshalAs(UnmanagedType.LPTStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPTStr)] string lpWindowName);

        /// <summary>
        /// 查找子窗口句柄
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(
            IntPtr hwndParent,
            IntPtr hwndChildAfter,
            [MarshalAs(UnmanagedType.LPTStr)] string lpszClass,
            [MarshalAs(UnmanagedType.LPTStr)] string lpszWindow);

        /// <summary>
        /// 枚举窗口
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(
            EnumWindowsProc lpEnumFunc,
            IntPtr lParam);

        /// <summary>
        /// 枚举窗口回调函数
        /// </summary>
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// 获取窗口类名
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(
            IntPtr hWnd,
            StringBuilder lpClassName,
            int nMaxCount);

        /// <summary>
        /// 获取窗口标题
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(
            IntPtr hWnd,
            StringBuilder lpString,
            int nMaxCount);

        /// <summary>
        /// 获取窗口标题长度
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// 获取窗口进程ID
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        #endregion

        #region 窗口位置和状态

        /// <summary>
        /// 获取窗口矩形
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// 获取客户区矩形
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// 屏幕坐标转客户区坐标
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        /// <summary>
        /// 客户区坐标转屏幕坐标
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        /// <summary>
        /// 获取窗口信息
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowInfo(IntPtr hWnd, ref WINDOWINFO pwi);

        /// <summary>
        /// 检查窗口是否可见
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// 检查窗口是否最小化
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        /// <summary>
        /// 检查窗口是否最大化
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(IntPtr hWnd);

        #endregion

        #region Windows事件钩子

        /// <summary>
        /// 设置Windows事件钩子
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWinEventHook(
            uint eventMin,
            uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc,
            uint idProcess,
            uint idThread,
            uint dwFlags);

        /// <summary>
        /// Windows事件回调函数
        /// </summary>
        public delegate void WinEventDelegate(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hWnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime);

        /// <summary>
        /// 移除Windows事件钩子
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        #endregion

        #region 错误处理

        /// <summary>
        /// 获取最后的错误代码
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetLastError();

        /// <summary>
        /// 格式化错误消息
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint FormatMessage(
            uint dwFlags,
            IntPtr lpSource,
            uint dwMessageId,
            uint dwLanguageId,
            StringBuilder lpBuffer,
            uint nSize,
            IntPtr Arguments);

        #endregion

        #region 常量定义

        // 窗口扩展样式
        public const uint WS_EX_LAYERED = 0x00080000;
        public const uint WS_EX_TRANSPARENT = 0x00000020;
        public const uint WS_EX_TOOLWINDOW = 0x00000080;
        public const uint WS_EX_NOACTIVATE = 0x08000000;

        // 窗口样式
        public const uint WS_POPUP = 0x80000000;
        public const uint WS_VISIBLE = 0x10000000;

        // 分层窗口标志
        public const uint LWA_COLORKEY = 0x00000001;
        public const uint LWA_ALPHA = 0x00000002;
        public const uint ULW_ALPHA = 0x00000002;
        public const uint ULW_COLORKEY = 0x00000001;
        public const uint ULW_OPAQUE = 0x00000004;

        // 窗口位置标志
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_HIDEWINDOW = 0x0080;
        public const uint SWP_NOACTIVATE = 0x0010;

        // 显示命令
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const int SW_SHOWNA = 8;
        public const int SW_SHOWNOACTIVATE = 4;

        // 窗口长整型索引
        public const int GWL_EXSTYLE = -20;
        public const int GWL_STYLE = -16;

        // Windows事件常量
        public const uint EVENT_OBJECT_CREATE = 0x8000;
        public const uint EVENT_OBJECT_DESTROY = 0x8001;
        public const uint EVENT_OBJECT_SHOW = 0x8002;
        public const uint EVENT_OBJECT_HIDE = 0x8003;
        public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        public const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
        public const uint EVENT_MIN = 0x00000001;
        public const uint EVENT_MAX = 0x7FFFFFFF;

        // 窗口事件钩子标志
        public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        public const uint WINEVENT_SKIPOWNTHREAD = 0x0001;
        public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
        public const uint WINEVENT_INCONTEXT = 0x0004;

        // 错误消息格式化标志
        public const uint FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
        public const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        public const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        public const uint FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取格式化的错误消息
        /// </summary>
        /// <param name="errorCode">错误代码</param>
        /// <returns>格式化的错误消息</returns>
        public static string GetErrorMessage(uint errorCode)
        {
            StringBuilder buffer = new StringBuilder(256);
            FormatMessage(
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                IntPtr.Zero,
                errorCode,
                0,
                buffer,
                (uint)buffer.Capacity,
                IntPtr.Zero);

            return buffer.ToString().Trim();
        }

        /// <summary>
        /// 获取最后的错误消息
        /// </summary>
        /// <returns>最后的错误消息</returns>
        public static string GetLastErrorMessage()
        {
            uint errorCode = GetLastError();
            return GetErrorMessage(errorCode);
        }

        /// <summary>
        /// 检查Windows API调用是否成功
        /// </summary>
        /// <param name="result">API调用结果</param>
        /// <param name="operation">操作名称</param>
        /// <exception cref="System.ComponentModel.Win32Exception">API调用失败时抛出</exception>
        public static void CheckWin32Result(bool result, string operation)
        {
            if (!result)
            {
                uint errorCode = GetLastError();
                string errorMessage = GetErrorMessage(errorCode);
                throw new System.ComponentModel.Win32Exception((int)errorCode, $"{operation} 失败: {errorMessage}");
            }
        }

        /// <summary>
        /// 检查Windows API调用是否成功（句柄版本）
        /// </summary>
        /// <param name="handle">API调用返回的句柄</param>
        /// <param name="operation">操作名称</param>
        /// <exception cref="System.ComponentModel.Win32Exception">API调用失败时抛出</exception>
        public static void CheckWin32Handle(IntPtr handle, string operation)
        {
            if (handle == IntPtr.Zero)
            {
                uint errorCode = GetLastError();
                string errorMessage = GetErrorMessage(errorCode);
                throw new System.ComponentModel.Win32Exception((int)errorCode, $"{operation} 失败: {errorMessage}");
            }
        }

        #endregion
    }
}