using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 影线测量工具类
    /// 提供精确的K线影线测量功能，支持多种测量模式和可视化效果
    /// </summary>
    public class ShadowMeasurementTool
    {
        #region 私有字段

        private readonly CoordinateHelper _coordinateHelper;
        private readonly List<ShadowMeasurement> _measurements;
        private ShadowMeasurement _currentMeasurement;
        private ShadowMeasurementMode _measurementMode;
        private bool _showMeasurementLines;
        private bool _showMeasurementLabels;
        private bool _showMeasurementValues;
        private Color _measurementColor;
        private int _measurementThickness;
        private Font _labelFont;
        private Font _valueFont;
        private readonly Pen _measurementPen;
        private readonly SolidBrush _labelBrush;
        private readonly SolidBrush _valueBrush;
        private readonly SolidBrush _backgroundBrush;

        #endregion

        #region 枚举

        /// <summary>
        /// 影线测量模式
        /// </summary>
        public enum ShadowMeasurementMode
        {
            /// <summary>
            /// 上影线测量
            /// </summary>
            UpperShadow,
            /// <summary>
            /// 下影线测量
            /// </summary>
            LowerShadow,
            /// <summary>
            /// 完整影线测量
            /// </summary>
            FullShadow,
            /// <summary>
            /// 主体测量（实体部分）
            /// </summary>
            BodyMeasurement,
            /// <summary>
            /// 完整K线测量
            /// </summary>
            FullCandle
        }

        #endregion

        #region 构造函数

        public ShadowMeasurementTool(CoordinateHelper coordinateHelper)
        {
            _coordinateHelper = coordinateHelper ?? throw new ArgumentNullException(nameof(coordinateHelper));
            _measurements = new List<ShadowMeasurement>();
            _measurementMode = ShadowMeasurementMode.FullShadow;
            _showMeasurementLines = true;
            _showMeasurementLabels = true;
            _showMeasurementValues = true;
            _measurementColor = Color.Orange;
            _measurementThickness = 2;

            _labelFont = new Font("Arial", 8, FontStyle.Bold);
            _valueFont = new Font("Arial", 7);
            _measurementPen = new Pen(_measurementColor, _measurementThickness);
            _labelBrush = new SolidBrush(_measurementColor);
            _valueBrush = new SolidBrush(Color.DarkBlue);
            _backgroundBrush = new SolidBrush(Color.FromArgb(240, Color.White));
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前测量模式
        /// </summary>
        public ShadowMeasurementMode MeasurementMode
        {
            get => _measurementMode;
            set
            {
                if (_measurementMode != value)
                {
                    _measurementMode = value;
                    _currentMeasurement = null;
                }
            }
        }

        /// <summary>
        /// 是否显示测量线
        /// </summary>
        public bool ShowMeasurementLines
        {
            get => _showMeasurementLines;
            set => _showMeasurementLines = value;
        }

        /// <summary>
        /// 是否显示测量标签
        /// </summary>
        public bool ShowMeasurementLabels
        {
            get => _showMeasurementLabels;
            set => _showMeasurementLabels = value;
        }

        /// <summary>
        /// 是否显示测量数值
        /// </summary>
        public bool ShowMeasurementValues
        {
            get => _showMeasurementValues;
            set => _showMeasurementValues = value;
        }

        /// <summary>
        /// 测量颜色
        /// </summary>
        public Color MeasurementColor
        {
            get => _measurementColor;
            set
            {
                if (_measurementColor != value)
                {
                    _measurementColor = value;
                    _measurementPen.Color = value;
                    _labelBrush.Color = value;
                }
            }
        }

        /// <summary>
        /// 测量线粗细
        /// </summary>
        public int MeasurementThickness
        {
            get => _measurementThickness;
            set
            {
                if (_measurementThickness != value && value > 0)
                {
                    _measurementThickness = value;
                    _measurementPen.Width = value;
                }
            }
        }

        /// <summary>
        /// 当前测量结果
        /// </summary>
        public ShadowMeasurement CurrentMeasurement => _currentMeasurement;

        /// <summary>
        /// 所有测量历史
        /// </summary>
        public IReadOnlyList<ShadowMeasurement> MeasurementHistory => _measurements.AsReadOnly();

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始新的测量
        /// </summary>
        /// <param name="startPoint">起始点</param>
        /// <param name="candleData">K线数据（可选）</param>
        public void StartMeasurement(Point startPoint, CandleData candleData = null)
        {
            _currentMeasurement = new ShadowMeasurement
            {
                StartPoint = startPoint,
                StartTime = DateTime.Now,
                Mode = _measurementMode,
                CandleData = candleData,
                StartPrice = _coordinateHelper.YToPrice(startPoint.Y)
            };
        }

        /// <summary>
        /// 更新测量终点
        /// </summary>
        /// <param name="endPoint">终点</param>
        public void UpdateMeasurement(Point endPoint)
        {
            if (_currentMeasurement == null) return;

            _currentMeasurement.EndPoint = endPoint;
            _currentMeasurement.EndPrice = _coordinateHelper.YToPrice(endPoint.Y);
            _currentMeasurement.Length = Math.Abs(_currentMeasurement.EndPrice - _currentMeasurement.StartPrice);
            _currentMeasurement.PixelLength = Math.Abs(endPoint.Y - _currentMeasurement.StartPoint.Y);

            // 根据测量模式计算额外信息
            CalculateMeasurementDetails();
        }

        /// <summary>
        /// 完成测量
        /// </summary>
        public void CompleteMeasurement()
        {
            if (_currentMeasurement == null) return;

            _currentMeasurement.EndTime = DateTime.Now;
            _currentMeasurement.IsCompleted = true;

            _measurements.Add(_currentMeasurement);

            // 限制历史记录数量
            if (_measurements.Count > 100)
            {
                _measurements.RemoveAt(0);
            }
        }

        /// <summary>
        /// 取消当前测量
        /// </summary>
        public void CancelMeasurement()
        {
            _currentMeasurement = null;
        }

        /// <summary>
        /// 清除所有测量历史
        /// </summary>
        public void ClearHistory()
        {
            _measurements.Clear();
            _currentMeasurement = null;
        }

        /// <summary>
        /// 删除指定的测量记录
        /// </summary>
        /// <param name="measurement">要删除的测量记录</param>
        /// <returns>是否删除成功</returns>
        public bool RemoveMeasurement(ShadowMeasurement measurement)
        {
            return _measurements.Remove(measurement);
        }

        /// <summary>
        /// 基于K线数据自动测量影线
        /// </summary>
        /// <param name="candleData">K线数据</param>
        /// <param name="referencePoint">参考点位置</param>
        /// <returns>测量结果</returns>
        public ShadowMeasurement AutoMeasureShadow(CandleData candleData, Point referencePoint)
        {
            if (candleData == null) return null;

            var measurement = new ShadowMeasurement
            {
                CandleData = candleData,
                Mode = _measurementMode,
                StartTime = DateTime.Now,
                IsCompleted = true
            };

            switch (_measurementMode)
            {
                case ShadowMeasurementMode.UpperShadow:
                    measurement = MeasureUpperShadow(candleData, referencePoint, measurement);
                    break;
                case ShadowMeasurementMode.LowerShadow:
                    measurement = MeasureLowerShadow(candleData, referencePoint, measurement);
                    break;
                case ShadowMeasurementMode.FullShadow:
                    measurement = MeasureFullShadow(candleData, referencePoint, measurement);
                    break;
                case ShadowMeasurementMode.BodyMeasurement:
                    measurement = MeasureBody(candleData, referencePoint, measurement);
                    break;
                case ShadowMeasurementMode.FullCandle:
                    measurement = MeasureFullCandle(candleData, referencePoint, measurement);
                    break;
            }

            if (measurement != null)
            {
                _measurements.Add(measurement);
                _currentMeasurement = measurement;
            }

            return measurement;
        }

        /// <summary>
        /// 绘制测量结果
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        public void DrawMeasurements(Graphics graphics)
        {
            if (graphics == null) return;

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 绘制历史测量
            foreach (var measurement in _measurements)
            {
                DrawMeasurement(graphics, measurement, false);
            }

            // 绘制当前测量
            if (_currentMeasurement != null && !_currentMeasurement.IsCompleted)
            {
                DrawMeasurement(graphics, _currentMeasurement, true);
            }
        }

        /// <summary>
        /// 获取测量统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public ShadowMeasurementStatistics GetStatistics()
        {
            var completedMeasurements = _measurements.Where(m => m.IsCompleted).ToList();

            if (!completedMeasurements.Any())
            {
                return new ShadowMeasurementStatistics();
            }

            return new ShadowMeasurementStatistics
            {
                TotalMeasurements = completedMeasurements.Count,
                AverageLength = completedMeasurements.Average(m => m.Length),
                MaxLength = completedMeasurements.Max(m => m.Length),
                MinLength = completedMeasurements.Min(m => m.Length),
                AveragePixelLength = completedMeasurements.Average(m => m.PixelLength),
                ModeDistribution = completedMeasurements
                    .GroupBy(m => m.Mode)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// 查找最近的测量
        /// </summary>
        /// <param name="point">查找点</param>
        /// <param name="maxDistance">最大距离</param>
        /// <returns>最近的测量</returns>
        public ShadowMeasurement FindNearestMeasurement(Point point, int maxDistance = 10)
        {
            return _measurements
                .Where(m => IsPointNearMeasurement(point, m, maxDistance))
                .OrderBy(m => GetDistanceToMeasurement(point, m))
                .FirstOrDefault();
        }

        #endregion

        #region 私有方法

        private void CalculateMeasurementDetails()
        {
            if (_currentMeasurement == null || _currentMeasurement.CandleData == null) return;

            var candle = _currentMeasurement.CandleData;

            switch (_measurementMode)
            {
                case ShadowMeasurementMode.UpperShadow:
                    _currentMeasurement.UpperShadowLength = candle.High - Math.Max(candle.Open, candle.Close);
                    _currentMeasurement.BodyRatio = _currentMeasurement.UpperShadowLength / Math.Abs(candle.Close - candle.Open);
                    break;
                case ShadowMeasurementMode.LowerShadow:
                    _currentMeasurement.LowerShadowLength = Math.Min(candle.Open, candle.Close) - candle.Low;
                    _currentMeasurement.BodyRatio = _currentMeasurement.LowerShadowLength / Math.Abs(candle.Close - candle.Open);
                    break;
                case ShadowMeasurementMode.FullShadow:
                    _currentMeasurement.UpperShadowLength = candle.High - Math.Max(candle.Open, candle.Close);
                    _currentMeasurement.LowerShadowLength = Math.Min(candle.Open, candle.Close) - candle.Low;
                    _currentMeasurement.TotalShadowLength = candle.High - candle.Low;
                    _currentMeasurement.BodyRatio = _currentMeasurement.TotalShadowLength / Math.Abs(candle.Close - candle.Open);
                    break;
            }
        }

        private ShadowMeasurement MeasureUpperShadow(CandleData candleData, Point referencePoint, ShadowMeasurement measurement)
        {
            var topY = _coordinateHelper.PriceToY(candleData.High);
            var bodyY = _coordinateHelper.PriceToY(Math.Max(candleData.Open, candleData.Close));

            measurement.StartPoint = new Point(referencePoint.X, topY);
            measurement.EndPoint = new Point(referencePoint.X, bodyY);
            measurement.StartPrice = candleData.High;
            measurement.EndPrice = Math.Max(candleData.Open, candleData.Close);
            measurement.Length = candleData.High - Math.Max(candleData.Open, candleData.Close);
            measurement.PixelLength = Math.Abs(bodyY - topY);
            measurement.UpperShadowLength = measurement.Length;

            return measurement;
        }

        private ShadowMeasurement MeasureLowerShadow(CandleData candleData, Point referencePoint, ShadowMeasurement measurement)
        {
            var bodyY = _coordinateHelper.PriceToY(Math.Min(candleData.Open, candleData.Close));
            var bottomY = _coordinateHelper.PriceToY(candleData.Low);

            measurement.StartPoint = new Point(referencePoint.X, bodyY);
            measurement.EndPoint = new Point(referencePoint.X, bottomY);
            measurement.StartPrice = Math.Min(candleData.Open, candleData.Close);
            measurement.EndPrice = candleData.Low;
            measurement.Length = Math.Min(candleData.Open, candleData.Close) - candleData.Low;
            measurement.PixelLength = Math.Abs(bottomY - bodyY);
            measurement.LowerShadowLength = measurement.Length;

            return measurement;
        }

        private ShadowMeasurement MeasureFullShadow(CandleData candleData, Point referencePoint, ShadowMeasurement measurement)
        {
            var topY = _coordinateHelper.PriceToY(candleData.High);
            var bottomY = _coordinateHelper.PriceToY(candleData.Low);

            measurement.StartPoint = new Point(referencePoint.X, topY);
            measurement.EndPoint = new Point(referencePoint.X, bottomY);
            measurement.StartPrice = candleData.High;
            measurement.EndPrice = candleData.Low;
            measurement.Length = candleData.High - candleData.Low;
            measurement.PixelLength = Math.Abs(bottomY - topY);
            measurement.UpperShadowLength = candleData.High - Math.Max(candleData.Open, candleData.Close);
            measurement.LowerShadowLength = Math.Min(candleData.Open, candleData.Close) - candleData.Low;
            measurement.TotalShadowLength = measurement.Length;

            return measurement;
        }

        private ShadowMeasurement MeasureBody(CandleData candleData, Point referencePoint, ShadowMeasurement measurement)
        {
            var openY = _coordinateHelper.PriceToY(candleData.Open);
            var closeY = _coordinateHelper.PriceToY(candleData.Close);

            measurement.StartPoint = new Point(referencePoint.X, openY);
            measurement.EndPoint = new Point(referencePoint.X, closeY);
            measurement.StartPrice = candleData.Open;
            measurement.EndPrice = candleData.Close;
            measurement.Length = Math.Abs(candleData.Close - candleData.Open);
            measurement.PixelLength = Math.Abs(closeY - openY);

            return measurement;
        }

        private ShadowMeasurement MeasureFullCandle(CandleData candleData, Point referencePoint, ShadowMeasurement measurement)
        {
            var topY = _coordinateHelper.PriceToY(candleData.High);
            var bottomY = _coordinateHelper.PriceToY(candleData.Low);

            measurement.StartPoint = new Point(referencePoint.X, topY);
            measurement.EndPoint = new Point(referencePoint.X, bottomY);
            measurement.StartPrice = candleData.High;
            measurement.EndPrice = candleData.Low;
            measurement.Length = candleData.High - candleData.Low;
            measurement.PixelLength = Math.Abs(bottomY - topY);

            // 包含完整的K线信息
            measurement.UpperShadowLength = candleData.High - Math.Max(candleData.Open, candleData.Close);
            measurement.LowerShadowLength = Math.Min(candleData.Open, candleData.Close) - candleData.Low;
            measurement.BodyLength = Math.Abs(candleData.Close - candleData.Open);

            return measurement;
        }

        private void DrawMeasurement(Graphics g, ShadowMeasurement measurement, bool isCurrent)
        {
            if (!measurement.StartPoint.HasValue || !measurement.EndPoint.HasValue) return;

            var pen = isCurrent ?
                new Pen(MeasurementColor, MeasurementThickness) { DashStyle = DashStyle.Dash } :
                _measurementPen;

            // 绘制测量线
            if (_showMeasurementLines)
            {
                g.DrawLine(pen, measurement.StartPoint.Value, measurement.EndPoint.Value);
            }

            // 绘制端点
            DrawEndpoint(g, measurement.StartPoint.Value, isCurrent);
            DrawEndpoint(g, measurement.EndPoint.Value, isCurrent);

            // 绘制标签
            if (_showMeasurementLabels)
            {
                DrawMeasurementLabel(g, measurement, isCurrent);
            }

            // 绘制数值
            if (_showMeasurementValues)
            {
                DrawMeasurementValue(g, measurement, isCurrent);
            }

            pen?.Dispose();
        }

        private void DrawEndpoint(Graphics g, Point point, bool isCurrent)
        {
            const int radius = 4;
            var rect = new Rectangle(point.X - radius, point.Y - radius, radius * 2, radius * 2);

            using (var brush = new SolidBrush(isCurrent ? Color.Red : MeasurementColor))
            using (var pen = new Pen(Color.White, 1))
            {
                g.FillEllipse(brush, rect);
                g.DrawEllipse(pen, rect);
            }
        }

        private void DrawMeasurementLabel(Graphics g, ShadowMeasurement measurement, bool isCurrent)
        {
            var labelText = GetMeasurementLabel(measurement);
            var textSize = g.MeasureString(labelText, _labelFont);

            var centerX = (measurement.StartPoint.Value.X + measurement.EndPoint.Value.X) / 2;
            var centerY = (measurement.StartPoint.Value.Y + measurement.EndPoint.Value.Y) / 2;

            var labelRect = new Rectangle(
                centerX - (int)textSize.Width / 2 - 5,
                centerY - (int)textSize.Height / 2 - 2,
                (int)textSize.Width + 10,
                (int)textSize.Height + 4);

            // 确保标签在控件范围内
            labelRect = ConstrainRectangle(labelRect, g.VisibleClipBounds);

            using (var brush = new SolidBrush(isCurrent ? Color.FromArgb(200, Color.Yellow) : _backgroundBrush))
            using (var pen = new Pen(MeasurementColor, 1))
            {
                g.FillRectangle(brush, labelRect);
                g.DrawRectangle(pen, labelRect);
            }

            g.DrawString(labelText, _labelFont, _labelBrush, labelRect.X + 5, labelRect.Y + 2);
        }

        private void DrawMeasurementValue(Graphics g, ShadowMeasurement measurement, bool isCurrent)
        {
            var valueText = GetMeasurementValue(measurement);
            var textSize = g.MeasureString(valueText, _valueFont);

            var endPoint = measurement.EndPoint.Value;
            var valueRect = new Rectangle(
                endPoint.X + 10,
                endPoint.Y - (int)textSize.Height / 2,
                (int)textSize.Width + 6,
                (int)textSize.Height + 2);

            // 确保值标签不超出控件范围
            if (valueRect.Right > g.VisibleClipBounds.Right)
            {
                valueRect.X = endPoint.X - (int)textSize.Width - 12;
            }

            using (var brush = new SolidBrush(isCurrent ? Color.FromArgb(200, Color.LightBlue) : _backgroundBrush))
            using (var pen = new Pen(MeasurementColor, 1))
            {
                g.FillRectangle(brush, valueRect);
                g.DrawRectangle(pen, valueRect);
            }

            g.DrawString(valueText, _valueFont, _valueBrush, valueRect.X + 3, valueRect.Y + 1);
        }

        private string GetMeasurementLabel(ShadowMeasurement measurement)
        {
            return measurement.Mode switch
            {
                ShadowMeasurementMode.UpperShadow => "上影线",
                ShadowMeasurementMode.LowerShadow => "下影线",
                ShadowMeasurementMode.FullShadow => "影线",
                ShadowMeasurementMode.BodyMeasurement => "实体",
                ShadowMeasurementMode.FullCandle => "K线",
                _ => "测量"
            };
        }

        private string GetMeasurementValue(ShadowMeasurement measurement)
        {
            return $"{measurement.Length:F2} ({measurement.PixelLength}px)";
        }

        private Rectangle ConstrainRectangle(Rectangle rect, Rectangle bounds)
        {
            if (rect.X < bounds.X) rect.X = bounds.X;
            if (rect.Y < bounds.Y) rect.Y = bounds.Y;
            if (rect.Right > bounds.Right) rect.X = bounds.Right - rect.Width;
            if (rect.Bottom > bounds.Bottom) rect.Y = bounds.Bottom - rect.Height;
            return rect;
        }

        private bool IsPointNearMeasurement(Point point, ShadowMeasurement measurement, int maxDistance)
        {
            if (!measurement.StartPoint.HasValue || !measurement.EndPoint.HasValue) return false;

            var distanceToStart = Math.Sqrt(
                Math.Pow(point.X - measurement.StartPoint.Value.X, 2) +
                Math.Pow(point.Y - measurement.StartPoint.Value.Y, 2));

            var distanceToEnd = Math.Sqrt(
                Math.Pow(point.X - measurement.EndPoint.Value.X, 2) +
                Math.Pow(point.Y - measurement.EndPoint.Value.Y, 2));

            return distanceToStart <= maxDistance || distanceToEnd <= maxDistance;
        }

        private double GetDistanceToMeasurement(Point point, ShadowMeasurement measurement)
        {
            if (!measurement.StartPoint.HasValue || !measurement.EndPoint.HasValue) return double.MaxValue;

            var distanceToStart = Math.Sqrt(
                Math.Pow(point.X - measurement.StartPoint.Value.X, 2) +
                Math.Pow(point.Y - measurement.StartPoint.Value.Y, 2));

            var distanceToEnd = Math.Sqrt(
                Math.Pow(point.X - measurement.EndPoint.Value.X, 2) +
                Math.Pow(point.Y - measurement.EndPoint.Value.Y, 2));

            return Math.Min(distanceToStart, distanceToEnd);
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _measurementPen?.Dispose();
            _labelBrush?.Dispose();
            _valueBrush?.Dispose();
            _backgroundBrush?.Dispose();
            _labelFont?.Dispose();
            _valueFont?.Dispose();
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 影线测量结果
    /// </summary>
    public class ShadowMeasurement
    {
        public Point? StartPoint { get; set; }
        public Point? EndPoint { get; set; }
        public double StartPrice { get; set; }
        public double EndPrice { get; set; }
        public double Length { get; set; }
        public int PixelLength { get; set; }
        public ShadowMeasurementTool.ShadowMeasurementMode Mode { get; set; }
        public CandleData CandleData { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsCompleted { get; set; }

        // 影线特定属性
        public double UpperShadowLength { get; set; }
        public double LowerShadowLength { get; set; }
        public double TotalShadowLength { get; set; }
        public double BodyLength { get; set; }
        public double BodyRatio { get; set; }
    }

    /// <summary>
    /// 影线测量统计信息
    /// </summary>
    public class ShadowMeasurementStatistics
    {
        public int TotalMeasurements { get; set; }
        public double AverageLength { get; set; }
        public double MaxLength { get; set; }
        public double MinLength { get; set; }
        public double AveragePixelLength { get; set; }
        public Dictionary<ShadowMeasurementTool.ShadowMeasurementMode, int> ModeDistribution { get; set; } = new();
    }

    #endregion
}