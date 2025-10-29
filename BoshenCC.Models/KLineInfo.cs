using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.Drawing;

namespace BoshenCC.Models
{
    /// <summary>
    /// K线信息模型
    /// 包含K线的完整信息：价格、位置、颜色、结构等
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class KLineInfo
    {
        #region 基本位置和边界信息

        /// <summary>
        /// K线完整边界矩形
        /// </summary>
        [JsonProperty]
        [Required(ErrorMessage = "K线边界不能为空")]
        public Rectangle Bounds { get; set; }

        /// <summary>
        /// K线在图像中的索引位置（从左到右）
        /// </summary>
        [JsonProperty]
        [Range(0, int.MaxValue, ErrorMessage = "索引位置不能为负数")]
        public int Index { get; set; }

        /// <summary>
        /// K线所属的时间周期（如1分钟、5分钟、日线等）
        /// </summary>
        [JsonProperty]
        public string TimeFrame { get; set; }

        /// <summary>
        /// K线的时间戳
        /// </summary>
        [JsonProperty]
        public DateTime Timestamp { get; set; }

        #endregion

        #region 价格信息

        /// <summary>
        /// 开盘价
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "开盘价不能为负数")]
        public double OpenPrice { get; set; }

        /// <summary>
        /// 收盘价
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "收盘价不能为负数")]
        public double ClosePrice { get; set; }

        /// <summary>
        /// 最高价
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "最高价不能为负数")]
        public double HighPrice { get; set; }

        /// <summary>
        /// 最低价
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "最低价不能为负数")]
        public double LowPrice { get; set; }

        /// <summary>
        /// 价格变动（收盘价 - 开盘价）
        /// </summary>
        [JsonProperty]
        public double PriceChange => ClosePrice - OpenPrice;

        /// <summary>
        /// 价格变动百分比（相对于开盘价）
        /// </summary>
        [JsonProperty]
        public double PriceChangePercent => OpenPrice > 0 ? (PriceChange / OpenPrice) * 100.0 : 0.0;

        /// <summary>
        /// 价格区间（最高价 - 最低价）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "价格区间不能为负数")]
        public double PriceRange => HighPrice - LowPrice;

        /// <summary>
        /// 实体价格区间（开盘价和收盘价之间的差额）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "实体价格区间不能为负数")]
        public double BodyPriceRange => Math.Abs(ClosePrice - OpenPrice);

        /// <summary>
        /// 上影线价格区间（最高价 - max(开盘价,收盘价)）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "上影线价格区间不能为负数")]
        public double UpperShadowPriceRange => HighPrice - Math.Max(OpenPrice, ClosePrice);

        /// <summary>
        /// 下影线价格区间（min(开盘价,收盘价) - 最低价）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "下影线价格区间不能为负数")]
        public double LowerShadowPriceRange => Math.Min(OpenPrice, ClosePrice) - LowPrice;

        #endregion

        #region 颜色信息

        /// <summary>
        /// K线颜色类型
        /// </summary>
        [JsonProperty]
        [Required(ErrorMessage = "K线颜色不能为空")]
        public KLineColor Color { get; set; }

        /// <summary>
        /// 颜色检测置信度（0-1）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "颜色置信度必须在0-1之间")]
        public double ColorConfidence { get; set; }

        /// <summary>
        /// 是否为阳线（收盘价 > 开盘价）
        /// </summary>
        [JsonProperty]
        public bool IsBullish => ClosePrice > OpenPrice;

        /// <summary>
        /// 是否为阴线（收盘价 < 开盘价）
        /// </summary>
        [JsonProperty]
        public bool IsBearish => ClosePrice < OpenPrice;

        /// <summary>
        /// 是否为平盘（收盘价 = 开盘价）
        /// </summary>
        [JsonProperty]
        public bool IsDojiPrice => Math.Abs(ClosePrice - OpenPrice) < GetPriceTolerance();

        /// <summary>
        /// 主要颜色的HSV值（可选）
        /// </summary>
        [JsonProperty]
        public Emgu.CV.Structure.Hsv? DominantColorHsv { get; set; }

        /// <summary>
        /// 主要颜色的RGB值（可选）
        /// </summary>
        [JsonProperty]
        public Color? DominantColorRgb { get; set; }

        #endregion

        #region 结构信息

        /// <summary>
        /// K线结构详细信息
        /// </summary>
        [JsonProperty]
        [Required(ErrorMessage = "K线结构信息不能为空")]
        public KLineStructure Structure { get; set; }

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
        /// 边界检测置信度（0-1）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "边界置信度必须在0-1之间")]
        public double BoundaryConfidence { get; set; }

        /// <summary>
        /// K线形态类型
        /// </summary>
        [JsonProperty]
        public KLinePatternType PatternType => Structure?.PatternType ?? KLinePatternType.Unknown;

        /// <summary>
        /// K线形态强度评分（0-1）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "形态强度评分必须在0-1之间")]
        public double PatternStrength => Structure?.ClarityScore ?? 0.0;

        #endregion

        #region 成交量和市场信息

        /// <summary>
        /// 成交量（如果可获取）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "成交量不能为负数")]
        public double Volume { get; set; }

        /// <summary>
        /// 成交额（如果可获取）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "成交额不能为负数")]
        public double Amount { get; set; }

        /// <summary>
        /// 持仓量（期货，如果可获取）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "持仓量不能为负数")]
        public double OpenInterest { get; set; }

        /// <summary>
        /// 持仓量变化
        /// </summary>
        [JsonProperty]
        public double OpenInterestChange { get; set; }

        #endregion

        #region 技术指标信息

        /// <summary>
        /// 移动平均线值（可选）
        /// </summary>
        [JsonProperty]
        public Dictionary<string, double> MovingAverages { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// 技术指标值（可选）
        /// </summary>
        [JsonProperty]
        public Dictionary<string, double> TechnicalIndicators { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// 支撑位价格
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "支撑位价格不能为负数")]
        public double SupportLevel { get; set; }

        /// <summary>
        /// 阻力位价格
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "阻力位价格不能为负数")]
        public double ResistanceLevel { get; set; }

        #endregion

        #region 质量和置信度信息

        /// <summary>
        /// 整体识别置信度（0-1）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "整体置信度必须在0-1之间")]
        public double Confidence { get; set; }

        /// <summary>
        /// 识别质量评分（0-1）
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "质量评分必须在0-1之间")]
        public double QualityScore { get; set; }

        /// <summary>
        /// 数据完整度评分（0-1）
        /// 基于价格、结构、颜色等数据的完整程度
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "数据完整度评分必须在0-1之间")]
        public double CompletenessScore => CalculateCompletenessScore();

        /// <summary>
        /// 识别时间
        /// </summary>
        [JsonProperty]
        public DateTime RecognitionTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 数据来源（截图、文件、API等）
        /// </summary>
        [JsonProperty]
        public string DataSource { get; set; }

        /// <summary>
        /// 处理耗时（毫秒）
        /// </summary>
        [JsonProperty]
        [Range(0.0, double.MaxValue, ErrorMessage = "处理耗时不能为负数")]
        public double ProcessingTimeMs { get; set; }

        #endregion

        #region 关联和上下文信息

        /// <summary>
        /// 前一根K线信息（可选）
        /// </summary>
        [JsonProperty]
        public KLineInfo PreviousKLine { get; set; }

        /// <summary>
        /// 后一根K线信息（可选）
        /// </summary>
        [JsonProperty]
        public KLineInfo NextKLine { get; set; }

        /// <summary>
        /// 所属的K线序列ID
        /// </summary>
        [JsonProperty]
        public string SequenceId { get; set; }

        /// <summary>
        /// 在序列中的位置
        /// </summary>
        [JsonProperty]
        [Range(0, int.MaxValue, ErrorMessage = "序列位置不能为负数")]
        public int SequencePosition { get; set; }

        /// <summary>
        /// 图表类型（K线图、美国线等）
        /// </summary>
        [JsonProperty]
        public string ChartType { get; set; }

        /// <summary>
        /// 交易品种代码
        /// </summary>
        [JsonProperty]
        public string Symbol { get; set; }

        /// <summary>
        /// 交易所名称
        /// </summary>
        [JsonProperty]
        public string Exchange { get; set; }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证K线数据的完整性和有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateKLineData()
        {
            var errors = new List<string>();

            try
            {
                // 1. 基本数据验证
                if (Bounds.IsEmpty)
                    errors.Add("K线边界不能为空");

                if (Structure == null)
                    errors.Add("K线结构信息不能为空");
                else
                {
                    var structureValidation = Structure.ValidateStructure();
                    if (!structureValidation.IsValid)
                        errors.AddRange(structureValidation.Errors);
                }

                // 2. 价格数据验证
                if (OpenPrice <= 0 || ClosePrice <= 0 || HighPrice <= 0 || LowPrice <= 0)
                    errors.Add("价格数据必须大于0");

                if (HighPrice < Math.Max(OpenPrice, ClosePrice))
                    errors.Add("最高价不能小于开盘价或收盘价");

                if (LowPrice > Math.Min(OpenPrice, ClosePrice))
                    errors.Add("最低价不能大于开盘价或收盘价");

                if (HighPrice < LowPrice)
                    errors.Add("最高价不能小于最低价");

                // 3. 价格一致性验证
                if (Math.Abs(PriceRange - (UpperShadowPriceRange + BodyPriceRange + LowerShadowPriceRange)) > GetPriceTolerance())
                    errors.Add("价格区间计算不一致");

                // 4. 颜色和价格一致性验证
                ValidateColorPriceConsistency(errors);

                // 5. 置信度验证
                if (Confidence < 0 || Confidence > 1)
                    errors.Add("置信度必须在0-1之间");

                if (ColorConfidence < 0 || ColorConfidence > 1)
                    errors.Add("颜色置信度必须在0-1之间");

                if (BoundaryConfidence < 0 || BoundaryConfidence > 1)
                    errors.Add("边界置信度必须在0-1之间");

                // 6. 边界一致性验证
                ValidateBoundaryConsistency(errors);

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
        /// 验证颜色和价格的一致性
        /// </summary>
        /// <param name="errors">错误列表</param>
        private void ValidateColorPriceConsistency(List<string> errors)
        {
            try
            {
                // 检查颜色和价格变动是否匹配
                if (IsBullish && Color == KLineColor.Red)
                {
                    errors.Add("价格显示为上涨但颜色为红色（中国市场通常红色表示下跌）");
                }
                else if (IsBearish && Color == KLineColor.Green)
                {
                    errors.Add("价格显示为下跌但颜色为绿色（中国市场通常绿色表示上涨）");
                }

                // 检查十字星的一致性
                if (IsDojiPrice && PatternType != KLinePatternType.Doji && !Structure.IsDoji)
                {
                    // 价格是十字星但结构不是，可能是数据不一致
                    // 这只是警告，不作为错误
                }
            }
            catch (Exception ex)
            {
                errors.Add($"颜色价格一致性验证失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证边界一致性
        /// </summary>
        /// <param name="errors">错误列表</param>
        private void ValidateBoundaryConsistency(List<string> errors)
        {
            try
            {
                if (!Bounds.IsEmpty)
                {
                    if (!BodyBoundary.IsEmpty && !Bounds.Contains(BodyBoundary))
                        errors.Add("实体边界超出K线完整边界");

                    if (!UpperShadowBoundary.IsEmpty && !Bounds.Contains(UpperShadowBoundary))
                        errors.Add("上影线边界超出K线完整边界");

                    if (!LowerShadowBoundary.IsEmpty && !Bounds.Contains(LowerShadowBoundary))
                        errors.Add("下影线边界超出K线完整边界");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"边界一致性验证失败: {ex.Message}");
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取价格容差
        /// </summary>
        /// <returns>价格容差</returns>
        private double GetPriceTolerance()
        {
            return Math.Max(0.001, OpenPrice * 0.0001); // 最小0.001，或价格的万分之一
        }

        /// <summary>
        /// 计算数据完整度评分
        /// </summary>
        /// <returns>完整度评分（0-1）</returns>
        private double CalculateCompletenessScore()
        {
            double score = 0.0;
            int totalFactors = 0;

            // 基础信息（30%）
            if (!Bounds.IsEmpty) { score += 0.1; }
            if (Structure != null) { score += 0.1; }
            if (Color != KLineColor.Unknown) { score += 0.1; }
            totalFactors += 3;

            // 价格信息（40%）
            if (OpenPrice > 0) { score += 0.1; }
            if (ClosePrice > 0) { score += 0.1; }
            if (HighPrice > 0) { score += 0.1; }
            if (LowPrice > 0) { score += 0.1; }
            totalFactors += 4;

            // 质量信息（20%）
            if (Confidence > 0) { score += 0.05; }
            if (ColorConfidence > 0) { score += 0.05; }
            if (BoundaryConfidence > 0) { score += 0.05; }
            if (QualityScore > 0) { score += 0.05; }
            totalFactors += 4;

            // 扩展信息（10%）
            if (Volume > 0) { score += 0.025; }
            if (!string.IsNullOrEmpty(Symbol)) { score += 0.025; }
            if (!string.IsNullOrEmpty(TimeFrame)) { score += 0.025; }
            if (Timestamp > DateTime.MinValue) { score += 0.025; }
            totalFactors += 4;

            return totalFactors > 0 ? score / totalFactors * 4 : 0.0; // 归一化到0-1
        }

        /// <summary>
        /// 获取K线简要描述
        /// </summary>
        /// <returns>描述文本</returns>
        public string GetDescription()
        {
            var parts = new List<string>();

            // 基本类型
            if (IsBullish)
                parts.Add("阳线");
            else if (IsBearish)
                parts.Add("阴线");
            else
                parts.Add("平盘");

            // 形态
            if (Structure != null)
            {
                parts.Add(Structure.GetStructureDescription());
            }

            // 价格变动
            if (Math.Abs(PriceChangePercent) > 0.01)
            {
                parts.Add($"{PriceChangePercent:F2}%");
            }

            // 置信度
            if (Confidence < 0.8)
            {
                parts.Add($"置信度{Confidence:F1}");
            }

            return string.Join(" ", parts);
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
        /// <returns>KLineInfo实例</returns>
        public static KLineInfo FromJson(string json)
        {
            return JsonConvert.DeserializeObject<KLineInfo>(json);
        }

        /// <summary>
        /// 创建标准K线信息
        /// </summary>
        /// <param name="bounds">边界</param>
        /// <param name="openPrice">开盘价</param>
        /// <param name="closePrice">收盘价</param>
        /// <param name="highPrice">最高价</param>
        /// <param name="lowPrice">最低价</param>
        /// <param name="color">颜色</param>
        /// <param name="structure">结构</param>
        /// <returns>K线信息实例</returns>
        public static KLineInfo CreateStandard(Rectangle bounds, double openPrice, double closePrice,
            double highPrice, double lowPrice, KLineColor color, KLineStructure structure)
        {
            return new KLineInfo
            {
                Bounds = bounds,
                OpenPrice = openPrice,
                ClosePrice = closePrice,
                HighPrice = highPrice,
                LowPrice = lowPrice,
                Color = color,
                Structure = structure,
                Timestamp = DateTime.Now,
                Confidence = 0.8,
                QualityScore = 0.8
            };
        }

        /// <summary>
        /// 克隆K线信息
        /// </summary>
        /// <returns>克隆的K线信息</returns>
        public KLineInfo Clone()
        {
            return new KLineInfo
            {
                // 基本信息克隆
                Bounds = Bounds,
                Index = Index,
                TimeFrame = TimeFrame,
                Timestamp = Timestamp,

                // 价格信息克隆
                OpenPrice = OpenPrice,
                ClosePrice = ClosePrice,
                HighPrice = HighPrice,
                LowPrice = LowPrice,
                Volume = Volume,
                Amount = Amount,
                OpenInterest = OpenInterest,
                OpenInterestChange = OpenInterestChange,

                // 颜色信息克隆
                Color = Color,
                ColorConfidence = ColorConfidence,
                DominantColorHsv = DominantColorHsv,
                DominantColorRgb = DominantColorRgb,

                // 结构信息克隆
                Structure = Structure, // 注意：这里只是引用复制
                BodyBoundary = BodyBoundary,
                UpperShadowBoundary = UpperShadowBoundary,
                LowerShadowBoundary = LowerShadowBoundary,
                BoundaryConfidence = BoundaryConfidence,

                // 技术指标克隆
                MovingAverages = new Dictionary<string, double>(MovingAverages),
                TechnicalIndicators = new Dictionary<string, double>(TechnicalIndicators),
                SupportLevel = SupportLevel,
                ResistanceLevel = ResistanceLevel,

                // 质量信息克隆
                Confidence = Confidence,
                QualityScore = QualityScore,
                RecognitionTime = RecognitionTime,
                DataSource = DataSource,
                ProcessingTimeMs = ProcessingTimeMs,

                // 关联信息克隆
                PreviousKLine = PreviousKLine, // 注意：这里只是引用复制
                NextKLine = NextKLine, // 注意：这里只是引用复制
                SequenceId = SequenceId,
                SequencePosition = SequencePosition,
                ChartType = ChartType,
                Symbol = Symbol,
                Exchange = Exchange
            };
        }

        #endregion
    }
}