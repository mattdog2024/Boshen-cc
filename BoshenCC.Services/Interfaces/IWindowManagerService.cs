using System;
using System.Collections.Generic;
using System.Drawing;

namespace BoshenCC.Services.Interfaces
{
    /// <summary>
    /// 窗口管理服务接口
    /// 负责透明窗口的创建、管理和生命周期控制
    /// </summary>
    public interface IWindowManagerService
    {
        /// <summary>
        /// 创建新的透明窗口
        /// </summary>
        /// <param name="name">窗口名称</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="alpha">透明度 (0-255)</param>
        /// <param name="clickThrough">是否支持点击穿透</param>
        /// <returns>窗口ID，创建失败返回null</returns>
        string CreateWindow(string name, int x, int y, int width, int height, byte alpha = 255, bool clickThrough = true);

        /// <summary>
        /// 销毁指定的透明窗口
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>是否销毁成功</returns>
        bool DestroyWindow(string windowId);

        /// <summary>
        /// 销毁所有透明窗口
        /// </summary>
        void DestroyAllWindows();

        /// <summary>
        /// 获取透明窗口
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>透明窗口实例，不存在返回null</returns>
        BoshenCC.Core.Utils.TransparentWindow GetWindow(string windowId);

        /// <summary>
        /// 获取所有窗口ID列表
        /// </summary>
        /// <returns>窗口ID列表</returns>
        IEnumerable<string> GetAllWindowIds();

        /// <summary>
        /// 显示窗口
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>是否成功</returns>
        bool ShowWindow(string windowId);

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>是否成功</returns>
        bool HideWindow(string windowId);

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>是否成功</returns>
        bool SetWindowPosition(string windowId, int x, int y);

        /// <summary>
        /// 设置窗口尺寸
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>是否成功</returns>
        bool SetWindowSize(string windowId, int width, int height);

        /// <summary>
        /// 设置窗口透明度
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <param name="alpha">透明度 (0-255)</param>
        /// <returns>是否成功</returns>
        bool SetWindowAlpha(string windowId, byte alpha);

        /// <summary>
        /// 设置窗口点击穿透
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <param name="enabled">是否启用点击穿透</param>
        /// <returns>是否成功</returns>
        bool SetWindowClickThrough(string windowId, bool enabled);

        /// <summary>
        /// 检查窗口是否存在
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>窗口是否存在</returns>
        bool WindowExists(string windowId);

        /// <summary>
        /// 获取窗口数量
        /// </summary>
        int WindowCount { get; }

        /// <summary>
        /// 窗口创建事件
        /// </summary>
        event EventHandler<WindowEventArgs> WindowCreated;

        /// <summary>
        /// 窗口销毁事件
        /// </summary>
        event EventHandler<WindowEventArgs> WindowDestroyed;

        /// <summary>
        /// 窗口属性更改事件
        /// </summary>
        event EventHandler<WindowPropertyChangedEventArgs> WindowPropertyChanged;
    }

    /// <summary>
    /// 窗口事件参数
    /// </summary>
    public class WindowEventArgs : EventArgs
    {
        public string WindowId { get; }
        public string WindowName { get; }

        public WindowEventArgs(string windowId, string windowName)
        {
            WindowId = windowId;
            WindowName = windowName;
        }
    }

    /// <summary>
    /// 窗口属性更改事件参数
    /// </summary>
    public class WindowPropertyChangedEventArgs : WindowEventArgs
    {
        public string PropertyName { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        public WindowPropertyChangedEventArgs(string windowId, string windowName, string propertyName, object oldValue, object newValue)
            : base(windowId, windowName)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}