using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 选择面板控件
    /// 管理K线选择状态、操作按钮和快捷键支持
    /// </summary>
    public class SelectionPanel : Control
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

        #endregion

        #region 私有字段

        private KLineSelector.SelectionState _currentState;
        private bool _canUndo;
        private bool _canRedo;
        private bool _hasSelection;
        private bool _isCalculating;
        private readonly List<ButtonInfo> _buttons;
        private readonly Font _titleFont;
        private readonly Font _statusFont;
        private readonly Font _buttonFont;
        private readonly SolidBrush _textBrush;
        private readonly SolidBrush _statusBrush;
        private readonly SolidBrush _backgroundBrush;
        private readonly Pen _borderPen;
        private Point _mousePosition;

        #endregion

        #region 构造函数

        public SelectionPanel()
        {
            InitializeComponent();

            _currentState = KLineSelector.SelectionState.None;
            _buttons = new List<ButtonInfo>();
            _mousePosition = Point.Empty;

            // 初始化字体和画笔
            _titleFont = new Font("Arial", 10, FontStyle.Bold);
            _statusFont = new Font("Arial", 9);
            _buttonFont = new Font("Arial", 8);
            _textBrush = new SolidBrush(Color.FromArgb(51, 51, 51));
            _statusBrush = new SolidBrush(Color.FromArgb(102, 102, 102));
            _backgroundBrush = new SolidBrush(Color.White);
            _borderPen = new Pen(Color.FromArgb(204, 204, 204));

            InitializeButtons();
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
        }

        #endregion

        #region 重写方法

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 绘制背景
            e.Graphics.FillRectangle(_backgroundBrush, ClientRectangle);
            e.Graphics.DrawRectangle(_borderPen, 0, 0, Width - 1, Height - 1);

            // 绘制内容
            var y = 10;
            y = DrawTitle(e.Graphics, "操作面板", y);
            y = DrawSectionSeparator(e.Graphics, y);
            y = DrawStatus(e.Graphics, y);
            y = DrawSectionSeparator(e.Graphics, y);
            y = DrawButtons(e.Graphics, y);
            y = DrawSectionSeparator(e.Graphics, y);
            DrawShortcuts(e.Graphics, y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var oldPosition = _mousePosition;
            _mousePosition = e.Location;

            // 检查鼠标是否悬停在按钮上
            bool wasOnButton = GetButtonAt(oldPosition) != null;
            bool isOnButton = GetButtonAt(e.Location) != null;

            if (wasOnButton != isOnButton)
            {
                Invalidate();
            }

            // 更新鼠标光标
            Cursor = isOnButton ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
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
                _textBrush?.Dispose();
                _statusBrush?.Dispose();
                _backgroundBrush?.Dispose();
                _borderPen?.Dispose();
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
            MinimumSize = new Size(200, 250);
            Padding = new Padding(10);
        }

        private void EnableDoubleBuffering()
        {
            DoubleBuffered = true;
        }

        private void InitializeButtons()
        {
            _buttons.Add(new ButtonInfo("清除选择", ButtonAction.ClearSelection, "Ctrl+R"));
            _buttons.Add(new ButtonInfo("撤销", ButtonAction.Undo, "Ctrl+Z"));
            _buttons.Add(new ButtonInfo("重做", ButtonAction.Redo, "Ctrl+Y"));
            _buttons.Add(new ButtonInfo("计算预测", ButtonAction.CalculatePredictions, "Space"));
            _buttons.Add(new ButtonInfo("导出结果", ButtonAction.ExportResults, "Ctrl+E"));
            _buttons.Add(new ButtonInfo("设置", ButtonAction.ShowSettings, "Ctrl+S"));
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

        private int DrawTitle(Graphics g, string title, int y)
        {
            var titleSize = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, _textBrush, 10, y);
            return y + (int)titleSize.Height + 10;
        }

        private int DrawSectionSeparator(Graphics g, int y)
        {
            using (var separatorPen = new Pen(Color.FromArgb(230, 230, 230), 1))
            {
                separatorPen.DashStyle = DashStyle.Dash;
                g.DrawLine(separatorPen, 10, y + 5, Width - 10, y + 5);
            }
            return y + 15;
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
            switch (_currentState)
            {
                case KLineSelector.SelectionState.None:
                    return "请选择A点";
                case KLineSelector.SelectionState.PointASelected:
                    return "请选择B点";
                case KLineSelector.SelectionState.Complete:
                    return "选择完成，可计算预测线";
                default:
                    return "未知状态";
            }
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

        private void DrawShortcuts(Graphics g, int y)
        {
            g.DrawString("快捷键:", _statusFont, _statusBrush, 10, y);
            y += 20;

            var shortcuts = new[]
            {
                "Ctrl+R - 清除选择",
                "Ctrl+Z - 撤销",
                "Ctrl+Y - 重做",
                "Space - 计算预测",
                "Ctrl+E - 导出结果"
            };

            foreach (var shortcut in shortcuts)
            {
                g.DrawString(shortcut, _buttonFont, _statusBrush, 10, y);
                y += 16;
            }
        }

        private ButtonInfo GetButtonAt(Point location)
        {
            const int buttonHeight = 28;
            const int buttonSpacing = 5;
            const int margin = 10;
            var startY = 105; // 按钮开始位置

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
                    break;
                case ButtonAction.Undo:
                    Undo?.Invoke(this, EventArgs.Empty);
                    break;
                case ButtonAction.Redo:
                    Redo?.Invoke(this, EventArgs.Empty);
                    break;
                case ButtonAction.CalculatePredictions:
                    CalculatePredictions?.Invoke(this, EventArgs.Empty);
                    break;
                case ButtonAction.ExportResults:
                    ExportResults?.Invoke(this, EventArgs.Empty);
                    break;
                case ButtonAction.ShowSettings:
                    ShowSettings?.Invoke(this, EventArgs.Empty);
                    break;
            }
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
        /// 按钮动作
        /// </summary>
        private enum ButtonAction
        {
            ClearSelection,
            Undo,
            Redo,
            CalculatePredictions,
            ExportResults,
            ShowSettings
        }

        #endregion
    }
}