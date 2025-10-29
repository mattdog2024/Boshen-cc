using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using BoshenCC.Core.Services;
using BoshenCC.Core.Utils;
using BoshenCC.Models;

namespace BoshenCC.Examples
{
    /// <summary>
    /// 波神算法演示程序
    /// 展示如何使用波神11线算法进行预测线计算
    /// </summary>
    public class BoshenAlgorithmDemo
    {
        private readonly BoshenAlgorithmService _boshenService;

        public BoshenAlgorithmDemo()
        {
            _boshenService = new BoshenAlgorithmService();
        }

        /// <summary>
        /// 运行基本演示
        /// </summary>
        public async Task RunBasicDemo()
        {
            Console.WriteLine("=== 波神11线算法演示 ===\n");

            // 演示1：基于价格计算
            await DemonstratePriceBasedCalculation();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // 演示2：基于K线计算
            await DemonstrateKLineBasedCalculation();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // 演示3：标准测试案例
            await DemonstrateStandardTestCases();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // 演示4：预测线查询功能
            await DemonstrateLineQueryFeatures();

            Console.WriteLine("\n=== 演示完成 ===");
        }

        /// <summary>
        /// 演示基于价格的计算
        /// </summary>
        private async Task DemonstratePriceBasedCalculation()
        {
            Console.WriteLine("1. 基于价格计算预测线");
            Console.WriteLine($"输入：A点=100.50, B点=105.80 (涨幅=5.30)");

            try
            {
                // 计算预测线
                var lines = await _boshenService.CalculateBoshenLinesAsync(100.50, 105.80, "DEMO001", "1H");

                Console.WriteLine($"成功计算 {lines.Count} 条预测线：");

                // 显示所有预测线
                foreach (var line in lines)
                {
                    var marker = line.IsKeyLine ? "★" : " ";
                    Console.WriteLine($"{marker} {line.Name}: {line.Price:F2} (比例: {line.BoshenRatio:F3})");
                }

                // 显示分析摘要
                var summary = _boshenService.GetBoshenAnalysisSummary(CreateTestKLine(100.50, 105.80));
                Console.WriteLine($"\n分析摘要：\n{summary}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"计算失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 演示基于K线的计算
        /// </summary>
        private async Task DemonstrateKLineBasedCalculation()
        {
            Console.WriteLine("2. 基于K线数据计算预测线");

            // 创建测试K线
            var kline = new KLineInfo
            {
                LowPrice = 98.15,
                HighPrice = 101.35,
                OpenPrice = 98.50,
                ClosePrice = 100.80,
                Symbol = "AAPL",
                TimeFrame = "1D",
                Timestamp = DateTime.Now,
                Index = 10,
                Bounds = new Rectangle(100, 200, 30, 60),
                Color = KLineColor.Green,
                Structure = new KLineStructure
                {
                    PatternType = KLinePatternType.BullishEngulfing,
                    ClarityScore = 0.85
                }
            };

            Console.WriteLine($"K线信息：{kline.Symbol} {kline.TimeFrame}");
            Console.WriteLine($"价格范围：{kline.LowPrice:F2} - {kline.HighPrice:F2}");
            Console.WriteLine($"形态：{kline.PatternType}");

            try
            {
                var lines = await _boshenService.CalculateBoshenLinesAsync(kline);

                Console.WriteLine($"\n计算结果 ({lines.Count} 条预测线)：");

                // 显示重点线
                var keyLines = lines.Where(l => l.IsKeyLine).ToList();
                Console.WriteLine("重点线位：");
                foreach (var keyLine in keyLines)
                {
                    Console.WriteLine($"  {keyLine.Name}: {keyLine.Price:F2}");
                }

                // 显示极线
                var extremeLine = lines.Last();
                Console.WriteLine($"极线: {extremeLine.Name}: {extremeLine.Price:F2}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"计算失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 演示标准测试案例
        /// </summary>
        private async Task DemonstrateStandardTestCases()
        {
            Console.WriteLine("3. 标准测试案例验证");

            try
            {
                // 运行标准测试案例
                var testResult = await _boshenService.RunStandardTestCasesAsync();

                Console.WriteLine($"测试结果：{(testResult.IsValid ? "✅ 通过" : "❌ 失败")}");
                Console.WriteLine($"最大误差：{testResult.MaxError:F4}");
                Console.WriteLine($"平均误差：{testResult.AverageError:F4}");
                Console.WriteLine($"精度要求：{(testResult.MeetsAccuracyRequirement ? "✅ 满足" : "❌ 不满足")} (误差<0.1%)");

                Console.WriteLine("\n各测试案例详情：");
                foreach (var caseResult in testResult.TestCaseResults)
                {
                    var status = caseResult.IsValid ? "✅" : "❌";
                    Console.WriteLine($"  {status} {caseResult.TestCaseName}");
                    if (!caseResult.IsValid)
                    {
                        Console.WriteLine($"    错误：{caseResult.ErrorMessage}");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 演示预测线查询功能
        /// </summary>
        private async Task DemonstrateLineQueryFeatures()
        {
            Console.WriteLine("4. 预测线查询功能演示");

            try
            {
                // 创建一些预测线
                var lines = await _boshenService.CalculateBoshenLinesAsync(100.0, 105.0, "DEMO002", "4H");

                Console.WriteLine($"创建了 {lines.Count} 条预测线");

                // 查询接近当前价格的预测线
                var currentPrice = 103.50;
                var nearbyLines = _boshenService.FindNearbyLines(lines, currentPrice, 0.5);

                Console.WriteLine($"\n当前价格：{currentPrice:F2}");
                Console.WriteLine("接近的预测线（容差0.5%）：");

                foreach (var line in nearbyLines)
                {
                    var distance = line.GetDistanceFromCurrentPrice(currentPrice);
                    var percentDistance = line.GetPercentageDistanceFromCurrentPrice(currentPrice);
                    Console.WriteLine($"  {line.Name}: {line.Price:F2} (距离: {distance:F2}, {percentDistance:F2}%)");
                }

                // 获取详细信息
                var details = _boshenService.GetPredictionLineDetails(lines);
                Console.WriteLine("\n预测线详细信息：");
                foreach (var detail in details.Take(5)) // 只显示前5条
                {
                    Console.WriteLine($"  {detail}");
                }

                if (details.Count > 5)
                {
                    Console.WriteLine($"  ... 还有 {details.Count - 5} 条预测线");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"查询失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 演示渲染功能
        /// </summary>
        public void DemonstrateRendering()
        {
            Console.WriteLine("\n=== 预测线渲染演示 ===");

            try
            {
                // 创建预测线
                var pointA = 100.0;
                var pointB = 105.0;
                var lines = BoshenCalculator.CalculateBoshenLinesAsync(pointA, pointB).Result;

                Console.WriteLine("生成的预览图样式：");

                // 默认样式预览
                var defaultPreview = PredictionRenderer.RenderPreview(lines, 800, 400);
                Console.WriteLine("✅ 默认样式预览图已生成");

                // 高对比度样式预览
                var highContrastOptions = PredictionRenderer.CreateHighContrastOptions();
                var highContrastPreview = PredictionRenderer.RenderPreview(lines, 800, 400, highContrastOptions);
                Console.WriteLine("✅ 高对比度样式预览图已生成");

                // 简约样式预览
                var minimalOptions = PredictionRenderer.CreateMinimalOptions();
                var minimalPreview = PredictionRenderer.RenderPreview(lines, 800, 400, minimalOptions);
                Console.WriteLine("✅ 简约样式预览图已生成");

                // 清理资源
                defaultPreview.Dispose();
                highContrastPreview.Dispose();
                minimalPreview.Dispose();

                Console.WriteLine("渲染演示完成");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"渲染失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 演示性能测试
        /// </summary>
        public async Task DemonstratePerformance()
        {
            Console.WriteLine("\n=== 性能测试演示 ===");

            try
            {
                // 批量计算测试
                var klines = new List<KLineInfo>();
                var random = new Random();

                for (int i = 0; i < 50; i++)
                {
                    var basePrice = random.NextDouble() * 500 + 50;
                    var range = random.NextDouble() * 20 + 2;
                    var kline = CreateTestKLine(basePrice, basePrice + range);
                    klines.Add(kline);
                }

                Console.WriteLine($"批量计算 {klines.Count} 组K线...");

                var startTime = DateTime.Now;
                var results = await _boshenService.CalculateBatchBoshenLinesAsync(klines);
                var endTime = DateTime.Now;

                var processingTime = (endTime - startTime).TotalMilliseconds;

                Console.WriteLine($"✅ 批量计算完成");
                Console.WriteLine($"处理时间：{processingTime:F2}ms");
                Console.WriteLine($"平均每组：{processingTime / klines.Count:F2}ms");

                // 缓存统计
                var cacheStats = _boshenService.GetCacheStatistics();
                Console.WriteLine($"缓存统计：大小={cacheStats.CacheSize}, 最大={cacheStats.MaxCacheSize}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"性能测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建测试K线
        /// </summary>
        private KLineInfo CreateTestKLine(double lowPrice, double highPrice)
        {
            return new KLineInfo
            {
                LowPrice = lowPrice,
                HighPrice = highPrice,
                OpenPrice = lowPrice + (highPrice - lowPrice) * 0.3,
                ClosePrice = lowPrice + (highPrice - lowPrice) * 0.7,
                Symbol = "TEST",
                TimeFrame = "1H",
                Timestamp = DateTime.Now,
                Index = 1,
                Bounds = new Rectangle(0, 0, 20, 40),
                Color = KLineColor.Green,
                Structure = new KLineStructure()
            };
        }

        /// <summary>
        /// 主程序入口
        /// </summary>
        public static async Task Main(string[] args)
        {
            var demo = new BoshenAlgorithmDemo();

            try
            {
                // 运行基本演示
                await demo.RunBasicDemo();

                // 运行渲染演示
                demo.DemonstrateRendering();

                // 运行性能测试
                await demo.DemonstratePerformance();

                Console.WriteLine("\n程序执行完成！按任意键退出...");
                Console.ReadKey();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"程序执行出错: {ex.Message}");
                Console.WriteLine($"详细信息: {ex}");
            }
        }
    }
}