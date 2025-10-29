using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using BoshenCC.Core.Services;
using BoshenCC.Models;
using BoshenCC.WinForms.Utils;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 预测线可视化集成控件
    /// 集成Stream A、B、C的所有控件，提供完整的波神算法可视化解决方案
    /// </summary>
    public partial class PredictionVisualizationIntegration : UserControl
    {
        #region 私有字段

        private readonly BoshenAlgorithmService _algorithmService;
        private readonly KLineSelector _kLineSelector;
        private readonly PredictionDisplay _predictionDisplay;
        private readonly LineCanvas _lineCanvas;
        private readonly ChartRenderer _chartRenderer;
        private readonly VisualEffects.ParticleSystem _particleSystem;

        private List<PredictionLine> _currentPredictionLines;
        private KLineInfo _currentKLine;
        private bool _isProcessing;

        #endregion

        #region 构造函数

        public PredictionVisualizationIntegration()
        {
            InitializeComponent();
            InitializeServices();
            InitializeControls();
            InitializeEventHandlers();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前预测线列表
        /// </summary>
        public List<PredictionLine> CurrentPredictionLines
        {
            get => _currentPredictionLines;
            private set
            {
                _currentPredictionLines = value;
                OnPredictionLinesChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// 当前K线信息
        /// </summary>
        public KLineInfo CurrentKLine
        {
            get => _currentKLine;
            private set
            {
                _currentKLine = value;
                OnKLineChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// 是否正在处理
        /// </summary>
        public bool IsProcessing => _isProcessing;

        #endregion

        #region 公共事件

        /// <summary>
        /// 预测线改变事件
        /// </summary>
        public event EventHandler PredictionLinesChanged;

        /// <summary>
        /// K线改变事件
        /// </summary>
        public event EventHandler KLineChanged;

        /// <summary>
        /// 计算开始事件
        /// </summary>
        public event EventHandler CalculationStarted;

        /// <summary>
        /// 计算完成事件
        /// </summary>
        public event EventHandler<CalculationCompletedEventArgs> CalculationCompleted;

        /// <summary>
        /// 错误发生事件
        /// </summary>
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置K线图像
        /// </summary>
        /// <param name="image">K线图像</param>
        /// <param name="kLineInfo">K线信息</param>
        public void SetKLineImage(Image image, KLineInfo kLineInfo)
        {
            if (image == null || kLineInfo == null)
                return;

            _kLineSelector.BackgroundImage = image;
            _kLineSelector.SetPriceRange(kLineInfo.LowPrice, kLineInfo.HighPrice);
            CurrentKLine = kLineInfo;

            // 清除之前的选择和预测线
            ClearPrediction();
        }

        /// <summary>
        /// 手动设置A点和B点并计算预测线
        /// </summary>
        /// <param name="pointAPrice">A点价格</param>
        /// <param name="pointBPrice">B点价格</param>
        /// <param name="symbol">交易品种</param>
        /// <param name="timeFrame">时间周期</param>
        public async Task CalculatePredictionAsync(double pointAPrice, double pointBPrice, string symbol = null, string timeFrame = null)
        {
            if (pointAPrice <= 0 || pointBPrice <= 0 || pointBPrice <= pointAPrice)
            {
                OnErrorOccurred(new ErrorEventArgs("价格参数无效：A点价格必须大于0，B点价格必须大于A点价格"));
                return;
            }

            try
            {
                _isProcessing = true;
                OnCalculationStarted(EventArgs.Empty);

                // 使用算法服务计算预测线
                var predictionLines = await _algorithmService.CalculateBoshenLinesAsync(pointAPrice, pointBPrice, symbol, timeFrame);

                // 更新当前预测线
                CurrentPredictionLines = predictionLines;

                // 更新各个控件
                await UpdateControlsWithPredictionLines(predictionLines);

                // 添加视觉效果
                AddSuccessEffects();

                OnCalculationCompleted(new CalculationCompletedEventArgs(predictionLines, true, null));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"计算预测线失败: {ex.Message}"));
                OnCalculationCompleted(new CalculationCompletedEventArgs(null, false, ex.Message));
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 清除预测线
        /// </summary>
        public void ClearPrediction()
        {
            _kLineSelector.ClearSelection();
            _predictionDisplay.ClearPredictionLines();
            _lineCanvas.ClearLayers();
            _chartRenderer.ClearData();
            CurrentPredictionLines = new List<PredictionLine>();
        }

        /// <summary>
        /// 更新显示模式
        /// </summary>
        /// <param name="displayMode">显示模式</param>
        public void SetDisplayMode(PredictionDisplay.DisplayMode displayMode)
        {
            _predictionDisplay.SetDisplayMode(displayMode);
        }

        /// <summary>
        /// 更新渲染模式
        /// </summary>
        /// <param name="renderMode">渲染模式</param>
        public void SetRenderMode(ChartRenderer.RenderMode renderMode)
        {
            _chartRenderer.CurrentRenderMode = renderMode;
        }

        /// <summary>
        /// 更新图表主题
        /// </summary>
        /// <param name="theme">图表主题</param>
        public void SetChartTheme(ChartRenderer.ChartTheme theme)
        {
            _chartRenderer.CurrentTheme = theme;
        }

        /// <summary>
        /// 重置所有视图
        /// </summary>
        public void ResetAllViews()
        {
            _predictionDisplay.ResetView();
            _lineCanvas.ResetView();
            _chartRenderer.ResetView();
        }

        /// <summary>
        /// 导出当前视图为图像
        /// </summary>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <returns>导出的图像</returns>
        public Bitmap ExportCurrentView(int width, int height)
        {
            return _chartRenderer.ExportToImage(width, height);
        }

        /// <summary>
        /// 运行算法精度测试
        /// </summary>
        /// <returns>测试结果</returns>
        public async Task<BoshenTestResult> RunAlgorithmTest()
        {
            try
            {
                return await _algorithmService.RunStandardTestCasesAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"运行算法测试失败: {ex.Message}"));
                return null;
            }
        }

        #endregion

        #region 私有方法

        private void InitializeComponent()
        {
            SuspendLayout();

            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Name = "PredictionVisualizationIntegration";
            Size = new Size(1200, 800);

            ResumeLayout(false);
        }

        private void InitializeServices()
        {
            _algorithmService = new BoshenAlgorithmService();
            _particleSystem = new VisualEffects.ParticleSystem();
            _currentPredictionLines = new List<PredictionLine>();
        }

        private void InitializeControls()
        {
            // 创建K线选择器
            _kLineSelector = new KLineSelector
            {
                Dock = DockStyle.Left,
                Width = 400,
                BackColor = Color.FromArgb(248, 248, 248),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 创建预测线显示控件
            _predictionDisplay = new PredictionDisplay
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                CurrentAnimationMode = PredictionDisplay.AnimationMode.SlideIn,
                CurrentDisplayMode = PredictionDisplay.DisplayMode.Professional
            };

            // 创建线条画布
            _lineCanvas = new LineCanvas
            {
                Dock = DockStyle.Fill,
                Visible = false,
                CurrentRenderQuality = LineCanvas.RenderQuality.High
            };

            // 创建图表渲染器
            _chartRenderer = new ChartRenderer
            {
                Dock = DockStyle.Fill,
                Visible = false,
                CurrentRenderMode = ChartRenderer.RenderMode.Gradient,
                CurrentTheme = ChartRenderer.ChartTheme.Professional,
                EnableAnimation = true,
                ShowGrid = true,
                ShowLegend = true
            };

            // 创建分容器
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 400,
                BackColor = Color.White
            };

            // 右侧面板
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            // 右侧标签页
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            var predictionTab = new TabPage("预测线显示");
            var canvasTab = new TabPage("专业画布");
            var chartTab = new TabPage("图表渲染");

            predictionTab.Controls.Add(_predictionDisplay);
            canvasTab.Controls.Add(_lineCanvas);
            chartTab.Controls.Add(_chartRenderer);

            tabControl.TabPages.Add(predictionTab);
            tabControl.TabPages.Add(canvasTab);
            tabControl.TabPages.Add(chartTab);

            rightPanel.Controls.Add(tabControl);
            splitContainer.Panel1.Controls.Add(_kLineSelector);
            splitContainer.Panel2.Controls.Add(rightPanel);

            Controls.Add(splitContainer);
        }

        private void InitializeEventHandlers()
        {
            // K线选择器事件
            _kLineSelector.PointASelected += OnPointASelected;
            _kLineSelector.PointBSelected += OnPointBSelected;
            _kLineSelector.SelectionStateChanged += OnSelectionStateChanged;

            // 预测线显示控件事件
            _predictionDisplay.PredictionLineHover += OnPredictionLineHover;
            _predictionDisplay.PredictionLineClick += OnPredictionLineClick;
            _predictionDisplay.RenderStateChanged += OnRenderStateChanged;

            // 图表渲染器事件
            _chartRenderer.DataPointClick += OnDataPointClick;
            _chartRenderer.RenderComplete += OnChartRenderComplete;
        }

        private async void OnPointASelected(object sender, PointSelectedEventArgs e)
        {
            // 可以在这里添加A点选择的反馈效果
            AddSelectionEffects(e.Location, Color.Lime);
        }

        private async void OnPointBSelected(object sender, PointSelectedEventArgs e)
        {
            if (_kLineSelector.IsSelectionComplete())
            {
                var pointAPrice = _kLineSelector.GetPointAPrice() ?? 0;
                var pointBPrice = _kLineSelector.GetPointBPrice() ?? 0;

                if (pointAPrice > 0 && pointBPrice > 0)
                {
                    var symbol = CurrentKLine?.Symbol;
                    var timeFrame = CurrentKLine?.TimeFrame;

                    await CalculatePredictionAsync(pointAPrice, pointBPrice, symbol, timeFrame);
                }

                // 添加B点选择的反馈效果
                AddSelectionEffects(e.Location, Color.Red);
            }
        }

        private void OnSelectionStateChanged(object sender, SelectionStateChangedEventArgs e)
        {
            // 可以在这里添加选择状态变化的UI反馈
        }

        private void OnPredictionLineHover(object sender, PredictionLineHoverEventArgs e)
        {
            // 显示预测线详细信息
            if (e.Line != null)
            {
                var details = $"预测线: {e.Line.Name}\n价格: {e.Line.Price:F2}\n类型: {(e.Line.IsKeyLine ? "重点线" : "普通线")}";
                // 可以在这里显示工具提示或状态栏信息
            }
        }

        private void OnPredictionLineClick(object sender, PredictionLineClickEventArgs e)
        {
            // 处理预测线点击事件
            if (e.Line != null)
            {
                // 高亮选中的预测线
                _predictionDisplay.SelectedLine = e.Line;
                AddSelectionEffects(e.Location, Color.Blue);
            }
        }

        private void OnRenderStateChanged(object sender, RenderStateChangedEventArgs e)
        {
            // 处理渲染状态变化
            if (e.IsCompleted)
            {
                // 渲染完成后的处理
            }
        }

        private void OnDataPointClick(object sender, DataPointClickEventArgs e)
        {
            // 处理图表数据点点击
            AddSelectionEffects(e.Location, Color.Purple);
        }

        private void OnChartRenderComplete(object sender, RenderCompleteEventArgs e)
        {
            // 图表渲染完成
        }

        private async Task UpdateControlsWithPredictionLines(List<PredictionLine> predictionLines)
        {
            if (predictionLines == null || predictionLines.Count == 0)
                return;

            // 更新预测线显示控件
            await _predictionDisplay.SetPredictionLinesAsync(predictionLines);

            // 更新线条画布
            UpdateLineCanvas(predictionLines);

            // 更新图表渲染器
            UpdateChartRenderer(predictionLines);
        }

        private void UpdateLineCanvas(List<PredictionLine> predictionLines)
        {
            _lineCanvas.ClearLayers();

            // 创建背景层
            var backgroundLayer = _lineCanvas.AddLayer("Background", 0);
            var backgroundElement = new LineCanvas.LineElement(
                new PointF(0, 0),
                new PointF(_lineCanvas.Width, _lineCanvas.Height),
                new Pen(Color.White, _lineCanvas.Width)
            );
            backgroundLayer.AddElement(backgroundElement);

            // 创建预测线层
            var predictionLayer = _lineCanvas.AddLayer("PredictionLines", 1);

            foreach (var line in predictionLines)
            {
                var y = CalculateCanvasY(line.Price, _lineCanvas.Height);
                var lineColor = line.IsKeyLine ? Color.Red : Color.Blue;
                var lineWidth = line.IsKeyLine ? 2 : 1;

                var lineElement = new LineCanvas.LineElement(
                    new PointF(0, y),
                    new PointF(_lineCanvas.Width, y),
                    new Pen(lineColor, lineWidth)
                );
                predictionLayer.AddElement(lineElement);

                // 添加价格标签
                var labelElement = new LineCanvas.TextElement(
                    $"{line.Name}: {line.Price:F2}",
                    new PointF(10, y - 10),
                    new Font("Arial", 9),
                    new SolidBrush(lineColor)
                );
                predictionLayer.AddElement(labelElement);
            }
        }

        private void UpdateChartRenderer(List<PredictionLine> predictionLines)
        {
            var chartData = new ChartData
            {
                Title = "波神11线预测图",
                XAxisLabel = "预测线",
                YAxisLabel = "价格"
            };

            // 创建主线数据系列
            var mainSeries = new ChartSeries
            {
                Name = "预测线",
                Color = Color.Blue,
                Type = ChartRenderer.ChartType.Line
            };

            // 创建重点线数据系列
            var keyLineSeries = new ChartSeries
            {
                Name = "重点线",
                Color = Color.Red,
                Type = ChartRenderer.ChartType.Scatter
            };

            for (int i = 0; i < predictionLines.Count; i++)
            {
                var line = predictionLines[i];
                var dataPoint = new ChartDataPoint
                {
                    X = i,
                    Y = line.Price,
                    Value = line.Price,
                    Label = line.Name,
                    Series = mainSeries
                };

                mainSeries.Points.Add(dataPoint);

                if (line.IsKeyLine)
                {
                    var keyPoint = new ChartDataPoint
                    {
                        X = i,
                        Y = line.Price,
                        Value = line.Price,
                        Label = line.Name,
                        Series = keyLineSeries
                    };
                    keyLineSeries.Points.Add(keyPoint);
                }
            }

            chartData.Series.Add(mainSeries);
            if (keyLineSeries.Points.Count > 0)
            {
                chartData.Series.Add(keyLineSeries);
            }

            _chartRenderer.SetChartData(chartData);
        }

        private float CalculateCanvasY(double price, float canvasHeight)
        {
            if (_currentPredictionLines == null || _currentPredictionLines.Count == 0)
                return canvasHeight / 2;

            var minPrice = _currentPredictionLines.Min(l => l.Price);
            var maxPrice = _currentPredictionLines.Max(l => l.Price);
            var priceRange = maxPrice - minPrice;

            if (priceRange <= 0)
                return canvasHeight / 2;

            var ratio = (price - minPrice) / priceRange;
            return (float)(canvasHeight * (1 - ratio));
        }

        private void AddSelectionEffects(Point location, Color color)
        {
            // 添加粒子效果
            _particleSystem.Emit(new PointF(location.X, location.Y), 10, new VisualEffects.ParticleConfig
            {
                Colors = new[] { color, VisualEffects.AdjustBrightness(color, 0.3f) },
                MinSize = 2f,
                MaxSize = 5f,
                Lifetime = 1f,
                VelocityRange = 50f
            });
        }

        private void AddSuccessEffects()
        {
            // 添加成功计算的视觉效果
            var center = new PointF(Width / 2, Height / 2);
            _particleSystem.Emit(center, 20, new VisualEffects.ParticleConfig
            {
                Colors = new[] { Color.Lime, Color.Yellow, Color.Orange },
                MinSize = 3f,
                MaxSize = 8f,
                Lifetime = 2f,
                VelocityRange = 100f
            });
        }

        #endregion

        #region 事件触发器

        protected virtual void OnPredictionLinesChanged(EventArgs e)
        {
            PredictionLinesChanged?.Invoke(this, e);
        }

        protected virtual void OnKLineChanged(EventArgs e)
        {
            KLineChanged?.Invoke(this, e);
        }

        protected virtual void OnCalculationStarted(EventArgs e)
        {
            CalculationStarted?.Invoke(this, e);
        }

        protected virtual void OnCalculationCompleted(CalculationCompletedEventArgs e)
        {
            CalculationCompleted?.Invoke(this, e);
        }

        protected virtual void OnErrorOccurred(ErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        #endregion

        #region 重写方法

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // 可以在这里添加响应式布局逻辑
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _algorithmService?.Dispose();
                _kLineSelector?.Dispose();
                _predictionDisplay?.Dispose();
                _lineCanvas?.Dispose();
                _chartRenderer?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }

    #region 事件参数类

    /// <summary>
    /// 计算完成事件参数
    /// </summary>
    public class CalculationCompletedEventArgs : EventArgs
    {
        public List<PredictionLine> PredictionLines { get; }
        public bool Success { get; }
        public string ErrorMessage { get; }

        public CalculationCompletedEventArgs(List<PredictionLine> predictionLines, bool success, string errorMessage)
        {
            PredictionLines = predictionLines;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// 错误事件参数
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception Exception { get; }

        public ErrorEventArgs(string message, Exception exception = null)
        {
            Message = message;
            Exception = exception;
        }
    }

    #endregion
}