using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using BoshenCC.Core.Utils;

namespace BoshenCC.Services.Interfaces
{
    /// <summary>
    /// 截图服务接口
    /// </summary>
    public interface IScreenshotService
    {
        /// <summary>
        /// 截取全屏
        /// </summary>
        /// <returns>屏幕截图</returns>
        Bitmap CaptureFullScreen();

        /// <summary>
        /// 截取指定区域
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>区域截图</returns>
        Bitmap CaptureRegion(int x, int y, int width, int height);

        /// <summary>
        /// 截取指定窗口
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <returns>窗口截图</returns>
        Bitmap CaptureWindow(IntPtr handle);

        /// <summary>
        /// 获取屏幕尺寸
        /// </summary>
        /// <returns>屏幕尺寸</returns>
        Rectangle GetScreenBounds();

        /// <summary>
        /// 保存截图到文件
        /// </summary>
        /// <param name="screenshot">截图</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="format">图片格式</param>
        void SaveToFile(Bitmap screenshot, string filePath, System.Drawing.Imaging.ImageFormat format);

        #region 异步方法

        /// <summary>
        /// 异步截取全屏
        /// </summary>
        /// <returns>屏幕截图任务</returns>
        Task<Bitmap> CaptureFullScreenAsync();

        /// <summary>
        /// 异步截取指定区域
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>区域截图任务</returns>
        Task<Bitmap> CaptureRegionAsync(int x, int y, int width, int height);

        /// <summary>
        /// 异步截取指定窗口
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <returns>窗口截图任务</returns>
        Task<Bitmap> CaptureWindowAsync(IntPtr handle);

        /// <summary>
        /// 异步保存截图到文件
        /// </summary>
        /// <param name="screenshot">截图</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="format">图片格式</param>
        /// <returns>保存任务</returns>
        Task SaveToFileAsync(Bitmap screenshot, string filePath, System.Drawing.Imaging.ImageFormat format);

        #endregion

        #region 窗口检测

        /// <summary>
        /// 获取所有可见窗口
        /// </summary>
        /// <returns>可见窗口列表</returns>
        List<WindowCapture.WindowInfo> GetVisibleWindows();

        /// <summary>
        /// 查找指定标题的窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="exactMatch">是否精确匹配</param>
        /// <returns>窗口信息</returns>
        WindowCapture.WindowInfo FindWindowByTitle(string title, bool exactMatch = false);

        /// <summary>
        /// 查找文华行情软件窗口
        /// </summary>
        /// <returns>文华行情窗口信息</returns>
        WindowCapture.WindowInfo FindWenhuaWindow();

        /// <summary>
        /// 获取当前活动窗口
        /// </summary>
        /// <returns>当前活动窗口信息</returns>
        WindowCapture.WindowInfo GetActiveWindow();

        /// <summary>
        /// 等待文华行情窗口出现
        /// </summary>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>文华行情窗口信息</returns>
        Task<WindowCapture.WindowInfo> WaitForWenhuaWindowAsync(int timeoutMs = 10000);

        /// <summary>
        /// 截取文华行情软件窗口
        /// </summary>
        /// <returns>文华行情窗口截图</returns>
        Bitmap CaptureWenhuaWindow();

        /// <summary>
        /// 异步截取文华行情软件窗口
        /// </summary>
        /// <returns>文华行情窗口截图任务</returns>
        Task<Bitmap> CaptureWenhuaWindowAsync();

        #endregion

        #region 高级功能

        /// <summary>
        /// 截取指定窗口的客户端区域
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <returns>客户端区域截图</returns>
        Bitmap CaptureWindowClient(IntPtr handle);

        /// <summary>
        /// 异步截取指定窗口的客户端区域
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <returns>客户端区域截图任务</returns>
        Task<Bitmap> CaptureWindowClientAsync(IntPtr handle);

        /// <summary>
        /// 批量截取多个窗口
        /// </summary>
        /// <param name="handles">窗口句柄列表</param>
        /// <returns>截图字典（句柄 -> 截图）</returns>
        Dictionary<IntPtr, Bitmap> CaptureMultipleWindows(List<IntPtr> handles);

        /// <summary>
        /// 异步批量截取多个窗口
        /// </summary>
        /// <param name="handles">窗口句柄列表</param>
        /// <returns>截图字典任务</returns>
        Task<Dictionary<IntPtr, Bitmap>> CaptureMultipleWindowsAsync(List<IntPtr> handles);

        /// <summary>
        /// 截取屏幕区域（使用ScreenRegion工具）
        /// </summary>
        /// <param name="region">屏幕区域</param>
        /// <returns>区域截图</returns>
        Bitmap CaptureScreenRegion(ScreenRegion.Region region);

        /// <summary>
        /// 异步截取屏幕区域
        /// </summary>
        /// <param name="region">屏幕区域</param>
        /// <returns>区域截图任务</returns>
        Task<Bitmap> CaptureScreenRegionAsync(ScreenRegion.Region region);

        /// <summary>
        /// 连续截图监控
        /// </summary>
        /// <param name="region">监控区域</param>
        /// <param name="intervalMs">截图间隔（毫秒）</param>
        /// <param name="callback">截图回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>监控任务</returns>
        Task StartContinuousCapture(ScreenRegion.Region region, int intervalMs,
            Func<Bitmap, Task> callback, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// 高性能截图（优化版本）
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>区域截图</returns>
        Bitmap CaptureRegionFast(int x, int y, int width, int height);

        /// <summary>
        /// 批量截图并缓存
        /// </summary>
        /// <param name="regions">区域列表</param>
        /// <param name="cacheTimeoutMs">缓存超时时间（毫秒）</param>
        /// <returns>缓存管理器</returns>
        ScreenshotCache CreateScreenshotCache(List<ScreenRegion.Region> regions, int cacheTimeoutMs = 1000);

        /// <summary>
        /// 获取DPI缩放比例
        /// </summary>
        /// <returns>DPI缩放比例</returns>
        double GetDpiScale();

        /// <summary>
        /// 根据DPI调整坐标
        /// </summary>
        /// <param name="x">原始X坐标</param>
        /// <param name="y">原始Y坐标</param>
        /// <param name="width">原始宽度</param>
        /// <param name="height">原始高度</param>
        /// <returns>调整后的矩形</returns>
        Rectangle AdjustForDpi(int x, int y, int width, int height);

        #endregion
    }
}