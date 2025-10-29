using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.Drawing;

namespace BoshenCC.Models
{
    /// <summary>
    /// 波神预测线模型
    /// 表示基于波神11线算法计算出的单条预测线
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class PredictionLine
    {
        #region 基本属性

        /// <summary>
        /// 预测线索引（0-10，对应A线、B线、1线-8线、极线）
        /// </summary>
        [JsonProperty]
        [Range(0, 10, ErrorMessage = "预测线索引必须在0-10之间")]
        public int Index { get; set; }

        /// <summary>
        /// 预测线名称
        /// </summary>
        [JsonProperty]
        [Required(ErrorMessage = "预测线名称不能为空")]
        public string Name { get; set; }

        /// <summary>
        /// 预测线类型
        /// </summary>
        [JsonProperty]
        [Required(ErrorMessage = "预测线类型不能为空")]
        public PredictionLineType LineType { get; set; }

        /// <summary>
        /// 预测价格
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "预测价格不能为负数")]
        public double Price { get; set; }

        /// <summary>
        /// 波神比例系数
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "波神比例系数不能为负数")]
        public double BoshenRatio { get; set; }

        #endregion

        #region 计算相关信息

        /// <summary>
        /// 基准点A的价格（通常是K线最低价）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "A点价格不能为负数")]
        public double PointAPrice { get; set; }

        /// <summary>
        /// 基准点B的价格（通常是K线最高价）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "B点价格不能为负数")]
        public double PointBPrice { get; set; }

        /// <summary>
        /// AB涨幅（B点价格 - A点价格）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "AB涨幅不能为负数")]
        public double ABRange { get; set; }

        /// <summary>
        /// 计算公式
        /// </summary>
        [JsonProperty]
        public string Formula { get; set; }

        /// <summary>
        /// 计算时间戳
        /// </summary>
        [JsonProperty]
        public DateTime CalculationTime { get; set; } = DateTime.Now;

        #endregion

        #region 位置和渲染信息

        /// <summary>
        /// 预测线在图表中的Y坐标位置（像素坐标）
        /// </summary>
        [JsonProperty]
        public double YPosition { get; set; }

        /// <summary>
        /// 预测线颜色
        /// </summary>
        [JsonProperty]
        public Color Color { get; set; } = Color.Blue;

        /// <summary>
        /// 预测线样式（实线、虚线等）
        /// </summary>
        [JsonProperty]
        public PredictionLineStyle Style { get; set; } = PredictionLineStyle.Solid;

        /// <summary>
        /// 预测线宽度（像素）
        /// </summary>
        [JsonProperty]
        [Range(1, 10, ErrorMessage = "线宽必须在1-10像素之间")]
        public int Width { get; set; } = 1;

        /// <summary>
        /// 是否显示价格标签
        /// </summary>
        [JsonProperty]
        public bool ShowPriceLabel { get; set; } = true;

        /// <summary>
        /// 价格标签位置
        /// </summary>
        [JsonProperty]
        public Point LabelPosition { get; set; }

        /// <summary>
        /// 是否为重点线（3线、6线、8线）
        /// </summary>
        [JsonProperty]
        public bool IsKeyLine { get; set; }

        /// <summary>
        /// 线条透明度（0-1）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "透明度必须在0-1之间")]
        public double Opacity { get; set; } = 1.0;

        #endregion

        #region 交易相关信息

        /// <summary>
        /// 预测类型（支撑位、阻力位等）
        /// </summary>
        [JsonProperty]
        public PredictionType PredictionType { get; set; } = PredictionType.Resistance;

        /// <summary>
        /// 预测置信度（0-1）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "预测置信度必须在0-1之间")]
        public double Confidence { get; set; } = 0.8;

        /// <summary>
        /// 预测准确性评分（基于历史数据回测）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "准确性评分必须在0-1之间")]
        public double AccuracyScore { get; set; }

        /// <summary>
        /// 触发次数（历史数据中价格触及此线的次数）
        /// </summary>
        [JsonProperty]
        [Range(0, int.MaxValue, ErrorMessage = "触发次数不能为负数")]
        public int TriggerCount { get; set; }

        /// <summary>
        /// 成功率（触发次数中成功预测的比例）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "成功率必须在0-1之间")]
        public double SuccessRate { get; set; }

        #endregion

        #region 关联信息

        /// <summary>
        /// 所属的K线信息
        /// </summary>
        [JsonProperty]
        public KLineInfo SourceKLine { get; set; }

        /// <summary>
        /// 预测线组ID（同一次计算产生的11条线共享相同ID）
        /// </summary>
        [JsonProperty]
        public string GroupId { get; set; }

        /// <summary>
        /// 交易品种代码
        /// </summary>
        [JsonProperty]
        public string Symbol { get; set; }

        /// <summary>
        /// 时间周期
        /// </summary>
        [JsonProperty]
        public string TimeFrame { get; set; }

        /// <summary>
        /// 数据来源
        /// </summary>
        [JsonProperty]
        public string DataSource { get; set; }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证预测线数据的有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult ValidatePredictionLine()
        {
            var errors = new List<string>();

            try
            {
                // 1. 基本数据验证
                if (string.IsNullOrEmpty(Name))
                    errors.Add("预测线名称不能为空");

                if (Index < 0 || Index > 10)
                    errors.Add("预测线索引必须在0-10之间");

                if (Price <= 0)
                    errors.Add("预测价格必须大于0");

                // 2. 计算数据验证
                if (PointAPrice <= 0 || PointBPrice <= 0)
                    errors.Add("A点和B点价格必须大于0");

                if (PointBPrice <= PointAPrice)
                    errors.Add("B点价格必须大于A点价格");

                if (Math.Abs(ABRange - (PointBPrice - PointAPrice)) > 0.001)
                    errors.Add("AB涨幅计算不一致");

                // 3. 比例验证
                ValidateBoshenRatio(errors);

                // 4. 价格验证
                ValidateCalculatedPrice(errors);

                // 5. 置信度验证
                if (Confidence < 0 || Confidence > 1)
                    errors.Add("置信度必须在0-1之间");

                if (Opacity < 0 || Opacity > 1)
                    errors.Add("透明度必须在0-1之间");

                return new ValidationResult
                {
                    IsValid = errors.Count == 0,
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { $"验证过程异常: {ex.Message}" }
                };
            }
        }

        /// <summary>
        /// 验证波神比例
        /// </summary>
        /// <param name="errors">错误列表</param>
        private void ValidateBoshenRatio(List<string> errors)
        {
            try
            {
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

                if (Index >= 0 && Index < expectedRatios.Length)
                {
                    var expectedRatio = expectedRatios[Index];
                    if (Math.Abs(BoshenRatio - expectedRatio) > 0.001)
                        errors.Add($"波神比例不匹配，期望: {expectedRatio:F3}, 实际: {BoshenRatio:F3}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"波神比例验证失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证计算价格
        /// </summary>
        /// <param name="errors">错误列表</param>
        private void ValidateCalculatedPrice(List<string> errors)
        {
            try
            {
                double expectedPrice;

                if (Index == 0) // A线
                {
                    expectedPrice = PointAPrice;
                }
                else // B线及以上
                {
                    expectedPrice = PointBPrice + ABRange * (BoshenRatio - 1);
                }

                if (Math.Abs(Price - expectedPrice) > 0.01) // 允许0.01的误差
                {
                    errors.Add($"计算价格不匹配，期望: {expectedPrice:F2}, 实际: {Price:F2}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"计算价格验证失败: {ex.Message}");
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取预测线描述
        /// </summary>
        /// <returns>描述文本</returns>
        public string GetDescription()
        {
            var parts = new List<string>();

            parts.Add(Name);

            if (IsKeyLine)
                parts.Add("(重点线)");

            parts.Add($"价格: {Price:F2}");

            if (Confidence < 0.8)
                parts.Add($"置信度: {Confidence:F1}");

            if (AccuracyScore > 0)
                parts.Add($"准确率: {AccuracyScore:F1%}");

            return string.Join(" ", parts);
        }

        /// <summary>
        /// 获取价格标签文本
        /// </summary>
        /// <returns>标签文本</returns>
        public string GetPriceLabelText()
        {
            if (ShowPriceLabel)
            {
                return $"{Name}: {Price:F2}";
            }
            return string.Empty;
        }

        /// <summary>
        /// 计算与当前价格的距离
        /// </summary>
        /// <param name="currentPrice">当前价格</param>
        /// <returns>距离（绝对值）</returns>
        public double GetDistanceFromCurrentPrice(double currentPrice)
        {
            return Math.Abs(Price - currentPrice);
        }

        /// <summary>
        /// 计算与当前价格的百分比距离
        /// </summary>
        /// <param name="currentPrice">当前价格</param>
        /// <returns>百分比距离</returns>
        public double GetPercentageDistanceFromCurrentPrice(double currentPrice)
        {
            if (currentPrice <= 0)
                return double.MaxValue;

            return Math.Abs(Price - currentPrice) / currentPrice * 100.0;
        }

        /// <summary>
        /// 判断当前价格是否接近此预测线
        /// </summary>
        /// <param name="currentPrice">当前价格</param>
        /// <param name="tolerancePercent">容差百分比（默认0.1%）</param>
        /// <returns>是否接近</returns>
        public bool IsNearCurrentPrice(double currentPrice, double tolerancePercent = 0.1)
        {
            var percentageDistance = GetPercentageDistanceFromCurrentPrice(currentPrice);
            return percentageDistance <= tolerancePercent;
        }

        /// <summary>
        /// 转换为JSON字符串
        /// </summary>
        /// <returns>JSON字符串</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// 从JSON字符串创建实例
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>PredictionLine实例</returns>
        public static PredictionLine FromJson(string json)
        {
            return JsonConvert.DeserializeObject<PredictionLine>(json);
        }

        /// <summary>
        /// 创建标准预测线
        /// </summary>
        /// <param name="index">线索引</param>
        /// <param name="name">线名称</param>
        /// <param name="price">预测价格</param>
        /// <param name="pointAPrice">A点价格</param>
        /// <param name="pointBPrice">B点价格</param>
        /// <param name="boshenRatio">波神比例</param>
        /// <param name="sourceKLine">源K线</param>
        /// <returns>PredictionLine实例</returns>
        public static PredictionLine CreateStandard(int index, string name, double price,
            double pointAPrice, double pointBPrice, double boshenRatio, KLineInfo sourceKLine = null)
        {
            var lineType = GetPredictionLineType(index);
            var isKeyLine = index == 3 || index == 6 || index == 8; // 3线、6线、8线为重点线

            return new PredictionLine
            {
                Index = index,
                Name = name,
                LineType = lineType,
                Price = price,
                PointAPrice = pointAPrice,
                PointBPrice = pointBPrice,
                ABRange = pointBPrice - pointAPrice,
                BoshenRatio = boshenRatio,
                Formula = GenerateFormula(index, pointAPrice, pointBPrice, boshenRatio),
                SourceKLine = sourceKLine,
                IsKeyLine = isKeyLine,
                Symbol = sourceKLine?.Symbol,
                TimeFrame = sourceKLine?.TimeFrame,
                Confidence = 0.8,
                GroupId = Guid.NewGuid().ToString("N")[..8], // 生成8位组ID
                CalculationTime = DateTime.Now
            };
        }

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
        /// 生成计算公式
        /// </summary>
        /// <param name="index">线索引</param>
        /// <param name="pointAPrice">A点价格</param>
        /// <param name="pointBPrice">B点价格</param>
        /// <param name="boshenRatio">波神比例</param>
        /// <returns>公式字符串</returns>
        private static string GenerateFormula(int index, double pointAPrice, double pointBPrice, double boshenRatio)
        {
            var abRange = pointBPrice - pointAPrice;

            if (index == 0)
                return $"A线 = {pointAPrice:F2}";
            else if (index == 1)
                return $"B线 = {pointBPrice:F2}";
            else
                return $"{pointBPrice:F2} + {abRange:F2} × ({boshenRatio:F3} - 1) = {pointBPrice + abRange * (boshenRatio - 1):F2}";
        }

        /// <summary>
        /// 克隆预测线
        /// </summary>
        /// <returns>克隆的预测线</returns>
        public PredictionLine Clone()
        {
            return new PredictionLine
            {
                // 基本属性克隆
                Index = Index,
                Name = Name,
                LineType = LineType,
                Price = Price,
                BoshenRatio = BoshenRatio,

                // 计算信息克隆
                PointAPrice = PointAPrice,
                PointBPrice = PointBPrice,
                ABRange = ABRange,
                Formula = Formula,
                CalculationTime = CalculationTime,

                // 位置信息克隆
                YPosition = YPosition,
                Color = Color,
                Style = Style,
                Width = Width,
                ShowPriceLabel = ShowPriceLabel,
                LabelPosition = LabelPosition,
                IsKeyLine = IsKeyLine,
                Opacity = Opacity,

                // 交易信息克隆
                PredictionType = PredictionType,
                Confidence = Confidence,
                AccuracyScore = AccuracyScore,
                TriggerCount = TriggerCount,
                SuccessRate = SuccessRate,

                // 关联信息克隆
                SourceKLine = SourceKLine, // 注意：这里只是引用复制
                GroupId = GroupId,
                Symbol = Symbol,
                TimeFrame = TimeFrame,
                DataSource = DataSource
            };
        }

        /// <summary>
        /// 获取唯一的线索引ID
        /// </summary>
        /// <returns>唯一ID</returns>
        public string GetUniqueId()
        {
            return $"{GroupId}_{Index}_{Price:F2}_{CalculationTime:yyyyMMddHHmmss}";
        }

        #endregion
    }

    /// <summary>
    /// 预测线类型枚举
    /// </summary>
    public enum PredictionLineType
    {
        /// <summary>
        /// 未知类型
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A线（最低点）
        /// </summary>
        PointA = 1,

        /// <summary>
        /// B线（最高点）
        /// </summary>
        PointB = 2,

        /// <summary>
        /// 第1线
        /// </summary>
        Line1 = 3,

        /// <summary>
        /// 第2线
        /// </summary>
        Line2 = 4,

        /// <summary>
        /// 第3线
        /// </summary>
        Line3 = 5,

        /// <summary>
        /// 第4线
        /// </summary>
        Line4 = 6,

        /// <summary>
        /// 第5线
        /// </summary>
        Line5 = 7,

        /// <summary>
        /// 第6线
        /// </summary>
        Line6 = 8,

        /// <summary>
        /// 第7线
        /// </summary>
        Line7 = 9,

        /// <summary>
        /// 第8线
        /// </summary>
        Line8 = 10,

        /// <summary>
        /// 极线
        /// </summary>
        ExtremeLine = 11
    }

    /// <summary>
    /// 预测线样式枚举
    /// </summary>
    public enum PredictionLineStyle
    {
        /// <summary>
        /// 实线
        /// </summary>
        Solid = 0,

        /// <summary>
        /// 虚线
        /// </summary>
        Dashed = 1,

        /// <summary>
        /// 点线
        /// </summary>
        Dotted = 2,

        /// <summary>
        /// 点划线
        /// </summary>
        DashDot = 3
    }

    /// <summary>
    /// 预测类型枚举
    /// </summary>
    public enum PredictionType
    {
        /// <summary>
        /// 支撑位
        /// </summary>
        Support = 0,

        /// <summary>
        /// 阻力位
        /// </summary>
        Resistance = 1,

        /// <summary>
        /// 目标位
        /// </summary>
        Target = 2,

        /// <summary>
        /// 反转位
        /// </summary>
        Reversal = 3
    }
}