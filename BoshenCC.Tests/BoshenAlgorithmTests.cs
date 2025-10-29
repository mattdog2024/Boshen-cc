using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BoshenCC.Core.Services;
using BoshenCC.Core.Utils;
using BoshenCC.Models;

namespace BoshenCC.Tests
{
    /// <summary>
    /// 波神算法验证测试
    /// 确保算法精度与原版软件误差<0.1%
    /// </summary>
    [TestClass]
    public class BoshenAlgorithmTests
    {
        private BoshenAlgorithmService _boshenService;

        #region 测试初始化

        [TestInitialize]
        public void Setup()
        {
            _boshenService = new BoshenAlgorithmService();
        }

        #endregion

        #region 标准测试案例验证

        /// <summary>
        /// 测试案例1验证
        /// A=98.02, B=98.75, AB涨幅=0.73
        /// </summary>
        [TestMethod]
        public async Task TestBoshenCalculation_TestCase1()
        {
            // Arrange
            var pointA = 98.02;
            var pointB = 98.75;
            var expectedLines = BoshenCalculator.GetTestCase1();

            // Act
            var calculatedLines = await _boshenService.CalculateBoshenLinesAsync(pointA, pointB);

            // Assert
            Assert.IsNotNull(calculatedLines, "计算结果不能为空");
            Assert.AreEqual(11, calculatedLines.Count, "预测线数量必须为11");

            // 验证每条线的精度
            for (int i = 0; i < expectedLines.Count; i++)
            {
                var expected = expectedLines[i];
                var calculated = calculatedLines[i];

                Assert.AreEqual(expected.Name, calculated.Name, $"第{i}线名称不匹配");
                Assert.AreEqual(expected.Price, calculated.Price, 0.01, $"第{i}线价格误差超过0.01");
                Assert.AreEqual(expected.BoshenRatio, calculated.BoshenRatio, 0.001, $"第{i}线比例误差超过0.001");
            }

            // 验证整体精度
            var validationResult = _boshenService.ValidateCalculation(calculatedLines, expectedLines);
            Assert.IsTrue(validationResult.IsValid, $"计算验证失败: {validationResult.ErrorMessage}");
            Assert.IsTrue(validationResult.MaxError < 0.1, $"最大误差超过0.1%: {validationResult.MaxError}");
        }

        /// <summary>
        /// 测试案例2验证
        /// A=96.25, B=97.06, AB涨幅=0.81
        /// </summary>
        [TestMethod]
        public async Task TestBoshenCalculation_TestCase2()
        {
            // Arrange
            var pointA = 96.25;
            var pointB = 97.06;
            var expectedLines = BoshenCalculator.GetTestCase2();

            // Act
            var calculatedLines = await _boshenService.CalculateBoshenLinesAsync(pointA, pointB);

            // Assert
            Assert.IsNotNull(calculatedLines, "计算结果不能为空");
            Assert.AreEqual(11, calculatedLines.Count, "预测线数量必须为11");

            // 验证每条线的精度
            for (int i = 0; i < expectedLines.Count; i++)
            {
                var expected = expectedLines[i];
                var calculated = calculatedLines[i];

                Assert.AreEqual(expected.Name, calculated.Name, $"第{i}线名称不匹配");
                Assert.AreEqual(expected.Price, calculated.Price, 0.01, $"第{i}线价格误差超过0.01");
                Assert.AreEqual(expected.BoshenRatio, calculated.BoshenRatio, 0.001, $"第{i}线比例误差超过0.001");
            }

            // 验证整体精度
            var validationResult = _boshenService.ValidateCalculation(calculatedLines, expectedLines);
            Assert.IsTrue(validationResult.IsValid, $"计算验证失败: {validationResult.ErrorMessage}");
            Assert.IsTrue(validationResult.MaxError < 0.1, $"最大误差超过0.1%: {validationResult.MaxError}");
        }

        #endregion

        #region 算法规则验证

