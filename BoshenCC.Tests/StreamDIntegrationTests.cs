using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BoshenCC.Models;
using BoshenCC.Core.Utils;
using BoshenCC.Services.Interfaces;
using BoshenCC.Services.Implementations;
using BoshenCC.WinForms.Controls;

namespace BoshenCC.Tests
{
    /// <summary>
    /// Stream D 集成测试
    /// 测试绘制服务集成的完整功能
    /// </summary>
    [TestClass]
    public class StreamDIntegrationTests
    {
        private ILogService _logService;
        private IWindowManagerService _windowManagerService;
        private IDrawingService _drawingService;
        private PredictionLineManager _predictionLineManager;
        private OverlayManager _overlayManager;

        #region 测试初始化和清理

        [TestInitialize]
        public void TestInitialize()
        {
            try
            {
                // 创建基础服务
                _logService = new Services.Implementations.LogService();
                _windowManagerService = new Core.Services.WindowManagerService(_logService);

                // 创建绘制服务
                _drawingService = new DrawingService(_logService, _windowManagerService);

                // 创建预测线管理器
                _predictionLineManager = new PredictionLineManager(_logService);

                // 创建叠加管理器
                _overlayManager = new OverlayManager(_drawingService);

                Console.WriteLine("Stream D 集成测试初始化完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试初始化失败: {ex.Message}");
                Assert.Fail($"测试初始化失败: {ex.Message}");
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                // 清理资源
                _overlayManager?.Dispose();
                _predictionLineManager?.Dispose();
                _drawingService?.Dispose();
                _windowManagerService?.Dispose();
                _logService?.Dispose();

                Console.WriteLine("Stream D 集成测试清理完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试清理失败: {ex.Message}");
            }
        }

        #endregion

        #region DrawingService 测试

        /// <summary>
        /// 测试DrawingService基本功能
        /// </summary>
        [TestMethod]
        public async Task DrawingService_BasicFunctionality_ShouldWork()
        {
            // Arrange
            var targetWindowHandle = IntPtr.Zero; // 模拟窗口句柄
            var predictionLines = CreateTestPredictionLines();
            var config = DrawingConfiguration.CreateDefault();

            // Act & Assert - 检查服务状态
            Assert.IsNotNull(_drawingService, "绘制服务不应为空");
            Assert.IsTrue(_drawingService.IsServiceAvailable(), "绘制服务应该可用");
            Assert.IsFalse(_drawingService.IsDrawing, "初始状态不应在绘制");

            // 检查配置
            _drawingService.UpdateConfiguration(config);
            var currentConfig = _drawingService.Configuration;
            Assert.IsNotNull(currentConfig, "配置不应为空");
            Assert.AreEqual(config.WindowAlpha, currentConfig.WindowAlpha, "窗口透明度配置应该一致");

            // 检查预测线管理
            Assert.IsTrue(_drawingService.UpdatePredictionLines(predictionLines), "应该能更新预测线");
            Assert.AreEqual(predictionLines.Count, _drawingService.CurrentPredictionLines.Count, "预测线数量应该一致");

            Console.WriteLine("DrawingService 基本功能测试通过");
        }

        /// <summary>
        /// 测试DrawingService配置管理
        /// </summary>
        [TestMethod]
        public void DrawingService_ConfigurationManagement_ShouldWork()
        {
            // Arrange
            var testConfig = new DrawingConfiguration
            {
                WindowAlpha = 150,
                LineOpacity = 0.6f,
                LabelOpacity = 0.8f,
                EnableAntiAliasing = false,
                ShowPriceLabels = false,
                RefreshRate = 60
            };

            // Act
            _drawingService.UpdateConfiguration(testConfig);

            // Assert
            var currentConfig = _drawingService.Configuration;
            Assert.AreEqual(testConfig.WindowAlpha, currentConfig.WindowAlpha, "窗口透明度应该更新");
            Assert.AreEqual(testConfig.LineOpacity, currentConfig.LineOpacity, "线条透明度应该更新");
            Assert.AreEqual(testConfig.LabelOpacity, currentConfig.LabelOpacity, "标签透明度应该更新");
            Assert.AreEqual(testConfig.EnableAntiAliasing, currentConfig.EnableAntiAliasing, "抗锯齿设置应该更新");
            Assert.AreEqual(testConfig.ShowPriceLabels, currentConfig.ShowPriceLabels, "价格标签显示应该更新");
            Assert.AreEqual(testConfig.RefreshRate, currentConfig.RefreshRate, "刷新率应该更新");

            Console.WriteLine("DrawingService 配置管理测试通过");
        }

