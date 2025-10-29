using System;
using System.Collections.Generic;
using System.Linq;
using BoshenCC.Models;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 波神计算器
    /// 实现波神11线精确算法，基于K线数据计算预测线
    /// </summary>
    public static class BoshenCalculator
    {
        #region 常量定义

        /// <summary>
        /// 波神11线比例序列 - 基于原版软件精确计算
        /// 顺序：A线、B线、1线、2线、3线、4线、5线、6线、7线、8线、极线
        /// </summary>
        public static readonly double[] BOSHEN_RATIOS = {
            0.0,   // A线
            1.0,   // B线
            1.849, // 1线 (1 + 0.849)
            2.397, // 2线 (1 + 1.397)
            3.137, // 3线 (1 + 2.137)
            3.401, // 4线 (1 + 2.401)
            4.000, // 5线 (1 + 3.000)
            4.726, // 6线 (1 + 3.726)
            5.247, // 7线 (1 + 4.247)
            6.027, // 8线 (1 + 5.027)
            6.808  // 极线 (1 + 5.808)
        };

        /// <summary>
        /// 波神11线名称序列
        /// </summary>
        public static readonly string[] LINE_NAMES = {
            "A线", "B线", "1线", "2线", "3线", "4线", "5线", "6线", "7线", "8线", "极线"
        };

        /// <summary>
        /// 重点线索引（3线、6线、8线）
        /// </summary>
        public static readonly int[] KEY_LINE_INDICES = { 3, 6, 8 };

        /// <summary>
        /// 计算精度（小数位数）
        /// </summary>
        public const int CALCULATION_PRECISION = 2;

        /// <summary>
        /// 价格容差
        /// </summary>
        public const double PRICE_TOLERANCE = 0.01;

        #endregion

        #region 核心计算方法

        /// <summary>
        /// 计算波神11线
        /// 基于K线数据计算11条预测线
        /// </summary>
        /// <param name="kline">K线信息</param>
        /// <returns>预测线列表</returns>
        public static List<PredictionLine> CalculateBoshenLines(KLineInfo kline)
        {
            if (kline == null)
                throw new ArgumentNullException(nameof(kline), "K线信息不能为空");

            if (kline.HighPrice <= 0 || kline.LowPrice <= 0)
                throw new ArgumentException("K线价格数据无效", nameof(kline));

            if (kline.HighPrice <= kline.LowPrice)
                throw new ArgumentException("最高价必须大于最低价", nameof(kline));

            var pointA = kline.LowPrice;   // A点 = 最低价
            var pointB = kline.HighPrice;  // B点 = 最高价
            var abRange = pointB - pointA; // AB涨幅

            var lines = new List<PredictionLine>();
            var groupId = Guid.NewGuid().ToString("N")[..8];

            for (int i = 0; i < BOSHEN_RATIOS.Length; i++)
            {
                var line = CalculateSingleLine(i, pointA, pointB, abRange, kline, groupId);
                lines.Add(line);
            }

            return lines;
        }

        /// <summary>
        /// 基于指定的A点和B点计算波神11线
        /// </summary>
        /// <param name="pointAPrice">A点价格（最低价）</param>
        /// <param name="pointBPrice">B点价格（最高价）</param>
        /// <param name="sourceKLine">源K线信息（可选）</param>
        /// <returns>预测线列表</returns>
        public static List<PredictionLine> CalculateBoshenLines(double pointAPrice, double pointBPrice, KLineInfo sourceKLine = null)
        {
            if (pointAPrice <= 0 || pointBPrice <= 0)
                throw new ArgumentException("价格必须大于0");

            if (pointBPrice <= pointAPrice)
                throw new ArgumentException("B点价格必须大于A点价格");

            var abRange = pointBPrice - pointAPrice;
            var lines = new List<PredictionLine>();
            var groupId = Guid.NewGuid().ToString("N")[..8];

            for (int i = 0; i < BOSHEN_RATIOS.Length; i++)
            {
                var line = CalculateSingleLine(i, pointAPrice, pointBPrice, abRange, sourceKLine, groupId);
                lines.Add(line);
            }

            return lines;
        }

        /// <summary>
        /// 计算单条预测线
        /// </summary>
        /// <param name="lineIndex">线索引</param>
        /// <param name="pointAPrice">A点价格</param>
        /// <param name="pointBPrice">B点价格</param>
        /// <param name="abRange">AB涨幅</param>
        /// <param name="sourceKLine">源K线</param>
        /// <param name="groupId">组ID</param>
        /// <returns>预测线</returns>
        private static PredictionLine CalculateSingleLine(int lineIndex, double pointAPrice, double pointBPrice,
            double abRange, KLineInfo sourceKLine, string groupId)
        {
            double price;
            string formula;

            if (lineIndex == 0) // A线
            {
                price = pointAPrice;
                formula = $"A线 = {pointAPrice:F2}";
            }
            else if (lineIndex == 1) // B线
            {
                price = pointBPrice;
                formula = $"B线 = {pointBPrice:F2}";
            }
            else // 其他预测线
            {
                var ratio = BOSHEN_RATIOS[lineIndex];
                price = pointBPrice + abRange * (ratio - 1);
                formula = $"{pointBPrice:F2} + {abRange:F2} × ({ratio:F3} - 1) = {price:F2}";
            }

            // 精确到小数点后2位
            price = Math.Round(price, CALCULATION_PRECISION);

            var lineType = GetPredictionLineType(lineIndex);
            var isKeyLine = KEY_LINE_INDICES.Contains(lineIndex);

            return new PredictionLine
            {
                Index = lineIndex,
                Name = LINE_NAMES[lineIndex],
                LineType = lineType,
                Price = price,
                PointAPrice = pointAPrice,
                PointBPrice = pointBPrice,
                ABRange = abRange,
                BoshenRatio = BOSHEN_RATIOS[lineIndex],
                Formula = formula,
                SourceKLine = sourceKLine,
                GroupId = groupId,
                IsKeyLine = isKeyLine,
                Symbol = sourceKLine?.Symbol,
                TimeFrame = sourceKLine?.TimeFrame,
                Confidence = 0.8,
                CalculationTime = DateTime.Now,
                PredictionType = PredictionType.Resistance // 向上预测，全部为阻力位
            };
        }

        #endregion

        #region 验证和测试方法

        /// <summary>
        /// 验证计算结果的准确性
        /// 与期望结果进行对比，返回误差信息
        /// </summary>
        /// <param name="calculatedLines">计算出的预测线</param>
        /// <param name="expectedLines">期望的预测线</param>
        /// <returns>验证结果</returns>
        public static BoshenValidationResult ValidateCalculation(List<PredictionLine> calculatedLines, List<PredictionLine> expectedLines)
        {
            if (calculatedLines == null || expectedLines == null)
                return new BoshenValidationResult { IsValid = false, ErrorMessage = "预测线数据不能为空" };

            if (calculatedLines.Count != expectedLines.Count)
                return new BoshenValidationResult { IsValid = false, ErrorMessage = "预测线数量不匹配" };

            var errors = new List<string>();
            var maxError = 0.0;
            var totalError = 0.0;

            for (int i = 0; i < calculatedLines.Count; i++)
            {
                var calculated = calculatedLines[i];
                var expected = expectedLines[i];

                var error = Math.Abs(calculated.Price - expected.Price);
                totalError += error;
                maxError = Math.Max(maxError, error);

                if (error > PRICE_TOLERANCE)
                {
                    errors.Add($"{calculated.Name} 误差过大: 计算值={calculated.Price:F2}, 期望值={expected.Price:F2}, 误差={error:F2}");
                }
            }

            var averageError = totalError / calculatedLines.Count;

            return new BoshenValidationResult
            {
                IsValid = errors.Count == 0,
                ErrorMessage = errors.Count > 0 ? string.Join("; ", errors) : null,
                MaxError = maxError,
                AverageError = averageError,
                ErrorCount = errors.Count
            };
        }

        /// <summary>
        /// 验证计算结果是否符合波神算法规则
        /// </summary>
        /// <param name="lines">预测线列表</param>
        /// <returns>验证结果</returns>
        public static BoshenValidationResult ValidateBoshenRules(List<PredictionLine> lines)
        {
            if (lines == null || lines.Count != 11)
                return new BoshenValidationResult { IsValid = false, ErrorMessage = "预测线数量必须为11条" };

            var errors = new List<string>();

            try
            {
                // 1. 验证A线和B线
                var aLine = lines.FirstOrDefault(l => l.Index == 0);
                var bLine = lines.FirstOrDefault(l => l.Index == 1);

                if (aLine == null || bLine == null)
                    errors.Add("缺少A线或B线");

                if (aLine != null && bLine != null)
                {
                    if (Math.Abs(aLine.Price - aLine.PointAPrice) > PRICE_TOLERANCE)
                        errors.Add("A线价格必须等于A点价格");

                    if (Math.Abs(bLine.Price - bLine.PointBPrice) > PRICE_TOLERANCE)
                        errors.Add("B线价格必须等于B点价格");
                }

                // 2. 验证所有预测线都在B点之上（除了A线）
                foreach (var line in lines.Where(l => l.Index > 1))
                {
                    if (line.Price <= line.PointBPrice + PRICE_TOLERANCE)
                        errors.Add($"{line.Name} 价格必须在B点之上");
                }

                // 3. 验证价格递增
                for (int i = 1; i < lines.Count - 1; i++)
                {
                    if (lines[i].Price >= lines[i + 1].Price - PRICE_TOLERANCE)
                        errors.Add($"预测线价格必须递增: {lines[i].Name}({lines[i].Price:F2}) >= {lines[i + 1].Name}({lines[i + 1].Price:F2})");
                }

                // 4. 验证比例计算
                foreach (var line in lines)
                {
                    var expectedRatio = BOSHEN_RATIOS[line.Index];
                    if (Math.Abs(line.BoshenRatio - expectedRatio) > 0.001)
                        errors.Add($"{line.Name} 比例不匹配: 期望={expectedRatio:F3}, 实际={line.BoshenRatio:F3}");
                }

                // 5. 验证重点线标记
                var keyLines = lines.Where(l => l.IsKeyLine).ToList();
                var expectedKeyLineCount = KEY_LINE_INDICES.Length;
                if (keyLines.Count != expectedKeyLineCount)
                    errors.Add($"重点线数量不正确: 期望={expectedKeyLineCount}, 实际={keyLines.Count}");

                return new BoshenValidationResult
                {
                    IsValid = errors.Count == 0,
                    ErrorMessage = errors.Count > 0 ? string.Join("; ", errors) : null
                };
            }
            catch (Exception ex)
            {
                return new BoshenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"验证过程异常: {ex.Message}"
                };
            }
        }

        #endregion

        #region 测试案例方法

        /// <summary>
        /// 获取标准测试案例1
        /// A=98.02, B=98.75, AB涨幅=0.73
        /// </summary>
        /// <returns>测试案例的期望结果</returns>
        public static List<PredictionLine> GetTestCase1()
        {
            var pointA = 98.02;
            var pointB = 98.75;
            var abRange = 0.73;

            var expectedPrices = new[]
            {
                98.02,  // A线
                98.75,  // B线
                99.37,  // 1线 (98.75 + 0.73×0.849)
                99.77,  // 2线 (98.75 + 0.73×1.397)
                100.31, // 3线 (98.75 + 0.73×2.137)
                100.50, // 4线 (98.75 + 0.73×2.401)
                100.94, // 5线 (98.75 + 0.73×3.000)
                101.47, // 6线 (98.75 + 0.73×3.726)
                101.85, // 7线 (98.75 + 0.73×4.247)
                102.42, // 8线 (98.75 + 0.73×5.027)
                102.99  // 极线 (98.75 + 0.73×5.808)
            };

            return CreateExpectedLines(pointA, pointB, expectedPrices);
        }

        /// <summary>
        /// 获取标准测试案例2
        /// A=96.25, B=97.06, AB涨幅=0.81
        /// </summary>
        /// <returns>测试案例的期望结果</returns>
        public static List<PredictionLine> GetTestCase2()
        {
            var pointA = 96.25;
            var pointB = 97.06;
            var abRange = 0.81;

            var expectedPrices = new[]
            {
                96.25,  // A线
                97.06,  // B线
                97.75,  // 1线 (97.06 + 0.81×0.849)
                98.19,  // 2线 (97.06 + 0.81×1.397)
                98.79,  // 3线 (97.06 + 0.81×2.137)
                99.00,  // 4线 (97.06 + 0.81×2.401)
                99.49,  // 5线 (97.06 + 0.81×3.000)
                100.08, // 6线 (97.06 + 0.81×3.726)
                100.50, // 7线 (97.06 + 0.81×4.247)
                101.13, // 8线 (97.06 + 0.81×5.027)
                101.76  // 极线 (97.06 + 0.81×5.808)
            };

            return CreateExpectedLines(pointA, pointB, expectedPrices);
        }

        /// <summary>
        /// 创建期望的预测线列表
        /// </summary>
        /// <param name="pointA">A点价格</param>
        /// <param name="pointB">B点价格</param>
        /// <param name="expectedPrices">期望价格数组</param>
        /// <returns>期望预测线列表</returns>
        private static List<PredictionLine> CreateExpectedLines(double pointA, double pointB, double[] expectedPrices)
        {
            var lines = new List<PredictionLine>();
            var abRange = pointB - pointA;

            for (int i = 0; i < expectedPrices.Length; i++)
            {
                lines.Add(new PredictionLine
                {
                    Index = i,
                    Name = LINE_NAMES[i],
                    LineType = GetPredictionLineType(i),
                    Price = expectedPrices[i],
                    PointAPrice = pointA,
                    PointBPrice = pointB,
                    ABRange = abRange,
                    BoshenRatio = BOSHEN_RATIOS[i],
                    IsKeyLine = KEY_LINE_INDICES.Contains(i),
                    Confidence = 1.0 // 测试案例设置为最高置信度
                });
            }

            return lines;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 根据索引获取预测线类型
        /// </summary>
        /// <param name="index">线索引</param>
        /// <returns>预测线类型</returns>
        private static PredictionLineType GetPredictionLineType(int index)
        {
            return index switch
            {
                0 => PredictionLineType.PointA,
                1 => PredictionLineType.PointB,
                2 => PredictionLineType.Line1,
                3 => PredictionLineType.Line2,
                4 => PredictionLineType.Line3,
                5 => PredictionLineType.Line4,
                6 => PredictionLineType.Line5,
                7 => PredictionLineType.Line6,
                8 => PredictionLineType.Line7,
                9 => PredictionLineType.Line8,
                10 => PredictionLineType.ExtremeLine,
                _ => PredictionLineType.Unknown
            };
        }

        /// <summary>
        /// 获取指定K线的波神线信息摘要
        /// </summary>
        /// <param name="kline">K线信息</param>
        /// <returns>摘要信息</returns>
        public static string GetBoshenSummary(KLineInfo kline)
        {
            if (kline == null)
                return "K线信息为空";

            try
            {
                var lines = CalculateBoshenLines(kline);
                var keyLines = lines.Where(l => l.IsKeyLine).ToList();

                var summary = $"波神11线分析结果:\n";
                summary += $"A点: {kline.LowPrice:F2}, B点: {kline.HighPrice:F2}\n";
                summary += $"AB涨幅: {kline.PriceRange:F2}\n";
                summary += $"重点线位:\n";

                foreach (var keyLine in keyLines)
                {
                    summary += $"  {keyLine.Name}: {keyLine.Price:F2}\n";
                }

                summary += $"极线: {lines.Last().Price:F2}";

                return summary;
            }
            catch (Exception ex)
            {
                return $"计算失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 检查当前价格是否接近某条预测线
        /// </summary>
        /// <param name="lines">预测线列表</param>
        /// <param name="currentPrice">当前价格</param>
        /// <param name="tolerancePercent">容差百分比</param>
        /// <returns>接近的预测线列表</returns>
        public static List<PredictionLine> FindNearbyLines(List<PredictionLine> lines, double currentPrice, double tolerancePercent = 0.1)
        {
            if (lines == null || lines.Count == 0)
                return new List<PredictionLine>();

            return lines.Where(line => line.IsNearCurrentPrice(currentPrice, tolerancePercent))
                       .OrderBy(line => line.GetDistanceFromCurrentPrice(currentPrice))
                       .ToList();
        }

        #endregion
    }

    /// <summary>
    /// 波神算法验证结果
    /// </summary>
    public class BoshenValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 最大误差
        /// </summary>
        public double MaxError { get; set; }

        /// <summary>
        /// 平均误差
        /// </summary>
        public double AverageError { get; set; }

        /// <summary>
        /// 错误数量
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// 获取验证结果描述
        /// </summary>
        /// <returns>描述文本</returns>
        public string GetDescription()
        {
            if (IsValid)
                return "验证通过，计算结果符合波神算法要求";

            var description = $"验证失败，发现{ErrorCount}个问题";

            if (!string.IsNullOrEmpty(ErrorMessage))
                description += $": {ErrorMessage}";

            if (MaxError > 0)
                description += $"\n最大误差: {MaxError:F4}, 平均误差: {AverageError:F4}";

            return description;
        }
    }
}