        /// <summary>
        /// 验证波神11线基本规则
        /// </summary>
        [TestMethod]
        public async Task TestBoshenRules()
        {
            // Arrange
            var kline = CreateTestKLine(100.0, 105.0);

            // Act
            var lines = await _boshenService.CalculateBoshenLinesAsync(kline);

            // Assert
            var validationResult = BoshenCalculator.ValidateBoshenRules(lines);
            Assert.IsTrue(validationResult.IsValid, $"波神规则验证失败: {validationResult.ErrorMessage}");

            // 验证A线和B线
            var aLine = lines.First(l => l.Index == 0);
            var bLine = lines.First(l => l.Index == 1);
            Assert.AreEqual(kline.LowPrice, aLine.Price, 0.001, "A线价格必须等于K线最低价");
            Assert.AreEqual(kline.HighPrice, bLine.Price, 0.001, "B线价格必须等于K线最高价");

            // 验证价格递增
            for (int i = 0; i < lines.Count - 1; i++)
            {
                Assert.IsTrue(lines[i].Price < lines[i + 1].Price,
                    $"预测线价格必须递增: {lines[i].Name}({lines[i].Price:F2}) < {lines[i + 1].Name}({lines[i + 1].Price:F2})");
            }

            // 验证所有预测线都在B点之上（除了A线）
            foreach (var line in lines.Where(l => l.Index > 1))
            {
                Assert.IsTrue(line.Price > kline.HighPrice,
                    $"{line.Name} 价格({line.Price:F2}) 必须在B点({kline.HighPrice:F2})之上");
            }

            // 验证重点线标记
            var keyLines = lines.Where(l => l.IsKeyLine).ToList();
            Assert.AreEqual(3, keyLines.Count, "必须有3条重点线");
            Assert.IsTrue(keyLines.Any(l => l.Index == 3), "3线必须是重点线");
            Assert.IsTrue(keyLines.Any(l => l.Index == 6), "6线必须是重点线");
            Assert.IsTrue(keyLines.Any(l => l.Index == 8), "8线必须是重点线");
        }

        /// <summary>
        /// 验证波神比例序列
        /// </summary>
        [TestMethod]
        public void TestBoshenRatios()
        {
            // Arrange
            var expectedRatios = new[]
            {
                0.0,   // A线
                1.0,   // B线
                1.849, // 1线
                2.397, // 2线
                3.137, // 3线
                3.401, // 4线
                4.000, // 5线
                4.726, // 6线
                5.247, // 7线
                6.027, // 8线
                6.808  // 极线
            };

            // Act & Assert
            CollectionAssert.AreEqual(expectedRatios, BoshenCalculator.BOSHEN_RATIOS,
                "波神比例序列不匹配");
        }

        #endregion

        #region 边界条件测试

        /// <summary>
        /// 测试小价格区间的计算
        /// </summary>
        [TestMethod]
        public async Task TestSmallPriceRange()
        {
            // Arrange
            var pointA = 1.00;
            var pointB = 1.05; // 只有0.05的涨幅

            // Act
            var lines = await _boshenService.CalculateBoshenLinesAsync(pointA, pointB);

            // Assert
            Assert.IsNotNull(lines);
            Assert.AreEqual(11, lines.Count);

            // 验证A线和B线
            Assert.AreEqual(pointA, lines[0].Price, 0.001);
            Assert.AreEqual(pointB, lines[1].Price, 0.001);

            // 验证价格递增
            for (int i = 0; i < lines.Count - 1; i++)
            {
                Assert.IsTrue(lines[i].Price < lines[i + 1].Price);
            }
        }

        /// <summary>
        /// 测试大价格区间的计算
        /// </summary>
        [TestMethod]
        public async Task TestLargePriceRange()
        {
            // Arrange
            var pointA = 100.0;
            var pointB = 500.0; // 400的涨幅

            // Act
            var lines = await _boshenService.CalculateBoshenLinesAsync(pointA, pointB);

            // Assert
            Assert.IsNotNull(lines);
            Assert.AreEqual(11, lines.Count);

            // 验证A线和B线
            Assert.AreEqual(pointA, lines[0].Price, 0.001);
            Assert.AreEqual(pointB, lines[1].Price, 0.001);

            // 验证极线价格合理性
            var extremeLine = lines.Last();
            Assert.IsTrue(extremeLine.Price > pointB * 2, "极线价格应该超过B点的2倍");
        }

        #endregion

        #region 性能测试

