using System;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using BoshenCC.Core.Interfaces;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Core.Core
{
    /// <summary>
    /// 图像处理器实现
    /// </summary>
    public class ImageProcessor : IImageProcessor
    {
        private readonly ILogService _logService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        public ImageProcessor(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// 处理图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="options">处理选项</param>
        /// <returns>处理后的图像</returns>
        public Bitmap ProcessImage(Bitmap image, ProcessingOptions options = null)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                options = options ?? new ProcessingOptions();
                _logService.Info($"开始处理图像，宽度: {image.Width}, 高度: {image.Height}");

                Bitmap result = (Bitmap)image.Clone();

                // 预处理
                if (options.EnablePreprocessing)
                {
                    result = PreprocessImage(result, options);
                }

                _logService.Info("图像处理完成");
                return result;
            }
            catch (Exception ex)
            {
                _logService.Error("图像处理失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 识别图像中的字符
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>识别结果</returns>
        public RecognitionResult RecognizeCharacters(Bitmap image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info("开始字符识别");

                // 这里应该实现OCR识别逻辑
                // 目前返回一个模拟的结果
                var result = new RecognitionResult
                {
                    IsSuccess = true,
                    Confidence = 0.85,
                    ProcessedImage = (Bitmap)image.Clone(),
                    Message = "字符识别完成"
                };

                _logService.Info($"字符识别完成，置信度: {result.Confidence}");
                return result;
            }
            catch (Exception ex)
            {
                _logService.Error("字符识别失败", ex);
                return new RecognitionResult
                {
                    IsSuccess = false,
                    Confidence = 0.0,
                    ProcessedImage = image,
                    Message = $"识别失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 预处理图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="options">预处理选项</param>
        /// <returns>预处理后的图像</returns>
        public Bitmap PreprocessImage(Bitmap image, ProcessingOptions options = null)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                options = options ?? new ProcessingOptions();
                Bitmap result = (Bitmap)image.Clone();

                _logService.Info("开始图像预处理");

                // 灰度化
                if (options.ConvertToGrayscale)
                {
                    result = ConvertToGrayscale(result);
                }

                // 二值化
                if (options.EnableThreshold && options.Threshold > 0)
                {
                    result = ThresholdImage(result, options.Threshold);
                }

                // 降噪
                if (options.EnableDenoise)
                {
                    result = DenoiseImage(result);
                }

                _logService.Info("图像预处理完成");
                return result;
            }
            catch (Exception ex)
            {
                _logService.Error("图像预处理失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 灰度化图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>灰度图像</returns>
        public Bitmap ConvertToGrayscale(Bitmap image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info("开始灰度化处理");

                // 使用EmguCV进行灰度化
                using (var cvImage = new Image<Bgr, byte>(image))
                {
                    var grayImage = cvImage.Convert<Gray, byte>();
                    return grayImage.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                _logService.Error("灰度化处理失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 二值化图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="threshold">阈值</param>
        /// <returns>二值化图像</returns>
        public Bitmap ThresholdImage(Bitmap image, int threshold = 128)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info($"开始二值化处理，阈值: {threshold}");

                // 使用EmguCV进行二值化
                using (var cvImage = new Image<Gray, byte>(image))
                {
                    var thresholdImage = cvImage.ThresholdBinary(new Gray(threshold), new Gray(255));
                    return thresholdImage.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                _logService.Error("二值化处理失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 降噪处理
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>降噪后图像</returns>
        public Bitmap DenoiseImage(Bitmap image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info("开始降噪处理");

                // 使用EmguCV进行降噪（中值滤波）
                using (var cvImage = new Image<Bgr, byte>(image))
                {
                    var denoisedImage = cvImage.SmoothMedian(3);
                    return denoisedImage.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                _logService.Error("降噪处理失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 边缘检测
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>边缘图像</returns>
        public Bitmap DetectEdges(Bitmap image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info("开始边缘检测");

                // 使用EmguCV进行Canny边缘检测
                using (var cvImage = new Image<Gray, byte>(image))
                {
                    var edgeImage = cvImage.Canny(50, 150);
                    return edgeImage.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                _logService.Error("边缘检测失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 图像缩放
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>缩放后图像</returns>
        public Bitmap ScaleImage(Bitmap image, double scale)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                if (scale <= 0)
                    throw new ArgumentException("缩放比例必须大于0", nameof(scale));

                _logService.Info($"开始图像缩放，比例: {scale}");

                int newWidth = (int)(image.Width * scale);
                int newHeight = (int)(image.Height * scale);

                var newBitmap = new Bitmap(newWidth, newHeight);
                using (var graphics = Graphics.FromImage(newBitmap))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                }

                return newBitmap;
            }
            catch (Exception ex)
            {
                _logService.Error("图像缩放失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 裁剪图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="rectangle">裁剪矩形</param>
        /// <returns>裁剪后图像</returns>
        public Bitmap CropImage(Bitmap image, Rectangle rectangle)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info($"开始图像裁剪，区域: {rectangle}");

                // 确保裁剪区域在图像范围内
                rectangle = Rectangle.Intersect(rectangle, new Rectangle(0, 0, image.Width, image.Height));

                if (rectangle.IsEmpty)
                    throw new ArgumentException("裁剪区域无效或超出图像范围", nameof(rectangle));

                return image.Clone(rectangle, image.PixelFormat);
            }
            catch (Exception ex)
            {
                _logService.Error("图像裁剪失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 旋转图像
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="angle">旋转角度</param>
        /// <returns>旋转后图像</returns>
        public Bitmap RotateImage(Bitmap image, float angle)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info($"开始图像旋转，角度: {angle}");

                // 创建旋转后的图像
                float radians = (float)(angle * Math.PI / 180.0);
                double sin = Math.Abs(Math.Sin(radians));
                double cos = Math.Abs(Math.Cos(radians));

                int newWidth = (int)(image.Width * cos + image.Height * sin);
                int newHeight = (int)(image.Width * sin + image.Height * cos);

                var rotatedBitmap = new Bitmap(newWidth, newHeight);
                using (var graphics = Graphics.FromImage(rotatedBitmap))
                {
                    graphics.TranslateTransform(rotatedBitmap.Width / 2.0f, rotatedBitmap.Height / 2.0f);
                    graphics.RotateTransform(angle);
                    graphics.TranslateTransform(-image.Width / 2.0f, -image.Height / 2.0f);
                    graphics.DrawImage(image, 0, 0);
                }

                return rotatedBitmap;
            }
            catch (Exception ex)
            {
                _logService.Error("图像旋转失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 检测图像中的K线形态
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>K线识别结果</returns>
        public RecognitionResult DetectCandlestickPatterns(Bitmap image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info("开始K线形态检测");

                // 这里应该实现K线检测逻辑
                // 目前返回一个模拟的结果
                var result = new RecognitionResult
                {
                    IsSuccess = true,
                    Confidence = 0.75,
                    ProcessedImage = (Bitmap)image.Clone(),
                    Message = "K线形态检测完成"
                };

                _logService.Info($"K线形态检测完成，置信度: {result.Confidence}");
                return result;
            }
            catch (Exception ex)
            {
                _logService.Error("K线形态检测失败", ex);
                return new RecognitionResult
                {
                    IsSuccess = false,
                    Confidence = 0.0,
                    ProcessedImage = image,
                    Message = $"K线检测失败: {ex.Message}"
                };
            }
        }
    }
}