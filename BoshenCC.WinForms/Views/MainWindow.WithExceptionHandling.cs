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

namespace BoshenCC.WinForms.Views
{
    /// <summary>
    /// 主窗体 - 集成版本（带异常处理）
    /// 包含K线选择和波神算法功能，具有完整的异常处理机制
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

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            ExceptionHandler.SafeExecute(() =>
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
            }, _logService, "主窗体初始化", true, "MainWindow构造函数");
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化控件
        /// </summary>
        private void InitializeControls()
        {
            ExceptionHandler.SafeExecute(() =>
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
            }, _logService, "控件初始化", true, "InitializeControls方法");
        }

        /// <summary>
        /// 设置事件处理器
        /// </summary>
        private void SetupEventHandlers()
        {
            ExceptionHandler.SafeExecute(() =>
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
            }, _logService, "事件处理器设置", false, "SetupEventHandlers方法");
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        private void InitializeUI()
        {
            ExceptionHandler.SafeExecute(() =>
            {
                // 设置窗体属性
                this.Text = "Boshen CC - 股票图表识别工具 (集成版)";
                this.MinimumSize = new Size(1200, 800);

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
            }, _logService, "UI初始化", true, "InitializeUI方法");
        }

        #endregion

        #region 事件处理器

        /// <summary>
        /// A点选择事件处理
        /// </summary>
        private void OnPointASelected(object sender, PointSelectedEventArgs e)
        {
            ExceptionHandler.SafeExecute(() =>
            {
                _priceDisplay.UpdatePointAPrice(e.Price);
                LogMessage($"选择A点: 坐标({e.Location.X}, {e.Location.Y}), 价格: {e.Price:F2}");
                UpdateControlStates();
            }, _logService, "A点选择处理", false, $"坐标({e.Location.X}, {e.Location.Y}), 价格({e.Price:F2})");
        }

        /// <summary>
        /// B点选择事件处理
        /// </summary>
        private void OnPointBSelected(object sender, PointSelectedEventArgs e)
        {
            ExceptionHandler.SafeExecute(() =>
            {
                _priceDisplay.UpdatePointBPrice(e.Price);
                LogMessage($"选择B点: 坐标({e.Location.X}, {e.Location.Y}), 价格: {e.Price:F2}");
                UpdateControlStates();
            }, _logService, "B点选择处理", false, $"坐标({e.Location.X}, {e.Location.Y}), 价格({e.Price:F2})");
        }

        /// <summary>
        /// 选择状态改变事件处理
        /// </summary>
        private void OnSelectionStateChanged(object sender, SelectionStateChangedEventArgs e)
        {
            ExceptionHandler.SafeExecute(() =>
            {
                LogMessage($"选择状态改变: {e.OldState} -> {e.NewState}");
                _selectionPanel.CurrentState = e.NewState;
                UpdateControlStates();
            }, _logService, "选择状态改变处理", false, $"{e.OldState} -> {e.NewState}");
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
        private void OnCalculatePredictions(object sender, EventArgs e)
        {
            ExceptionHandler.SafeExecute(async () => await CalculatePredictionsAsync(),
                _logService, "计算预测线", true, "用户触发计算");
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
        /// 主窗体键盘事件处理
        /// </summary>
        private void OnMainWindowKeyDown(object sender, KeyEventArgs e)
        {
            ExceptionHandler.SafeExecute(() =>
            {
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
            }, _logService, "键盘事件处理", false, $"按键: {e.KeyCode}, Ctrl: {e.Control}");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 清除选择
        /// </summary>
        public void ClearSelection()
        {
            ExceptionHandler.SafeExecute(() =>
            {
                _kLineSelector.ClearSelection();
                _priceDisplay.ClearAll();
                _predictionLines.Clear();
                UpdateControlStates();
                LogMessage("已清除所有选择");
            }, _logService, "清除选择", true, "用户触发清除");
        }

        /// <summary>
        /// 异步计算预测线
        /// </summary>
        public async Task CalculatePredictionsAsync()
        {
            if (_isCalculating)
                return;

            await ExceptionHandler.SafeExecuteAsync(async () =>
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
                    else
                    {
                        throw new InvalidOperationException("波神算法服务未初始化");
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
            }, _logService, "预测线计算", true, $"A点={_kLineSelector.GetPointAPrice()}, B点={_kLineSelector.GetPointBPrice()}");
        }

        /// <summary>
        /// 导出结果
        /// </summary>
        public void ExportResults()
        {
            ExceptionHandler.SafeExecute(() =>
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
            }, _logService, "结果导出", true, $"导出{_predictionLines.Count}条预测线");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新控件状态
        /// </summary>
        private void UpdateControlStates()
        {
            ExceptionHandler.SafeExecute(() =>
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
            }, _logService, "控件状态更新", false, $"状态: {_kLineSelector.CurrentState}, 计算: {_isCalculating}");
        }

        /// <summary>
        /// 更新K线选择器显示
        /// </summary>
        private void UpdateKLineSelectorDisplay()
        {
            ExceptionHandler.SafeExecute(() =>
            {
                // 这里可以添加预测线到KLineSelector的显示逻辑
                _kLineSelector.Invalidate();
            }, _logService, "K线选择器显示更新", false);
        }

        /// <summary>
        /// 生成导出内容
        /// </summary>
        private string GenerateExportContent()
        {
            return ExceptionHandler.SafeExecute(() =>
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
            }, _logService, "导出内容生成", false, "生成预测线文本内容", "");
        }

        #endregion

        #region 原有方法（保持兼容）

        /// <summary>
        /// 更新状态栏
        /// </summary>
        private void UpdateStatus(string status)
        {
            ExceptionHandler.SafeInvoke(toolStripStatusLabel, () =>
            {
                toolStripStatusLabel.Text = $"状态：{status}";
            }, _logService, "状态栏更新");
        }

        /// <summary>
        /// 记录日志消息
        /// </summary>
        private void LogMessage(string message, bool isError = false)
        {
            ExceptionHandler.SafeInvoke(_richTextBoxLog, () =>
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
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
            }, _logService, "日志记录");
        }

        /// <summary>
        /// 显示进度
        /// </summary>
        private void ShowProgress(int progress)
        {
            ExceptionHandler.SafeInvoke(toolStripProgressBar, () =>
            {
                toolStripProgressBar.Value = Math.Max(0, Math.Min(100, progress));
                toolStripProgressBar.Visible = progress > 0 && progress < 100;
            }, _logService, "进度显示");
        }

        /// <summary>
        /// 打开图像文件
        /// </summary>
        private void OpenImageFile()
        {
            ExceptionHandler.SafeExecute(() =>
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
            }, _logService, "图像文件打开", true, openFileDialog?.FileName);
        }

        /// <summary>
        /// 保存图像文件
        /// </summary>
        private void SaveImageFile()
        {
            ExceptionHandler.SafeExecute(() =>
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
            }, _logService, "图像文件保存", true, saveFileDialog?.FileName);
        }

        /// <summary>
        /// 根据文件扩展名获取图像格式
        /// </summary>
        private System.Drawing.Imaging.ImageFormat GetImageFormat(string fileName)
        {
            return ExceptionHandler.SafeExecute(() =>
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
            }, _logService, "图像格式获取", false, fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            ExceptionHandler.SafeExecute(() =>
            {
                if (_configService != null)
                {
                    var settings = _configService.LoadSettings();
                    LogMessage("配置加载完成");
                }
            }, _logService, "设置加载", false);
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void SaveSettings()
        {
            ExceptionHandler.SafeExecute(() =>
            {
                if (_configService != null)
                {
                    _configService.SaveSettings(new AppSettings());
                    LogMessage("配置保存完成");
                }
            }, _logService, "设置保存", false);
        }

        #endregion

        #region 窗体事件

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        private void MainWindow_Load(object sender, EventArgs e)
        {
            ExceptionHandler.SafeExecute(() =>
            {
                LogMessage("主窗体加载完成 - 集成版");
            }, _logService, "窗体加载", false);
        }

        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            ExceptionHandler.SafeExecute(() =>
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
                _currentImage?.Dispose();
                LogMessage("主窗体关闭，应用程序退出");
            }, _logService, "窗体关闭处理", true);
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