        /// <summary>
        /// 批量计算性能测试
        /// </summary>
        [TestMethod]
        public async Task TestBatchCalculationPerformance()
        {
            // Arrange
            var klines = new List<KLineInfo>();
            var random = new Random();

            for (int i = 0; i < 100; i++)
            {
                var basePrice = random.NextDouble() * 1000 + 50;
                var range = random.NextDouble() * 50 + 5;
                var kline = CreateTestKLine(basePrice, basePrice + range);
                klines.Add(kline);
            }

            // Act
            var startTime = DateTime.Now;
            var results = await _boshenService.CalculateBatchBoshenLinesAsync(klines);
            var endTime = DateTime.Now;

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(klines.Count, results.Count);

            var processingTime = (endTime - startTime).TotalMilliseconds;
            Assert.IsTrue(processingTime < 5000, $"批量计算耗时过长: {processingTime}ms");

            // 验证每个结果
            foreach (var lines in results)
            {
                Assert.AreEqual(11, lines.Count);
                var validationResult = BoshenCalculator.ValidateBoshenRules(lines);
                Assert.IsTrue(validationResult.IsValid);
            }

            Console.WriteLine($"批量计算{ klines.Count }组K线耗时: {processingTime:F2}ms");
        }

        #endregion

        #region 精度验证测试

        /// <summary>
        /// 运行所有标准测试案例
        /// </summary>
        [TestMethod]
        public async Task RunAllStandardTestCases()
        {
            // Act
            var testResult = await _boshenService.RunStandardTestCasesAsync();

            // Assert
            Assert.IsNotNull(testResult, "测试结果不能为空");
            Assert.IsTrue(testResult.IsValid, $"标准测试案例失败: {testResult.GetSummary()}");
            Assert.IsTrue(testResult.MeetsAccuracyRequirement,
                $"精度要求不满足，最大误差: {testResult.MaxError:F4}");

            Console.WriteLine(testResult.GetSummary());

            // 验证每个测试案例
            foreach (var caseResult in testResult.TestCaseResults)
            {
                Assert.IsTrue(caseResult.IsValid,
                    $"测试案例失败: {caseResult.TestCaseName}, 错误: {caseResult.ErrorMessage}");
            }
        }

        /// <summary>
        /// 验证计算精度稳定性
        /// </summary>
        [TestMethod]
        public async Task TestCalculationStability()
        {
            // Arrange
            var pointA = 100.0;
            var pointB = 105.0;
            var iterations = 100;

            var results = new List<List<PredictionLine>>();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var lines = await _boshenService.CalculateBoshenLinesAsync(pointA, pointB);
                results.Add(lines);
            }

            // Assert
            var firstResult = results.First();

            for (int i = 1; i < results.Count; i++)
            {
                var currentResult = results[i];

                for (int j = 0; j < firstResult.Count; j++)
                {
                    Assert.AreEqual(firstResult[j].Price, currentResult[j].Price, 0.000001,
                        $"第{j}线在第{i + 1}次计算中结果不一致");
                }
            }

            Console.WriteLine($"计算稳定性测试通过，{iterations}次计算结果完全一致");
        }

        #endregion

        #region 异常情况测试

        /// <summary>
        /// 测试无效输入参数
        /// </summary>
        [TestMethod]
        public async Task TestInvalidInputs()
        {
            // 测试相等的价格
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _boshenService.CalculateBoshenLinesAsync(100.0, 100.0),
                "相等的价格应该抛出异常");

