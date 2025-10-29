using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 增强选择面板控件
    /// 提供更丰富的交互功能，包括影线测量、多种测量模式和操作历史管理
    /// </summary>
    public class EnhancedSelectionPanel : Control
    {
        #region 事件

        /// <summary>
        /// 清除选择事件
        /// </summary>
        public event EventHandler ClearSelection;

        /// <summary>
        /// 撤销操作事件
        /// </summary>
        public event EventHandler Undo;

        /// <summary>
        /// 重做操作事件
        /// </summary>
        public event EventHandler Redo;

        /// <summary>
        /// 计算预测线事件
        /// </summary>
        public event EventHandler CalculatePredictions;

        /// <summary>
        /// 导出结果事件
        /// </summary>
        public event EventHandler ExportResults;

        /// <summary>
        /// 显示设置事件
        /// </summary>
        public event EventHandler ShowSettings;

        /// <summary>
        /// 测量模式改变事件
        /// </summary>
        public event EventHandler<MeasurementModeChangedEventArgs> MeasurementModeChanged;

        /// <summary>
        /// 影线测量事件
        /// </summary>
        public event EventHandler<ShadowMeasurementEventArgs> ShadowMeasurementRequested;

        #endregion

        #region 私有字段

        private KLineSelector.SelectionState _currentState;
        private bool _canUndo;
        private bool _canRedo;
        private bool _hasSelection;
        private bool _isCalculating;
        private MeasurementMode _measurementMode;
        private List<ButtonInfo> _buttons;
        private List<MeasurementModeInfo> _measurementModes;
        private Point _mousePosition;
        private Font _titleFont;
        private Font _statusFont;
        private Font _buttonFont;
        private Font _modeFont;
        private SolidBrush _textBrush;
        private SolidBrush _statusBrush;
        private SolidBrush _backgroundBrush;
        private SolidBrush _modeBackgroundBrush;
        private Pen _borderPen;
        private Pen _separatorPen;
        private string _lastAction;
        private DateTime _lastActionTime;

        #endregion

        #region 枚举

        /// <summary>
        /// 测量模式
        /// </summary>
        public enum MeasurementMode
        {
            Standard,      // 标准模式：A点到B点
            UpperShadow,   // 上影线测量
            LowerShadow,   // 下影线测量
            FullShadow     // 完整影线测量
        }

        #endregion

        #region 构造函数

        public EnhancedSelectionPanel()
        {
            InitializeComponent();
            InitializeControls();
            InitializeMeasurementModes();
            EnableDoubleBuffering();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前选择状态
        /// </summary>
        public KLineSelector.SelectionState CurrentState
        {
            get => _currentState;
            set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    UpdateButtonStates();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        public bool CanUndo
        {
            get => _canUndo;
            set
            {
                if (_canUndo != value)
                {
                    _canUndo = value;
                    UpdateButtonStates();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 是否可以重做
        /// </summary>
        public bool CanRedo
        {
            get => _canRedo;
            set
            {
                if (_canRedo != value)
                {
                    _canRedo = value;
                    UpdateButtonStates();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 是否有选择
        /// </summary>
        public bool HasSelection
        {
            get => _hasSelection;
            set
            {
                if (_hasSelection != value)
                {
                    _hasSelection = value;
                    UpdateButtonStates();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 是否正在计算
        /// </summary>
        public bool IsCalculating
        {
            get => _isCalculating;
            set
            {
                if (_isCalculating != value)
                {
                    _isCalculating = value;
                    UpdateButtonStates();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 当前测量模式
        /// </summary>
        public MeasurementMode CurrentMeasurementMode
        {
            get => _measurementMode;
            set
            {
                if (_measurementMode != value)
                {
                    var oldMode = _measurementMode;
                    _measurementMode = value;
                    OnMeasurementModeChanged(new MeasurementModeChangedEventArgs(oldMode, value));
                    UpdateButtonStates();
                    Invalidate();
                }
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 更新状态信息
        /// </summary>
        /// <param name="state">选择状态</param>
        /// <param name="canUndo">是否可撤销</param>
        /// <param name="canRedo">是否可重做</param>
        /// <param name="hasSelection">是否有选择</param>
        /// <param name="isCalculating">是否正在计算</param>
        public void UpdateState(KLineSelector.SelectionState state, bool canUndo = false,
            bool canRedo = false, bool hasSelection = false, bool isCalculating = false)
        {
            _currentState = state;
            _canUndo = canUndo;
            _canRedo = canRedo;
            _hasSelection = hasSelection;
            _isCalculating = isCalculating;
            UpdateButtonStates();
            Invalidate();
        }

        /// <summary>
        /// 重置面板状态
        /// </summary>
        public void Reset()
        {
            UpdateState(KLineSelector.SelectionState.None, false, false, false, false);
            _measurementMode = MeasurementMode.Standard;
            _lastAction = string.Empty;
            _lastActionTime = DateTime.MinValue;
        }

        /// <summary>
        /// 记录操作历史
        /// </summary>
        /// <param name="action">操作描述</param>
        public void RecordAction(string action)
        {
            _lastAction = action;
            _lastActionTime = DateTime.Now;
            Invalidate();
        }

        /// <summary>
        /// 获取测量模式描述
        /// </summary>
        /// <param name="mode">测量模式</param>
        /// <returns>模式描述</returns>
        public string GetMeasurementModeDescription(MeasurementMode mode)
        {
            var modeInfo = _measurementModes.FirstOrDefault(m => m.Mode == mode);
            return modeInfo?.Description ?? "未知模式";
        }

        #endregion

        #region 重写方法

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 绘制背景
            DrawBackground(e.Graphics);

            // 绘制内容
            var y = 10;
            y = DrawTitle(e.Graphics, "增强操作面板", y);
            y = DrawSectionSeparator(e.Graphics, y);
            y = DrawMeasurementModes(e.Graphics, y);
            y = DrawSectionSeparator(e.Graphics, y);
            y = DrawStatus(e.Graphics, y);
            y = DrawSectionSeparator(e.Graphics, y);
            y = DrawButtons(e.Graphics, y);
            y = DrawSectionSeparator(e.Graphics, y);
            y = DrawLastAction(e.Graphics, y);
            DrawSectionSeparator(e.Graphics, y);
            DrawShortcuts(e.Graphics, y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var oldPosition = _mousePosition;
            _mousePosition = e.Location;

            bool wasOnButton = GetButtonAt(oldPosition) != null;
            bool wasOnMode = GetMeasurementModeAt(oldPosition) != null;
            bool isOnButton = GetButtonAt(e.Location) != null;
            bool isOnMode = GetMeasurementModeAt(e.Location) != null;

            if (wasOnButton != isOnButton || wasOnMode != isOnMode)
            {
                Invalidate();
            }

            // 更新鼠标光标
            Cursor = (isOnButton || isOnMode) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                // 检查测量模式点击
                var modeInfo = GetMeasurementModeAt(e.Location);
                if (modeInfo != null)
                {
                    CurrentMeasurementMode = modeInfo.Mode;
                    return;
                }

                // 检查按钮点击
                var button = GetButtonAt(e.Location);
                if (button != null && button.Enabled)
                {
                    ExecuteButtonAction(button.Action);
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _mousePosition = Point.Empty;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _titleFont?.Dispose();
                _statusFont?.Dispose();
                _buttonFont?.Dispose();
                _modeFont?.Dispose();
                _textBrush?.Dispose();
                _statusBrush?.Dispose();
                _backgroundBrush?.Dispose();
                _modeBackgroundBrush?.Dispose();
                _borderPen?.Dispose();
                _separatorPen?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region 私有方法

        private void InitializeComponent()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.DoubleBuffer |
                    ControlStyles.ResizeRedraw, true);

            BackColor = Color.White;
            ForeColor = Color.FromArgb(51, 51, 51);
            MinimumSize = new Size(220, 400);
            Padding = new Padding(10);
        }

        private void InitializeControls()
        {
            _currentState = KLineSelector.SelectionState.None;
            _measurementMode = MeasurementMode.Standard;
            _buttons = new List<ButtonInfo>();
            _mousePosition = Point.Empty;
            _lastAction = string.Empty;
            _lastActionTime = DateTime.MinValue;

            // 初始化字体和画笔
            _titleFont = new Font("Arial", 10, FontStyle.Bold);
            _statusFont = new Font("Arial", 9);
            _buttonFont = new Font("Arial", 8);
            _modeFont = new Font("Arial", 8);
            _textBrush = new SolidBrush(Color.FromArgb(51, 51, 51));
            _statusBrush = new SolidBrush(Color.FromArgb(102, 102, 102));
            _backgroundBrush = new SolidBrush(Color.White);
            _modeBackgroundBrush = new SolidBrush(Color.FromArgb(248, 248, 248));
            _borderPen = new Pen(Color.FromArgb(204, 204, 204));
            _separatorPen = new Pen(Color.FromArgb(230, 230, 230), 1);

            InitializeButtons();
        }

        private void InitializeButtons()
        {
            _buttons.Clear();
            _buttons.Add(new ButtonInfo("清除选择", ButtonAction.ClearSelection, "Ctrl+R"));
            _buttons.Add(new ButtonInfo("撤销", ButtonAction.Undo, "Ctrl+Z"));
            _buttons.Add(new ButtonInfo("重做", ButtonAction.Redo, "Ctrl+Y"));
            _buttons.Add(new ButtonInfo("影线测量", ButtonAction.ShadowMeasurement, "Ctrl+M"));
            _buttons.Add(new ButtonInfo("计算预测", ButtonAction.CalculatePredictions, "Space"));
            _buttons.Add(new ButtonInfo("导出结果", ButtonAction.ExportResults, "Ctrl+E"));
            _buttons.Add(new ButtonInfo("设置", ButtonAction.ShowSettings, "Ctrl+S"));
        }

        private void InitializeMeasurementModes()
        {
            _measurementModes = new List<MeasurementModeInfo>
            {
                new MeasurementModeInfo(MeasurementMode.Standard, "标准模式", "A点到B点测量"),
                new MeasurementModeInfo(MeasurementMode.UpperShadow, "上影线", "测量K线上影线"),
                new MeasurementModeInfo(MeasurementMode.LowerShadow, "下影线", "测量K线下影线"),
                new MeasurementModeInfo(MeasurementMode.FullShadow, "完整影线", "测量完整影线")
            };
        }

        private void EnableDoubleBuffering()
        {
            DoubleBuffered = true;
        }

        private void UpdateButtonStates()
        {
            foreach (var button in _buttons)
            {
                button.Enabled = GetButtonEnabled(button.Action);
            }
        }

        private bool GetButtonEnabled(ButtonAction action)
        {
            switch (action)
            {
                case ButtonAction.ClearSelection:
                    return _hasSelection && !_isCalculating;
                case ButtonAction.Undo:
                    return _canUndo && !_isCalculating;
                case ButtonAction.Redo:
                    return _canRedo && !_isCalculating;
                case ButtonAction.ShadowMeasurement:
                    return _hasSelection && !_isCalculating;
                case ButtonAction.CalculatePredictions:
                    return _currentState == KLineSelector.SelectionState.Complete && !_isCalculating;
                case ButtonAction.ExportResults:
                    return _hasSelection && !_isCalculating;
                case ButtonAction.ShowSettings:
                    return !_isCalculating;
                default:
                    return false;
            }
        }

        private void DrawBackground(Graphics g)
        {
            g.FillRectangle(_backgroundBrush, ClientRectangle);
            g.DrawRectangle(_borderPen, 0, 0, Width - 1, Height - 1);
        }

        private int DrawTitle(Graphics g, string title, int y)
        {
            var titleSize = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, _textBrush, 10, y);
            return y + (int)titleSize.Height + 10;
        }

        private int DrawSectionSeparator(Graphics g, int y)
        {
            _separatorPen.DashStyle = DashStyle.Dash;
            g.DrawLine(_separatorPen, 10, y + 5, Width - 10, y + 5);
            return y + 15;
        }

        private int DrawMeasurementModes(Graphics g, int y)
        {
            g.DrawString("测量模式:", _statusFont, _statusBrush, 10, y);
            y += 20;

            const int modeHeight = 25;
            const int modeSpacing = 3;

            foreach (var mode in _measurementModes)
            {
                var modeRect = new Rectangle(10, y, Width - 20, modeHeight);
                DrawMeasurementMode(g, mode, modeRect);
                y += modeHeight + modeSpacing;
            }

            return y + 10;
        }

        private void DrawMeasurementMode(Graphics g, MeasurementModeInfo modeInfo, Rectangle rect)
        {
            var isSelected = modeInfo.Mode == _measurementMode;
            var isHovered = rect.Contains(_mousePosition);

            // 确定颜色
            var backColor = isSelected ?
                Color.FromArgb(0, 122, 204) :
                (isHovered ? Color.FromArgb(240, 240, 240) : Color.FromArgb(248, 248, 248));
            var foreColor = isSelected ?
                Color.White :
                Color.FromArgb(51, 51, 51);
            var borderColor = isSelected ?
                Color.FromArgb(0, 122, 204) :
                Color.FromArgb(204, 204, 204);

            // 绘制背景
            using (var brush = new SolidBrush(backColor))
            using (var pen = new Pen(borderColor))
            {
                g.FillRectangle(brush, rect);
                g.DrawRectangle(pen, rect);
            }

            // 绘制模式名称
            var nameSize = g.MeasureString(modeInfo.Name, _modeFont);
            g.DrawString(modeInfo.Name, _modeFont, foreColor, rect.X + 8, rect.Y + (rect.Height - (int)nameSize.Height) / 2);

            // 绘制选中标记
            if (isSelected)
            {
                using (var brush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(brush, rect.Right - 20, rect.Y + (rect.Height - 8) / 2, 8, 8);
                }
            }
        }

        private int DrawStatus(Graphics g, int y)
        {
            var statusText = GetStatusText();
            g.DrawString("当前状态:", _statusFont, _statusBrush, 10, y);
            y += 20;

            g.DrawString(statusText, _statusFont, _textBrush, 10, y);
            y += 25;

            if (_isCalculating)
            {
                g.DrawString("正在计算...", _statusFont, Brushes.Blue, 10, y);
                y += 20;
            }

            return y;
        }

        private string GetStatusText()
        {
            var baseStatus = _currentState switch
            {
                KLineSelector.SelectionState.None => "请选择A点",
                KLineSelector.SelectionState.PointASelected => "请选择B点",
                KLineSelector.SelectionState.Complete => "选择完成，可计算预测线",
                _ => "未知状态"
            };

            var modeDesc = GetMeasurementModeDescription(_measurementMode);
            return $"{baseStatus} ({modeDesc})";
        }

        private int DrawButtons(Graphics g, int y)
        {
            const int buttonHeight = 28;
            const int buttonSpacing = 5;
            const int margin = 10;

            foreach (var button in _buttons)
            {
                var buttonRect = new Rectangle(margin, y, Width - 2 * margin, buttonHeight);
                DrawButton(g, button, buttonRect);
                y += buttonHeight + buttonSpacing;
            }

            return y;
        }

        private void DrawButton(Graphics g, ButtonInfo button, Rectangle rect)
        {
            var isHovered = rect.Contains(_mousePosition);
            var isEnabled = button.Enabled;

            // 确定颜色
            var backColor = isEnabled ?
                (isHovered ? Color.FromArgb(0, 122, 204) : Color.FromArgb(240, 240, 240)) :
                Color.FromArgb(248, 248, 248);
            var foreColor = isEnabled ?
                (isHovered ? Color.White : Color.FromArgb(51, 51, 51)) :
                Color.FromArgb(170, 170, 170);
            var borderColor = isEnabled ?
                (isHovered ? Color.FromArgb(0, 122, 204) : Color.FromArgb(204, 204, 204)) :
                Color.FromArgb(230, 230, 230);

            // 绘制背景
            using (var brush = new SolidBrush(backColor))
            using (var pen = new Pen(borderColor))
            {
                g.FillRectangle(brush, rect);
                g.DrawRectangle(pen, rect);
            }

            // 绘制文本
            var textSize = g.MeasureString(button.Text, _buttonFont);
            var textX = rect.X + (rect.Width - (int)textSize.Width) / 2;
            var textY = rect.Y + (rect.Height - (int)textSize.Height) / 2;

            using (var brush = new SolidBrush(foreColor))
            {
                g.DrawString(button.Text, _buttonFont, brush, textX, textY);
            }
        }

        private int DrawLastAction(Graphics g, int y)
        {
            if (!string.IsNullOrEmpty(_lastAction) && _lastActionTime != DateTime.MinValue)
            {
                g.DrawString("最近操作:", _statusFont, _statusBrush, 10, y);
                y += 20;

                var timeDiff = DateTime.Now - _lastActionTime;
                var timeText = timeDiff.TotalMinutes < 1 ? "刚刚" : $"{timeDiff.TotalMinutes:F0}分钟前";

                g.DrawString($"{_lastAction} ({timeText})", _buttonFont, _textBrush, 10, y);
                y += 20;
            }

            return y;
        }

        private void DrawShortcuts(Graphics g, int y)
        {
            g.DrawString("快捷键:", _statusFont, _statusBrush, 10, y);
            y += 20;

            var shortcuts = new[]
            {
                "Ctrl+R - 清除选择",
                "Ctrl+Z - 撤销",
                "Ctrl+Y - 重做",
                "Ctrl+M - 影线测量",
                "Space - 计算预测",
                "Ctrl+E - 导出结果",
                "1-4 - 切换测量模式"
            };

            foreach (var shortcut in shortcuts)
            {
                g.DrawString(shortcut, _buttonFont, _statusBrush, 10, y);
                y += 16;
            }
        }

        private MeasurementModeInfo GetMeasurementModeAt(Point location)
        {
            const int modeHeight = 25;
            const int modeSpacing = 3;
            const int startY = 60; // 测量模式开始位置

            for (int i = 0; i < _measurementModes.Count; i++)
            {
                var modeRect = new Rectangle(
                    10,
                    startY + i * (modeHeight + modeSpacing),
                    Width - 20,
                    modeHeight);

                if (modeRect.Contains(location))
                {
                    return _measurementModes[i];
                }
            }

            return null;
        }

        private ButtonInfo GetButtonAt(Point location)
        {
            const int buttonHeight = 28;
            const int buttonSpacing = 5;
            const int margin = 10;
            var startY = 175; // 按钮开始位置

            for (int i = 0; i < _buttons.Count; i++)
            {
                var buttonRect = new Rectangle(
                    margin,
                    startY + i * (buttonHeight + buttonSpacing),
                    Width - 2 * margin,
                    buttonHeight);

                if (buttonRect.Contains(location))
                {
                    return _buttons[i];
                }
            }

            return null;
        }

        private void ExecuteButtonAction(ButtonAction action)
        {
            switch (action)
            {
                case ButtonAction.ClearSelection:
                    ClearSelection?.Invoke(this, EventArgs.Empty);
                    RecordAction("清除选择");
                    break;
                case ButtonAction.Undo:
                    Undo?.Invoke(this, EventArgs.Empty);
                    RecordAction("撤销操作");
                    break;
                case ButtonAction.Redo:
                    Redo?.Invoke(this, EventArgs.Empty);
                    RecordAction("重做操作");
                    break;
                case ButtonAction.ShadowMeasurement:
                    OnShadowMeasurementRequested(new ShadowMeasurementEventArgs(_measurementMode));
                    RecordAction($"影线测量 ({GetMeasurementModeDescription(_measurementMode)})");
                    break;
                case ButtonAction.CalculatePredictions:
                    CalculatePredictions?.Invoke(this, EventArgs.Empty);
                    RecordAction("计算预测线");
                    break;
                case ButtonAction.ExportResults:
                    ExportResults?.Invoke(this, EventArgs.Empty);
                    RecordAction("导出结果");
                    break;
                case ButtonAction.ShowSettings:
                    ShowSettings?.Invoke(this, EventArgs.Empty);
                    RecordAction("打开设置");
                    break;
            }
        }

        #endregion

        #region 事件触发器

        protected virtual void OnMeasurementModeChanged(MeasurementModeChangedEventArgs e)
        {
            MeasurementModeChanged?.Invoke(this, e);
        }

        protected virtual void OnShadowMeasurementRequested(ShadowMeasurementEventArgs e)
        {
            ShadowMeasurementRequested?.Invoke(this, e);
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 按钮信息
        /// </summary>
        private class ButtonInfo
        {
            public string Text { get; }
            public ButtonAction Action { get; }
            public string Shortcut { get; }
            public bool Enabled { get; set; }

            public ButtonInfo(string text, ButtonAction action, string shortcut)
            {
                Text = text;
                Action = action;
                Shortcut = shortcut;
                Enabled = true;
            }
        }

        /// <summary>
        /// 测量模式信息
        /// </summary>
        private class MeasurementModeInfo
        {
            public MeasurementMode Mode { get; }
            public string Name { get; }
            public string Description { get; }

            public MeasurementModeInfo(MeasurementMode mode, string name, string description)
            {
                Mode = mode;
                Name = name;
                Description = description;
            }
        }

        /// <summary>
        /// 按钮动作
        /// </summary>
        private enum ButtonAction
        {
            ClearSelection,
            Undo,
            Redo,
            ShadowMeasurement,
            CalculatePredictions,
            ExportResults,
            ShowSettings
        }

        #endregion
    }

    #region 事件参数类

    /// <summary>
    /// 测量模式改变事件参数
    /// </summary>
    public class MeasurementModeChangedEventArgs : EventArgs
    {
        public EnhancedSelectionPanel.MeasurementMode OldMode { get; }
        public EnhancedSelectionPanel.MeasurementMode NewMode { get; }

        public MeasurementModeChangedEventArgs(EnhancedSelectionPanel.MeasurementMode oldMode, EnhancedSelectionPanel.MeasurementMode newMode)
        {
            OldMode = oldMode;
            NewMode = newMode;
        }
    }

    /// <summary>
    /// 影线测量事件参数
    /// </summary>
    public class ShadowMeasurementEventArgs : EventArgs
    {
        public EnhancedSelectionPanel.MeasurementMode Mode { get; }

        public ShadowMeasurementEventArgs(EnhancedSelectionPanel.MeasurementMode mode)
        {
            Mode = mode;
        }
    }

    #endregion
}