using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoshenCC.Core.Utils;
using BoshenCC.Models;

namespace BoshenCC.Core.Services
{
    /// <summary>
    /// 波神算法服务
    /// 提供波神11线算法的核心功能，包括计算、验证、管理等
    /// </summary>
    public class BoshenAlgorithmService : IBoshenAlgorithmService
    {
        #region 私有字段

        private readonly Dictionary<string, List<PredictionLine>> _predictionCache;
        private readonly Dictionary<string, DateTime> _cacheTimestamps;
        private readonly object _cacheLock = new object();

        // 缓存配置
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
        private readonly int _maxCacheSize = 100;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化波神算法服务
        /// </summary>
        public BoshenAlgorithmService()
        {
            _predictionCache = new Dictionary<string, List<PredictionLine>>();
            _cacheTimestamps = new Dictionary<string, DateTime>();
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 基于K线信息计算波神11线
        /// </summary>
        /// <param name="kline">K线信息</param>
        /// <returns>预测线列表</returns>
        public async Task<List<PredictionLine>> CalculateBoshenLinesAsync(KLineInfo kline)
        {
            if (kline == null)
                throw new ArgumentNullException(nameof(kline));

            return await Task.Run(() =>
            {
                try
                {
                    // 验证K线数据
                    ValidateKLineData(kline);

                    // 生成缓存键
                    var cacheKey = GenerateCacheKey(kline);

                    // 检查缓存
                    lock (_cacheLock)
                    {
                        if (TryGetFromCache(cacheKey, out var cachedLines))
                        {
                            return cachedLines.Select(line => line.Clone()).ToList();
                        }
                    }

                    // 计算预测线
                    var lines = BoshenCalculator.CalculateBoshenLines(kline);

                    // 验证计算结果
                    var validationResult = BoshenCalculator.ValidateBoshenRules(lines);
                    if (!validationResult.IsValid)
                    {
                        throw new InvalidOperationException($"波神算法验证失败: {validationResult.ErrorMessage}");
                    }

                    // 更新缓存
                    lock (_cacheLock)
                    {
                        UpdateCache(cacheKey, lines);
                    }

                    return lines;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"计算波神11线失败: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// 基于指定的A点和B点计算波神11线
        /// </summary>
        /// <param name="pointAPrice">A点价格</param>
        /// <param name="pointBPrice">B点价格</param>
        /// <param name="symbol">交易品种</param>
        /// <param name="timeFrame">时间周期</param>
        /// <returns>预测线列表</returns>
        public async Task<List<PredictionLine>> CalculateBoshenLinesAsync(double pointAPrice, double pointBPrice,
            string symbol = null, string timeFrame = null)
        {
            if (pointAPrice <= 0 || pointBPrice <= 0)
                throw new ArgumentException("价格必须大于0");

            if (pointBPrice <= pointAPrice)
                throw new ArgumentException("B点价格必须大于A点价格");

            return await Task.Run(() =>
            {
                try
                {
                    // 创建虚拟K线信息用于计算
                    var virtualKLine = new KLineInfo
                    {
                        LowPrice = pointAPrice,
                        HighPrice = pointBPrice,
                        OpenPrice = pointAPrice,
                        ClosePrice = pointBPrice,
                        Symbol = symbol,
                        TimeFrame = timeFrame,
                        Timestamp = DateTime.Now,
                        Confidence = 0.8
                    };

                    return BoshenCalculator.CalculateBoshenLines(virtualKLine);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"计算波神11线失败: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// 批量计算多组K线的波神11线
        /// </summary>
        /// <param name="klines">K线列表</param>
        /// <returns>预测线组列表</returns>
        public async Task<List<List<PredictionLine>>> CalculateBatchBoshenLinesAsync(List<KLineInfo> klines)
        {
            if (klines == null || klines.Count == 0)
                return new List<List<PredictionLine>>();

            var results = new List<List<PredictionLine>>();

            foreach (var kline in klines)
            {
                try
                {
                    var lines = await CalculateBoshenLinesAsync(kline);
                    results.Add(lines);
                }
                catch (Exception ex)
                {
                    // 记录错误但继续处理其他K线
                    Console.WriteLine($"批量计算失败 - K线索引:{kline.Index}, 错误:{ex.Message}");
                }
            }

            return results;
        }

        /// <summary>
        /// 验证波神计算结果的准确性
        /// </summary>
        /// <param name="calculatedLines">计算出的预测线</param>
        /// <param name="expectedLines">期望的预测线</param>
        /// <returns>验证结果</returns>
        public BoshenValidationResult ValidateCalculation(List<PredictionLine> calculatedLines, List<PredictionLine> expectedLines)
        {
            return BoshenCalculator.ValidateCalculation(calculatedLines, expectedLines);
        }

        /// <summary>
        /// 检查当前价格是否接近预测线
        /// </summary>
        /// <param name="lines">预测线列表</param>
        /// <param name="currentPrice">当前价格</param>
        /// <param name="tolerancePercent">容差百分比</param>
        /// <returns>接近的预测线列表</returns>
        public List<PredictionLine> FindNearbyLines(List<PredictionLine> lines, double currentPrice, double tolerancePercent = 0.1)
        {
            return BoshenCalculator.FindNearbyLines(lines, currentPrice, tolerancePercent);
        }

        /// <summary>
        /// 获取波神线分析摘要
        /// </summary>
        /// <param name="kline">K线信息</param>
        /// <returns>分析摘要</returns>
        public string GetBoshenAnalysisSummary(KLineInfo kline)
        {
            return BoshenCalculator.GetBoshenSummary(kline);
        }

        /// <summary>
        /// 获取预测线的详细信息
        /// </summary>
        /// <param name="lines">预测线列表</param>
        /// <returns>详细信息列表</returns>
        public List<string> GetPredictionLineDetails(List<PredictionLine> lines)
        {
            if (lines == null || lines.Count == 0)
                return new List<string>();

            return lines.Select(line =>
                $"{line.Name}: {line.Price:F2} (比率: {line.BoshenRatio:F3}, {(line.IsKeyLine ? "重点线" : "普通线")})")
                       .ToList();
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _predictionCache.Clear();
                _cacheTimestamps.Clear();
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计</returns>
        public BoshenCacheStatistics GetCacheStatistics()
        {
            lock (_cacheLock)
            {
                return new BoshenCacheStatistics
                {
                    CacheSize = _predictionCache.Count,
                    MaxCacheSize = _maxCacheSize,
                    ExpiredEntries = _cacheTimestamps.Count(kv =>
                        DateTime.Now - kv.Value > _cacheExpiry)
                };
            }
        }

        #endregion

        #region 测试和验证方法

        /// <summary>
        /// 运行标准测试案例验证算法精度
        /// </summary>
        /// <returns>测试结果</returns>
        public async Task<BoshenTestResult> RunStandardTestCasesAsync()
        {
            var testResults = new List<BoshenTestCaseResult>();

            // 测试案例1
            try
            {
                var testCase1Expected = BoshenCalculator.GetTestCase1();
                var testCase1Calculated = await CalculateBoshenLinesAsync(
                    testCase1Expected.First().PointAPrice,
                    testCase1Expected.First().PointBPrice);

                var validation1 = ValidateCalculation(testCase1Calculated, testCase1Expected);
                testResults.Add(new BoshenTestCaseResult
                {
                    TestCaseName = "测试案例1 (A=98.02, B=98.75)",
                    IsValid = validation1.IsValid,
                    MaxError = validation1.MaxError,
                    AverageError = validation1.AverageError,
                    ErrorMessage = validation1.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                testResults.Add(new BoshenTestCaseResult
                {
                    TestCaseName = "测试案例1 (A=98.02, B=98.75)",
                    IsValid = false,
                    ErrorMessage = ex.Message
                });
            }

            // 测试案例2
            try
            {
                var testCase2Expected = BoshenCalculator.GetTestCase2();
                var testCase2Calculated = await CalculateBoshenLinesAsync(
                    testCase2Expected.First().PointAPrice,
                    testCase2Expected.First().PointBPrice);

                var validation2 = ValidateCalculation(testCase2Calculated, testCase2Expected);
                testResults.Add(new BoshenTestCaseResult
                {
                    TestCaseName = "测试案例2 (A=96.25, B=97.06)",
                    IsValid = validation2.IsValid,
                    MaxError = validation2.MaxError,
                    AverageError = validation2.AverageError,
                    ErrorMessage = validation2.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                testResults.Add(new BoshenTestCaseResult
                {
                    TestCaseName = "测试案例2 (A=96.25, B=97.06)",
                    IsValid = false,
                    ErrorMessage = ex.Message
                });
            }

            // 计算总体结果
            var overallValid = testResults.All(r => r.IsValid);
            var overallMaxError = testResults.Where(r => r.MaxError > 0).Select(r => r.MaxError).DefaultIfEmpty(0).Max();
            var overallAvgError = testResults.Where(r => r.AverageError > 0).Select(r => r.AverageError).DefaultIfEmpty(0).Average();

            return new BoshenTestResult
            {
                IsValid = overallValid,
                MaxError = overallMaxError,
                AverageError = overallAvgError,
                TestCaseResults = testResults,
                TestTime = DateTime.Now,
                MeetsAccuracyRequirement = overallMaxError < 0.1 // 误差小于0.1%
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 验证K线数据
        /// </summary>
        /// <param name="kline">K线信息</param>
        private void ValidateKLineData(KLineInfo kline)
        {
            if (kline.HighPrice <= 0 || kline.LowPrice <= 0)
                throw new ArgumentException("K线价格数据无效");

            if (kline.HighPrice <= kline.LowPrice)
                throw new ArgumentException("最高价必须大于最低价");

            if (kline.PriceRange <= 0)
                throw new ArgumentException("价格区间必须大于0");
        }

        /// <summary>
        /// 生成缓存键
        /// </summary>
        /// <param name="kline">K线信息</param>
        /// <returns>缓存键</returns>
        private string GenerateCacheKey(KLineInfo kline)
        {
            var keyData = $"{kline.LowPrice:F6}_{kline.HighPrice:F6}_{kline.Symbol}_{kline.TimeFrame}";
            return $"boshen_{keyData.GetHashCode():X}";
        }

        /// <summary>
        /// 尝试从缓存获取数据
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="lines">输出的预测线</param>
        /// <returns>是否找到有效缓存</returns>
        private bool TryGetFromCache(string cacheKey, out List<PredictionLine> lines)
        {
            lines = null;

            if (_predictionCache.TryGetValue(cacheKey, out var cachedLines) &&
                _cacheTimestamps.TryGetValue(cacheKey, out var timestamp))
            {
                // 检查是否过期
                if (DateTime.Now - timestamp < _cacheExpiry)
                {
                    lines = cachedLines;
                    return true;
                }
                else
                {
                    // 清理过期缓存
                    _predictionCache.Remove(cacheKey);
                    _cacheTimestamps.Remove(cacheKey);
                }
            }

            return false;
        }

        /// <summary>
        /// 更新缓存
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="lines">预测线列表</param>
        private void UpdateCache(string cacheKey, List<PredictionLine> lines)
        {
            // 检查缓存大小限制
            if (_predictionCache.Count >= _maxCacheSize)
            {
                // 清理最旧的缓存项
                var oldestKey = _cacheTimestamps.OrderBy(kv => kv.Value).First().Key;
                _predictionCache.Remove(oldestKey);
                _cacheTimestamps.Remove(oldestKey);
            }

            _predictionCache[cacheKey] = lines.Select(line => line.Clone()).ToList();
            _cacheTimestamps[cacheKey] = DateTime.Now;
        }

        #endregion
    }

    #region 接口定义

    /// <summary>
    /// 波神算法服务接口
    /// </summary>
    public interface IBoshenAlgorithmService
    {
        /// <summary>
        /// 基于K线信息计算波神11线
        /// </summary>
        Task<List<PredictionLine>> CalculateBoshenLinesAsync(KLineInfo kline);

        /// <summary>
        /// 基于指定的A点和B点计算波神11线
        /// </summary>
        Task<List<PredictionLine>> CalculateBoshenLinesAsync(double pointAPrice, double pointBPrice,
            string symbol = null, string timeFrame = null);

        /// <summary>
        /// 批量计算多组K线的波神11线
        /// </summary>
        Task<List<List<PredictionLine>>> CalculateBatchBoshenLinesAsync(List<KLineInfo> klines);

        /// <summary>
        /// 验证波神计算结果的准确性
        /// </summary>
        BoshenValidationResult ValidateCalculation(List<PredictionLine> calculatedLines, List<PredictionLine> expectedLines);

        /// <summary>
        /// 检查当前价格是否接近预测线
        /// </summary>
        List<PredictionLine> FindNearbyLines(List<PredictionLine> lines, double currentPrice, double tolerancePercent = 0.1);

        /// <summary>
        /// 获取波神线分析摘要
        /// </summary>
        string GetBoshenAnalysisSummary(KLineInfo kline);

        /// <summary>
        /// 获取预测线的详细信息
        /// </summary>
        List<string> GetPredictionLineDetails(List<PredictionLine> lines);

        /// <summary>
        /// 运行标准测试案例验证算法精度
        /// </summary>
        Task<BoshenTestResult> RunStandardTestCasesAsync();

        /// <summary>
        /// 清空缓存
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        BoshenCacheStatistics GetCacheStatistics();
    }

    #endregion

    #region 数据模型

    /// <summary>
    /// 波神算法缓存统计信息
    /// </summary>
    public class BoshenCacheStatistics
    {
        /// <summary>
        /// 当前缓存大小
        /// </summary>
        public int CacheSize { get; set; }

        /// <summary>
        /// 最大缓存大小
        /// </summary>
        public int MaxCacheSize { get; set; }

        /// <summary>
        /// 过期条目数量
        /// </summary>
        public int ExpiredEntries { get; set; }

        /// <summary>
        /// 缓存命中率（需要额外统计）
        /// </summary>
        public double HitRate { get; set; }
    }

    /// <summary>
    /// 波神算法测试结果
    /// </summary>
    public class BoshenTestResult
    {
        /// <summary>
        /// 是否通过所有测试
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 最大误差
        /// </summary>
        public double MaxError { get; set; }

        /// <summary>
        /// 平均误差
        /// </summary>
        public double AverageError { get; set; }

        /// <summary>
        /// 是否满足精度要求（误差<0.1%）
        /// </summary>
        public bool MeetsAccuracyRequirement { get; set; }

        /// <summary>
        /// 测试时间
        /// </summary>
        public DateTime TestTime { get; set; }

        /// <summary>
        /// 各测试案例结果
        /// </summary>
        public List<BoshenTestCaseResult> TestCaseResults { get; set; } = new List<BoshenTestCaseResult>();

        /// <summary>
        /// 获取测试结果摘要
        /// </summary>
        /// <returns>摘要文本</returns>
        public string GetSummary()
        {
            var summary = $"波神算法测试结果:\n";
            summary += $"总体结果: {(IsValid ? "通过" : "失败")}\n";
            summary += $"最大误差: {MaxError:F4}\n";
            summary += $"平均误差: {AverageError:F4}\n";
            summary += $"精度要求: {(MeetsAccuracyRequirement ? "满足" : "不满足")} (误差<0.1%)\n";
            summary += $"测试案例数: {TestCaseResults.Count}\n";

            foreach (var result in TestCaseResults)
            {
                summary += $"  {result.TestCaseName}: {(result.IsValid ? "通过" : "失败")}";
                if (!result.IsValid && !string.IsNullOrEmpty(result.ErrorMessage))
                    summary += $" - {result.ErrorMessage}";
                summary += "\n";
            }

            return summary;
        }
    }

    /// <summary>
    /// 波神算法测试案例结果
    /// </summary>
    public class BoshenTestCaseResult
    {
        /// <summary>
        /// 测试案例名称
        /// </summary>
        public string TestCaseName { get; set; }

        /// <summary>
        /// 是否通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 最大误差
        /// </summary>
        public double MaxError { get; set; }

        /// <summary>
        /// 平均误差
        /// </summary>
        public double AverageError { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    #endregion
}