        /// <summary>
        /// 测试DrawingService性能统计
        /// </summary>
        [TestMethod]
        public void DrawingService_PerformanceStats_ShouldWork()
        {
            // Act
            var serviceStatus = _drawingService.GetServiceStatus();
            var performanceStats = _drawingService.GetPerformanceStats();

            // Assert
            Assert.IsNotNull(serviceStatus, "服务状态不应为空");
            Assert.IsTrue(serviceStatus.ContainsKey("IsDrawing"), "应包含绘制状态");
            Assert.IsTrue(serviceStatus.ContainsKey("PredictionLineCount"), "应包含预测线数量");
            Assert.IsTrue(serviceStatus.ContainsKey("ServiceStartTime"), "应包含服务启动时间");

            Assert.IsNotNull(performanceStats, "性能统计不应为空");
            Assert.IsTrue(performanceStats.ServiceStartTime <= DateTime.Now, "服务启动时间应该有效");

            Console.WriteLine("DrawingService 性能统计测试通过");
        }

        #endregion

        #region PredictionLineManager 测试

        /// <summary>
        /// 测试PredictionLineManager基本功能
        /// </summary>
        [TestMethod]
        public async Task PredictionLineManager_BasicFunctionality_ShouldWork()
        {
            // Arrange
            var testLines = CreateTestPredictionLines();

            // Act & Assert - 检查初始状态
            Assert.IsNotNull(_predictionLineManager, "预测线管理器不应为空");
            Assert.AreEqual(0, _predictionLineManager.Count, "初始预测线数量应该为0");
            Assert.IsFalse(_predictionLineManager.IsDisposed, "不应已释放");

            // 添加预测线
            foreach (var line in testLines)
            {
                var added = _predictionLineManager.AddPredictionLine(line);
                Assert.IsTrue(added, $"应该能添加预测线: {line.Name}");
            }

            Assert.AreEqual(testLines.Count, _predictionLineManager.Count, "预测线数量应该正确");

            // 获取预测线
            var allLines = _predictionLineManager.GetAllPredictionLines();
            Assert.AreEqual(testLines.Count, allLines.Count, "获取的预测线数量应该正确");

            foreach (var line in testLines)
            {
                var retrievedLine = _predictionLineManager.GetPredictionLine(line.Name);
                Assert.IsNotNull(retrievedLine, $"应该能获取预测线: {line.Name}");
                Assert.AreEqual(line.Name, retrievedLine.Name, "预测线名称应该一致");
            }

            Console.WriteLine("PredictionLineManager 基本功能测试通过");
        }

        /// <summary>
        /// 测试PredictionLineManager计算功能
        /// </summary>
        [TestMethod]
        public async Task PredictionLineManager_Calculation_ShouldWork()
        {
            // Arrange
            var kLineData = CreateTestKLineData();
            var basePrice = 100.0;

            // Act
            var boshenLines = await _predictionLineManager.CalculatePredictionLinesFromKLineAsync(
                kLineData, basePrice, PredictionLineCalculationType.BoshenStandard);

            var fibonacciLines = await _predictionLineManager.CalculatePredictionLinesFromKLineAsync(
                kLineData, basePrice, PredictionLineCalculationType.Fibonacci);

            // Assert
            Assert.IsNotNull(boshenLines, "波神线计算结果不应为空");
            Assert.IsTrue(boshenLines.Count > 0, "应该计算出波神线");

            Assert.IsNotNull(fibonacciLines, "斐波那契线计算结果不应为空");
            Assert.IsTrue(fibonacciLines.Count > 0, "应该计算出斐波那契线");

            // 验证计算结果
            foreach (var line in boshenLines)
            {
                Assert.IsTrue(line.Price > 0, "预测线价格应该大于0");
                Assert.IsFalse(string.IsNullOrWhiteSpace(line.Name), "预测线名称不应为空");
            }

            Console.WriteLine($"PredictionLineManager 计算测试通过，波神线: {boshenLines.Count}条，斐波那契线: {fibonacciLines.Count}条");
        }

        /// <summary>
        /// 测试PredictionLineManager验证功能
        /// </summary>
        [TestMethod]
        public async Task PredictionLineManager_Validation_ShouldWork()
        {
            // Arrange
            var validLine = PredictionLine.CreateStandard(0, "测试线", 105.0, 100.0, 105.0, 0.0);
            var invalidLine = PredictionLine.CreateStandard(1, "", -10.0, 100.0, -10.0, 0.0);
            var currentPrice = 102.0;

            // Act
            var validResult = await _predictionLineManager.ValidatePredictionLineAsync(validLine, currentPrice);
            var invalidResult = await _predictionLineManager.ValidatePredictionLineAsync(invalidLine, currentPrice);

            // Assert
            Assert.IsNotNull(validResult, "验证结果不应为空");
            Assert.IsTrue(validResult.IsValid, "有效预测线应该通过验证");

            Assert.IsNotNull(invalidResult, "验证结果不应为空");
            Assert.IsFalse(invalidResult.IsValid, "无效预测线应该验证失败");
            Assert.IsTrue(invalidResult.ValidationMessages.Length > 0, "应该有验证错误消息");

            Console.WriteLine("PredictionLineManager 验证测试通过");
        }

