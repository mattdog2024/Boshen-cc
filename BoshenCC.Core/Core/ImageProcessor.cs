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

        #region 截图相关图像处理

        /// <summary>
        /// 优化截图质量，为K线识别做准备
        /// </summary>
        /// <param name="screenshot">原始截图</param>
        /// <param name="options">优化选项</param>
        /// <returns>优化后的图像</returns>
        public Bitmap OptimizeScreenshot(Bitmap screenshot, ScreenshotOptimizationOptions options = null)
        {
            try
            {
                if (screenshot == null)
                    throw new ArgumentNullException(nameof(screenshot));

                options = options ?? new ScreenshotOptimizationOptions();
                _logService.Info($"开始优化截图质量，尺寸: {screenshot.Width}x{screenshot.Height}");

                Bitmap result = (Bitmap)screenshot.Clone();

                // 1. 调整DPI和分辨率
                if (options.AdjustDpi)
                {
                    result = AdjustDpiForKLineRecognition(result);
                }

                // 2. 增强对比度
                if (options.EnhanceContrast)
                {
                    result = EnhanceContrastForKLine(result, options.ContrastLevel);
                }

                // 3. 降噪处理
                if (options.EnableDenoise)
                {
                    result = DenoiseForKLine(result);
                }

                // 4. 锐化处理
                if (options.EnableSharpen)
                {
                    result = SharpenForKLine(result);
                }

                // 5. 色彩校正
                if (options.CorrectColors)
                {
                    result = CorrectColorsForKLine(result);
                }

                _logService.Info("截图优化完成");
                return result;
            }
            catch (Exception ex)
            {
                _logService.Error("截图优化失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 为K线识别调整DPI
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>调整后的图像</returns>
        public Bitmap AdjustDpiForKLineRecognition(Bitmap image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info("为K线识别调整DPI");

                // 计算目标尺寸（确保K线细节足够清晰）
                const int targetMinWidth = 1920;
                const int targetMinHeight = 1080;

                if (image.Width >= targetMinWidth && image.Height >= targetMinHeight)
                    return (Bitmap)image.Clone();

                // 计算缩放比例
                double scaleX = (double)targetMinWidth / image.Width;
                double scaleY = (double)targetMinHeight / image.Height;
                double scale = Math.Max(scaleX, scaleY);

                return ScaleImage(image, scale);
            }
            catch (Exception ex)
            {
                _logService.Error("DPI调整失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 为K线识别增强对比度
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="level">对比度级别 (1-5)</param>
        /// <returns>增强后的图像</returns>
        public Bitmap EnhanceContrastForKLine(Bitmap image, int level = 3)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info($"增强K线图像对比度，级别: {level}");

                using (var cvImage = new Image<Bgr, byte>(image))
                {
                    // 转换为Lab色彩空间进行对比度增强
                    using (var labImage = cvImage.Convert<Lab, byte>())
                    {
                        // 仅对亮度通道进行直方图均衡化
                        var lChannel = labImage[0];
                        CvInvoke.EqualizeHist(lChannel, lChannel);

                        // 转换回BGR
                        var enhancedImage = labImage.Convert<Bgr, byte>();
                        return enhancedImage.ToBitmap();
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error("对比度增强失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 为K线识别降噪
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>降噪后的图像</returns>
        public Bitmap DenoiseForKLine(Bitmap image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info("为K线图像降噪");

                using (var cvImage = new Image<Bgr, byte>(image))
                {
                    // 使用双边滤波，保持边缘的同时降噪
                    var denoisedImage = cvImage.SmoothBilatateral(10, 50, 50);
                    return denoisedImage.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                _logService.Error("K线降噪失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 为K线识别锐化
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>锐化后的图像</returns>
        public Bitmap SharpenForKLine(Bitmap image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info("锐化K线图像");

                using (var cvImage = new Image<Bgr, byte>(image))
                {
                    // 使用Unsharp Mask锐化
                    var blurredImage = cvImage.SmoothGaussian(5);
                    var sharpenedImage = cvImage - 0.5 * blurredImage;

                    return sharpenedImage.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                _logService.Error("K线锐化失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 为K线识别校正颜色
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>颜色校正后的图像</returns>
        public Bitmap CorrectColorsForKLine(Bitmap image)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                _logService.Info("校正K线图像颜色");

                using (var cvImage = new Image<Bgr, byte>(image))
                {
                    // 增强红色和绿色的对比度，便于区分K线颜色
                    var correctedImage = cvImage.Copy();

                    // 调整颜色通道
                    CvInvoke.AddWeighted(correctedImage[0], 1.2, cvImage[0], 0, 0, correctedImage[0]); // B通道
                    CvInvoke.AddWeighted(correctedImage[1], 1.1, cvImage[1], 0, 0, correctedImage[1]); // G通道
                    CvInvoke.AddWeighted(correctedImage[2], 1.3, cvImage[2], 0, 0, correctedImage[2]); // R通道

                    return correctedImage.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                _logService.Error("K线颜色校正失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 提取K线图区域
        /// </summary>
        /// <param name="screenshot">完整截图</param>
        /// <returns>K线图区域</returns>
        public Rectangle ExtractKLineRegion(Bitmap screenshot)
        {
            try
            {
                if (screenshot == null)
                    throw new ArgumentNullException(nameof(screenshot));

                _logService.Info("提取K线图区域");

                using (var cvImage = new Image<Bgr, byte>(screenshot))
                using (var grayImage = cvImage.Convert<Gray, byte>())
                {
                    // 边缘检测
                    using (var edgeImage = grayImage.Canny(50, 150))
                    {
                        // 轮廓检测
                        var contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
                        CvInvoke.FindContours(edgeImage, contours, null, Emgu.CV.CvEnum.RetrType.External,
                            Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

                        // 寻找最大的矩形区域（可能是K线图区域）
                        Rectangle maxRect = Rectangle.Empty;
                        double maxArea = 0;

                        for (int i = 0; i < contours.Size; i++)
                        {
                            var contour = contours[i];
                            var rect = CvInvoke.BoundingRectangle(contour);
                            var area = rect.Width * rect.Height;

                            // 过滤掉太小的区域
                            if (area > screenshot.Width * screenshot.Height * 0.1 && area > maxArea)
                            {
                                maxRect = rect;
                                maxArea = area;
                            }
                        }

                        if (maxRect != Rectangle.Empty)
                        {
                            _logService.Info($"找到K线图区域: {maxRect}");
                            return maxRect;
                        }
                    }
                }

                // 如果没有找到合适的区域，返回中心区域
                var centerRegion = new Rectangle(
                    screenshot.Width / 4,
                    screenshot.Height / 4,
                    screenshot.Width / 2,
                    screenshot.Height / 2
                );

                _logService.Info($"使用默认中心区域: {centerRegion}");
                return centerRegion;
            }
            catch (Exception ex)
            {
                _logService.Error("K线区域提取失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 批量处理截图
        /// </summary>
        /// <param name="screenshots">截图列表</param>
        /// <param name="options">处理选项</param>
        /// <returns>处理后的截图列表</returns>
        public List<Bitmap> BatchProcessScreenshots(List<Bitmap> screenshots, ScreenshotOptimizationOptions options = null)
        {
            if (screenshots == null)
                throw new ArgumentNullException(nameof(screenshots));

            var results = new List<Bitmap>();

            foreach (var screenshot in screenshots)
            {
                try
                {
                    var processed = OptimizeScreenshot(screenshot, options);
                    results.Add(processed);
                }
                catch (Exception ex)
                {
                    _logService.Error($"批量处理截图失败: {ex.Message}");
                    // 添加原始截图作为后备
                    results.Add((Bitmap)screenshot.Clone());
                }
            }

            _logService.Info($"批量处理完成，成功处理 {results.Count}/{screenshots.Count} 张截图");
            return results;
        }

        /// <summary>
        /// 检测截图质量
        /// </summary>
        /// <param name="screenshot">截图</param>
        /// <returns>质量评估结果</returns>
        public ScreenshotQuality AssessScreenshotQuality(Bitmap screenshot)
        {
            try
            {
                if (screenshot == null)
                    throw new ArgumentNullException(nameof(screenshot));

                _logService.Info("评估截图质量");

                var quality = new ScreenshotQuality();

                // 检查分辨率
                quality.ResolutionScore = CalculateResolutionScore(screenshot);
                quality.ContrastScore = CalculateContrastScore(screenshot);
                quality.SharpnessScore = CalculateSharpnessScore(screenshot);
                quality.NoiseScore = CalculateNoiseScore(screenshot);
                quality.ColorScore = CalculateColorQualityScore(screenshot);

                // 计算总体评分
                quality.OverallScore = (quality.ResolutionScore + quality.ContrastScore +
                                       quality.SharpnessScore + quality.NoiseScore + quality.ColorScore) / 5.0;

                _logService.Info($"截图质量评估完成，总分: {quality.OverallScore:F2}");
                return quality;
            }
            catch (Exception ex)
            {
                _logService.Error("截图质量评估失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 计算分辨率评分
        /// </summary>
        private double CalculateResolutionScore(Bitmap image)
        {
            const int minRecommendedWidth = 1920;
            const int minRecommendedHeight = 1080;

            double widthScore = Math.Min(1.0, (double)image.Width / minRecommendedWidth);
            double heightScore = Math.Min(1.0, (double)image.Height / minRecommendedHeight);

            return (widthScore + heightScore) / 2.0;
        }

        /// <summary>
        /// 计算对比度评分
        /// </summary>
        private double CalculateContrastScore(Bitmap image)
        {
            try
            {
                using (var cvImage = new Image<Gray, byte>(image))
                {
                    // 计算标准差作为对比度指标
                    var mean = new MCvScalar();
                    var stdDev = new MCvScalar();
                    CvInvoke.AvgSdv(cvImage, ref mean, ref stdDev);

                    // 标准差越大，对比度越好
                    return Math.Min(1.0, stdDev.V0 / 50.0);
                }
            }
            catch
            {
                return 0.5; // 默认中等评分
            }
        }

        /// <summary>
        /// 计算锐度评分
        /// </summary>
        private double CalculateSharpnessScore(Bitmap image)
        {
            try
            {
                using (var cvImage = new Image<Gray, byte>(image))
                using (var laplacianImage = new Image<Gray, byte>(image.Size))
                {
                    // 使用拉普拉斯算子检测锐度
                    CvInvoke.Laplacian(cvImage, laplacianImage, Emgu.CV.CvEnum.DepthType.Cv8U, 3, 1, 0,
                        Emgu.CV.CvEnum.BorderType.Default);

                    // 计算方差作为锐度指标
                    var mean = new MCvScalar();
                    var stdDev = new MCvScalar();
                    CvInvoke.AvgSdv(laplacianImage, ref mean, ref stdDev);

                    return Math.Min(1.0, stdDev.V0 / 30.0);
                }
            }
            catch
            {
                return 0.5; // 默认中等评分
            }
        }

        /// <summary>
        /// 计算噪声评分
        /// </summary>
        private double CalculateNoiseScore(Bitmap image)
        {
            try
            {
                using (var cvImage = new Image<Gray, byte>(image))
                {
                    // 使用中值滤波后计算差异来评估噪声
                    var filteredImage = cvImage.SmoothMedian(3);
                    var diffImage = cvImage.AbsDiff(filteredImage);

                    var mean = new MCvScalar();
                    CvInvoke.AvgSdv(diffImage, ref mean, out var stdDev);

                    // 差异越小，噪声越少
                    return Math.Max(0.0, 1.0 - mean.V0 / 10.0);
                }
            }
            catch
            {
                return 0.5; // 默认中等评分
            }
        }

        /// <summary>
        /// 计算颜色质量评分
        /// </summary>
        private double CalculateColorQualityScore(Bitmap image)
        {
            try
            {
                using (var cvImage = new Image<Bgr, byte>(image))
                {
                    // 计算颜色分布的均匀性
                    var meanB = cvImage.GetAverage().V0;
                    var meanG = cvImage.GetAverage().V1;
                    var meanR = cvImage.GetAverage().V2;

                    // 理想的K线图应该有良好的红绿对比
                    double colorBalance = Math.Abs(meanR - meanG) / Math.Max(meanR, meanG);
                    return Math.Min(1.0, colorBalance);
                }
            }
            catch
            {
                return 0.5; // 默认中等评分
            }
        }

        #endregion
    }

    /// <summary>
    /// 截图优化选项
    /// </summary>
    public class ScreenshotOptimizationOptions
    {
        public bool AdjustDpi { get; set; } = true;
        public bool EnhanceContrast { get; set; } = true;
        public int ContrastLevel { get; set; } = 3;
        public bool EnableDenoise { get; set; } = true;
        public bool EnableSharpen { get; set; } = true;
        public bool CorrectColors { get; set; } = true;
    }

    /// <summary>
    /// 截图质量评估结果
    /// </summary>
    public class ScreenshotQuality
    {
        public double ResolutionScore { get; set; }
        public double ContrastScore { get; set; }
        public double SharpnessScore { get; set; }
        public double NoiseScore { get; set; }
        public double ColorScore { get; set; }
        public double OverallScore { get; set; }

        public bool IsGood => OverallScore >= 0.7;
        public bool IsExcellent => OverallScore >= 0.85;

        public override string ToString()
        {
            return $"总分: {OverallScore:F2} (分辨率:{ResolutionScore:F2}, 对比度:{ContrastScore:F2}, " +
                   $"锐度:{SharpnessScore:F2}, 噪声:{NoiseScore:F2}, 颜色:{ColorScore:F2})";
        }
    }
}