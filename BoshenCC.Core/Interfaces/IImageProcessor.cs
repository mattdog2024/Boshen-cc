using System;
using System.Drawing;
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
        /// <returns>处理后的图像</returns>
        Bitmap ProcessImage(Bitmap image);

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
        /// <returns>预处理后的图像</returns>
        Bitmap PreprocessImage(Bitmap image);
    }
}