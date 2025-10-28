using System;
using System.Drawing;
using System.Drawing.Imaging;
using BoshenCC.Models;

namespace BoshenCC.Core.Interfaces
{
    /// <summary>
    /// 图像处理器接口
    /// </summary>
    public interface IImageProcessor
    {
        /// <summary>
        /// 处理图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="options">处理选项</param>
        /// <returns>处理后的图像</returns>
        Bitmap ProcessImage(Bitmap image, ProcessingOptions options = null);

        /// <summary>
        /// 识别图像中的字符
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>识别结果</returns>
        RecognitionResult RecognizeCharacters(Bitmap image);

        /// <summary>
        /// 预处理图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="options">预处理选项</param>
        /// <returns>预处理后的图像</returns>
        Bitmap PreprocessImage(Bitmap image, ProcessingOptions options = null);

        /// <summary>
        /// 灰度化图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>灰度图像</returns>
        Bitmap ConvertToGrayscale(Bitmap image);

        /// <summary>
        /// 二值化图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="threshold">阈值</param>
        /// <returns>二值化图像</returns>
        Bitmap ThresholdImage(Bitmap image, int threshold = 128);

        /// <summary>
        /// 降噪处理
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>降噪后的图像</returns>
        Bitmap DenoiseImage(Bitmap image);

        /// <summary>
        /// 边缘检测
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>边缘图像</returns>
        Bitmap DetectEdges(Bitmap image);

        /// <summary>
        /// 图像缩放
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>缩放后的图像</returns>
        Bitmap ScaleImage(Bitmap image, double scale);

        /// <summary>
        /// 裁剪图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="rectangle">裁剪区域</param>
        /// <returns>裁剪后的图像</returns>
        Bitmap CropImage(Bitmap image, Rectangle rectangle);

        /// <summary>
        /// 旋转图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="angle">旋转角度</param>
        /// <returns>旋转后的图像</returns>
        Bitmap RotateImage(Bitmap image, float angle);

        /// <summary>
        /// 检测图像中的K线形态
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>K线识别结果</returns>
        RecognitionResult DetectCandlestickPatterns(Bitmap image);
    }
}
