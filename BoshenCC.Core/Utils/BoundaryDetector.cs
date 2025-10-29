using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// K线边界检测器
    /// 实现K线的精确边界提取和结构分析
    /// </summary>
    public class BoundaryDetector
    {
        private readonly ILogService _logService;

        // 默认边界检测配置
        private readonly BoundaryDetectionConfig _defaultConfig;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        public BoundaryDetector(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _defaultConfig = CreateDefaultConfig();
        }

        /// <summary>
        /// 检测K线边界
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="region">K线区域</param>
        /// <param name="config">检测配置</param>
        /// <returns>边界检测结果</returns>
        public BoundaryDetectionResult DetectKLineBoundary(Bitmap image, Rectangle region, BoundaryDetectionConfig config = null)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));

                config = config ?? _defaultConfig;
                _logService.Info($"开始检测K线边界，区域: {region}");

                // 裁剪K线区域
                var kLineImage = CropImage(image, region);

                // 执行边界检测
                var result = AnalyzeBoundary(kLineImage, config);

                // 调整坐标到原图像坐标系
                AdjustCoordinatesToOriginal(result, region);

                _logService.Info($"K线边界检测完成，边界: {result.FullBoundary}, 实体: {result.BodyBoundary}");
                return result;
            }
            catch (Exception ex)
            {
                _logService.Error("K线边界检测失败", ex);
                return new BoundaryDetectionResult
                {
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 批量检测K线边界
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="regions">K线区域列表</param>
        /// <param name="config">检测配置</param>
        /// <returns>边界检测结果列表</returns>
        public BoundaryDetectionResult[] DetectKLineBoundaries(Bitmap image, Rectangle[] regions, BoundaryDetectionConfig config = null)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            if (regions == null || regions.Length == 0)
                throw new ArgumentException("K线区域列表不能为空", nameof(regions));

            config = config ?? _defaultConfig;
            var results = new BoundaryDetectionResult[regions.Length];

            _logService.Info($"开始批量检测K线边界，数量: {regions.Length}");

            for (int i = 0; i < regions.Length; i++)
            {
                results[i] = DetectKLineBoundary(image, regions[i], config);
            }

            _logService.Info($"批量K线边界检测完成");
            return results;
        }

        /// <summary>
        /// 分析边界
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="config">检测配置</param>
        /// <returns>边界分析结果</returns>
        private BoundaryDetectionResult AnalyzeBoundary(Bitmap image, BoundaryDetectionConfig config)
        {
            try
            {
                using (var cvImage = new Image<Bgr, byte>(image))
                using (var grayImage = cvImage.Convert<Gray, byte>())
                {
                    // 1. 图像预处理
                    var preprocessedImage = PreprocessImage(grayImage, config);

                    // 2. 边缘检测
                    var edgeImage = DetectEdges(preprocessedImage, config);

                    // 3. 轮廓检测
                    var contours = DetectContours(edgeImage, config);

                    // 4. 轮廓分析和筛选
                    var validContours = FilterContours(contours, config);

                    // 5. 边界提取
                    var boundaries = ExtractBoundaries(validContours, config);

                    // 6. 结构分析
                    var structure = AnalyzeKLineStructure(boundaries, config);

                    return new BoundaryDetectionResult
                    {
                        IsSuccessful = true,
                        FullBoundary = boundaries.FullBoundary,
                        BodyBoundary = boundaries.BodyBoundary,
                        UpperShadowBoundary = boundaries.UpperShadowBoundary,
                        LowerShadowBoundary = boundaries.LowerShadowBoundary,
                        Structure = structure,
                        Confidence = CalculateConfidence(boundaries, structure, config)
                    };
                }
            }
            catch (Exception ex)
            {
                _logService.Error("边界分析失败", ex);
                return new BoundaryDetectionResult
                {
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 图像预处理
        /// </summary>
        /// <param name="grayImage">灰度图像</param>
        /// <param name="config">检测配置</param>
        /// <returns>预处理后的图像</returns>
        private Image<Gray, byte> PreprocessImage(Image<Gray, byte> grayImage, BoundaryDetectionConfig config)
        {
            try
            {
                var result = grayImage.Copy();

                // 1. 高斯模糊去噪
                if (config.EnableGaussianBlur)
                {
                    result = result.SmoothGaussian(config.GaussianBlurSize);
                }

                // 2. 对比度增强
                if (config.EnableContrastEnhancement)
                {
                    result = EnhanceContrast(result, config.ContrastFactor);
                }

                // 3. 形态学操作
                if (config.EnableMorphology)
                {
                    result = ApplyMorphology(result, config.MorphologyKernelSize, config.MorphologyOperation);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logService.Error("图像预处理失败", ex);
                return grayImage;
            }
        }

        /// <summary>
        /// 边缘检测
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="config">检测配置</param>
        /// <returns>边缘图像</returns>
        private Image<Gray, byte> DetectEdges(Image<Gray, byte> image, BoundaryDetectionConfig config)
        {
            try
            {
                // 使用Canny边缘检测
                var edgeImage = image.Canny(config.CannyLowThreshold, config.CannyHighThreshold);

                // 可选的边缘细化
                if (config.EnableEdgeThinning)
                {
                    edgeImage = ThinEdges(edgeImage);
                }

                return edgeImage;
            }
            catch (Exception ex)
            {
                _logService.Error("边缘检测失败", ex);
                return new Image<Gray, byte>(image.Size);
            }
        }

        /// <summary>
        /// 轮廓检测
        /// </summary>
        /// <param name="edgeImage">边缘图像</param>
        /// <param name="config">检测配置</param>
        /// <returns>轮廓列表</returns>
        private VectorOfVectorOfPoint DetectContours(Image<Gray, byte> edgeImage, BoundaryDetectionConfig config)
        {
            try
            {
                var contours = new VectorOfVectorOfPoint();
                var hierarchy = new Mat();

                CvInvoke.FindContours(
                    edgeImage,
                    contours,
                    hierarchy,
                    config.ContourRetrievalMode,
                    config.ContourApproximationMethod
                );

                return contours;
            }
            catch (Exception ex)
            {
                _logService.Error("轮廓检测失败", ex);
                return new VectorOfVectorOfPoint();
            }
        }

        /// <summary>
        /// 筛选有效轮廓
        /// </summary>
        /// <param name="contours">所有轮廓</param>
        /// <param name="config">检测配置</param>
        /// <returns>有效轮廓列表</returns>
        private List<ContourInfo> FilterContours(VectorOfVectorOfPoint contours, BoundaryDetectionConfig config)
        {
            var validContours = new List<ContourInfo>();

            try
            {
                for (int i = 0; i < contours.Size; i++)
                {
                    var contour = contours[i];
                    var contourInfo = AnalyzeContour(contour, config);

                    if (IsValidContour(contourInfo, config))
                    {
                        validContours.Add(contourInfo);
                    }
                }

                // 按面积排序，取最大的几个轮廓
                validContours.Sort((a, b) => b.Area.CompareTo(a.Area));

                // 限制轮廓数量
                if (validContours.Count > config.MaxContours)
                {
                    validContours = validContours.GetRange(0, config.MaxContours);
                }

                _logService.Info($"找到 {validContours.Count} 个有效轮廓");
                return validContours;
            }
            catch (Exception ex)
            {
                _logService.Error("轮廓筛选失败", ex);
                return validContours;
            }
        }

        /// <summary>
        /// 分析轮廓
        /// </summary>
        /// <param name="contour">轮廓</param>
        /// <param name="config">检测配置</param>
        /// <returns>轮廓信息</returns>
        private ContourInfo AnalyzeContour(VectorOfPoint contour, BoundaryDetectionConfig config)
        {
            var area = CvInvoke.ContourArea(contour);
            var perimeter = CvInvoke.ArcLength(contour, true);
            var boundingRect = CvInvoke.BoundingRectangle(contour);
            var aspectRatio = (double)boundingRect.Width / boundingRect.Height;
            var compactness = (perimeter * perimeter) / area; // 圆形度指标

            // 计算轮廓的中心点
            var moments = CvInvoke.Moments(contour);
            var centerX = moments.M10 / moments.M00;
            var centerY = moments.M01 / moments.M00;

            return new ContourInfo
            {
                Contour = contour,
                Area = area,
                Perimeter = perimeter,
                BoundingRect = boundingRect,
                AspectRatio = aspectRatio,
                Compactness = compactness,
                CenterPoint = new Point((int)centerX, (int)centerY)
            };
        }

        /// <summary>
        /// 检查轮廓是否有效
        /// </summary>
        /// <param name="contourInfo">轮廓信息</param>
        /// <param name="config">检测配置</param>
        /// <returns>是否有效</returns>
        private bool IsValidContour(ContourInfo contourInfo, BoundaryDetectionConfig config)
        {
            // 1. 面积检查
            if (contourInfo.Area < config.MinContourArea || contourInfo.Area > config.MaxContourArea)
                return false;

            // 2. 长宽比检查（K线通常是垂直的）
            if (contourInfo.AspectRatio > config.MaxAspectRatio)
                return false;

            // 3. 紧凑度检查（K线通常不是圆形的）
            if (contourInfo.Compactness < config.MinCompactness)
                return false;

            // 4. 位置检查（排除边缘噪音）
            if (IsEdgeNoise(contourInfo, config))
                return false;

            return true;
        }

        /// <summary>
        /// 检查是否为边缘噪音
        /// </summary>
        /// <param name="contourInfo">轮廓信息</param>
        /// <param name="config">检测配置</param>
        /// <returns>是否为边缘噪音</returns>
        private bool IsEdgeNoise(ContourInfo contourInfo, BoundaryDetectionConfig config)
        {
            var rect = contourInfo.BoundingRect;
            var margin = config.EdgeNoiseMargin;

            // 检查是否靠近图像边缘
            return rect.Left < margin ||
                   rect.Top < margin ||
                   rect.Right > config.ImageWidth - margin ||
                   rect.Bottom > config.ImageHeight - margin;
        }

        /// <summary>
        /// 提取边界
        /// </summary>
        /// <param name="validContours">有效轮廓</param>
        /// <param name="config">检测配置</param>
        /// <returns>边界信息</returns>
        private KLineBoundaries ExtractBoundaries(List<ContourInfo> validContours, BoundaryDetectionConfig config)
        {
            try
            {
                if (validContours.Count == 0)
                {
                    return CreateDefaultBoundaries();
                }

                // 合并所有轮廓的外边界
                var allPoints = new List<Point>();
                foreach (var contour in validContours)
                {
                    allPoints.AddRange(contour.Contour.ToArray());
                }

                var fullBoundary = CvInvoke.BoundingRectangle(new VectorOfPoint(allPoints.ToArray()));

                // 分析K线结构，提取实体和影线边界
                var structureBoundaries = ExtractStructureBoundaries(validContours, fullBoundary, config);

                return structureBoundaries;
            }
            catch (Exception ex)
            {
                _logService.Error("边界提取失败", ex);
                return CreateDefaultBoundaries();
            }
        }

        /// <summary>
        /// 提取结构边界
        /// </summary>
        /// <param name="contours">有效轮廓</param>
        /// <param name="fullBoundary">完整边界</param>
        /// <param name="config">检测配置</param>
        /// <returns>结构边界</returns>
        private KLineBoundaries ExtractStructureBoundaries(List<ContourInfo> contours, Rectangle fullBoundary, BoundaryDetectionConfig config)
        {
            var boundaries = new KLineBoundaries
            {
                FullBoundary = fullBoundary
            };

            try
            {
                // 分析轮廓的垂直分布，找出实体和影线
                var centerY = fullBoundary.Y + fullBoundary.Height / 2;
                var bodyHeightThreshold = fullBoundary.Height * config.BodyHeightRatio;

                // 找出实体区域（通常在中间，较宽）
                var bodyContours = contours.FindAll(c =>
                    c.CenterPoint.Y >= centerY - bodyHeightThreshold / 2 &&
                    c.CenterPoint.Y <= centerY + bodyHeightThreshold / 2 &&
                    c.BoundingRect.Width >= config.MinBodyWidth
                );

                if (bodyContours.Count > 0)
                {
                    var bodyPoints = new List<Point>();
                    foreach (var contour in bodyContours)
                    {
                        bodyPoints.AddRange(contour.Contour.ToArray());
                    }
                    boundaries.BodyBoundary = CvInvoke.BoundingRectangle(new VectorOfPoint(bodyPoints.ToArray()));
                }
                else
                {
                    // 如果没有找到明确的实体，使用完整边界的中间部分
                    boundaries.BodyBoundary = new Rectangle(
                        fullBoundary.X,
                        centerY - (int)(bodyHeightThreshold / 2),
                        fullBoundary.Width,
                        (int)bodyHeightThreshold
                    );
                }

                // 确定上影线边界
                if (boundaries.BodyBoundary.Top > fullBoundary.Top)
                {
                    boundaries.UpperShadowBoundary = new Rectangle(
                        fullBoundary.X,
                        fullBoundary.Top,
                        fullBoundary.Width,
                        boundaries.BodyBoundary.Top - fullBoundary.Top
                    );
                }

                // 确定下影线边界
                if (boundaries.BodyBoundary.Bottom < fullBoundary.Bottom)
                {
                    boundaries.LowerShadowBoundary = new Rectangle(
                        fullBoundary.X,
                        boundaries.BodyBoundary.Bottom,
                        fullBoundary.Width,
                        fullBoundary.Bottom - boundaries.BodyBoundary.Bottom
                    );
                }

                return boundaries;
            }
            catch (Exception ex)
            {
                _logService.Error("结构边界提取失败", ex);
                return boundaries;
            }
        }

        /// <summary>
        /// 分析K线结构
        /// </summary>
        /// <param name="boundaries">边界信息</param>
        /// <param name="config">检测配置</param>
        /// <returns>结构分析结果</returns>
        private KLineStructure AnalyzeKLineStructure(KLineBoundaries boundaries, BoundaryDetectionConfig config)
        {
            try
            {
                var structure = new KLineStructure();

                // 计算实体高度
                if (!boundaries.BodyBoundary.IsEmpty)
                {
                    structure.BodyHeight = boundaries.BodyBoundary.Height;
                }

                // 计算上影线高度
                if (!boundaries.UpperShadowBoundary.IsEmpty)
                {
                    structure.UpperShadowHeight = boundaries.UpperShadowBoundary.Height;
                }

                // 计算下影线高度
                if (!boundaries.LowerShadowBoundary.IsEmpty)
                {
                    structure.LowerShadowHeight = boundaries.LowerShadowBoundary.Height;
                }

                // 计算总高度
                structure.TotalHeight = boundaries.FullBoundary.Height;

                // 判断K线形态
                structure.PatternType = DeterminePatternType(structure, config);

                return structure;
            }
            catch (Exception ex)
            {
                _logService.Error("K线结构分析失败", ex);
                return new KLineStructure();
            }
        }

        /// <summary>
        /// 确定K线形态类型
        /// </summary>
        /// <param name="structure">结构信息</param>
        /// <param name="config">检测配置</param>
        /// <returns>形态类型</returns>
        private KLinePatternType DeterminePatternType(KLineStructure structure, BoundaryDetectionConfig config)
        {
            try
            {
                var bodyRatio = structure.BodyHeight / Math.Max(1, structure.TotalHeight);
                var upperShadowRatio = structure.UpperShadowHeight / Math.Max(1, structure.TotalHeight);
                var lowerShadowRatio = structure.LowerShadowHeight / Math.Max(1, structure.TotalHeight);

                // 十字星：实体很小
                if (bodyRatio < config.DojiBodyRatio)
                {
                    return KLinePatternType.Doji;
                }

                // 锤子线：下影线很长，实体很小
                if (lowerShadowRatio > config.HammerShadowRatio && bodyRatio < config.HammerBodyRatio)
                {
                    return KLinePatternType.Hammer;
                }

                // 倒锤子线：上影线很长，实体很小
                if (upperShadowRatio > config.HammerShadowRatio && bodyRatio < config.HammerBodyRatio)
                {
                    return KLinePatternType.InvertedHammer;
                }

                // 普通K线
                return KLinePatternType.Normal;
            }
            catch (Exception ex)
            {
                _logService.Error("形态类型判断失败", ex);
                return KLinePatternType.Unknown;
            }
        }

        /// <summary>
        /// 计算置信度
        /// </summary>
        /// <param name="boundaries">边界信息</param>
        /// <param name="structure">结构信息</param>
        /// <param name="config">检测配置</param>
        /// <returns>置信度</returns>
        private double CalculateConfidence(KLineBoundaries boundaries, KLineStructure structure, BoundaryDetectionConfig config)
        {
            try
            {
                double confidence = 0.0;

                // 1. 边界完整性检查
                if (!boundaries.FullBoundary.IsEmpty)
                    confidence += 0.3;

                // 2. 结构合理性检查
                if (structure.TotalHeight > 0 && structure.BodyHeight <= structure.TotalHeight)
                    confidence += 0.3;

                // 3. 形态清晰度检查
                var bodyRatio = structure.BodyHeight / Math.Max(1, structure.TotalHeight);
                if (bodyRatio >= config.MinBodyRatio && bodyRatio <= config.MaxBodyRatio)
                    confidence += 0.2;

                // 4. 影线比例检查
                var shadowRatio = (structure.UpperShadowHeight + structure.LowerShadowHeight) / Math.Max(1, structure.TotalHeight);
                if (shadowRatio <= config.MaxShadowRatio)
                    confidence += 0.2;

                return Math.Min(1.0, confidence);
            }
            catch (Exception ex)
            {
                _logService.Error("置信度计算失败", ex);
                return 0.0;
            }
        }

        /// <summary>
        /// 增强对比度
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="factor">增强因子</param>
        /// <returns>增强后的图像</returns>
        private Image<Gray, byte> EnhanceContrast(Image<Gray, byte> image, double factor)
        {
            try
            {
                var result = image.Copy();
                CvInvoke.ConvertScaleAbs(result, result, factor, 0);
                return result;
            }
            catch
            {
                return image;
            }
        }

        /// <summary>
        /// 应用形态学操作
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="kernelSize">核大小</param>
        /// <param name="operation">操作类型</param>
        /// <returns>处理后的图像</returns>
        private Image<Gray, byte> ApplyMorphology(Image<Gray, byte> image, int kernelSize, MorphOp operation)
        {
            try
            {
                var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(kernelSize, kernelSize), new Point(-1, -1));
                var result = image.Copy();
                CvInvoke.MorphologyEx(result, result, operation, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                return result;
            }
            catch
            {
                return image;
            }
        }

        /// <summary>
        /// 边缘细化
        /// </summary>
        /// <param name="edgeImage">边缘图像</param>
        /// <returns>细化后的边缘图像</returns>
        private Image<Gray, byte> ThinEdges(Image<Gray, byte> edgeImage)
        {
            try
            {
                // 简化的边缘细化实现
                var result = edgeImage.Copy();
                var kernel = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(3, 3), new Point(-1, -1));
                CvInvoke.MorphologyEx(result, result, MorphOp.Thin, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                return result;
            }
            catch
            {
                return edgeImage;
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
            region = Rectangle.Intersect(region, new Rectangle(0, 0, image.Width, image.Height));

            if (region.IsEmpty)
                throw new ArgumentException("裁剪区域无效或超出图像范围", nameof(region));

            return image.Clone(region, image.PixelFormat);
        }

        /// <summary>
        /// 调整坐标到原图像坐标系
        /// </summary>
        /// <param name="result">检测结果</param>
        /// <param name="originalRegion">原始区域</param>
        private void AdjustCoordinatesToOriginal(BoundaryDetectionResult result, Rectangle originalRegion)
        {
            if (result == null || originalRegion.IsEmpty)
                return;

            var offsetX = originalRegion.X;
            var offsetY = originalRegion.Y;

            result.FullBoundary = new Rectangle(
                result.FullBoundary.X + offsetX,
                result.FullBoundary.Y + offsetY,
                result.FullBoundary.Width,
                result.FullBoundary.Height
            );

            if (!result.BodyBoundary.IsEmpty)
            {
                result.BodyBoundary = new Rectangle(
                    result.BodyBoundary.X + offsetX,
                    result.BodyBoundary.Y + offsetY,
                    result.BodyBoundary.Width,
                    result.BodyBoundary.Height
                );
            }

            if (!result.UpperShadowBoundary.IsEmpty)
            {
                result.UpperShadowBoundary = new Rectangle(
                    result.UpperShadowBoundary.X + offsetX,
                    result.UpperShadowBoundary.Y + offsetY,
                    result.UpperShadowBoundary.Width,
                    result.UpperShadowBoundary.Height
                );
            }

            if (!result.LowerShadowBoundary.IsEmpty)
            {
                result.LowerShadowBoundary = new Rectangle(
                    result.LowerShadowBoundary.X + offsetX,
                    result.LowerShadowBoundary.Y + offsetY,
                    result.LowerShadowBoundary.Width,
                    result.LowerShadowBoundary.Height
                );
            }
        }

        /// <summary>
        /// 创建默认边界
        /// </summary>
        /// <returns>默认边界</returns>
        private KLineBoundaries CreateDefaultBoundaries()
        {
            return new KLineBoundaries
            {
                FullBoundary = Rectangle.Empty,
                BodyBoundary = Rectangle.Empty,
                UpperShadowBoundary = Rectangle.Empty,
                LowerShadowBoundary = Rectangle.Empty
            };
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认配置</returns>
        private BoundaryDetectionConfig CreateDefaultConfig()
        {
            return new BoundaryDetectionConfig
            {
                // 边缘检测参数
                CannyLowThreshold = 50,
                CannyHighThreshold = 150,
                EnableEdgeThinning = false,

                // 预处理参数
                EnableGaussianBlur = true,
                GaussianBlurSize = 3,
                EnableContrastEnhancement = true,
                ContrastFactor = 1.2,
                EnableMorphology = true,
                MorphologyKernelSize = 3,
                MorphologyOperation = MorphOp.Close,

                // 轮廓检测参数
                ContourRetrievalMode = RetrType.External,
                ContourApproximationMethod = ChainApproxMethod.ChainApproxSimple,

                // 轮廓筛选参数
                MinContourArea = 100,
                MaxContourArea = 10000,
                MaxAspectRatio = 3.0,
                MinCompactness = 10.0,
                MaxContours = 10,
                EdgeNoiseMargin = 5,
                ImageWidth = 1920,
                ImageHeight = 1080,

                // 结构分析参数
                BodyHeightRatio = 0.6,
                MinBodyWidth = 5,
                DojiBodyRatio = 0.1,
                HammerShadowRatio = 0.6,
                HammerBodyRatio = 0.3,
                MinBodyRatio = 0.1,
                MaxBodyRatio = 0.8,
                MaxShadowRatio = 0.9
            };
        }
    }

    /// <summary>
    /// 边界检测结果
    /// </summary>
    public class BoundaryDetectionResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// 完整边界
        /// </summary>
        public Rectangle FullBoundary { get; set; }

        /// <summary>
        /// 实体边界
        /// </summary>
        public Rectangle BodyBoundary { get; set; }

        /// <summary>
        /// 上影线边界
        /// </summary>
        public Rectangle UpperShadowBoundary { get; set; }

        /// <summary>
        /// 下影线边界
        /// </summary>
        public Rectangle LowerShadowBoundary { get; set; }

        /// <summary>
        /// K线结构信息
        /// </summary>
        public KLineStructure Structure { get; set; }

        /// <summary>
        /// 置信度 (0-1)
        /// </summary>
        public double Confidence { get; set; }

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
    /// K线边界信息
    /// </summary>
    public class KLineBoundaries
    {
        /// <summary>
        /// 完整边界
        /// </summary>
        public Rectangle FullBoundary { get; set; }

        /// <summary>
        /// 实体边界
        /// </summary>
        public Rectangle BodyBoundary { get; set; }

        /// <summary>
        /// 上影线边界
        /// </summary>
        public Rectangle UpperShadowBoundary { get; set; }

        /// <summary>
        /// 下影线边界
        /// </summary>
        public Rectangle LowerShadowBoundary { get; set; }
    }

    /// <summary>
    /// 轮廓信息
    /// </summary>
    internal class ContourInfo
    {
        public VectorOfPoint Contour { get; set; }
        public double Area { get; set; }
        public double Perimeter { get; set; }
        public Rectangle BoundingRect { get; set; }
        public double AspectRatio { get; set; }
        public double Compactness { get; set; }
        public Point CenterPoint { get; set; }
    }

    /// <summary>
    /// K线形态类型
    /// </summary>
    public enum KLinePatternType
    {
        /// <summary>
        /// 未知
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 普通K线
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 十字星
        /// </summary>
        Doji = 2,

        /// <summary>
        /// 锤子线
        /// </summary>
        Hammer = 3,

        /// <summary>
        /// 倒锤子线
        /// </summary>
        InvertedHammer = 4
    }

    /// <summary>
    /// K线结构信息
    /// </summary>
    public class KLineStructure
    {
        /// <summary>
        /// 实体高度
        /// </summary>
        public int BodyHeight { get; set; }

        /// <summary>
        /// 上影线高度
        /// </summary>
        public int UpperShadowHeight { get; set; }

        /// <summary>
        /// 下影线高度
        /// </summary>
        public int LowerShadowHeight { get; set; }

        /// <summary>
        /// 总高度
        /// </summary>
        public int TotalHeight { get; set; }

        /// <summary>
        /// 形态类型
        /// </summary>
        public KLinePatternType PatternType { get; set; } = KLinePatternType.Unknown;
    }

    /// <summary>
    /// 边界检测配置
    /// </summary>
    public class BoundaryDetectionConfig
    {
        // 边缘检测参数
        public int CannyLowThreshold { get; set; }
        public int CannyHighThreshold { get; set; }
        public bool EnableEdgeThinning { get; set; }

        // 预处理参数
        public bool EnableGaussianBlur { get; set; }
        public int GaussianBlurSize { get; set; }
        public bool EnableContrastEnhancement { get; set; }
        public double ContrastFactor { get; set; }
        public bool EnableMorphology { get; set; }
        public int MorphologyKernelSize { get; set; }
        public MorphOp MorphologyOperation { get; set; }

        // 轮廓检测参数
        public RetrType ContourRetrievalMode { get; set; }
        public ChainApproxMethod ContourApproximationMethod { get; set; }

        // 轮廓筛选参数
        public double MinContourArea { get; set; }
        public double MaxContourArea { get; set; }
        public double MaxAspectRatio { get; set; }
        public double MinCompactness { get; set; }
        public int MaxContours { get; set; }
        public int EdgeNoiseMargin { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }

        // 结构分析参数
        public double BodyHeightRatio { get; set; }
        public int MinBodyWidth { get; set; }
        public double DojiBodyRatio { get; set; }
        public double HammerShadowRatio { get; set; }
        public double HammerBodyRatio { get; set; }
        public double MinBodyRatio { get; set; }
        public double MaxBodyRatio { get; set; }
        public double MaxShadowRatio { get; set; }
    }
}