        #endregion

        #region OverlayManager 测试

        /// <summary>
        /// 测试OverlayManager基本功能
        /// </summary>
        [TestMethod]
        public async Task OverlayManager_BasicFunctionality_ShouldWork()
        {
            // Arrange
            var testLines = CreateTestPredictionLines();
            var config = DrawingConfiguration.CreateDefault();

            // Act & Assert - 检查初始状态
            Assert.IsNotNull(_overlayManager, "叠加管理器不应为空");
            Assert.IsTrue(_overlayManager.IsAvailable(), "叠加管理器应该可用");
            Assert.IsFalse(_overlayManager.IsDrawing, "初始状态不应在绘制");
            Assert.AreEqual(0, _overlayManager.PredictionLineCount, "初始预测线数量应该为0");

            // 更新预测线
            await _overlayManager.UpdatePredictionLinesAsync(testLines);
            Assert.AreEqual(testLines.Count, _overlayManager.PredictionLineCount, "预测线数量应该正确");

            // 更新配置
            _overlayManager.UpdateConfiguration(config);
            Assert.AreEqual(config.WindowAlpha, _overlayManager.CurrentConfiguration.WindowAlpha, "配置应该更新");

            // 获取状态
            var status = _overlayManager.Status;
            var serviceStatus = _overlayManager.GetServiceStatus();

            Assert.IsNotNull(serviceStatus, "服务状态不应为空");
            Assert.IsTrue(serviceStatus.ContainsKey("OverlayManagerStatus"), "应包含管理器状态");

            Console.WriteLine("OverlayManager 基本功能测试通过");
        }

        /// <summary>
        /// 测试OverlayManager预测线管理
        /// </summary>
        [TestMethod]
        public async Task OverlayManager_PredictionLineManagement_ShouldWork()
        {
            // Arrange
            var initialLines = CreateTestPredictionLines().Take(2).ToList();
            var additionalLine = PredictionLine.CreateCustom(2, "额外线", 110.0, 100.0, 110.0, 0.0);

            // Act & Assert - 添加初始预测线
            await _overlayManager.UpdatePredictionLinesAsync(initialLines);
            Assert.AreEqual(initialLines.Count, _overlayManager.PredictionLineCount, "初始预测线数量应该正确");

            // 添加额外预测线
            var added = await _overlayManager.AddPredictionLineAsync(additionalLine);
            Assert.IsTrue(added, "应该能添加额外预测线");
            Assert.AreEqual(initialLines.Count + 1, _overlayManager.PredictionLineCount, "预测线总数应该正确");

            // 获取预测线
            var allLines = _overlayManager.GetPredictionLines();
            Assert.AreEqual(initialLines.Count + 1, allLines.Count, "获取的预测线数量应该正确");

            var retrievedLine = _overlayManager.GetPredictionLine(additionalLine.Name);
            Assert.IsNotNull(retrievedLine, "应该能获取额外预测线");
            Assert.AreEqual(additionalLine.Name, retrievedLine.Name, "预测线名称应该一致");

            // 移除预测线
            var removed = await _overlayManager.RemovePredictionLineAsync(additionalLine.Name);
            Assert.IsTrue(removed, "应该能移除预测线");
            Assert.AreEqual(initialLines.Count, _overlayManager.PredictionLineCount, "预测线数量应该恢复");

            Console.WriteLine("OverlayManager 预测线管理测试通过");
        }

        #endregion

        #region 集成测试

