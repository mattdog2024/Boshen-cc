using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BoshenCC.Core.Utils;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.WinForms.Forms
{
    /// <summary>
    /// 透明叠加窗口
    /// 用于在目标窗口上绘制预测线
    /// </summary>
    public partial class TransparentOverlayForm : Form
    {
        #region Win32 API 声明

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);

        #endregion

        #region 结构体和常量

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;

            public SIZE(int cx, int cy)
            {
                this.cx = cx;
                this.cy = cy;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;

            public static BLENDFUNCTION CreateDefault(byte alpha)
            {
                return new BLENDFUNCTION
                {
                    BlendOp = 0, // AC_SRC_OVER
                    BlendFlags = 0,
                    SourceConstantAlpha = alpha,
                    AlphaFormat = 1 // AC_SRC_ALPHA
                };
            }
        }

        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int GWL_EXSTYLE = -20;
        private const int LWA_ALPHA = 0x00000002;
        private const int ULW_ALPHA = 0x00000002;

        #endregion

        #region 私有字段

        private readonly IDrawingService _drawingService;
        private readonly List<PredictionLine> _predictionLines;
        private readonly DrawingEngine _drawingEngine;
        private bool _isInitialized;
        private IntPtr _targetWindowHandle;
        private byte _windowAlpha = 200;
        private bool _clickThrough = true;
        private IntPtr _memoryDC = IntPtr.Zero;
        private IntPtr _memoryBitmap = IntPtr.Zero;
        private IntPtr _oldBitmap = IntPtr.Zero;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化TransparentOverlayForm
        /// </summary>
        /// <param name="drawingService">绘制服务</param>
        public TransparentOverlayForm(IDrawingService drawingService)
        {
            _drawingService = drawingService ?? throw new ArgumentNullException(nameof(drawingService));
            _predictionLines = new List<PredictionLine>();

            InitializeComponent();
            InitializeForm();

            // 创建绘制引擎
            var logService = drawingService.GetServiceStatus().ContainsKey("LogService") ?
                (ILogService)drawingService.GetServiceStatus()["LogService"] : null;
            _drawingEngine = new DrawingEngine(logService);

            _isInitialized = true;
        }

        /// <summary>
        /// 无参构造函数（用于设计器）
        /// </summary>
        public TransparentOverlayForm()
        {
            _predictionLines = new List<PredictionLine>();
            InitializeComponent();
            InitializeForm();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 目标窗口句柄
        /// </summary>
        public IntPtr TargetWindowHandle
        {
            get => _targetWindowHandle;
            set
            {
                if (_targetWindowHandle != value)
                {
                    _targetWindowHandle = value;
                    UpdateWindowPosition();
                }
            }
        }

        /// <summary>
        /// 窗口透明度
        /// </summary>
        public byte WindowAlpha
        {
            get => _windowAlpha;
            set
            {
                if (_windowAlpha != value)
                {
                    _windowAlpha = value;
                    SetWindowAlpha(value);
                }
            }
        }

        /// <summary>
        /// 是否点击穿透
        /// </summary>
        public bool ClickThrough
        {
            get => _clickThrough;
            set
            {
                if (_clickThrough != value)
                {
                    _clickThrough = value;
                    SetClickThrough(value);
                }
            }
        }

        /// <summary>
        /// 预测线集合
        /// </summary>
        public IReadOnlyList<PredictionLine> PredictionLines => _predictionLines;

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置预测线集合
        /// </summary>
        /// <param name="predictionLines">预测线集合</param>
        public void SetPredictionLines(IEnumerable<PredictionLine> predictionLines)
        {
            try
            {
                if (predictionLines == null)
                {
                    _predictionLines.Clear();
                }
                else
                {
                    _predictionLines.Clear();
                    _predictionLines.AddRange(predictionLines.Where(p => p != null));
                }

                Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置预测线失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 添加预测线
        /// </summary>
        /// <param name="predictionLine">预测线</param>
        public void AddPredictionLine(PredictionLine predictionLine)
        {
            try
            {
                if (predictionLine != null && !_predictionLines.Any(p => p.Name == predictionLine.Name))
                {
                    _predictionLines.Add(predictionLine);
                    Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加预测线失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 移除预测线
        /// </summary>
        /// <param name="lineName">预测线名称</param>
        public void RemovePredictionLine(string lineName)
        {
            try
            {
                var line = _predictionLines.FirstOrDefault(p => p.Name == lineName);
                if (line != null)
                {
                    _predictionLines.Remove(line);
                    Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移除预测线失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 清空所有预测线
        /// </summary>
        public void ClearPredictionLines()
        {
            try
            {
                _predictionLines.Clear();
                Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清空预测线失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 更新窗口位置跟随目标窗口
        /// </summary>
        public void UpdateWindowPosition()
        {
            try
            {
                if (_targetWindowHandle != IntPtr.Zero)
                {
                    if (Win32Api.GetWindowRect(_targetWindowHandle, out WindowStructs.RECT rect))
                    {
                        this.SetBounds(rect.Left, rect.Top, rect.Width, rect.Height);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新窗口位置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 强制重绘
        /// </summary>
        public void ForceRedraw()
        {
            try
            {
                Invalidate();
                Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重绘失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 私有方法 - 初始化

        /// <summary>
        /// 初始化窗体
        /// </summary>
        private void InitializeForm()
        {
            try
            {
                // 设置窗体样式
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.Manual;
                this.ShowInTaskbar = false;
                this.TopMost = true;
                this.BackColor = Color.Black;
                this.TransparencyKey = Color.Black;

                // 设置分层窗口样式
                SetLayeredWindowStyle();

                // 设置点击穿透
                SetClickThrough(_clickThrough);

                // 设置透明度
                SetWindowAlpha(_windowAlpha);

                // 创建内存DC
                CreateMemoryDC();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化窗体失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 设置分层窗口样式
        /// </summary>
        private void SetLayeredWindowStyle()
        {
            try
            {
                int extendedStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                SetWindowLong(this.Handle, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置分层窗口样式失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 设置点击穿透
        /// </summary>
        /// <param name="enabled">是否启用</param>
        private void SetClickThrough(bool enabled)
        {
            try
            {
                int extendedStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                if (enabled)
                {
                    SetWindowLong(this.Handle, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
                }
                else
                {
                    SetWindowLong(this.Handle, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置点击穿透失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 设置窗口透明度
        /// </summary>
        /// <param name="alpha">透明度 (0-255)</param>
        private void SetWindowAlpha(byte alpha)
        {
            try
            {
                SetLayeredWindowAttributes(this.Handle, 0, alpha, LWA_ALPHA);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置窗口透明度失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 创建内存DC
        /// </summary>
        private void CreateMemoryDC()
        {
            try
            {
                // 释放旧的资源
                ReleaseMemoryDC();

                // 创建内存DC
                IntPtr screenDC = GetDC(IntPtr.Zero);
                _memoryDC = CreateCompatibleDC(screenDC);
                _memoryBitmap = CreateCompatibleBitmap(screenDC, this.Width, this.Height);
                _oldBitmap = SelectObject(_memoryDC, _memoryBitmap);
                ReleaseDC(IntPtr.Zero, screenDC);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建内存DC失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 释放内存DC
        /// </summary>
        private void ReleaseMemoryDC()
        {
            try
            {
                if (_oldBitmap != IntPtr.Zero)
                {
                    SelectObject(_memoryDC, _oldBitmap);
                    _oldBitmap = IntPtr.Zero;
                }

                if (_memoryBitmap != IntPtr.Zero)
                {
                    DeleteObject(_memoryBitmap);
                    _memoryBitmap = IntPtr.Zero;
                }

                if (_memoryDC != IntPtr.Zero)
                {
                    DeleteDC(_memoryDC);
                    _memoryDC = IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"释放内存DC失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 重写OnPaint方法进行绘制
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                if (!_isInitialized || _drawingEngine == null)
                {
                    base.OnPaint(e);
                    return;
                }

                // 清空背景
                e.Graphics.Clear(Color.Transparent);

                // 绘制预测线
                if (_predictionLines.Count > 0)
                {
                    var chartBounds = new RectangleF(0, 0, this.Width, this.Height);
                    _drawingEngine.DrawPredictionLines(e.Graphics, _predictionLines, chartBounds);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"绘制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                base.OnPaint(e);
            }
        }

        /// <summary>
        /// 重写OnSizeChanged方法
        /// </summary>
        protected override void OnSizeChanged(EventArgs e)
        {
            try
            {
                base.OnSizeChanged(e);

                // 重新创建内存DC
                CreateMemoryDC();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"窗口大小变化处理失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 重写OnFormClosing方法
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // 释放资源
                ReleaseMemoryDC();
                _drawingEngine?.Dispose();

                base.OnFormClosing(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"窗体关闭处理失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 组件设计器生成的代码

        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            // 释放自定义资源
            ReleaseMemoryDC();
            _drawingEngine?.Dispose();

            base.Dispose(disposing);
        }

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            //
            // TransparentOverlayForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.ControlBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TransparentOverlayForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "透明叠加窗口";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.Black;
            this.ResumeLayout(false);

        }

        #endregion
    }
}