            // 测试B点小于A点
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _boshenService.CalculateBoshenLinesAsync(105.0, 100.0),
                "B点小于A点应该抛出异常");

            // 测试负价格
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _boshenService.CalculateBoshenLinesAsync(-10.0, 100.0),
                "负价格应该抛出异常");

            // 测试零价格
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _boshenService.CalculateBoshenLinesAsync(0.0, 100.0),
                "零价格应该抛出异常");
        }

        #endregion

        #region 实用工具方法

        /// <summary>
        /// 创建测试用K线
        /// </summary>
        private KLineInfo CreateTestKLine(double lowPrice, double highPrice)
        {
            return new KLineInfo
            {
                LowPrice = lowPrice,
                HighPrice = highPrice,
                OpenPrice = lowPrice,
                ClosePrice = highPrice,
                Symbol = "TEST",
                TimeFrame = "1D",
                Timestamp = DateTime.Now,
                Index = 1,
                Bounds = new System.Drawing.Rectangle(0, 0, 10, 10),
                Color = KLineColor.Green,
                Structure = new KLineStructure()
            };
        }

        #endregion
    }

    /// <summary>
    /// 波神算法集成测试
    /// 测试完整的工作流程
    /// </summary>
    [TestClass]
    public class BoshenAlgorithmIntegrationTests
    {
        private BoshenAlgorithmService _boshenService;

        [TestInitialize]
        public void Setup()
        {
            _boshenService = new BoshenAlgorithmService();
        }

        /// <summary>
        /// 完整工作流程测试
        /// </summary>
        [TestMethod]
        public async Task TestCompleteWorkflow()
        {
            // 1. 创建K线数据
            var kline = new KLineInfo
            {
                LowPrice = 98.02,
                HighPrice = 98.75,
                OpenPrice = 98.10,
                ClosePrice = 98.65,
                Symbol = "TEST001",
                TimeFrame = "1H",
                Timestamp = DateTime.Now,
                Index = 5,
                Bounds = new System.Drawing.Rectangle(100, 200, 20, 50),
                Color = KLineColor.Red,
                Structure = new KLineStructure
                {
                    PatternType = KLinePatternType.BullishEngulfing,
                    ClarityScore = 0.9
                }
            };

            // 2. 计算预测线
            var lines = await _boshenService.CalculateBoshenLinesAsync(kline);

            // 3. 验证结果
            Assert.IsNotNull(lines);
            Assert.AreEqual(11, lines.Count);

            // 4. 获取分析摘要
            var summary = _boshenService.GetBoshenAnalysisSummary(kline);
            Assert.IsNotNull(summary);
            Assert.IsTrue(summary.Contains("波神11线分析结果"));

            // 5. 查找接近的预测线
            var currentPrice = 100.31;
            var nearbyLines = _boshenService.FindNearbyLines(lines, currentPrice, 0.5);
            Assert.IsTrue(nearbyLines.Any());

            // 6. 获取详细信息
            var details = _boshenService.GetPredictionLineDetails(lines);
            Assert.IsNotNull(details);
            Assert.AreEqual(11, details.Count);

            Console.WriteLine("完整工作流程测试通过");
            Console.WriteLine(summary);
            Console.WriteLine($"接近价格 {currentPrice} 的预测线:");
            foreach (var line in nearbyLines)
            {
                Console.WriteLine($"  {line.Name}: {line.Price:F2}");
            }
        }

        /// <summary>
        /// 缓存功能测试
        /// </summary>
        [TestMethod]
        public async Task TestCachingFunctionality()
        {
            // Arrange
            var pointA = 100.0;
            var pointB = 105.0;

            // Act - 第一次计算
            var startTime1 = DateTime.Now;
            var lines1 = await _boshenService.CalculateBoshenLinesAsync(pointA, pointB);
            var endTime1 = DateTime.Now;

            // Act - 第二次计算（应该使用缓存）
            var startTime2 = DateTime.Now;
            var lines2 = await _boshenService.CalculateBoshenLinesAsync(pointA, pointB);
            var endTime2 = DateTime.Now;

            // Assert
            Assert.IsNotNull(lines1);
            Assert.IsNotNull(lines2);
            Assert.AreEqual(lines1.Count, lines2.Count);

            // 验证结果一致性
            for (int i = 0; i < lines1.Count; i++)
            {
                Assert.AreEqual(lines1[i].Price, lines2[i].Price, 0.000001);
            }

            // 第二次计算应该更快（使用缓存）
            var time1 = (endTime1 - startTime1).TotalMilliseconds;
            var time2 = (endTime2 - startTime2).TotalMilliseconds;

            Console.WriteLine($"第一次计算耗时: {time1:F2}ms");
            Console.WriteLine($"第二次计算耗时: {time2:F2}ms");

            // 获取缓存统计
            var cacheStats = _boshenService.GetCacheStatistics();
            Assert.IsNotNull(cacheStats);
            Assert.IsTrue(cacheStats.CacheSize > 0);

            Console.WriteLine($"缓存统计: 大小={cacheStats.CacheSize}, 最大={cacheStats.MaxCacheSize}");
        }
    }
}