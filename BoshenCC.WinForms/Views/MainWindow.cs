using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BoshenCC.Core.Utils;
using BoshenCC.Core.Interfaces;
using BoshenCC.Services.Interfaces;
using BoshenCC.Models;
using BoshenCC.WinForms.Controls;
using BoshenCC.WinForms.Utils;

namespace BoshenCC.WinForms.Views
{
    /// <summary>
    /// 主窗体 - 集成版本
    /// 包含K线选择和波神算法功能
    /// </summary>
    public partial class MainWindow : Form
    {
        #region 私有字段

        private readonly ILogService _logService;
        private readonly IImageProcessor _imageProcessor;
        private readonly IConfigService _configService;
        private readonly IScreenshotService _screenshotService;
        private readonly IBoshenAlgorithmService _boshenAlgorithmService;

        private Bitmap _currentImage;
        private bool _isModified = false;
        private List<PredictionLine> _predictionLines;
        private bool _isCalculating = false;

        // 新增控件
        private KLineSelector _kLineSelector;
        private PriceDisplay _priceDisplay;
        private SelectionPanel _selectionPanel;
        private RichTextBox _richTextBoxLog;

        // 布局控件
        private SplitContainer _splitContainerMain;
        private SplitContainer _splitContainerKLine;
        private TabControl _tabControlBottom;
        private TabPage _tabPageLog;
        private TabPage _tabPageBoshen;
        private TabPage _tabPageSettings;
        private SplitContainer _splitContainerBoshen;
        private PictureBox _pictureBoxMain;

        // 快捷键和鼠标交互系统
        private KeyboardShortcuts _keyboardShortcuts;
        private MouseInteractionHandler _mouseInteractionHandler;
        private CursorManager _cursorManager;
        private GestureHandler _gestureHandler;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            InitializeControls();

            // 从服务定位器获取服务
            _logService = ServiceLocator.GetService<ILogService>();
            _imageProcessor = ServiceLocator.GetService<IImageProcessor>();
            _configService = ServiceLocator.GetService<IConfigService>();
            _screenshotService = ServiceLocator.GetService<IScreenshotService>();
            _boshenAlgorithmService = ServiceLocator.GetService<IBoshenAlgorithmService>();

            // 初始化数据
            _predictionLines = new List<PredictionLine>();

            // 初始化UI
            InitializeUI();
            SetupEventHandlers();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化控件
        /// </summary>
        private void InitializeControls()
        {
            // 创建主要容器
            _splitContainerMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 500
            };

            // 创建K线选择容器
            _splitContainerKLine = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400
            };

            // 创建K线选择器
            _kLineSelector = new KLineSelector
            {
                Dock = DockStyle.Fill
            };

            // 创建原始图像显示
            _pictureBoxMain = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            // 添加Paint事件处理以绘制十字准星和手势轨迹
            _pictureBoxMain.Paint += OnPictureBoxMainPaint;

