using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using BoshenCC.Core.Utils;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Core.Services
{
    /// <summary>
    /// K线识别服务
    /// 整合颜色检测和边界检测，提供完整的K线识别功能
    /// </summary>
    public class KLineRecognitionService
    {
        private readonly ILogService _logService;
        private readonly ColorDetector _colorDetector;
        private readonly BoundaryDetector _boundaryDetector;

        // 识别配置
        private readonly KLineRecognitionConfig _defaultConfig;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="colorDetector">颜色检测器</param>
        /// <param name="boundaryDetector">边界检测器</param>
        public KLineRecognitionService(ILogService logService, ColorDetector colorDetector, BoundaryDetector boundaryDetector)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _colorDetector = colorDetector ?? throw new ArgumentNullException(nameof(colorDetector));
            _boundaryDetector = boundaryDetector ?? throw new ArgumentNullException(nameof(boundaryDetector));
            _defaultConfig = CreateDefaultConfig();
        }

        /// <summary>
        /// 识别单个K线
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="region">K线区域</param>
        /// <param name="config">识别配置</param>
        /// <returns>K线识别结果</returns>
        public KLineRecognitionResult RecognizeKLine(Bitmap image, Rectangle region, KLineRecognitionConfig config = null)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                config = config ?? _defaultConfig;
                _logService.Info($"开始识别K线，区域: {region}");

                var startTime = DateTime.Now;

                // 1. 颜色检测
                var colorResult = _colorDetector.DetectKLineColor(image, region, config.ColorConfig);
                if (!colorResult.IsSuccessful)
                {
                    return CreateFailureResult($"颜色检测失败: {colorResult.ErrorMessage}", startTime);
                }

                // 2. 边界检测
                var boundaryResult = _boundaryDetector.DetectKLineBoundary(image, region, config.BoundaryConfig);
                if (!boundaryResult.IsSuccessful)
                {
                    return CreateFailureResult($"边界检测失败: {boundaryResult.ErrorMessage}", startTime);
                }

                // 3. 数据融合和验证
                var kLineInfo = CreateKLineInfo(colorResult, boundaryResult, config);
                var validation = ValidateKLineInfo(kLineInfo, config);

                // 4. 创建识别结果
                var result = new KLineRecognitionResult
                {
                    IsSuccessful = validation.IsValid,
                    KLineInfo = kLineInfo,
                    ColorResult = colorResult,
                    BoundaryResult = boundaryResult,
                    Validation = validation,
                    ProcessingTime = (DateTime.Now - startTime).TotalMilliseconds,
                    RecognitionTime = DateTime.Now
                };

                if (!validation.IsValid)
                {
                    result.ErrorMessage = $"K线验证失败: {string.Join(", ", validation.Errors)}";
                }

                _logService.Info($"K线识别完成，{(result.IsSuccessful ? "成功" : "失败")}，耗时: {result.ProcessingTime:F2}ms");
                return result;
            }
            catch (Exception ex)
            {
                _logService.Error("K线识别失败", ex);
                return CreateFailureResult($"识别过程异常: {ex.Message}", DateTime.Now);
            }
        }

        /// <summary>
        /// 异步识别单个K线
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="region">K线区域</param>
        /// <param name="config">识别配置</param>
        /// <returns>K线识别结果</returns>
        public async Task<KLineRecognitionResult> RecognizeKLineAsync(Bitmap image, Rectangle region, KLineRecognitionConfig config = null)
        {
            return await Task.Run(() => RecognizeKLine(image, region, config));
        }

        /// <summary>
        /// 批量识别K线
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="regions">K线区域列表</param>
        /// <param name="config">识别配置</param>
        /// <returns>K线识别结果列表</returns>
        public KLineRecognitionResult[] RecognizeKLines(Bitmap image, Rectangle[] regions, KLineRecognitionConfig config = null)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                if (regions == null || regions.Length == 0)
                    throw new ArgumentException("K线区域列表不能为空", nameof(regions));

                config = config ?? _defaultConfig;
                _logService.Info($"开始批量识别K线，数量: {regions.Length}");

                var startTime = DateTime.Now;
                var results = new KLineRecognitionResult[regions.Length];

                // 并行处理以提高性能
                if (config.EnableParallelProcessing && regions.Length > config.ParallelThreshold)
                {
                    results = ProcessKLinesParallel(image, regions, config);
                }
                else
                {
                    results = ProcessKLinesSequential(image, regions, config);
                }

                var processingTime = (DateTime.Now - startTime).TotalMilliseconds;
                var successCount = results.Count(r => r.IsSuccessful);

                _logService.Info($"批量K线识别完成，成功: {successCount}/{regions.Length}，总耗时: {processingTime:F2}ms");
                return results;
            }
            catch (Exception ex)
            {
                _logService.Error("批量K线识别失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 异步批量识别K线
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="regions">K线区域列表</param>
        /// <param name="config">识别配置</param>
        /// <returns>K线识别结果列表</returns>
        public async Task<KLineRecognitionResult[]> RecognizeKLinesAsync(Bitmap image, Rectangle[] regions, KLineRecognitionConfig config = null)
        {
            return await Task.Run(() => RecognizeKLines(image, regions, config));
        }

        /// <summary>
        /// 从图像中自动检测和识别K线
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="config">识别配置</param>
        /// <returns>K线识别结果列表</returns>
        public KLineRecognitionResult[] DetectAndRecognizeKLines(Bitmap image, KLineRecognitionConfig config = null)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                config = config ?? _defaultConfig;
                _logService.Info($"开始自动检测和识别K线");

                // 1. 自动检测K线区域
                var regions = DetectKLineRegions(image, config);
                if (regions.Length == 0)
                {
                    _logService.Warning("未检测到任何K线区域");
                    return new KLineRecognitionResult[0];
                }

                _logService.Info($"检测到 {regions.Length} 个K线区域");

                // 2. 识别检测到的K线
                var results = RecognizeKLines(image, regions, config);

                // 3. 后处理和结果优化
                if (config.EnablePostProcessing)
                {
                    results = PostProcessResults(results, config);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logService.Error("自动检测和识别K线失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 自动检测K线区域
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="config">识别配置</param>
        /// <returns>K线区域列表</returns>
        private Rectangle[] DetectKLineRegions(Bitmap image, KLineRecognitionConfig config)
        {
            try
            {
                using (var cvImage = new Image<Bgr, byte>(image))
                using (var grayImage = cvImage.Convert<Gray, byte>())
                {
                    // 边缘检测
                    using (var edgeImage = grayImage.Canny(50, 150))
                    {
                        // 轮廓检测
                        var contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
                        CvInvoke.FindContours(edgeImage, contours, null, Emgu.CV.CvEnum.RetrType.External,
                            Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

                        // 分析轮廓，提取可能的K线区域
                        var regions = new List<Rectangle>();

                        for (int i = 0; i < contours.Size; i++)
                        {
                            var contour = contours[i];
                            var rect = CvInvoke.BoundingRectangle(contour);

                            // 筛选符合K线特征的轮廓
                            if (IsValidKLineRegion(rect, config))
                            {
                                regions.Add(rect);
                            }
                        }

                        // 对区域进行排序和优化
                        regions = OptimizeRegions(regions, config);

                        return regions.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error("K线区域检测失败", ex);
                return new Rectangle[0];
            }
        }

        /// <summary>
        /// 检查是否为有效的K线区域
        /// </summary>
        /// <param name="rect">区域矩形</param>
        /// <param name="config">识别配置</param>
        /// <returns>是否有效</returns>
        private bool IsValidKLineRegion(Rectangle rect, KLineRecognitionConfig config)
        {
            // 1. 尺寸检查
            if (rect.Width < config.MinKLineWidth || rect.Width > config.MaxKLineWidth)
                return false;

            if (rect.Height < config.MinKLineHeight || rect.Height > config.MaxKLineHeight)
                return false;

            // 2. 长宽比检查（K线通常是垂直的）
            var aspectRatio = (double)rect.Width / rect.Height;
            if (aspectRatio > config.MaxKLineAspectRatio)
                return false;

            // 3. 位置检查（排除边缘噪音）
            if (rect.Left < config.EdgeMargin || rect.Top < config.EdgeMargin ||
                rect.Right > config.ImageWidth - config.EdgeMargin ||
                rect.Bottom > config.ImageHeight - config.EdgeMargin)
                return false;

            return true;
        }

        /// <summary>
        /// 优化K线区域
        /// </summary>
        /// <param name="regions">区域列表</param>
        /// <param name="config">识别配置</param>
        /// <returns>优化后的区域列表</returns>
        private List<Rectangle> OptimizeRegions(List<Rectangle> regions, KLineRecognitionConfig config)
        {
            try
            {
                // 1. 按X坐标排序
                regions.Sort((a, b) => a.X.CompareTo(b.X));

                // 2. 去除重叠区域
                var optimizedRegions = new List<Rectangle>();
                foreach (var region in regions)
                {
                    var isOverlapping = false;
                    foreach (var existing in optimizedRegions)
                    {
                        if (IsOverlapping(region, existing, config.OverlapThreshold))
                        {
                            isOverlapping = true;
                            break;
                        }
                    }

                    if (!isOverlapping)
                    {
                        optimizedRegions.Add(region);
                    }
                }

                // 3. 限制数量
                if (optimizedRegions.Count > config.MaxKLines)
                {
                    optimizedRegions = optimizedRegions.Take(config.MaxKLines).ToList();
                }

                return optimizedRegions;
            }
            catch (Exception ex)
            {
                _logService.Error("区域优化失败", ex);
                return regions;
            }
        }

        /// <summary>
        /// 检查区域是否重叠
        /// </summary>
        /// <param name="rect1">矩形1</param>
        /// <param name="rect2">矩形2</param>
        /// <param name="threshold">重叠阈值</param>
        /// <returns>是否重叠</returns>
        private bool IsOverlapping(Rectangle rect1, Rectangle rect2, double threshold)
        {
            var intersection = Rectangle.Intersect(rect1, rect2);
            if (intersection.IsEmpty)
                return false;

            var intersectionArea = intersection.Width * intersection.Height;
            var area1 = rect1.Width * rect1.Height;
            var area2 = rect2.Width * rect2.Height;

            var overlapRatio = intersectionArea / Math.Min(area1, area2);
            return overlapRatio > threshold;
        }

        /// <summary>
        /// 并行处理K线
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="regions">K线区域列表</param>
        /// <param name="config">识别配置</param>
        /// <returns>K线识别结果列表</returns>
        private KLineRecognitionResult[] ProcessKLinesParallel(Bitmap image, Rectangle[] regions, KLineRecognitionConfig config)
        {
            var results = new KLineRecognitionResult[regions.Length];

            Parallel.For(0, regions.Length, new ParallelOptions
            {
                MaxDegreeOfParallelism = config.MaxDegreeOfParallelism
            }, i =>
            {
                results[i] = RecognizeKLine(image, regions[i], config);
            });

            return results;
        }

        /// <summary>
        /// 顺序处理K线
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="regions">K线区域列表</param>
        /// <param name="config">识别配置</param>
        /// <returns>K线识别结果列表</returns>
        private KLineRecognitionResult[] ProcessKLinesSequential(Bitmap image, Rectangle[] regions, KLineRecognitionConfig config)
        {
            var results = new KLineRecognitionResult[regions.Length];

            for (int i = 0; i < regions.Length; i++)
            {
                results[i] = RecognizeKLine(image, regions[i], config);
            }

            return results;
        }

        /// <summary>
        /// 创建K线信息
        /// </summary>
        /// <param name="colorResult">颜色检测结果</param>
        /// <param name="boundaryResult">边界检测结果</param>
        /// <param name="config">识别配置</param>
        /// <returns>K线信息</returns>
        private KLineInfo CreateKLineInfo(ColorDetectionResult colorResult, BoundaryDetectionResult boundaryResult, KLineRecognitionConfig config)
        {
            var kLineInfo = new KLineInfo
            {
                // 基本信息
                Bounds = boundaryResult.FullBoundary,
                Color = colorResult.ColorType,
                ColorConfidence = colorResult.Confidence,
                BoundaryConfidence = boundaryResult.Confidence,

                // 结构信息
                Structure = boundaryResult.Structure,
                BodyBoundary = boundaryResult.BodyBoundary,
                UpperShadowBoundary = boundaryResult.UpperShadowBoundary,
                LowerShadowBoundary = boundaryResult.LowerShadowBoundary,

                // 价格信息（占位，需要根据实际价格轴计算）
                HighPrice = 0,
                LowPrice = 0,
                OpenPrice = 0,
                ClosePrice = 0,

                // 其他信息
                Confidence = CalculateOverallConfidence(colorResult.Confidence, boundaryResult.Confidence),
                RecognitionTime = DateTime.Now
            };

            return kLineInfo;
        }

        /// <summary>
        /// 计算总体置信度
        /// </summary>
        /// <param name="colorConfidence">颜色置信度</param>
        /// <param name="boundaryConfidence">边界置信度</param>
        /// <returns>总体置信度</returns>
        private double CalculateOverallConfidence(double colorConfidence, double boundaryConfidence)
        {
            // 加权平均
            return (colorConfidence * 0.4 + boundaryConfidence * 0.6);
        }

        /// <summary>
        /// 验证K线信息
        /// </summary>
        /// <param name="kLineInfo">K线信息</param>
        /// <param name="config">识别配置</param>
        /// <returns>验证结果</returns>
        private ValidationResult ValidateKLineInfo(KLineInfo kLineInfo, KLineRecognitionConfig config)
        {
            var errors = new List<string>();

            try
            {
                // 1. 基本信息验证
                if (kLineInfo.Bounds.IsEmpty)
                    errors.Add("K线边界为空");

                if (kLineInfo.Color == KLineColor.Unknown)
                    errors.Add("K线颜色未知");

                // 2. 置信度验证
                if (kLineInfo.Confidence < config.MinConfidenceThreshold)
                    errors.Add($"置信度过低: {kLineInfo.Confidence:F2}");

                // 3. 结构验证
                if (kLineInfo.Structure == null)
                    errors.Add("K线结构信息缺失");
                else
                {
                    if (kLineInfo.Structure.TotalHeight <= 0)
                        errors.Add("K线总高度无效");

                    if (kLineInfo.Structure.BodyHeight < 0 ||
                        kLineInfo.Structure.UpperShadowHeight < 0 ||
                        kLineInfo.Structure.LowerShadowHeight < 0)
                        errors.Add("K线结构尺寸无效");

                    // 检查结构一致性
                    var calculatedHeight = kLineInfo.Structure.BodyHeight +
                                         kLineInfo.Structure.UpperShadowHeight +
                                         kLineInfo.Structure.LowerShadowHeight;
                    if (Math.Abs(calculatedHeight - kLineInfo.Structure.TotalHeight) > config.StructureTolerance)
                        errors.Add("K线结构尺寸不一致");
                }

                // 4. 边界一致性验证
                if (!ValidateBoundaryConsistency(kLineInfo, config))
                    errors.Add("K线边界不一致");

                return new ValidationResult
                {
                    IsValid = errors.Count == 0,
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                _logService.Error("K线信息验证失败", ex);
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { $"验证过程异常: {ex.Message}" }
                };
            }
        }

        /// <summary>
        /// 验证边界一致性
        /// </summary>
        /// <param name="kLineInfo">K线信息</param>
        /// <param name="config">识别配置</param>
        /// <returns>是否一致</returns>
        private bool ValidateBoundaryConsistency(KLineInfo kLineInfo, KLineRecognitionConfig config)
        {
            try
            {
                var fullBounds = kLineInfo.Bounds;
                var bodyBounds = kLineInfo.BodyBoundary;
                var upperShadowBounds = kLineInfo.UpperShadowBoundary;
                var lowerShadowBounds = kLineInfo.LowerShadowBoundary;

                // 1. 检查子边界是否在主边界内
                if (!bodyBounds.IsEmpty && !fullBounds.Contains(bodyBounds))
                    return false;

                if (!upperShadowBounds.IsEmpty && !fullBounds.Contains(upperShadowBounds))
                    return false;

                if (!lowerShadowBounds.IsEmpty && !fullBounds.Contains(lowerShadowBounds))
                    return false;

                // 2. 检查边界间的逻辑关系
                if (!bodyBounds.IsEmpty)
                {
                    // 上影线应该在实体上方
                    if (!upperShadowBounds.IsEmpty && upperShadowBounds.Bottom > bodyBounds.Top)
                        return false;

                    // 下影线应该在实体下方
                    if (!lowerShadowBounds.IsEmpty && lowerShadowBounds.Top < bodyBounds.Bottom)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logService.Error("边界一致性验证失败", ex);
                return false;
            }
        }

        /// <summary>
        /// 后处理识别结果
        /// </summary>
        /// <param name="results">识别结果列表</param>
        /// <param name="config">识别配置</param>
        /// <returns>处理后的结果列表</returns>
        private KLineRecognitionResult[] PostProcessResults(KLineRecognitionResult[] results, KLineRecognitionConfig config)
        {
            try
            {
                // 1. 过滤低置信度结果
                if (config.EnableConfidenceFiltering)
                {
                    results = results.Where(r => r.IsSuccessful && r.KLineInfo.Confidence >= config.MinConfidenceThreshold).ToArray();
                }

                // 2. 结果排序
                if (config.EnableResultSorting)
                {
                    results = results.OrderBy(r => r.KLineInfo.Bounds.X).ToArray();
                }

                // 3. 结果校正（基于相邻K线的逻辑关系）
                if (config.EnableResultCorrection)
                {
                    results = CorrectResults(results, config);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logService.Error("结果后处理失败", ex);
                return results;
            }
        }

        /// <summary>
        /// 校正识别结果
        /// </summary>
        /// <param name="results">识别结果列表</param>
        /// <param name="config">识别配置</param>
        /// <returns>校正后的结果列表</returns>
        private KLineRecognitionResult[] CorrectResults(KLineRecognitionResult[] results, KLineRecognitionConfig config)
        {
            // 这里可以实现基于相邻K线逻辑关系的校正算法
            // 例如：价格连续性检查、颜色序列合理性检查等
            return results;
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        /// <param name="startTime">开始时间</param>
        /// <returns>失败结果</returns>
        private KLineRecognitionResult CreateFailureResult(string errorMessage, DateTime startTime)
        {
            return new KLineRecognitionResult
            {
                IsSuccessful = false,
                ErrorMessage = errorMessage,
                ProcessingTime = (DateTime.Now - startTime).TotalMilliseconds,
                RecognitionTime = DateTime.Now
            };
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认配置</returns>
        private KLineRecognitionConfig CreateDefaultConfig()
        {
            return new KLineRecognitionConfig
            {
                // 颜色和边界配置（使用默认配置）
                ColorConfig = null,
                BoundaryConfig = null,

                // 性能配置
                EnableParallelProcessing = true,
                ParallelThreshold = 5,
                MaxDegreeOfParallelism = 4,

                // 区域检测配置
                MinKLineWidth = 5,
                MaxKLineWidth = 50,
                MinKLineHeight = 20,
                MaxKLineHeight = 500,
                MaxKLineAspectRatio = 1.0,
                EdgeMargin = 10,
                MaxKLines = 100,
                OverlapThreshold = 0.3,
                ImageWidth = 1920,
                ImageHeight = 1080,

                // 验证配置
                MinConfidenceThreshold = 0.6,
                StructureTolerance = 5,

                // 后处理配置
                EnablePostProcessing = true,
                EnableConfidenceFiltering = true,
                EnableResultSorting = true,
                EnableResultCorrection = false
            };
        }
    }

    /// <summary>
    /// K线识别结果
    /// </summary>
    public class KLineRecognitionResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// K线信息
        /// </summary>
        public KLineInfo KLineInfo { get; set; }

        /// <summary>
        /// 颜色检测结果
        /// </summary>
        public ColorDetectionResult ColorResult { get; set; }

        /// <summary>
        /// 边界检测结果
        /// </summary>
        public BoundaryDetectionResult BoundaryResult { get; set; }

        /// <summary>
        /// 验证结果
        /// </summary>
        public ValidationResult Validation { get; set; }

        /// <summary>
        /// 处理时间（毫秒）
        /// </summary>
        public double ProcessingTime { get; set; }

        /// <summary>
        /// 识别时间
        /// </summary>
        public DateTime RecognitionTime { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// K线识别配置
    /// </summary>
    public class KLineRecognitionConfig
    {
        /// <summary>
        /// 颜色检测配置
        /// </summary>
        public ColorThresholdConfig ColorConfig { get; set; }

        /// <summary>
        /// 边界检测配置
        /// </summary>
        public BoundaryDetectionConfig BoundaryConfig { get; set; }

        // 性能配置
        public bool EnableParallelProcessing { get; set; }
        public int ParallelThreshold { get; set; }
        public int MaxDegreeOfParallelism { get; set; }

        // 区域检测配置
        public int MinKLineWidth { get; set; }
        public int MaxKLineWidth { get; set; }
        public int MinKLineHeight { get; set; }
        public int MaxKLineHeight { get; set; }
        public double MaxKLineAspectRatio { get; set; }
        public int EdgeMargin { get; set; }
        public int MaxKLines { get; set; }
        public double OverlapThreshold { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }

        // 验证配置
        public double MinConfidenceThreshold { get; set; }
        public int StructureTolerance { get; set; }

        // 后处理配置
        public bool EnablePostProcessing { get; set; }
        public bool EnableConfidenceFiltering { get; set; }
        public bool EnableResultSorting { get; set; }
        public bool EnableResultCorrection { get; set; }
    }
}