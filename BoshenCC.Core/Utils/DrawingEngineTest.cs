using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 绘图引擎测试工具
    /// 用于验证GDI+绘制引擎功能的完整性
    /// </summary>
    public class DrawingEngineTest : IDisposable
    {
        private readonly ILogService _logService;
        private readonly DrawingEngine _drawingEngine;
        private bool _disposed = false;

        #region 构造函数和析构函数

        /// <summary>
        /// 初始化绘图引擎测试工具
        /// </summary>
        /// <param name="logService">日志服务</param>
        public DrawingEngineTest(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _drawingEngine = new DrawingEngine(_logService);
            _logService.LogInfo("DrawingEngineTest 初始化");
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~DrawingEngineTest()
        {
            Dispose(false);
        }

        #endregion

        #region 测试方法

        /// <summary>
        /// 运行完整的绘制引擎测试
        /// </summary>
        /// <param name="outputPath">测试图片输出路径</param>
        /// <returns>测试结果</returns>
        public TestResult RunFullTest(string outputPath = "DrawingEngineTest.png")
        {
            try
            {
                _logService.LogInfo("开始运行完整绘制引擎测试");

                var result = new TestResult();
                var testBounds = new RectangleF(0, 0, 800, 600);

                // 创建测试图片
                using var bitmap = new Bitmap((int)testBounds.Width, (int)testBounds.Height);
                using var graphics = Graphics.FromImage(bitmap);

                // 配置高质量绘图
                ConfigureTestGraphics(graphics);

                // 测试1：绘制背景
                result.TestBackground = TestBackground(graphics, testBounds);

                // 测试2：创建测试预测线
                var testLines = CreateTestPredictionLines();
                result.TestLineCreation = testLines != null && testLines.Count > 0;

                // 测试3：绘制预测线
                if (result.TestLineCreation)
                {
                    result.TestLineDrawing = _drawingEngine.DrawPredictionLines(graphics, testLines, testBounds);
                }

                // 测试4：绘制网格
                result.TestGridDrawing = TestGridDrawing(graphics, testBounds);

                // 测试5：测试样式主题
                result.TestStyles = TestStyleThemes(graphics, testBounds);

                // 测试6：测试配置更新
                result.TestConfiguration = TestConfigurationUpdates(graphics, testBounds, testLines);

                // 保存测试结果
                if (result.IsAllTestsPassed())
                {
                    bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                    _logService.LogInfo($"测试图片已保存: {Path.GetFullPath(outputPath)}");
                }

                // 计算总体结果
                result.OverallSuccess = result.IsAllTestsPassed();
                result.TestTime = DateTime.Now;

                _logService.LogInfo($"绘制引擎测试完成，总体结果: {(result.OverallSuccess ? "通过" : "失败")}");
                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"运行完整测试失败: {ex.Message}", ex);
                return new TestResult { OverallSuccess = false, TestTime = DateTime.Now };
            }
        }

        /// <summary>
        /// 测试基础绘制功能
        /// </summary>
        /// <param name="outputPath">输出路径</param>
        /// <returns>是否成功</returns>
        public bool TestBasicDrawing(string outputPath = "BasicDrawingTest.png")
        {
            try
            {
                _logService.LogInfo("开始基础绘制功能测试");

                var testBounds = new RectangleF(0, 0, 600, 400);

                using var bitmap = new Bitmap((int)testBounds.Width, (int)testBounds.Height);
                using var graphics = Graphics.FromImage(bitmap);

                ConfigureTestGraphics(graphics);

                // 创建简单的测试线条
                var testLines = new List<PredictionLine>
                {
                    PredictionLine.CreateStandard(0, "A线", 100.0, 100.0, 120.0, 0.0),
                    PredictionLine.CreateStandard(3, "3线", 137.4, 100.0, 120.0, 3.137)
                };

                // 设置Y位置
                testLines[0].YPosition = 100;
                testLines[1].YPosition = 200;

                var success = _drawingEngine.DrawPredictionLines(graphics, testLines, testBounds);

                if (success)
                {
                    bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                    _logService.LogInfo($"基础绘制测试成功，图片已保存: {Path.GetFullPath(outputPath)}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logService.LogError($"基础绘制测试失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 测试性能
        /// </summary>
        /// <param name="lineCount">测试线条数量</param>
        /// <param name="iterations">迭代次数</param>
        /// <returns>性能测试结果</returns>
        public PerformanceTestResult TestPerformance(int lineCount = 100, int iterations = 10)
        {
            try
            {
                _logService.LogInfo($"开始性能测试: {lineCount}条线 × {iterations}次迭代");

                var result = new PerformanceTestResult
                {
                    LineCount = lineCount,
                    Iterations = iterations,
                    StartTime = DateTime.Now
                };

                var testBounds = new RectangleF(0, 0, 800, 600);
                var testLines = CreateTestPredictionLines(lineCount);

                // 预热
                using (var warmupBitmap = new Bitmap((int)testBounds.Width, (int)testBounds.Height))
                using (var warmupGraphics = Graphics.FromImage(warmupBitmap))
                {
                    _drawingEngine.DrawPredictionLines(warmupGraphics, testLines, testBounds);
                }

                // 正式测试
                var durations = new List<long>();
                for (int i = 0; i < iterations; i++)
                {
                    using var bitmap = new Bitmap((int)testBounds.Width, (int)testBounds.Height);
                    using var graphics = Graphics.FromImage(bitmap);

                    var startTime = DateTime.Now;
                    _drawingEngine.DrawPredictionLines(graphics, testLines, testBounds);
                    var duration = DateTime.Now - startTime;

                    durations.Add(duration.Ticks);
                }

                result.EndTime = DateTime.Now;
                result.TotalDuration = result.EndTime - result.StartTime;
                result.AverageDuration = TimeSpan.FromTicks((long)durations.Average());
                result.MinDuration = TimeSpan.FromTicks(durations.Min());
                result.MaxDuration = TimeSpan.FromTicks(durations.Max());
                result.Success = true;

                _logService.LogInfo($"性能测试完成，平均耗时: {result.AverageDuration.TotalMilliseconds:F2}ms");
                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"性能测试失败: {ex.Message}", ex);
                return new PerformanceTestResult { Success = false };
            }
        }

        #endregion

        #region 私有测试方法

        /// <summary>
        /// 配置测试用绘图对象
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        private void ConfigureTestGraphics(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 填充白色背景
            graphics.Clear(Color.White);
        }

        /// <summary>
        /// 测试背景绘制
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">绘制区域</param>
        /// <returns>是否成功</returns>
        private bool TestBackground(Graphics graphics, RectangleF bounds)
        {
            try
            {
                using var brush = new SolidBrush(Color.FromArgb(248, 248, 248));
                graphics.FillRectangle(brush, bounds);

                using var borderPen = new Pen(Color.FromArgb(200, 200, 200), 1);
                graphics.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);

                _logService.LogDebug("背景绘制测试通过");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"背景绘制测试失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 创建测试预测线
        /// </summary>
        /// <param name="count">线条数量</param>
        /// <returns>预测线列表</returns>
        private List<PredictionLine> CreateTestPredictionLines(int count = 11)
        {
            try
            {
                var lines = new List<PredictionLine>();
                var ratios = new[] { 0.0, 1.0, 1.849, 2.397, 3.137, 3.401, 4.0, 4.726, 5.247, 6.027, 6.808 };
                var names = new[] { "A线", "B线", "1线", "2线", "3线", "4线", "5线", "6线", "7线", "8线", "极线" };

                var pointAPrice = 100.0;
                var pointBPrice = 120.0;
                var abRange = pointBPrice - pointAPrice;

                for (int i = 0; i < Math.Min(count, ratios.Length); i++)
                {
                    var price = i == 0 ? pointAPrice : pointBPrice + abRange * (ratios[i] - 1);
                    var line = PredictionLine.CreateStandard(i, names[i], price, pointAPrice, pointBPrice, ratios[i]);
                    line.YPosition = 50 + i * 40; // 设置Y坐标
                    lines.Add(line);
                }

                _logService.LogDebug($"创建 {lines.Count} 条测试预测线");
                return lines;
            }
            catch (Exception ex)
            {
                _logService.LogError($"创建测试预测线失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 测试网格绘制
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">绘制区域</param>
        /// <returns>是否成功</returns>
        private bool TestGridDrawing(Graphics graphics, RectangleF bounds)
        {
            try
            {
                var lineRenderer = new LineRenderer(_logService);
                var success = lineRenderer.DrawGrid(graphics, bounds, 50.0f, Color.FromArgb(200, 200, 200), 0.5f, 0.3f);
                lineRenderer.Dispose();

                _logService.LogDebug($"网格绘制测试{(success ? "通过" : "失败")}");
                return success;
            }
            catch (Exception ex)
            {
                _logService.LogError($"网格绘制测试失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 测试样式主题
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">绘制区域</param>
        /// <returns>是否成功</returns>
        private bool TestStyleThemes(Graphics graphics, RectangleF bounds)
        {
            try
            {
                // 测试默认主题
                var defaultTheme = DrawingStyles.GetDefaultThemeColors();
                var defaultValid = DrawingStyles.ValidateThemeColors(defaultTheme);

                // 测试深色主题
                var darkTheme = DrawingStyles.GetDarkThemeColors();
                var darkValid = DrawingStyles.ValidateThemeColors(darkTheme);

                // 测试高对比度主题
                var highContrastTheme = DrawingStyles.GetHighContrastThemeColors();
                var highContrastValid = DrawingStyles.ValidateThemeColors(highContrastTheme);

                var success = defaultValid && darkValid && highContrastValid;

                _logService.LogDebug($"样式主题测试{(success ? "通过" : "失败")}");
                return success;
            }
            catch (Exception ex)
            {
                _logService.LogError($"样式主题测试失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 测试配置更新
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">绘制区域</param>
        /// <param name="testLines">测试线条</param>
        /// <returns>是否成功</returns>
        private bool TestConfigurationUpdates(Graphics graphics, RectangleF bounds, List<PredictionLine> testLines)
        {
            try
            {
                // 测试配置更新
                _drawingEngine.SetAntiAliasing(true);
                _drawingEngine.SetLineOpacity(0.8f);
                _drawingEngine.SetLabelOpacity(0.9f);
                _drawingEngine.SetGridVisibility(true);
                _drawingEngine.SetPriceLabelsVisibility(true);
                _drawingEngine.SetCurrentPrice(110.0);

                // 测试预设配置
                var highQualityPreset = DrawingStyles.GetDrawingPreset("HighQuality");
                _drawingEngine.UpdateConfig(highQualityPreset);

                // 测试引擎状态
                var status = _drawingEngine.GetEngineStatus();
                var statusValid = status != null && status.Count > 0;

                // 测试绘制功能
                var drawTest = _drawingEngine.TestDrawingFunctionality(graphics, bounds);

                var success = statusValid && drawTest;

                _logService.LogDebug($"配置更新测试{(success ? "通过" : "失败")}");
                return success;
            }
            catch (Exception ex)
            {
                _logService.LogError($"配置更新测试失败: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region 测试结果类

        /// <summary>
        /// 测试结果类
        /// </summary>
        public class TestResult
        {
            public bool TestBackground { get; set; }
            public bool TestLineCreation { get; set; }
            public bool TestLineDrawing { get; set; }
            public bool TestGridDrawing { get; set; }
            public bool TestStyles { get; set; }
            public bool TestConfiguration { get; set; }
            public bool OverallSuccess { get; set; }
            public DateTime TestTime { get; set; }

            public bool IsAllTestsPassed()
            {
                return TestBackground && TestLineCreation && TestLineDrawing &&
                       TestGridDrawing && TestStyles && TestConfiguration;
            }

            public override string ToString()
            {
                return $"测试结果: 总体{(OverallSuccess ? "通过" : "失败")} | " +
                       $"背景:{TestBackground} | 线条创建:{TestLineCreation} | 线条绘制:{TestLineDrawing} | " +
                       $"网格:{TestGridDrawing} | 样式:{TestStyles} | 配置:{TestConfiguration}";
            }
        }

        /// <summary>
        /// 性能测试结果类
        /// </summary>
        public class PerformanceTestResult
        {
            public int LineCount { get; set; }
            public int Iterations { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public TimeSpan TotalDuration { get; set; }
            public TimeSpan AverageDuration { get; set; }
            public TimeSpan MinDuration { get; set; }
            public TimeSpan MaxDuration { get; set; }
            public bool Success { get; set; }

            public override string ToString()
            {
                return $"性能测试: {LineCount}条线 × {Iterations}次 | " +
                       $"平均:{AverageDuration.TotalMilliseconds:F2}ms | " +
                       $"最小:{MinDuration.TotalMilliseconds:F2}ms | " +
                       $"最大:{MaxDuration.TotalMilliseconds:F2}ms";
            }
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logService.LogInfo("DrawingEngineTest 正在释放托管资源");
                }

                _drawingEngine?.Dispose();

                _disposed = true;
                _logService.LogInfo("DrawingEngineTest 资源释放完成");
            }
        }

        #endregion
    }
}