using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.Drawing;

namespace BoshenCC.Models
{
    /// <summary>
    /// K线结构信息模型
    /// 描述K线的形态、结构和特征信息
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class KLineStructure
    {
        #region 基本结构信息

        /// <summary>
        /// 实体高度（像素）
        /// </summary>
        [JsonProperty]
        [Range(0, int.MaxValue, ErrorMessage = "实体高度不能为负数")]
        public int BodyHeight { get; set; }

        /// <summary>
        /// 上影线高度（像素）
        /// </summary>
        [JsonProperty]
        [Range(0, int.MaxValue, ErrorMessage = "上影线高度不能为负数")]
        public int UpperShadowHeight { get; set; }

        /// <summary>
        /// 下影线高度（像素）
        /// </summary>
        [JsonProperty]
        [Range(0, int.MaxValue, ErrorMessage = "下影线高度不能为负数")]
        public int LowerShadowHeight { get; set; }

        /// <summary>
        /// 总高度（像素）
        /// </summary>
        [JsonProperty]
        [Range(1, int.MaxValue, ErrorMessage = "总高度必须大于0")]
        public int TotalHeight { get; set; }

        /// <summary>
        /// 实体宽度（像素）
        /// </summary>
        [JsonProperty]
        [Range(1, int.MaxValue, ErrorMessage = "实体宽度必须大于0")]
        public int BodyWidth { get; set; }

        #endregion

        #region 比例和特征

        /// <summary>
        /// 实体比例（实体高度 / 总高度）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "实体比例必须在0-1之间")]
        public double BodyRatio => TotalHeight > 0 ? (double)BodyHeight / TotalHeight : 0.0;

        /// <summary>
        /// 上影线比例（上影线高度 / 总高度）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "上影线比例必须在0-1之间")]
        public double UpperShadowRatio => TotalHeight > 0 ? (double)UpperShadowHeight / TotalHeight : 0.0;

        /// <summary>
        /// 下影线比例（下影线高度 / 总高度）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "下影线比例必须在0-1之间")]
        public double LowerShadowRatio => TotalHeight > 0 ? (double)LowerShadowHeight / TotalHeight : 0.0;

        /// <summary>
        /// 影线总比例（上下影线总高度 / 总高度）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "影线总比例必须在0-1之间")]
        public double ShadowRatio => TotalHeight > 0 ? (double)(UpperShadowHeight + LowerShadowHeight) / TotalHeight : 0.0;

        /// <summary>
        /// 长宽比（总高度 / 实体宽度）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "长宽比不能为负数")]
        public double AspectRatio => BodyWidth > 0 ? (double)TotalHeight / BodyWidth : 0.0;

        #endregion

        #region 形态分类

        /// <summary>
        /// K线形态类型
        /// </summary>
        [JsonProperty]
        public KLinePatternType PatternType { get; set; } = KLinePatternType.Unknown;

        /// <summary>
        /// 是否为十字星形态
        /// </summary>
        [JsonProperty]
        public bool IsDoji => PatternType == KLinePatternType.Doji || BodyRatio < 0.1;

        /// <summary>
        /// 是否为锤子线形态
        /// </summary>
        [JsonProperty]
        public bool IsHammer => PatternType == KLinePatternType.Hammer ||
                               (LowerShadowRatio > 0.6 && BodyRatio < 0.3 && UpperShadowRatio < 0.2);

        /// <summary>
        /// 是否为倒锤子线形态
        /// </summary>
        [JsonProperty]
        public bool IsInvertedHammer => PatternType == KLinePatternType.InvertedHammer ||
                                       (UpperShadowRatio > 0.6 && BodyRatio < 0.3 && LowerShadowRatio < 0.2);

        /// <summary>
        /// 是否为光头形态（无上影线）
        /// </summary>
        [JsonProperty]
        public bool IsBaldHead => UpperShadowHeight == 0;

        /// <summary>
        /// 是否为光脚形态（无下影线）
        /// </summary>
        [JsonProperty]
        public bool IsBaldFoot => LowerShadowHeight == 0;

        /// <summary>
        /// 是否为光头光脚形态（无影线）
        /// </summary>
        [JsonProperty]
        public bool IsBaldBoth => IsBaldHead && IsBaldFoot;

        /// <summary>
        /// 是否为长上影线形态
        /// </summary>
        [JsonProperty]
        public bool HasLongUpperShadow => UpperShadowRatio > 0.5;

        /// <summary>
        /// 是否为长下影线形态
        /// </summary>
        [JsonProperty]
        public bool HasLongLowerShadow => LowerShadowRatio > 0.5;

        #endregion

        #region 强度和特征评分

        /// <summary>
        /// 实体强度评分（0-1）
        /// 实体越大，强度越高
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "实体强度评分必须在0-1之间")]
        public double BodyStrengthScore => Math.Min(1.0, BodyRatio * 2); // 实体比例乘以2作为强度评分

        /// <summary>
        /// 趋势强度评分（0-1）
        /// 影线越少，趋势越明确
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "趋势强度评分必须在0-1之间")]
        public double TrendStrengthScore => Math.Max(0.0, 1.0 - ShadowRatio);

        /// <summary>
        /// 形态清晰度评分（0-1）
        /// 基于结构的清晰度和可识别性
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "形态清晰度评分必须在0-1之间")]
        public double ClarityScore
        {
            get
            {
                double score = 0.0;

                // 实体清晰度
                if (BodyHeight > 5)
                    score += 0.3;
                else if (BodyHeight > 0)
                    score += 0.1;

                // 影线清晰度
                if (UpperShadowHeight > 3 || LowerShadowHeight > 3)
                    score += 0.3;

                // 比例合理性
                if (BodyRatio >= 0.1 && BodyRatio <= 0.8)
                    score += 0.2;

                // 整体合理性
                if (TotalHeight == BodyHeight + UpperShadowHeight + LowerShadowHeight)
                    score += 0.2;

                return Math.Min(1.0, score);
            }
        }

        #endregion

        #region 边界和位置信息

        /// <summary>
        /// 完整边界矩形
        /// </summary>
        [JsonProperty]
        public Rectangle FullBoundary { get; set; }

        /// <summary>
        /// 实体边界矩形
        /// </summary>
        [JsonProperty]
        public Rectangle BodyBoundary { get; set; }

        /// <summary>
        /// 上影线边界矩形
        /// </summary>
        [JsonProperty]
        public Rectangle UpperShadowBoundary { get; set; }

        /// <summary>
        /// 下影线边界矩形
        /// </summary>
        [JsonProperty]
        public Rectangle LowerShadowBoundary { get; set; }

        /// <summary>
        /// 中心点坐标
        /// </summary>
        [JsonProperty]
        public Point CenterPoint => new Point(
            FullBoundary.X + FullBoundary.Width / 2,
            FullBoundary.Y + FullBoundary.Height / 2
        );

        /// <summary>
        /// 顶部坐标
        /// </summary>
        [JsonProperty]
        public Point TopPoint => new Point(
            FullBoundary.X + FullBoundary.Width / 2,
            FullBoundary.Top
        );

        /// <summary>
        /// 底部坐标
        /// </summary>
        [JsonProperty]
        public Point BottomPoint => new Point(
            FullBoundary.X + FullBoundary.Width / 2,
            FullBoundary.Bottom
        );

        #endregion

        #region 方法和工具

        /// <summary>
        /// 验证结构数据的有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateStructure()
        {
            var errors = new List<string>();

            // 1. 基本尺寸检查
            if (TotalHeight <= 0)
                errors.Add("总高度必须大于0");

            if (BodyHeight < 0 || UpperShadowHeight < 0 || LowerShadowHeight < 0)
                errors.Add("各部分高度不能为负数");

            if (BodyWidth <= 0)
                errors.Add("实体宽度必须大于0");

            // 2. 尺寸一致性检查
            var calculatedHeight = BodyHeight + UpperShadowHeight + LowerShadowHeight;
            if (Math.Abs(calculatedHeight - TotalHeight) > 2) // 允许2像素误差
                errors.Add($"尺寸不一致: 计算高度({calculatedHeight}) != 总高度({TotalHeight})");

            // 3. 比例合理性检查
            if (BodyRatio > 1.0 || UpperShadowRatio > 1.0 || LowerShadowRatio > 1.0)
                errors.Add("比例数据异常");

            // 4. 边界一致性检查
            if (!FullBoundary.IsEmpty)
            {
                if (!BodyBoundary.IsEmpty && !FullBoundary.Contains(BodyBoundary))
                    errors.Add("实体边界超出完整边界");

                if (!UpperShadowBoundary.IsEmpty && !FullBoundary.Contains(UpperShadowBoundary))
                    errors.Add("上影线边界超出完整边界");

                if (!LowerShadowBoundary.IsEmpty && !FullBoundary.Contains(LowerShadowBoundary))
                    errors.Add("下影线边界超出完整边界");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// 计算与另一个K线结构的相似度
        /// </summary>
        /// <param name="other">另一个K线结构</param>
        /// <returns>相似度评分（0-1）</returns>
        public double CalculateSimilarity(KLineStructure other)
        {
            if (other == null)
                return 0.0;

            try
            {
                double similarity = 0.0;
                int factors = 0;

                // 形态类型比较
                if (PatternType == other.PatternType)
                {
                    similarity += 0.3;
                }
                factors++;

                // 比例相似度
                similarity += (1.0 - Math.Abs(BodyRatio - other.BodyRatio)) * 0.2;
                similarity += (1.0 - Math.Abs(UpperShadowRatio - other.UpperShadowRatio)) * 0.2;
                similarity += (1.0 - Math.Abs(LowerShadowRatio - other.LowerShadowRatio)) * 0.2;
                factors += 3;

                // 长宽比相似度
                var aspectRatioDiff = Math.Abs(AspectRatio - other.AspectRatio) / Math.Max(AspectRatio, other.AspectRatio);
                similarity += (1.0 - aspectRatioDiff) * 0.1;
                factors++;

                // 特征相似度
                if (IsDoji == other.IsDoji) similarity += 0.05;
                if (IsHammer == other.IsHammer) similarity += 0.05;
                if (IsInvertedHammer == other.IsInvertedHammer) similarity += 0.05;
                if (IsBaldHead == other.IsBaldHead) similarity += 0.05;
                if (IsBaldFoot == other.IsBaldFoot) similarity += 0.05;
                factors += 5;

                return similarity / factors;
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// 获取结构描述文本
        /// </summary>
        /// <returns>结构描述</returns>
        public string GetStructureDescription()
        {
            var description = new List<string>();

            // 基本形态
            switch (PatternType)
            {
                case KLinePatternType.Doji:
                    description.Add("十字星");
                    break;
                case KLinePatternType.Hammer:
                    description.Add("锤子线");
                    break;
                case KLinePatternType.InvertedHammer:
                    description.Add("倒锤子线");
                    break;
                default:
                    if (IsDoji)
                        description.Add("十字星");
                    else if (IsHammer)
                        description.Add("锤子线");
                    else if (IsInvertedHammer)
                        description.Add("倒锤子线");
                    else
                        description.Add("普通K线");
                    break;
            }

            // 影线特征
            if (IsBaldBoth)
                description.Add("光头光脚");
            else if (IsBaldHead)
                description.Add("光头");
            else if (IsBaldFoot)
                description.Add("光脚");
            else if (HasLongUpperShadow)
                description.Add("长上影线");
            else if (HasLongLowerShadow)
                description.Add("长下影线");

            // 实体特征
            if (BodyRatio > 0.7)
                description.Add("大实体");
            else if (BodyRatio < 0.3)
                description.Add("小实体");

            return string.Join("，", description);
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
        /// <returns>KLineStructure实例</returns>
        public static KLineStructure FromJson(string json)
        {
            return JsonConvert.DeserializeObject<KLineStructure>(json);
        }

        /// <summary>
        /// 创建标准K线结构
        /// </summary>
        /// <param name="bodyHeight">实体高度</param>
        /// <param name="upperShadowHeight">上影线高度</param>
        /// <param name="lowerShadowHeight">下影线高度</param>
        /// <param name="bodyWidth">实体宽度</param>
        /// <returns>K线结构实例</returns>
        public static KLineStructure CreateStandard(int bodyHeight, int upperShadowHeight, int lowerShadowHeight, int bodyWidth)
        {
            return new KLineStructure
            {
                BodyHeight = bodyHeight,
                UpperShadowHeight = upperShadowHeight,
                LowerShadowHeight = lowerShadowHeight,
                TotalHeight = bodyHeight + upperShadowHeight + lowerShadowHeight,
                BodyWidth = bodyWidth,
                PatternType = DeterminePatternType(bodyHeight, upperShadowHeight, lowerShadowHeight)
            };
        }

        /// <summary>
        /// 根据尺寸确定形态类型
        /// </summary>
        /// <param name="bodyHeight">实体高度</param>
        /// <param name="upperShadowHeight">上影线高度</param>
        /// <param name="lowerShadowHeight">下影线高度</param>
        /// <returns>形态类型</returns>
        private static KLinePatternType DeterminePatternType(int bodyHeight, int upperShadowHeight, int lowerShadowHeight)
        {
            var totalHeight = bodyHeight + upperShadowHeight + lowerShadowHeight;
            if (totalHeight == 0)
                return KLinePatternType.Unknown;

            var bodyRatio = (double)bodyHeight / totalHeight;
            var upperShadowRatio = (double)upperShadowHeight / totalHeight;
            var lowerShadowRatio = (double)lowerShadowHeight / totalHeight;

            // 十字星：实体很小
            if (bodyRatio < 0.1)
                return KLinePatternType.Doji;

            // 锤子线：下影线很长，实体很小
            if (lowerShadowRatio > 0.6 && bodyRatio < 0.3 && upperShadowRatio < 0.2)
                return KLinePatternType.Hammer;

            // 倒锤子线：上影线很长，实体很小
            if (upperShadowRatio > 0.6 && bodyRatio < 0.3 && lowerShadowRatio < 0.2)
                return KLinePatternType.InvertedHammer;

            // 普通K线
            return KLinePatternType.Normal;
        }

        #endregion
    }

    /// <summary>
    /// K线形态类型枚举
    /// </summary>
    public enum KLinePatternType
    {
        /// <summary>
        /// 未知形态
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
        InvertedHammer = 4,

        /// <summary>
        /// 流星线（长上影线，小实体）
        /// </summary>
        ShootingStar = 5,

        /// <summary>
        /// 上吊线（长下影线，小实体）
        /// </summary>
        HangingMan = 6,

        /// <summary>
        /// 光头阳线（无上影线，阳线）
        /// </summary>
        BaldBullish = 7,

        /// <summary>
        /// 光脚阴线（无下影线，阴线）
        /// </summary>
        BaldBearish = 8,

        /// <summary>
        /// 光头光脚阳线（无影线，阳线）
        /// </summary>
        BaldBothBullish = 9,

        /// <summary>
        /// 光头光脚阴线（无影线，阴线）
        /// </summary>
        BaldBothBearish = 10
    }
}