        /// <summary>
        /// 测试完整集成流程
        /// </summary>
        [TestMethod]
        public async Task CompleteIntegration_Workflow_ShouldWork()
        {
            try
            {
                // Arrange - 准备测试数据
                var kLineData = CreateTestKLineData();
                var basePrice = 100.0;
                var config = DrawingConfiguration.CreateHighQuality();

                // Step 1: 使用PredictionLineManager计算预测线
                Console.WriteLine("步骤1: 计算预测线");
                var calculatedLines = await _predictionLineManager.CalculatePredictionLinesFromKLineAsync(
                    kLineData, basePrice, PredictionLineCalculationType.BoshenStandard);

                Assert.IsNotNull(calculatedLines, "计算出的预测线不应为空");
                Assert.IsTrue(calculatedLines.Count > 0, "应该计算出预测线");

                // Step 2: 添加到PredictionLineManager
                Console.WriteLine("步骤2: 添加预测线到管理器");
                var addedCount = _predictionLineManager.AddPredictionLinesBatch(calculatedLines);
                Assert.AreEqual(calculatedLines.Count, addedCount, "所有预测线应该添加成功");

                // Step 3: 验证预测线
                Console.WriteLine("步骤3: 验证预测线");
                var validationResult = await _predictionLineManager.ValidateAllPredictionLinesAsync(basePrice);
                Assert.IsNotNull(validationResult, "验证结果不应为空");
                Assert.AreEqual(calculatedLines.Count, validationResult.Count, "所有预测线应该验证完成");

                var validLines = validationResult.Where(r => r.IsValid).ToList();
                Console.WriteLine($"验证完成，有效预测线: {validLines.Count}/{calculatedLines.Count}");

                // Step 4: 更新DrawingService配置
                Console.WriteLine("步骤4: 更新绘制服务配置");
                _drawingService.UpdateConfiguration(config);
                Assert.AreEqual(config.WindowAlpha, _drawingService.Configuration.WindowAlpha, "配置应该更新");

                // Step 5: 更新DrawingService预测线
                Console.WriteLine("步骤5: 更新绘制服务预测线");
                var validPredictionLines = validLines.Select(r => r.PredictionLine).ToList();
                var updated = _drawingService.UpdatePredictionLines(validPredictionLines);
                Assert.IsTrue(updated, "预测线应该更新成功");

                // Step 6: 更新OverlayManager
                Console.WriteLine("步骤6: 更新叠加管理器");
                await _overlayManager.UpdatePredictionLinesAsync(validPredictionLines);
                _overlayManager.UpdateConfiguration(config);

                // Step 7: 验证集成状态
                Console.WriteLine("步骤7: 验证集成状态");
                Assert.AreEqual(validPredictionLines.Count, _overlayManager.PredictionLineCount, "叠加管理器预测线数量应该正确");
                Assert.AreEqual(validPredictionLines.Count, _drawingService.CurrentPredictionLines.Count, "绘制服务预测线数量应该正确");
                Assert.AreEqual(validPredictionLines.Count, _predictionLineManager.Count, "预测线管理器数量应该正确");

                // Step 8: 获取完整状态报告
                Console.WriteLine("步骤8: 生成状态报告");
                var overlayManagerStatus = _overlayManager.GetServiceStatus();
                var drawingServiceStatus = _drawingService.GetServiceStatus();
                var performanceStats = _drawingService.GetPerformanceStats();

                Assert.IsNotNull(overlayManagerStatus, "叠加管理器状态不应为空");
                Assert.IsNotNull(drawingServiceStatus, "绘制服务状态不应为空");
                Assert.IsNotNull(performanceStats, "性能统计不应为空");

                Console.WriteLine($"集成测试完成！");
                Console.WriteLine($"- 预测线总数: {validPredictionLines.Count}");
                Console.WriteLine($"- 叠加管理器状态: {overlayManagerStatus["OverlayManagerStatus"]}");
                Console.WriteLine($"- 绘制服务状态: {drawingServiceStatus["IsDrawing"]}");
                Console.WriteLine($"- 服务可用性: {_drawingService.IsServiceAvailable()}");

                Console.WriteLine("完整集成流程测试通过");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"集成测试失败: {ex.Message}");
                Assert.Fail($"集成测试失败: {ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建测试预测线
        /// </summary>
        private List<PredictionLine> CreateTestPredictionLines()
        {
            return new List<PredictionLine>
            {
                PredictionLine.CreateStandard(0, "A线", 105.0, 100.0, 105.0, 0.0),
                PredictionLine.CreateStandard(1, "B线", 120.0, 100.0, 120.0, 0.0),
                PredictionLine.CreateStandard(3, "3线", 137.4, 100.0, 137.4, 3.137),
                PredictionLine.CreateAdvanced(4, "高级线", 150.0, 100.0, 150.0, 0.0)
            };
        }

        /// <summary>
        /// 创建测试K线数据
        /// </summary>
        private KLineInfo CreateTestKLineData()
        {
            return new KLineInfo
            {
                Open = 100.0,
                High = 105.0,
                Low = 98.0,
                Close = 103.0,
                Volume = 1000000,
                Timestamp = DateTime.Now
            };
        }

        #endregion
    }
}