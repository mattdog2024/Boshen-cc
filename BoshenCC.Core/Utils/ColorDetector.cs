using System;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// K线颜色检测器
    /// 支持红色、绿色K线的自动识别和分类
    /// </summary>
    public class ColorDetector
    {
        private readonly ILogService _logService;

        // 默认颜色阈值配置
        private readonly ColorThresholdConfig _defaultConfig;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        public ColorDetector(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _defaultConfig = CreateDefaultConfig();
        }

        /// <summary>
        /// 检测K线颜色
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="region">K线区域</param>
        /// <param name="config">颜色阈值配置</param>
        /// <returns>颜色检测结果</returns>
        public ColorDetectionResult DetectKLineColor(Bitmap image, Rectangle region, ColorThresholdConfig config = null)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                config = config ?? _defaultConfig;
                _logService.Info($"开始检测K线颜色，区域: {region}");

                // 裁剪K线区域
                var kLineImage = CropImage(image, region);

                // 执行颜色分析
                var result = AnalyzeColor(kLineImage, config);

                _logService.Info($"K线颜色检测完成，颜色类型: {result.ColorType}, 置信度: {result.Confidence:F2}");
                return result;
            }
            catch (Exception ex)
            {
                _logService.Error("K线颜色检测失败", ex);
                return new ColorDetectionResult
                {
                    ColorType = KLineColor.Unknown,
                    Confidence = 0.0,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 批量检测K线颜色
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="regions">K线区域列表</param>
        /// <param name="config">颜色阈值配置</param>
        /// <returns>颜色检测结果列表</returns>
        public ColorDetectionResult[] DetectKLineColors(Bitmap image, Rectangle[] regions, ColorThresholdConfig config = null)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            if (regions == null || regions.Length == 0)
                throw new ArgumentException("K线区域列表不能为空", nameof(regions));

            config = config ?? _defaultConfig;
            var results = new ColorDetectionResult[regions.Length];

            _logService.Info($"开始批量检测K线颜色，数量: {regions.Length}");

            for (int i = 0; i < regions.Length; i++)
            {
                results[i] = DetectKLineColor(image, regions[i], config);
            }

            _logService.Info($"批量K线颜色检测完成");
            return results;
        }

        /// <summary>
        /// 分析图像颜色
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="config">颜色阈值配置</param>
        /// <returns>颜色分析结果</returns>
        private ColorDetectionResult AnalyzeColor(Bitmap image, ColorThresholdConfig config)
        {
            try
            {
                using (var cvImage = new Image<Bgr, byte>(image))
                {
                    // 转换为HSV颜色空间进行分析
                    using (var hsvImage = cvImage.Convert<Hsv, byte>())
                    {
                        // 计算主要颜色
                        var dominantColor = CalculateDominantColor(hsvImage, config);

                        // 计算颜色置信度
                        var confidence = CalculateColorConfidence(hsvImage, dominantColor, config);

                        // 确定颜色类型
                        var colorType = DetermineColorType(dominantColor, config);

                        return new ColorDetectionResult
                        {
                            ColorType = colorType,
                            Confidence = confidence,
                            DominantColor = dominantColor,
                            IsSuccessful = true
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error("颜色分析失败", ex);
                return new ColorDetectionResult
                {
                    ColorType = KLineColor.Unknown,
                    Confidence = 0.0,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 计算主要颜色
        /// </summary>
        /// <param name="hsvImage">HSV图像</param>
        /// <param name="config">颜色配置</param>
        /// <returns>主要颜色</returns>
        private Hsv CalculateDominantColor(Image<Hsv, byte> hsvImage, ColorThresholdConfig config)
        {
            // 计算图像的平均颜色
            var meanColor = hsvImage.GetAverage();

            // 对颜色进行聚类分析，找到主要颜色
            var clusteredColor = PerformColorClustering(hsvImage, config);

            return clusteredColor;
        }

        /// <summary>
        /// 执行颜色聚类
        /// </summary>
        /// <param name="hsvImage">HSV图像</param>
        /// <param name="config">颜色配置</param>
        /// <returns>聚类后的主要颜色</returns>
        private Hsv PerformColorClustering(Image<Hsv, byte> hsvImage, ColorThresholdConfig config)
        {
            // 简化的K-means聚类实现
            // 这里使用直方图分析来找到主要颜色

            // 计算色调直方图
            var hist = new Image<Gray, byte>(180, 256);
            CvInvoke.CalcHist(
                new Image<Gray, byte>[] { hsvImage[0] }, // H通道
                new int[] { 0 },
                null,
                hist,
                new int[] { 180 },
                new float[] { 0, 180 }
            );

            // 找到直方图中的峰值
            double minValue, maxValue;
            Point minLoc, maxLoc;
            CvInvoke.MinMaxLoc(hist, out minValue, out maxValue, out minLoc, out maxLoc);

            // 计算峰值周围的平均颜色
            var dominantHue = maxLoc.X;
            var region = new Rectangle(
                Math.Max(0, dominantHue - config.ColorTolerance),
                0,
                Math.Min(180 - dominantHue, config.ColorTolerance * 2),
                hsvImage.Height
            );

            using (var regionImage = hsvImage.Copy(region))
            {
                var avgColor = regionImage.GetAverage();
                return new Hsv(avgColor.V0, avgColor.V1, avgColor.V2);
            }
        }

        /// <summary>
        /// 计算颜色置信度
        /// </summary>
        /// <param name="hsvImage">HSV图像</param>
        /// <param name="dominantColor">主要颜色</param>
        /// <param name="config">颜色配置</param>
        /// <returns>置信度 (0-1)</returns>
        private double CalculateColorConfidence(Image<Hsv, byte> hsvImage, Hsv dominantColor, ColorThresholdConfig config)
        {
            try
            {
                // 计算颜色分布的一致性
                double totalPixels = hsvImage.Width * hsvImage.Height;
                double matchingPixels = 0;

                for (int y = 0; y < hsvImage.Height; y++)
                {
                    for (int x = 0; x < hsvImage.Width; x++)
                    {
                        var pixelColor = hsvImage[y, x];

                        // 检查颜色是否在主要颜色的容差范围内
                        if (IsColorWithinTolerance(pixelColor, dominantColor, config))
                        {
                            matchingPixels++;
                        }
                    }
                }

                // 计算置信度
                double confidence = matchingPixels / totalPixels;

                // 对置信度进行平滑处理
                confidence = Math.Max(0.0, Math.Min(1.0, confidence));

                return confidence;
            }
            catch (Exception ex)
            {
                _logService.Error("计算颜色置信度失败", ex);
                return 0.0;
            }
        }

        /// <summary>
        /// 检查颜色是否在容差范围内
        /// </summary>
        /// <param name="color1">颜色1</param>
        /// <param name="color2">颜色2</param>
        /// <param name="config">颜色配置</param>
        /// <returns>是否在容差范围内</returns>
        private bool IsColorWithinTolerance(Hsv color1, Hsv color2, ColorThresholdConfig config)
        {
            // 计算色调差异
            double hueDiff = Math.Abs(color1.Hue - color2.Hue);
            if (hueDiff > 90) // 考虑色调的周期性
                hueDiff = 180 - hueDiff;

            // 计算饱和度和明度差异
            double satDiff = Math.Abs(color1.Satuation - color2.Satuation);
            double valDiff = Math.Abs(color1.Value - color2.Value);

            return hueDiff <= config.ColorTolerance &&
                   satDiff <= config.SaturationTolerance &&
                   valDiff <= config.BrightnessTolerance;
        }

        /// <summary>
        /// 确定颜色类型
        /// </summary>
        /// <param name="dominantColor">主要颜色</param>
        /// <param name="config">颜色配置</param>
        /// <returns>K线颜色类型</returns>
        private KLineColor DetermineColorType(Hsv dominantColor, ColorThresholdConfig config)
        {
            try
            {
                var hue = dominantColor.Hue;
                var saturation = dominantColor.Satuation;
                var brightness = dominantColor.Value;

                // 检查是否为灰色/白色（可能是十字星或无颜色信息）
                if (saturation < config.GrayThreshold)
                {
                    return KLineColor.Neutral;
                }

                // 检查亮度是否过低
                if (brightness < config.DarknessThreshold)
                {
                    return KLineColor.Unknown;
                }

                // 根据色调判断颜色
                if (hue >= config.RedHueMin && hue <= config.RedHueMax)
                {
                    return KLineColor.Red;
                }
                else if (hue >= config.GreenHueMin && hue <= config.GreenHueMax)
                {
                    return KLineColor.Green;
                }
                else
                {
                    // 颜色不明确
                    return KLineColor.Unknown;
                }
            }
            catch (Exception ex)
            {
                _logService.Error("确定颜色类型失败", ex);
                return KLineColor.Unknown;
            }
        }

        /// <summary>
        /// 裁剪图像
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <param name="region">裁剪区域</param>
        /// <returns>裁剪后的图像</returns>
        private Bitmap CropImage(Bitmap image, Rectangle region)
        {
            // 确保区域在图像范围内
            region = Rectangle.Intersect(region, new Rectangle(0, 0, image.Width, image.Height));

            if (region.IsEmpty)
                throw new ArgumentException("裁剪区域无效或超出图像范围", nameof(region));

            return image.Clone(region, image.PixelFormat);
        }

        /// <summary>
        /// 创建默认颜色阈值配置
        /// </summary>
        /// <returns>默认配置</returns>
        private ColorThresholdConfig CreateDefaultConfig()
        {
            return new ColorThresholdConfig
            {
                // 红色色调范围 (0-180)
                RedHueMin = 0,
                RedHueMax = 10,

                // 绿色色调范围 (50-90)
                GreenHueMin = 50,
                GreenHueMax = 90,

                // 容差设置
                ColorTolerance = 15,
                SaturationTolerance = 50,
                BrightnessTolerance = 60,

                // 阈值设置
                GrayThreshold = 30,      // 饱和度低于此值认为是灰色
                DarknessThreshold = 40   // 亮度低于此值认为太暗
            };
        }

        /// <summary>
        /// 自动调整颜色阈值
        /// </summary>
        /// <param name="image">参考图像</param>
        /// <returns>调整后的配置</returns>
        public ColorThresholdConfig AutoAdjustThresholds(Bitmap image)
        {
            try
            {
                _logService.Info("开始自动调整颜色阈值");

                using (var cvImage = new Image<Bgr, byte>(image))
                using (var hsvImage = cvImage.Convert<Hsv, byte>())
                {
                    var config = CreateDefaultConfig();

                    // 分析图像的整体颜色分布
                    var meanColor = hsvImage.GetAverage();
                    var stdDev = CalculateColorStandardDeviation(hsvImage);

                    // 根据图像特性调整阈值
                    config.ColorTolerance = (int)(config.ColorTolerance * (1 + stdDev.Hue / 30.0));
                    config.SaturationTolerance = (int)(config.SaturationTolerance * (1 + stdDev.Satuation / 50.0));
                    config.BrightnessTolerance = (int)(config.BrightnessTolerance * (1 + stdDev.Value / 60.0));

                    // 确保阈值在合理范围内
                    config.ColorTolerance = Math.Max(5, Math.Min(30, config.ColorTolerance));
                    config.SaturationTolerance = Math.Max(20, Math.Min(80, config.SaturationTolerance));
                    config.BrightnessTolerance = Math.Max(30, Math.Min(100, config.BrightnessTolerance));

                    _logService.Info($"自动调整完成，色调容差: {config.ColorTolerance}, 饱和度容差: {config.SaturationTolerance}");

                    return config;
                }
            }
            catch (Exception ex)
            {
                _logService.Error("自动调整颜色阈值失败", ex);
                return CreateDefaultConfig();
            }
        }

        /// <summary>
        /// 计算颜色标准差
        /// </summary>
        /// <param name="hsvImage">HSV图像</param>
        /// <returns>颜色标准差</returns>
        private Hsv CalculateColorStandardDeviation(Image<Hsv, byte> hsvImage)
        {
            var mean = new MCvScalar();
            var stdDev = new MCvScalar();

            CvInvoke.AvgSdv(hsvImage, ref mean, ref stdDev);

            return new Hsv(stdDev.V0, stdDev.V1, stdDev.V2);
        }
    }

    /// <summary>
    /// K线颜色类型
    /// </summary>
    public enum KLineColor
    {
        /// <summary>
        /// 未知
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 红色（下跌）
        /// </summary>
        Red = 1,

        /// <summary>
        /// 绿色（上涨）
        /// </summary>
        Green = 2,

        /// <summary>
        /// 中性（十字星等）
        /// </summary>
        Neutral = 3
    }

    /// <summary>
    /// 颜色检测结果
    /// </summary>
    public class ColorDetectionResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// K线颜色类型
        /// </summary>
        public KLineColor ColorType { get; set; }

        /// <summary>
        /// 置信度 (0-1)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 主要颜色
        /// </summary>
        public Hsv DominantColor { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 检测时间
        /// </summary>
        public DateTime DetectionTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 颜色阈值配置
    /// </summary>
    public class ColorThresholdConfig
    {
        /// <summary>
        /// 红色色调最小值
        /// </summary>
        public int RedHueMin { get; set; }

        /// <summary>
        /// 红色色调最大值
        /// </summary>
        public int RedHueMax { get; set; }

        /// <summary>
        /// 绿色色调最小值
        /// </summary>
        public int GreenHueMin { get; set; }

        /// <summary>
        /// 绿色色调最大值
        /// </summary>
        public int GreenHueMax { get; set; }

        /// <summary>
        /// 颜色容差
        /// </summary>
        public int ColorTolerance { get; set; }

        /// <summary>
        /// 饱和度容差
        /// </summary>
        public int SaturationTolerance { get; set; }

        /// <summary>
        /// 亮度容差
        /// </summary>
        public int BrightnessTolerance { get; set; }

        /// <summary>
        /// 灰色阈值（饱和度低于此值认为是灰色）
        /// </summary>
        public int GrayThreshold { get; set; }

        /// <summary>
        /// 暗度阈值（亮度低于此值认为太暗）
        /// </summary>
        public int DarknessThreshold { get; set; }
    }
}