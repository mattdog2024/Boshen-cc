using System;
using System.Drawing;

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
    }
}