            // 创建底部标签页
            _tabControlBottom = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 创建日志页
            _tabPageLog = new TabPage("日志");
            _richTextBoxLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Font = new Font("Consolas", 9)
            };
            _tabPageLog.Controls.Add(_richTextBoxLog);

            // 创建波神算法页
            _tabPageBoshen = new TabPage("波神算法");
            _splitContainerBoshen = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 700
            };

            _priceDisplay = new PriceDisplay
            {
                Dock = DockStyle.Fill
            };

            _selectionPanel = new SelectionPanel
            {
                Dock = DockStyle.Fill
            };

            _splitContainerBoshen.Panel1.Controls.Add(_priceDisplay);
            _splitContainerBoshen.Panel2.Controls.Add(_selectionPanel);
            _tabPageBoshen.Controls.Add(_splitContainerBoshen);

            // 创建设置页
            _tabPageSettings = new TabPage("设置");
            _tabPageSettings.Controls.Add(new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "应用程序设置"
            });

            // 添加标签页
            _tabControlBottom.TabPages.Add(_tabPageLog);
            _tabControlBottom.TabPages.Add(_tabPageBoshen);
            _tabControlBottom.TabPages.Add(_tabPageSettings);

            // 组装布局
            _splitContainerKLine.Panel1.Controls.Add(_kLineSelector);
            _splitContainerKLine.Panel2.Controls.Add(_pictureBoxMain);
            _splitContainerMain.Panel1.Controls.Add(_splitContainerKLine);
            _splitContainerMain.Panel2.Controls.Add(_tabControlBottom);

            // 添加到主窗体
            this.Controls.Add(_splitContainerMain);
        }

        /// <summary>
        /// 设置事件处理器
        /// </summary>
        private void SetupEventHandlers()
        {
            // K线选择器事件
            _kLineSelector.PointASelected += OnPointASelected;
            _kLineSelector.PointBSelected += OnPointBSelected;
            _kLineSelector.SelectionStateChanged += OnSelectionStateChanged;

            // 选择面板事件
            _selectionPanel.ClearSelection += OnClearSelection;
            _selectionPanel.CalculatePredictions += OnCalculatePredictions;
            _selectionPanel.Undo += OnUndo;
            _selectionPanel.Redo += OnRedo;
            _selectionPanel.ExportResults += OnExportResults;
            _selectionPanel.ShowSettings += OnShowSettings;

            // 窗体事件
            this.KeyDown += OnMainWindowKeyDown;
            this.KeyUp += OnMainWindowKeyUp;
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        private void InitializeUI()
        {
            try
            {
                // 设置窗体属性
                this.Text = "Boshen CC - 股票图表识别工具 (集成版)";
                this.MinimumSize = new Size(1200, 800);

                // 初始化快捷键和鼠标交互系统
                InitializeKeyboardAndMouseSystem();

                // 初始化状态栏
                UpdateStatus("就绪 - 集成波神算法K线选择功能");

                // 初始化日志
                if (_logService != null)
                {
                    _logService.Info("主窗体初始化完成 - 集成版");
                    LogMessage("应用程序启动成功 - 集成波神算法功能");
                }

                // 加载配置
                LoadSettings();

                // 初始化控件状态
                UpdateControlStates();
            }
            catch (Exception ex)
            {
                LogMessage($"初始化UI失败: {ex.Message}", true);
                MessageBox.Show($"初始化UI失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 初始化快捷键和鼠标交互系统
        /// </summary>
        private void InitializeKeyboardAndMouseSystem()
        {
            try
            {
                // 初始化快捷键系统
                _keyboardShortcuts = new KeyboardShortcuts();
                SetupKeyboardShortcuts();

                // 初始化鼠标交互处理器
                _mouseInteractionHandler = new MouseInteractionHandler(this);
                SetupMouseInteractionHandlers();

                // 初始化光标管理器
                _cursorManager = new CursorManager(this);
                SetupCursorManager();

                // 初始化手势处理器
                _gestureHandler = new GestureHandler(this);
                SetupGestureHandlers();

                LogMessage("快捷键和鼠标交互系统初始化完成");
            }
            catch (Exception ex)
            {
                LogMessage($"初始化快捷键和鼠标交互系统失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 设置快捷键
        /// </summary>
        private void SetupKeyboardShortcuts()
        {
            // 文件操作事件绑定
            _keyboardShortcuts.OnFileOpen += (s, e) => OpenImageFile();
            _keyboardShortcuts.OnFileSave += (s, e) => SaveImageFile();
            _keyboardShortcuts.OnFileSaveAs += (s, e) => SaveImageFileAs();
            _keyboardShortcuts.OnFileExport += (s, e) => ExportResults();
            _keyboardShortcuts.OnFilePrint += (s, e) => PrintImage();

            // 编辑操作事件绑定
            _keyboardShortcuts.OnEditUndo += (s, e) => Undo();
            _keyboardShortcuts.OnEditRedo += (s, e) => Redo();
            _keyboardShortcuts.OnEditClear += (s, e) => ClearSelection();
            _keyboardShortcuts.OnEditDelete += (s, e) => DeleteSelected();
            _keyboardShortcuts.OnEditSelectAll += (s, e) => SelectAll();

            // 视图操作事件绑定
            _keyboardShortcuts.OnViewCalculate += (s, e) => OnCalculatePredictions(this, EventArgs.Empty);
            _keyboardShortcuts.OnViewRefresh += (s, e) => RefreshView();
            _keyboardShortcuts.OnViewFullscreen += (s, e) => ToggleFullscreen();
            _keyboardShortcuts.OnViewZoomIn += (s, e) => ZoomIn();
            _keyboardShortcuts.OnViewZoomOut += (s, e) => ZoomOut();
            _keyboardShortcuts.OnViewZoomReset += (s, e) => ZoomReset();

            // 工具操作事件绑定
            _keyboardShortcuts.OnToolSelectMode += (s, e) => SetSelectMode();
            _keyboardShortcuts.OnToolMeasureMode += (s, e) => SetMeasureMode();
            _keyboardShortcuts.OnToolShadowMode += (s, e) => SetShadowMode();
            _keyboardShortcuts.OnToolStandardMode += (s, e) => SetStandardMode();
            _keyboardShortcuts.OnToolMeasurement += (s, e) => QuickMeasurement();

            // 导航操作事件绑定
            _keyboardShortcuts.OnNavigationEscape += (s, e) => HandleEscape();
            _keyboardShortcuts.OnNavigationConfirm += (s, e) => HandleConfirm();
            _keyboardShortcuts.OnNavigationNext += (s, e) => NavigateNext();
            _keyboardShortcuts.OnNavigationPrevious += (s, e) => NavigatePrevious();

            // 帮助操作事件绑定
            _keyboardShortcuts.OnHelpHelp += (s, e) => ShowHelp();
            _keyboardShortcuts.OnHelpAbout += (s, e) => ShowAbout();

            // K线选择器特定快捷键绑定
            _keyboardShortcuts.OnKLineStandardMode += (s, e) => SetKLineStandardMode();
            _keyboardShortcuts.OnKLineUpperShadowMode += (s, e) => SetKLineUpperShadowMode();
            _keyboardShortcuts.OnKLineLowerShadowMode += (s, e) => SetKLineLowerShadowMode();
            _keyboardShortcuts.OnKLineFullShadowMode += (s, e) => SetKLineFullShadowMode();
            _keyboardShortcuts.OnKLineClearSelection += (s, e) => ClearKLineSelection();
            _keyboardShortcuts.OnKLineUndo += (s, e) => UndoKLine();
            _keyboardShortcuts.OnKLineRedo += (s, e) => RedoKLine();

            // 监听快捷键执行事件
            _keyboardShortcuts.ShortcutExecuted += OnShortcutExecuted;
            _keyboardShortcuts.ShortcutConflict += OnShortcutConflict;
        }

        /// <summary>
        /// 设置鼠标交互处理器
        /// </summary>
        private void SetupMouseInteractionHandlers()
        {
            // 鼠标状态变化处理
            _mouseInteractionHandler.MouseStateChanged += OnMouseStateChanged;

            // 手势识别处理
            _mouseInteractionHandler.GestureRecognized += OnGestureRecognized;

            // 拖拽处理
            _mouseInteractionHandler.DragStart += OnDragStart;
            _mouseInteractionHandler.Dragging += OnDragging;
            _mouseInteractionHandler.DragEnd += OnDragEnd;

            // 点击处理
            _mouseInteractionHandler.SingleClick += OnSingleClick;
            _mouseInteractionHandler.DoubleClick += OnDoubleClick;
            _mouseInteractionHandler.RightClick += OnRightClick;

            // 增强的鼠标移动处理
            _mouseInteractionHandler.MouseMoveEnhanced += OnMouseMoveEnhanced;
            _mouseInteractionHandler.MouseWheelEnhanced += OnMouseWheelEnhanced;
        }

        /// <summary>
        /// 设置光标管理器
        /// </summary>
        private void SetupCursorManager()
        {
            // 光标变化处理
            _cursorManager.CursorChanged += OnCursorChanged;
            _cursorManager.CrosshairStateChanged += OnCrosshairStateChanged;

            // 设置默认光标样式
            _cursorManager.CrosshairColor = Color.Red;
            _cursorManager.CrosshairSize = 60;
            _cursorManager.CrosshairOpacity = 0.7f;
        }

        /// <summary>
        /// 设置手势处理器
        /// </summary>
        private void SetupGestureHandlers()
        {
            // 手势事件处理
            _gestureHandler.GestureStart += OnGestureStart;
            _gestureHandler.GestureProgress += OnGestureProgress;
            _gestureHandler.GestureEnd += OnGestureEnd;
            _gestureHandler.GestureRecognized += OnGestureRecognizedAdvanced;
            _gestureHandler.GestureFailed += OnGestureFailed;

            // 添加自定义手势
            AddCustomGestures();
        }

        /// <summary>
        /// 添加自定义手势
        /// </summary>
        private void AddCustomGestures()
        {
            // 添加测量手势（圆形手势触发测量）
            var measureGesture = _gestureHandler.CreateCustomGesture(
                "QuickMeasure",
                _gestureHandler.GetAllGestures().FirstOrDefault(g => g.Name == "Circle")?.TemplatePoints ?? new List<Point>(),
                "快速测量",
                () => QuickMeasurement()
            );
            _gestureHandler.AddGesture(measureGesture);

            // 添加清除手势（左滑动手势清除选择）
            var clearGesture = _gestureHandler.CreateCustomGesture(
                "ClearSelection",
                _gestureHandler.GetAllGestures().FirstOrDefault(g => g.Name == "SwipeLeft")?.TemplatePoints ?? new List<Point>(),
                "清除选择",
                () => ClearSelection()
            );
            _gestureHandler.AddGesture(clearGesture);
        }

        #endregion

        #region 事件处理器

        /// <summary>
        /// A点选择事件处理
        /// </summary>
        private void OnPointASelected(object sender, PointSelectedEventArgs e)
        {
            try
            {
                _priceDisplay.UpdatePointAPrice(e.Price);
                LogMessage($"选择A点: 坐标({e.Location.X}, {e.Location.Y}), 价格: {e.Price:F2}");
                UpdateControlStates();
            }
            catch (Exception ex)
            {
                LogMessage($"处理A点选择失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// B点选择事件处理
        /// </summary>
        private void OnPointBSelected(object sender, PointSelectedEventArgs e)
        {
            try
            {
                _priceDisplay.UpdatePointBPrice(e.Price);
                LogMessage($"选择B点: 坐标({e.Location.X}, {e.Location.Y}), 价格: {e.Price:F2}");
                UpdateControlStates();
            }
            catch (Exception ex)
            {
                LogMessage($"处理B点选择失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 选择状态改变事件处理
        /// </summary>
        private void OnSelectionStateChanged(object sender, SelectionStateChangedEventArgs e)
        {
            try
            {
                LogMessage($"选择状态改变: {e.OldState} -> {e.NewState}");
                _selectionPanel.CurrentState = e.NewState;
                UpdateControlStates();
            }
            catch (Exception ex)
            {
                LogMessage($"处理选择状态改变失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 清除选择事件处理
        /// </summary>
        private void OnClearSelection(object sender, EventArgs e)
        {
            ClearSelection();
        }

        /// <summary>
        /// 计算预测线事件处理
        /// </summary>
        private async void OnCalculatePredictions(object sender, EventArgs e)
        {
            await CalculatePredictionsAsync();
        }

        /// <summary>
        /// 撤销事件处理
        /// </summary>
        private void OnUndo(object sender, EventArgs e)
        {
            LogMessage("撤销操作 - 功能开发中");
        }

        /// <summary>
        /// 重做事件处理
        /// </summary>
        private void OnRedo(object sender, EventArgs e)
        {
            LogMessage("重做操作 - 功能开发中");
        }

        /// <summary>
        /// 导出结果事件处理
        /// </summary>
        private void OnExportResults(object sender, EventArgs e)
        {
            ExportResults();
        }

        /// <summary>
        /// 显示设置事件处理
        /// </summary>
        private void OnShowSettings(object sender, EventArgs e)
        {
            LogMessage("显示设置 - 功能开发中");
        }

        /// <summary>
        /// 主窗体键盘事件处理 - 委托给快捷键系统
        /// </summary>
        private void OnMainWindowKeyDown(object sender, KeyEventArgs e)
        {
            // 首先尝试由快捷键系统处理
            if (_keyboardShortcuts != null && _keyboardShortcuts.ProcessKeyDown(e.KeyData))
            {
                e.Handled = true;
                return;
            }

            // 如果快捷键系统没有处理，使用原有的备用处理逻辑
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.O:
                        OpenImageFile();
                        e.Handled = true;
                        break;
                    case Keys.S:
                        SaveImageFile();
                        e.Handled = true;
                        break;
                    case Keys.R:
                        ClearSelection();
                        e.Handled = true;
                        break;
                    case Keys.E:
                        ExportResults();
                        e.Handled = true;
                        break;
                }
            }
            else if (e.KeyCode == Keys.Space)
            {
                OnCalculatePredictions(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 主窗体键盘释放事件处理
        /// </summary>
        private void OnMainWindowKeyUp(object sender, KeyEventArgs e)
        {
            // 委托给快捷键系统处理
            _keyboardShortcuts?.ProcessKeyUp(e.KeyData);
        }

        #endregion

        #region 快捷键和鼠标交互事件处理

        /// <summary>
        /// 快捷键执行事件处理
        /// </summary>
        private void OnShortcutExecuted(object sender, ShortcutExecutedEventArgs e)
        {
            LogMessage($"快捷键执行: {e.Description} ({e.Keys})");
        }

        /// <summary>
        /// 快捷键冲突事件处理
        /// </summary>
        private void OnShortcutConflict(object sender, ShortcutConflictEventArgs e)
        {
            LogMessage($"快捷键冲突: {e.Keys} - 现有: {e.ExistingDescription}, 新: {e.NewDescription}", true);
        }

        /// <summary>
        /// 鼠标状态变化事件处理
        /// </summary>
        private void OnMouseStateChanged(object sender, MouseStateChangedEventArgs e)
        {
            // 更新状态栏或光标样式
            if (e.CurrentState.IsPressed)
            {
                _cursorManager?.SetCursor(CursorType.Selecting);
            }
            else
            {
                _cursorManager?.RestoreDefaultCursor();
            }
        }

        /// <summary>
        /// 手势识别事件处理（来自MouseInteractionHandler）
        /// </summary>
        private void OnGestureRecognized(object sender, GestureRecognizedEventArgs e)
        {
            LogMessage($"识别手势: {e.Gesture.Description}");
        }

        /// <summary>
        /// 拖拽开始事件处理
        /// </summary>
        private void OnDragStart(object sender, DragStartEventArgs e)
        {
            LogMessage($"开始拖拽: {e.StartPosition}");
            _cursorManager?.SetCursor(CursorType.Grabbing);
        }

        /// <summary>
        /// 拖拽进行中事件处理
        /// </summary>
        private void OnDragging(object sender, DragEventArgs e)
        {
            // 可以在这里添加实时拖拽反馈
        }

        /// <summary>
        /// 拖拽结束事件处理
        /// </summary>
        private void OnDragEnd(object sender, DragEndEventArgs e)
        {
            LogMessage($"拖拽结束: {e.EndPosition}, 距离: {e.TotalDistance:F2}px");
            _cursorManager?.RestoreDefaultCursor();
        }

        /// <summary>
        /// 单击事件处理
        /// </summary>
        private void OnSingleClick(object sender, MouseClickEventArgs e)
        {
            LogMessage($"单击: ({e.Position.X}, {e.Position.Y})");
        }

        /// <summary>
        /// 双击事件处理
        /// </summary>
        private void OnDoubleClick(object sender, MouseClickEventArgs e)
        {
            LogMessage($"双击: ({e.Position.X}, {e.Position.Y})");
            // 可以在这里添加双击相关的功能
        }

        /// <summary>
        /// 右键单击事件处理
        /// </summary>
        private void OnRightClick(object sender, MouseClickEventArgs e)
        {
            LogMessage($"右键单击: ({e.Position.X}, {e.Position.Y})");
            // 可以在这里显示上下文菜单
        }

        /// <summary>
        /// 增强的鼠标移动事件处理
        /// </summary>
        private void OnMouseMoveEnhanced(object sender, EnhancedMouseEventArgs e)
        {
            // 显示十字准星
            if (_cursorManager != null && _kLineSelector != null)
            {
                _cursorManager.ShowCrosshairAt(e.Location);
            }

            // 更新状态栏显示坐标
            UpdateStatus($"坐标: ({e.X}, {e.Y}) | 速度: {e.Speed:F1}px/s | 方向: {e.Direction}");
        }

        /// <summary>
        /// 增强的鼠标滚轮事件处理
        /// </summary>
        private void OnMouseWheelEnhanced(object sender, EnhancedMouseEventArgs e)
        {
            // 处理缩放功能
            if (e.Delta > 0)
            {
                ZoomIn();
            }
            else
            {
                ZoomOut();
            }
        }

        /// <summary>
        /// 光标变化事件处理
        /// </summary>
        private void OnCursorChanged(object sender, CursorChangedEventArgs e)
        {
            LogMessage($"光标变化: {e.PreviousCursor} -> {e.CurrentCursor}");
        }

        /// <summary>
        /// 十字准星状态变化事件处理
        /// </summary>
        private void OnCrosshairStateChanged(object sender, CrosshairStateChangedEventArgs e)
        {
            // 十字准星状态变化时的处理
            if (e.IsVisible)
            {
                // 十字准星显示
            }
            else
            {
                // 十字准星隐藏
            }
        }

        /// <summary>
        /// 手势开始事件处理（来自GestureHandler）
        /// </summary>
        private void OnGestureStart(object sender, GestureStartEventArgs e)
        {
            LogMessage($"手势开始: {e.StartPosition}");
        }

        /// <summary>
        /// 手势进行中事件处理
        /// </summary>
        private void OnGestureProgress(object sender, GestureProgressEventArgs e)
        {
            // 手势进行中的实时反馈
        }

        /// <summary>
        /// 手势结束事件处理
        /// </summary>
        private void OnGestureEnd(object sender, GestureEndEventArgs e)
        {
            LogMessage($"手势结束: {e.EndPosition}, 持续时间: {e.Duration.TotalMilliseconds:F0}ms, 点数: {e.AllPoints.Count}");
        }

        /// <summary>
        /// 高级手势识别事件处理
        /// </summary>
        private void OnGestureRecognizedAdvanced(object sender, GestureRecognizedEventArgs e)
        {
            LogMessage($"高级手势识别: {e.Gesture.Name} - {e.Gesture.Description} (置信度: {e.ConfidenceScore:P1})");
        }

        /// <summary>
        /// 手势识别失败事件处理
        /// </summary>
        private void OnGestureFailed(object sender, GestureFailedEventArgs e)
        {
            LogMessage($"手势识别失败: {e.Reason} (最佳匹配: {e.BestScore:P1})");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 清除选择
        /// </summary>
        public void ClearSelection()
        {
            try
            {
                _kLineSelector.ClearSelection();
                _priceDisplay.ClearAll();
                _predictionLines.Clear();
                UpdateControlStates();
                LogMessage("已清除所有选择");
            }
            catch (Exception ex)
            {
                LogMessage($"清除选择失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 异步计算预测线
        /// </summary>
        public async Task CalculatePredictionsAsync()
        {
            if (_isCalculating)
                return;

            try
            {
                var pointAPrice = _kLineSelector.GetPointAPrice();
                var pointBPrice = _kLineSelector.GetPointBPrice();

                if (!pointAPrice.HasValue || !pointBPrice.HasValue)
                {
                    MessageBox.Show("请先选择A点和B点", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _isCalculating = true;
                UpdateControlStates();
                UpdateStatus("正在计算预测线...");
                ShowProgress(10);

                // 异步计算
                await Task.Run(() =>
                {
                    if (_boshenAlgorithmService != null)
                    {
                        _predictionLines = _boshenAlgorithmService.CalculatePredictionLines(pointAPrice.Value, pointBPrice.Value);
                    }
                });

                ShowProgress(80);

                // 更新UI
                _priceDisplay.UpdatePredictionLines(_predictionLines);
                UpdateKLineSelectorDisplay();

                ShowProgress(100);
                UpdateStatus($"预测线计算完成 - 共{_predictionLines.Count}条线");
                LogMessage($"成功计算预测线: A点={pointAPrice:F2}, B点={pointBPrice:F2}, 生成{_predictionLines.Count}条预测线");

                ShowProgress(0);
            }
            catch (Exception ex)
            {
                ShowProgress(0);
                UpdateStatus("预测线计算失败");
                LogMessage($"计算预测线失败: {ex.Message}", true);
                MessageBox.Show($"计算预测线失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isCalculating = false;
                UpdateControlStates();
            }
        }

        /// <summary>
        /// 导出结果
        /// </summary>
        public void ExportResults()
        {
            try
            {
                if (_predictionLines.Count == 0)
                {
                    MessageBox.Show("没有可导出的预测线数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                saveFileDialog.Filter = "文本文件|*.txt|CSV文件|*.csv|所有文件|*.*";
                saveFileDialog.Title = "导出预测线结果";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var content = GenerateExportContent();
                    File.WriteAllText(saveFileDialog.FileName, content);

                    LogMessage($"成功导出结果到: {saveFileDialog.FileName}");
                    MessageBox.Show("导出成功", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"导出结果失败: {ex.Message}", true);
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// PictureBox绘制事件处理 - 绘制十字准星和手势轨迹
        /// </summary>
        private void OnPictureBoxMainPaint(object sender, PaintEventArgs e)
        {
            try
            {
                // 绘制十字准星
                if (_cursorManager != null && _cursorManager.ShowCrosshair)
                {
                    // 获取图像在PictureBox中的实际位置
                    var imageRect = GetImageRect();
                    var offset = new Point(imageRect.X, imageRect.Y);
                    _cursorManager.DrawCrosshair(e.Graphics, offset);
                }

                // 绘制手势轨迹
                if (_gestureHandler != null && _gestureHandler.ShowGestureTrail)
                {
                    // 获取图像在PictureBox中的实际位置
                    var imageRect = GetImageRect();
                    var offset = new Point(imageRect.X, imageRect.Y);
                    _gestureHandler.DrawGestureTrail(e.Graphics, offset);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"绘制十字准星或手势轨迹失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 获取图像在PictureBox中的显示区域
        /// </summary>
        private Rectangle GetImageRect()
        {
            if (_pictureBoxMain.Image == null)
                return Rectangle.Empty;

            var image = _pictureBoxMain.Image;
            var pictureBoxSize = _pictureBoxMain.ClientSize;
            var imageSize = image.Size;

            // 根据SizeMode计算图像显示区域
            switch (_pictureBoxMain.SizeMode)
            {
                case PictureBoxSizeMode.Normal:
                    return new Rectangle(0, 0, imageSize.Width, imageSize.Height);

                case PictureBoxSizeMode.StretchImage:
                    return new Rectangle(0, 0, pictureBoxSize.Width, pictureBoxSize.Height);

                case PictureBoxSizeMode.CenterImage:
                    var x = (pictureBoxSize.Width - imageSize.Width) / 2;
                    var y = (pictureBoxSize.Height - imageSize.Height) / 2;
                    return new Rectangle(x, y, imageSize.Width, imageSize.Height);

                case PictureBoxSizeMode.Zoom:
                    // 计算缩放比例
                    var scaleX = (float)pictureBoxSize.Width / imageSize.Width;
                    var scaleY = (float)pictureBoxSize.Height / imageSize.Height;
                    var scale = Math.Min(scaleX, scaleY);

                    var scaledWidth = (int)(imageSize.Width * scale);
                    var scaledHeight = (int)(imageSize.Height * scale);
                    var scaledX = (pictureBoxSize.Width - scaledWidth) / 2;
                    var scaledY = (pictureBoxSize.Height - scaledHeight) / 2;

                    return new Rectangle(scaledX, scaledY, scaledWidth, scaledHeight);

                default:
                    return Rectangle.Empty;
            }
        }

        #endregion

        #region 快捷键和鼠标交互相关方法

        /// <summary>
        /// 撤销操作
        /// </summary>
        private void Undo()
        {
            try
            {
                _selectionPanel?.Undo();
                LogMessage("撤销操作");
            }
            catch (Exception ex)
            {
                LogMessage($"撤销失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 重做操作
        /// </summary>
        private void Redo()
        {
            try
            {
                _selectionPanel?.Redo();
                LogMessage("重做操作");
            }
            catch (Exception ex)
            {
                LogMessage($"重做失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 删除选中项
        /// </summary>
        private void DeleteSelected()
        {
            try
            {
                // 实现删除选中项的逻辑
                LogMessage("删除选中项");
            }
            catch (Exception ex)
            {
                LogMessage($"删除失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 全选
        /// </summary>
        private void SelectAll()
        {
            try
            {
                // 实现全选逻辑
                LogMessage("全选");
            }
            catch (Exception ex)
            {
                LogMessage($"全选失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 刷新视图
        /// </summary>
        private void RefreshView()
        {
            try
            {
                _pictureBoxMain?.Invalidate();
                LogMessage("刷新视图");
            }
            catch (Exception ex)
            {
                LogMessage($"刷新视图失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 切换全屏
        /// </summary>
        private void ToggleFullscreen()
        {
            try
            {
                if (FormBorderStyle == FormBorderStyle.None)
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                    WindowState = FormWindowState.Normal;
                }
                else
                {
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                }
                LogMessage("切换全屏模式");
            }
            catch (Exception ex)
            {
                LogMessage($"切换全屏失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 放大
        /// </summary>
        private void ZoomIn()
        {
            try
            {
                // 实现放大逻辑
                LogMessage("放大");
            }
            catch (Exception ex)
            {
                LogMessage($"放大失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 缩小
        /// </summary>
        private void ZoomOut()
        {
            try
            {
                // 实现缩小逻辑
                LogMessage("缩小");
            }
            catch (Exception ex)
            {
                LogMessage($"缩小失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 重置缩放
        /// </summary>
        private void ZoomReset()
        {
            try
            {
                // 实现重置缩放逻辑
                LogMessage("重置缩放");
            }
            catch (Exception ex)
            {
                LogMessage($"重置缩放失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 设置选择模式
        /// </summary>
        private void SetSelectMode()
        {
            try
            {
                _cursorManager?.SetCursor(CursorType.Default);
                _keyboardShortcuts?.SetContext("Select");
                LogMessage("切换到选择模式");
            }
            catch (Exception ex)
            {
                LogMessage($"设置选择模式失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 设置测量模式
        /// </summary>
        private void SetMeasureMode()
        {
            try
            {
                _cursorManager?.SetCursor(CursorType.Measuring);
                _keyboardShortcuts?.SetContext("Measure");
                LogMessage("切换到测量模式");
            }
            catch (Exception ex)
            {
                LogMessage($"设置测量模式失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 设置影线模式
        /// </summary>
        private void SetShadowMode()
        {
            try
            {
                _cursorManager?.SetCursor(CursorType.Crosshair);
                _keyboardShortcuts?.SetContext("Shadow");
                LogMessage("切换到影线模式");
            }
            catch (Exception ex)
            {
                LogMessage($"设置影线模式失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 设置标准模式
        /// </summary>
        private void SetStandardMode()
        {
            try
            {
                _cursorManager?.SetCursor(CursorType.Crosshair);
                _keyboardShortcuts?.SetContext("Standard");
                LogMessage("切换到标准模式");
            }
            catch (Exception ex)
            {
                LogMessage($"设置标准模式失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 快速测量
        /// </summary>
        private void QuickMeasurement()
        {
            try
            {
                // 实现快速测量逻辑
                LogMessage("执行快速测量");
            }
            catch (Exception ex)
            {
                LogMessage($"快速测量失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 处理ESC键
        /// </summary>
        private void HandleEscape()
        {
            try
            {
                ClearSelection();
                _cursorManager?.RestoreDefaultCursor();
                _keyboardShortcuts?.SetContext("Global");
                LogMessage("ESC操作：清除选择并恢复默认状态");
            }
            catch (Exception ex)
            {
                LogMessage($"ESC操作失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 处理确认键
        /// </summary>
        private void HandleConfirm()
        {
            try
            {
                OnCalculatePredictions(this, EventArgs.Empty);
                LogMessage("确认操作：执行计算");
            }
            catch (Exception ex)
            {
                LogMessage($"确认操作失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 导航到下一个
        /// </summary>
        private void NavigateNext()
        {
            try
            {
                // 实现导航逻辑
                LogMessage("导航到下一个");
            }
            catch (Exception ex)
            {
                LogMessage($"导航失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 导航到上一个
        /// </summary>
        private void NavigatePrevious()
        {
            try
            {
                // 实现导航逻辑
                LogMessage("导航到上一个");
            }
            catch (Exception ex)
            {
                LogMessage($"导航失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 显示帮助
        /// </summary>
        private void ShowHelp()
        {
            try
            {
                var helpText = @"快捷键帮助：

文件操作：
Ctrl+O - 打开图片文件
Ctrl+S - 保存图片文件
Ctrl+Shift+S - 另存为
Ctrl+E - 导出结果
Ctrl+P - 打印

编辑操作：
Ctrl+Z - 撤销
Ctrl+Y - 重做
Ctrl+R - 清除选择
Delete - 删除选中项
Ctrl+A - 全选

视图操作：
Space - 计算预测
F5 - 刷新视图
F11 - 全屏切换
Ctrl+0 - 重置缩放
Ctrl+Plus - 放大
Ctrl+Minus - 缩小

工具操作：
1 - 选择模式
2 - 测量模式
3 - 影线模式
4 - 标准模式
M - 快速测量

导航操作：
ESC - 取消/退出
Enter - 确认/执行
Tab - 下一个
Shift+Tab - 上一个

帮助：
F1 - 显示帮助
F12 - 关于

鼠标手势：
右键拖拽 - 识别手势
圆形手势 - 快速测量
左滑手势 - 清除选择";

                MessageBox.Show(helpText, "快捷键帮助", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LogMessage("显示帮助信息");
            }
            catch (Exception ex)
            {
                LogMessage($"显示帮助失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 显示关于
        /// </summary>
        private void ShowAbout()
        {
            try
            {
                var aboutText = @"Boshen CC - 股票图表识别工具
版本: 1.0.0
集成波神算法K线选择功能

支持快捷键和鼠标手势操作
提供增强的用户交互体验

© 2025 Boshen CC Team";
                MessageBox.Show(aboutText, "关于 Boshen CC", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LogMessage("显示关于信息");
            }
            catch (Exception ex)
            {
                LogMessage($"显示关于失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// K线标准模式
        /// </summary>
        private void SetKLineStandardMode()
        {
            try
            {
                _kLineSelector?.SetMeasurementMode(Models.MeasurementMode.Standard);
                LogMessage("K线切换到标准模式");
            }
            catch (Exception ex)
            {
                LogMessage($"K线标准模式切换失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// K线上影线模式
        /// </summary>
        private void SetKLineUpperShadowMode()
        {
            try
            {
                _kLineSelector?.SetMeasurementMode(Models.MeasurementMode.UpperShadow);
                LogMessage("K线切换到上影线模式");
            }
            catch (Exception ex)
            {
                LogMessage($"K线上影线模式切换失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// K线下影线模式
        /// </summary>
        private void SetKLineLowerShadowMode()
        {
            try
            {
                _kLineSelector?.SetMeasurementMode(Models.MeasurementMode.LowerShadow);
                LogMessage("K线切换到下影线模式");
            }
            catch (Exception ex)
            {
                LogMessage($"K线下影线模式切换失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// K线完整影线模式
        /// </summary>
        private void SetKLineFullShadowMode()
        {
            try
            {
                _kLineSelector?.SetMeasurementMode(Models.MeasurementMode.FullShadow);
                LogMessage("K线切换到完整影线模式");
            }
            catch (Exception ex)
            {
                LogMessage($"K线完整影线模式切换失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 清除K线选择
        /// </summary>
        private void ClearKLineSelection()
        {
            try
            {
                _kLineSelector?.ClearSelection();
                LogMessage("清除K线选择");
            }
            catch (Exception ex)
            {
                LogMessage($"清除K线选择失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// K线撤销
        /// </summary>
        private void UndoKLine()
        {
            try
            {
                _kLineSelector?.Undo();
                LogMessage("K线撤销操作");
            }
            catch (Exception ex)
            {
                LogMessage($"K线撤销失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// K线重做
        /// </summary>
        private void RedoKLine()
        {
            try
            {
                _kLineSelector?.Redo();
                LogMessage("K线重做操作");
            }
            catch (Exception ex)
            {
                LogMessage($"K线重做失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 另存为
        /// </summary>
        private void SaveImageFileAs()
        {
            try
            {
                SaveImageFile(); // 复用现有的保存方法
                LogMessage("另存为操作");
            }
            catch (Exception ex)
            {
                LogMessage($"另存为失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 打印图片
        /// </summary>
        private void PrintImage()
        {
            try
            {
                if (_currentImage != null)
                {
                    var printDialog = new PrintDialog();
                    if (printDialog.ShowDialog() == DialogResult.OK)
                    {
                        // 实现打印逻辑
                        LogMessage("打印功能开发中");
                    }
                }
                else
                {
                    MessageBox.Show("没有可打印的图片", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"打印失败: {ex.Message}", true);
            }
        }

        #endregion

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新控件状态
        /// </summary>
        private void UpdateControlStates()
        {
            try
            {
                var state = _kLineSelector.CurrentState;
                var hasSelection = _kLineSelector.IsSelectionComplete();

                _selectionPanel.UpdateState(
                    state: state,
                    canUndo: false,
                    canRedo: false,
                    hasSelection: hasSelection,
                    isCalculating: _isCalculating
                );
            }
            catch (Exception ex)
            {
                LogMessage($"更新控件状态失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 更新K线选择器显示
        /// </summary>
        private void UpdateKLineSelectorDisplay()
        {
            try
            {
                // 这里可以添加预测线到KLineSelector的显示逻辑
                _kLineSelector.Invalidate();
            }
            catch (Exception ex)
            {
                LogMessage($"更新K线选择器显示失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 生成导出内容
        /// </summary>
        private string GenerateExportContent()
        {
            var content = new System.Text.StringBuilder();
            content.AppendLine("波神算法预测线结果");
            content.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine();

            var pointAPrice = _kLineSelector.GetPointAPrice();
            var pointBPrice = _kLineSelector.GetPointBPrice();

            if (pointAPrice.HasValue)
                content.AppendLine($"A点价格: {pointAPrice.Value:F2}");
            if (pointBPrice.HasValue)
                content.AppendLine($"B点价格: {pointBPrice.Value:F2}");

            content.AppendLine();
            content.AppendLine("预测线:");
            content.AppendLine("线号\t价格\t类型");

            foreach (var line in _predictionLines)
            {
                var type = (line.LineNumber == 3 || line.LineNumber == 6 || line.LineNumber == 8) ? "重点线" : "普通线";
                content.AppendLine($"{line.LineNumber}\t{line.Price:F2}\t{type}");
            }

            return content.ToString();
        }

        #endregion

        #region 原有方法（保持兼容）

        /// <summary>
        /// 更新状态栏
        /// </summary>
        private void UpdateStatus(string status)
        {
            if (toolStripStatusLabel.InvokeRequired)
            {
                toolStripStatusLabel.Invoke(new Action<string>(UpdateStatus), status);
                return;
            }

            toolStripStatusLabel.Text = $"状态：{status}";
        }

        /// <summary>
        /// 记录日志消息
        /// </summary>
        private void LogMessage(string message, bool isError = false)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

                if (_richTextBoxLog.InvokeRequired)
                {
                    _richTextBoxLog.Invoke(new Action<string, bool>(LogMessage), message, isError);
                    return;
                }

                _richTextBoxLog.AppendText(logEntry + Environment.NewLine);
                _richTextBoxLog.ScrollToCaret();

                // 同时记录到日志服务
                if (_logService != null)
                {
                    if (isError)
                        _logService.Error(message);
                    else
                        _logService.Info(message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日志记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示进度
        /// </summary>
        private void ShowProgress(int progress)
        {
            if (toolStripProgressBar.InvokeRequired)
            {
                toolStripProgressBar.Invoke(new Action<int>(ShowProgress), progress);
                return;
            }

            toolStripProgressBar.Value = Math.Max(0, Math.Min(100, progress));
            toolStripProgressBar.Visible = progress > 0 && progress < 100;
        }

        /// <summary>
        /// 打开图像文件
        /// </summary>
        private void OpenImageFile()
        {
            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ShowProgress(10);
                    UpdateStatus("正在加载图像...");

                    using (var fileStream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                    {
                        _currentImage = new Bitmap(fileStream);
                    }

                    ShowProgress(50);
                    UpdateStatus("图像加载完成");

                    _pictureBoxMain.Image = _currentImage;
                    _kLineSelector.BackgroundImage = _currentImage;
                    _isModified = false;

                    ShowProgress(100);
                    UpdateStatus($"已加载图像: {Path.GetFileName(openFileDialog.FileName)}");
                    LogMessage($"成功加载图像: {openFileDialog.FileName}");

                    ShowProgress(0);
                }
            }
            catch (Exception ex)
            {
                ShowProgress(0);
                UpdateStatus("图像加载失败");
                LogMessage($"加载图像失败: {ex.Message}", true);
                MessageBox.Show($"加载图像失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 保存图像文件
        /// </summary>
        private void SaveImageFile()
        {
            try
            {
                if (_currentImage == null)
                {
                    MessageBox.Show("没有可保存的图像", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ShowProgress(10);
                    UpdateStatus("正在保存图像...");

                    var format = GetImageFormat(saveFileDialog.FileName);
                    _currentImage.Save(saveFileDialog.FileName, format);

                    ShowProgress(100);
                    UpdateStatus($"图像保存完成: {Path.GetFileName(saveFileDialog.FileName)}");
                    LogMessage($"成功保存图像: {saveFileDialog.FileName}");
                    _isModified = false;

                    ShowProgress(0);
                }
            }
            catch (Exception ex)
            {
                ShowProgress(0);
                UpdateStatus("图像保存失败");
                LogMessage($"保存图像失败: {ex.Message}", true);
                MessageBox.Show($"保存图像失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 根据文件扩展名获取图像格式
        /// </summary>
        private System.Drawing.Imaging.ImageFormat GetImageFormat(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return System.Drawing.Imaging.ImageFormat.Jpeg;
                case ".png":
                    return System.Drawing.Imaging.ImageFormat.Png;
                case ".bmp":
                    return System.Drawing.Imaging.ImageFormat.Bmp;
                case ".gif":
                    return System.Drawing.Imaging.ImageFormat.Gif;
                case ".tif":
                case ".tiff":
                    return System.Drawing.Imaging.ImageFormat.Tiff;
                default:
                    return System.Drawing.Imaging.ImageFormat.Jpeg;
            }
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (_configService != null)
                {
                    var settings = _configService.LoadSettings();
                    LogMessage("配置加载完成");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"加载配置失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                if (_configService != null)
                {
                    _configService.SaveSettings(new AppSettings());
                    LogMessage("配置保存完成");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"保存配置失败: {ex.Message}", true);
            }
        }

        #endregion

        #region 窗体事件

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        private void MainWindow_Load(object sender, EventArgs e)
        {
            LogMessage("主窗体加载完成 - 集成版");
        }

        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (_isModified)
                {
                    var result = MessageBox.Show("有未保存的更改，确定要退出吗？", "确认退出",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                    if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                SaveSettings();

                // 释放快捷键和鼠标交互系统资源
                _keyboardShortcuts?.Dispose();
                _mouseInteractionHandler?.Dispose();
                _cursorManager?.Dispose();
                _gestureHandler?.Dispose();

                _currentImage?.Dispose();
                LogMessage("主窗体关闭，应用程序退出");
            }
            catch (Exception ex)
            {
                LogMessage($"窗体关闭处理失败: {ex.Message}", true);
            }
        }

        #endregion

        #region 菜单事件处理（保持原有接口）

        private void openImageToolStripMenuItem_Click(object sender, EventArgs e) => OpenImageFile();
        private void saveImageToolStripMenuItem_Click(object sender, EventArgs e) => SaveImageFile();
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) => this.Close();

        private void singleMeasureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("单体测量功能将在后续版本中实现", "功能开发中",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void lineMeasureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("影线测量功能将在后续版本中实现", "功能开发中",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void clearLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("清除线条功能将在后续版本中实现", "功能开发中",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("设置功能将在后续版本中实现", "功能开发中",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void calculateToolStripMenuItem_Click(object sender, EventArgs e) => OnCalculatePredictions(this, EventArgs.Empty);
        private void clearSelectionToolStripMenuItem_Click(object sender, EventArgs e) => ClearSelection();

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Boshen CC - 股票图表识别工具 (集成版)\n\n版本: 1.0.0\n集成功能: K线选择、波神算法计算\n\n© 2025 Boshen Technology",
                "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region 工具栏事件处理

        private void toolStripButtonOpen_Click(object sender, EventArgs e) => OpenImageFile();
        private void toolStripButtonBoshenCalculate_Click(object sender, EventArgs e) => OnCalculatePredictions(this, EventArgs.Empty);
        private void toolStripButtonClearSelection_Click(object sender, EventArgs e) => ClearSelection();

        #endregion
    }
}