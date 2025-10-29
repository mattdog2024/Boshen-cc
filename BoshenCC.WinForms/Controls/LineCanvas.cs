using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 线条画布控件
    /// 实现专业的图表绘制功能，支持高DPI渲染、平滑动画和多种绘制模式
    /// </summary>
    public class LineCanvas : Control
    {
        #region 事件

        /// <summary>
        /// 画布绘制事件
        /// </summary>
        public event EventHandler<CanvasPaintEventArgs> CanvasPaint;

        /// <summary>
        /// 缩放改变事件
        /// </summary>
        public event EventHandler<ZoomChangedEventArgs> ZoomChanged;

        /// <summary>
        /// 平移改变事件
        /// </summary>
        public event EventHandler<PanChangedEventArgs> PanChanged;

        /// <summary>
        /// 渲染质量改变事件
        /// </summary>
        public event EventHandler<RenderQualityChangedEventArgs> RenderQualityChanged;

        #endregion

        #region 私有字段

        private Bitmap _backBuffer;
        private Graphics _backBufferGraphics;
        private bool _needsRedraw;
        private float _zoomFactor = 1.0f;
        private PointF _panOffset = PointF.Empty;
        private RenderQuality _renderQuality = RenderQuality.High;
        private SmoothingMode _smoothingMode = SmoothingMode.AntiAlias;
        private TextRenderingHint _textRenderingHint = TextRenderingHint.ClearTypeGridFit;
        private InterpolationMode _interpolationMode = InterpolationMode.HighQualityBicubic;
        private PixelOffsetMode _pixelOffsetMode = PixelOffsetMode.HighQuality;
        private CompositingQuality _compositingQuality = CompositingQuality.HighQuality;

        // 绘制层管理
        private readonly Dictionary<string, CanvasLayer> _layers;
        private readonly List<string> _layerOrder;

        // 高DPI支持
        private float _dpiScaleX = 1.0f;
        private float _dpiScaleY = 1.0f;

        // 缓存和性能优化
        private readonly Dictionary<string, Bitmap> _layerCache;
        private bool _useLayerCache = true;
        private DateTime _lastRenderTime;
        private TimeSpan _renderTime = TimeSpan.Zero;

        // 交互状态
        private bool _isPanning;
        private Point _lastPanPoint;
        private Rectangle _selectionRect;
        private bool _isSelecting;

        #endregion

        #region 枚举

        /// <summary>
        /// 渲染质量
        /// </summary>
        public enum RenderQuality
        {
            /// <summary>
            /// 低质量（最快）
            /// </summary>
            Low,

            /// <summary>
            /// 中等质量
            /// </summary>
            Medium,

            /// <summary>
            /// 高质量（默认）
            /// </summary>
            High,

            /// <summary>
            /// 最高质量（最慢）
            /// </summary>
            Maximum
        }

        /// <summary>
        /// 层混合模式
        /// </summary>
        public enum LayerBlendMode
        {
            /// <summary>
            /// 正常
            /// </summary>
            Normal,

            /// <summary>
            /// 正片叠底
            /// </summary>
            Multiply,

            /// <summary>
            /// 滤色
            /// </summary>
            Screen,

            /// <summary>
            /// 叠加
            /// </summary>
            Overlay,

            /// <summary>
            /// 柔光
            /// </summary>
            SoftLight
        }

        #endregion

        #region 构造函数

        public LineCanvas()
        {
            InitializeComponent();

            _layers = new Dictionary<string, CanvasLayer>();
            _layerOrder = new List<string>();
            _layerCache = new Dictionary<string, Bitmap>();

            // 启用高性能渲染
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer, true);

            // 检测DPI设置
            DetectDpiSettings();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 缩放因子
        /// </summary>
        public float ZoomFactor
        {
            get => _zoomFactor;
            set
            {
                if (Math.Abs(_zoomFactor - value) > 0.001f)
                {
                    var oldZoom = _zoomFactor;
                    _zoomFactor = Math.Max(0.1f, Math.Min(10.0f, value));
                    InvalidateLayerCache();
                    OnZoomChanged(new ZoomChangedEventArgs(oldZoom, _zoomFactor));
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 平移偏移
        /// </summary>
        public PointF PanOffset
        {
            get => _panOffset;
            set
            {
                if (_panOffset != value)
                {
                    var oldOffset = _panOffset;
                    _panOffset = value;
                    InvalidateLayerCache();
                    OnPanChanged(new PanChangedEventArgs(oldOffset, _panOffset));
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 渲染质量
        /// </summary>
        public RenderQuality CurrentRenderQuality
        {
            get => _renderQuality;
            set
            {
                if (_renderQuality != value)
                {
                    var oldQuality = _renderQuality;
                    _renderQuality = value;
                    ApplyRenderQuality();
                    OnRenderQualityChanged(new RenderQualityChangedEventArgs(oldQuality, _renderQuality));
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 是否使用层缓存
        /// </summary>
        public bool UseLayerCache
        {
            get => _useLayerCache;
            set
            {
                if (_useLayerCache != value)
                {
                    _useLayerCache = value;
                    if (!_useLayerCache)
                    {
                        ClearLayerCache();
                    }
                }
            }
        }

        /// <summary>
        /// 画布边界
        /// </summary>
        public RectangleF CanvasBounds => new RectangleF(
            -_panOffset.X / _zoomFactor,
            -_panOffset.Y / _zoomFactor,
            Width / _zoomFactor,
            Height / _zoomFactor
        );

        /// <summary>
        /// 是否正在平移
        /// </summary>
        public bool IsPanning => _isPanning;

        /// <summary>
        /// 是否正在选择
        /// </summary>
        public bool IsSelecting => _isSelecting;

        /// <summary>
        /// 选择矩形
        /// </summary>
        public Rectangle SelectionRect => _selectionRect;

        /// <summary>
        /// 最后渲染时间
        /// </summary>
        public TimeSpan LastRenderTime => _renderTime;

        /// <summary>
        /// DPI缩放比例X
        /// </summary>
        public float DpiScaleX => _dpiScaleX;

        /// <summary>
        /// DPI缩放比例Y
        /// </summary>
        public float DpiScaleY => _dpiScaleY;

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加绘制层
        /// </summary>
        /// <param name="name">层名称</param>
        /// <param name="zIndex">Z索引</param>
        /// <returns>创建的层</returns>
        public CanvasLayer AddLayer(string name, int zIndex = 0)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("层名称不能为空", nameof(name));

            if (_layers.ContainsKey(name))
                throw new ArgumentException($"层 '{name}' 已存在", nameof(name));

            var layer = new CanvasLayer(name, zIndex);
            _layers[name] = layer;

            // 插入到正确的Z索引位置
            InsertLayerByZIndex(name, zIndex);

            InvalidateLayerCache();
            Invalidate();

            return layer;
        }

        /// <summary>
        /// 移除绘制层
        /// </summary>
        /// <param name="name">层名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveLayer(string name)
        {
            if (!_layers.ContainsKey(name))
                return false;

            _layers.Remove(name);
            _layerOrder.Remove(name);

            // 清理缓存
            if (_layerCache.ContainsKey(name))
            {
                _layerCache[name]?.Dispose();
                _layerCache.Remove(name);
            }

            Invalidate();
            return true;
        }

        /// <summary>
        /// 获取绘制层
        /// </summary>
        /// <param name="name">层名称</param>
        /// <returns>层，如果不存在返回null</returns>
        public CanvasLayer GetLayer(string name)
        {
            _layers.TryGetValue(name, out var layer);
            return layer;
        }

        /// <summary>
        /// 设置层Z索引
        /// </summary>
        /// <param name="name">层名称</param>
        /// <param name="zIndex">新的Z索引</param>
        public void SetLayerZIndex(string name, int zIndex)
        {
            if (!_layers.ContainsKey(name))
                return;

            var layer = _layers[name];
            if (layer.ZIndex != zIndex)
            {
                layer.ZIndex = zIndex;
                _layerOrder.Remove(name);
                InsertLayerByZIndex(name, zIndex);
                Invalidate();
            }
        }

        /// <summary>
        /// 清除所有层
        /// </summary>
        public void ClearLayers()
        {
            foreach (var cache in _layerCache.Values)
            {
                cache?.Dispose();
            }

            _layers.Clear();
            _layerOrder.Clear();
            _layerCache.Clear();
            Invalidate();
        }

        /// <summary>
        /// 缩放到指定矩形
        /// </summary>
        /// <param name="rect">目标矩形（画布坐标）</param>
        /// <param name="padding">填充边距</param>
        public void ZoomToRectangle(RectangleF rect, float padding = 10)
        {
            if (rect.IsEmpty)
                return;

            var scaleX = (Width - padding * 2) / rect.Width;
            var scaleY = (Height - padding * 2) / rect.Height;
            var newZoom = Math.Min(scaleX, scaleY);

            var centerX = rect.X + rect.Width / 2;
            var centerY = rect.Y + rect.Height / 2;

            ZoomFactor = newZoom;
            PanOffset = new PointF(
                Width / 2 - centerX * newZoom,
                Height / 2 - centerY * newZoom
            );
        }

        /// <summary>
        /// 适应视图到所有内容
        /// </summary>
        public void FitToContent()
        {
            if (_layers.Count == 0)
                return;

            var bounds = CalculateContentBounds();
            if (!bounds.IsEmpty)
            {
                ZoomToRectangle(bounds);
            }
        }

        /// <summary>
        /// 重置视图
        /// </summary>
        public void ResetView()
        {
            ZoomFactor = 1.0f;
            PanOffset = PointF.Empty;
        }

        /// <summary>
        /// 屏幕坐标转画布坐标
        /// </summary>
        /// <param name="screenPoint">屏幕坐标</param>
        /// <returns>画布坐标</returns>
        public PointF ScreenToCanvas(Point screenPoint)
        {
            return new PointF(
                (screenPoint.X - _panOffset.X) / _zoomFactor,
                (screenPoint.Y - _panOffset.Y) / _zoomFactor
            );
        }

        /// <summary>
        /// 画布坐标转屏幕坐标
        /// </summary>
        /// <param name="canvasPoint">画布坐标</param>
        /// <returns>屏幕坐标</returns>
        public PointF CanvasToScreen(PointF canvasPoint)
        {
            return new PointF(
                canvasPoint.X * _zoomFactor + _panOffset.X,
                canvasPoint.Y * _zoomFactor + _panOffset.Y
            );
        }

        /// <summary>
        /// 开始平移操作
        /// </summary>
        /// <param name="startPoint">起始点</param>
        public void BeginPan(Point startPoint)
        {
            _isPanning = true;
            _lastPanPoint = startPoint;
            Cursor = Cursors.Hand;
        }

        /// <summary>
        /// 更新平移操作
        /// </summary>
        /// <param name="currentPoint">当前点</param>
        public void UpdatePan(Point currentPoint)
        {
            if (!_isPanning)
                return;

            var delta = new PointF(
                currentPoint.X - _lastPanPoint.X,
                currentPoint.Y - _lastPanPoint.Y
            );

            PanOffset = new PointF(
                _panOffset.X + delta.X,
                _panOffset.Y + delta.Y
            );

            _lastPanPoint = currentPoint;
        }

        /// <summary>
        /// 结束平移操作
        /// </summary>
        public void EndPan()
        {
            _isPanning = false;
            Cursor = Cursors.Default;
        }

        /// <summary>
        /// 开始选择操作
        /// </summary>
        /// <param name="startPoint">起始点</param>
        public void BeginSelection(Point startPoint)
        {
            _isSelecting = true;
            _selectionRect = new Rectangle(startPoint, Size.Empty);
        }

        /// <summary>
        /// 更新选择操作
        /// </summary>
        /// <param name="currentPoint">当前点</param>
        public void UpdateSelection(Point currentPoint)
        {
            if (!_isSelecting)
                return;

            _selectionRect = new Rectangle(
                Math.Min(_selectionRect.X, currentPoint.X),
                Math.Min(_selectionRect.Y, currentPoint.Y),
                Math.Abs(currentPoint.X - _selectionRect.X),
                Math.Abs(currentPoint.Y - _selectionRect.Y)
            );

            Invalidate();
        }

        /// <summary>
        /// 结束选择操作
        /// </summary>
        /// <returns>选择矩形（画布坐标）</returns>
        public RectangleF EndSelection()
        {
            _isSelecting = false;
            var canvasRect = new RectangleF(
                ScreenToCanvas(_selectionRect.Location),
                new SizeF(
                    _selectionRect.Width / _zoomFactor,
                    _selectionRect.Height / _zoomFactor
                )
            );
            _selectionRect = Rectangle.Empty;
            Invalidate();
            return canvasRect;
        }

        /// <summary>
        /// 导出画布为图像
        /// </summary>
        /// <param name="bounds">导出边界（画布坐标）</param>
        /// <param name="dpi">DPI设置</param>
        /// <returns>导出的图像</returns>
        public Bitmap ExportToImage(RectangleF bounds, float dpi = 96)
        {
            var width = (int)(bounds.Width * dpi / 96);
            var height = (int)(bounds.Height * dpi / 96);
            var bitmap = new Bitmap(width, height);
            bitmap.SetResolution(dpi, dpi);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.ScaleTransform(dpi / 96f, dpi / 96f);
            graphics.TranslateTransform(-bounds.X, -bounds.Y);

            ApplyRenderQualityToGraphics(graphics);

            // 绘制所有层
            foreach (var layerName in _layerOrder)
            {
                if (_layers.TryGetValue(layerName, out var layer) && layer.Visible)
                {
                    RenderLayer(graphics, layer);
                }
            }

            return bitmap;
        }

        #endregion

        #region 重写方法

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var startTime = DateTime.Now;

            if (_backBuffer == null || _backBuffer.Size != Size)
            {
                CreateBackBuffer();
            }

            if (_needsRedraw)
            {
                DrawToBackBuffer();
                _needsRedraw = false;
            }

            // 绘制后备缓冲区内容
            e.Graphics.DrawImageUnscaled(_backBuffer, 0, 0);

            // 绘制选择矩形
            if (_isSelecting)
            {
                DrawSelectionRect(e.Graphics);
            }

            _renderTime = DateTime.Now - startTime;
            _lastRenderTime = DateTime.Now;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CreateBackBuffer();
            InvalidateLayerCache();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Alt))
            {
                BeginPan(e.Location);
            }
            else if (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Shift)
            {
                BeginSelection(e.Location);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isPanning)
            {
                UpdatePan(e.Location);
            }
            else if (_isSelecting)
            {
                UpdateSelection(e.Location);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isPanning)
            {
                EndPan();
            }
            else if (_isSelecting)
            {
                EndSelection();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (Control.ModifierKeys == Keys.Control)
            {
                var scaleFactor = e.Delta > 0 ? 1.1f : 0.9f;
                var newZoom = _zoomFactor * scaleFactor;

                // 以鼠标位置为中心缩放
                var mouseCanvas = ScreenToCanvas(e.Location);
                ZoomFactor = newZoom;
                var newScreenPos = CanvasToScreen(mouseCanvas);

                PanOffset = new PointF(
                    _panOffset.X + (e.Location.X - newScreenPos.X),
                    _panOffset.Y + (e.Location.Y - newScreenPos.Y)
                );
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _backBuffer?.Dispose();
                _backBufferGraphics?.Dispose();
                ClearLayerCache();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region 私有方法

        private void InitializeComponent()
        {
            BackColor = Color.White;
            ResizeRedraw = true;
        }

        private void DetectDpiSettings()
        {
            using var graphics = CreateGraphics();
            _dpiScaleX = graphics.DpiX / 96f;
            _dpiScaleY = graphics.DpiY / 96f;
        }

        private void CreateBackBuffer()
        {
            _backBuffer?.Dispose();
            _backBufferGraphics?.Dispose();

            _backBuffer = new Bitmap(Width, Height);
            _backBufferGraphics = Graphics.FromImage(_backBuffer);
            _needsRedraw = true;
        }

        private void DrawToBackBuffer()
        {
            var startTime = DateTime.Now;

            // 清除背景
            _backBufferGraphics.Clear(BackColor);

            // 设置变换
            _backBufferGraphics.ResetTransform();
            _backBufferGraphics.TranslateTransform(_panOffset.X, _panOffset.Y);
            _backBufferGraphics.ScaleTransform(_zoomFactor, _zoomFactor);

            // 应用渲染质量设置
            ApplyRenderQualityToGraphics(_backBufferGraphics);

            // 触发画布绘制事件
            var canvasArgs = new CanvasPaintEventArgs(_backBufferGraphics, CanvasBounds);
            OnCanvasPaint(canvasArgs);

            // 绘制所有层
            foreach (var layerName in _layerOrder)
            {
                if (_layers.TryGetValue(layerName, out var layer) && layer.Visible)
                {
                    if (_useLayerCache && layer.CacheEnabled)
                    {
                        RenderLayerFromCache(_backBufferGraphics, layer);
                    }
                    else
                    {
                        RenderLayer(_backBufferGraphics, layer);
                    }
                }
            }

            _renderTime = DateTime.Now - startTime;
        }

        private void ApplyRenderQuality()
        {
            switch (_renderQuality)
            {
                case RenderQuality.Low:
                    _smoothingMode = SmoothingMode.HighSpeed;
                    _textRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                    _interpolationMode = InterpolationMode.Low;
                    _pixelOffsetMode = PixelOffsetMode.None;
                    _compositingQuality = CompositingQuality.HighSpeed;
                    break;

                case RenderQuality.Medium:
                    _smoothingMode = SmoothingMode.AntiAlias;
                    _textRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    _interpolationMode = InterpolationMode.Bilinear;
                    _pixelOffsetMode = PixelOffsetMode.Half;
                    _compositingQuality = CompositingQuality.AssumeLinear;
                    break;

                case RenderQuality.High:
                    _smoothingMode = SmoothingMode.AntiAlias;
                    _textRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    _interpolationMode = InterpolationMode.HighQualityBicubic;
                    _pixelOffsetMode = PixelOffsetMode.HighQuality;
                    _compositingQuality = CompositingQuality.HighQuality;
                    break;

                case RenderQuality.Maximum:
                    _smoothingMode = SmoothingMode.AntiAlias;
                    _textRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    _interpolationMode = InterpolationMode.HighQualityBicubic;
                    _pixelOffsetMode = PixelOffsetMode.HighQuality;
                    _compositingQuality = CompositingQuality.HighQuality;
                    break;
            }
        }

        private void ApplyRenderQualityToGraphics(Graphics graphics)
        {
            graphics.SmoothingMode = _smoothingMode;
            graphics.TextRenderingHint = _textRenderingHint;
            graphics.InterpolationMode = _interpolationMode;
            graphics.PixelOffsetMode = _pixelOffsetMode;
            graphics.CompositingQuality = _compositingQuality;
        }

        private void InsertLayerByZIndex(string name, int zIndex)
        {
            var insertIndex = 0;
            for (int i = 0; i < _layerOrder.Count; i++)
            {
                if (_layers[_layerOrder[i]].ZIndex <= zIndex)
                {
                    insertIndex = i + 1;
                }
                else
                {
                    break;
                }
            }
            _layerOrder.Insert(insertIndex, name);
        }

        private void RenderLayer(Graphics graphics, CanvasLayer layer)
        {
            if (!layer.Visible || layer.IsEmpty)
                return;

            graphics.Save();

            // 应用层变换
            if (layer.Transform != null)
            {
                graphics.MultiplyTransform(layer.Transform);
            }

            // 设置层透明度
            if (layer.Opacity < 1.0f)
            {
                var oldCompositingMode = graphics.CompositingMode;
                graphics.CompositingMode = CompositingMode.SourceOver;

                var colorMatrix = new ColorMatrix { Matrix33 = layer.Opacity };
                using var attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);

                // 创建临时位图应用透明度
                var tempRect = layer.Bounds;
                using var tempBitmap = new Bitmap((int)tempRect.Width, (int)tempRect.Height);
                using var tempGraphics = Graphics.FromImage(tempBitmap);

                tempGraphics.TranslateTransform(-tempRect.X, -tempRect.Y);

                // 重绘层内容到临时位图
                layer.Render(tempGraphics);

                // 应用透明度绘制
                graphics.DrawImage(tempBitmap, tempRect, 0, 0, tempBitmap.Width, tempBitmap.Height,
                    GraphicsUnit.Pixel, attributes);

                graphics.CompositingMode = oldCompositingMode;
            }
            else
            {
                layer.Render(graphics);
            }

            graphics.Restore();
        }

        private void RenderLayerFromCache(Graphics graphics, CanvasLayer layer)
        {
            var cacheKey = layer.Name;

            if (!_layerCache.ContainsKey(cacheKey) || _layerCache[cacheKey] == null)
            {
                RebuildLayerCache(layer);
            }

            var cachedBitmap = _layerCache[cacheKey];
            if (cachedBitmap != null)
            {
                graphics.DrawImageUnscaled(cachedBitmap, 0, 0);
            }
        }

        private void RebuildLayerCache(CanvasLayer layer)
        {
            var cacheKey = layer.Name;

            _layerCache[cacheKey]?.Dispose();

            var bounds = layer.Bounds;
            if (bounds.IsEmpty)
                return;

            var cacheBitmap = new Bitmap((int)bounds.Width, (int)bounds.Height);
            using var cacheGraphics = Graphics.FromImage(cacheBitmap);

            ApplyRenderQualityToGraphics(cacheGraphics);
            cacheGraphics.TranslateTransform(-bounds.X, -bounds.Y);

            layer.Render(cacheGraphics);

            _layerCache[cacheKey] = cacheBitmap;
        }

        private void InvalidateLayerCache()
        {
            if (!_useLayerCache)
                return;

            foreach (var layer in _layers.Values.Where(l => l.CacheEnabled))
            {
                RebuildLayerCache(layer);
            }
        }

        private void ClearLayerCache()
        {
            foreach (var bitmap in _layerCache.Values)
            {
                bitmap?.Dispose();
            }
            _layerCache.Clear();
        }

        private RectangleF CalculateContentBounds()
        {
            var bounds = RectangleF.Empty;

            foreach (var layer in _layers.Values.Where(l => l.Visible))
            {
                var layerBounds = layer.Bounds;
                if (!layerBounds.IsEmpty)
                {
                    if (bounds.IsEmpty)
                    {
                        bounds = layerBounds;
                    }
                    else
                    {
                        bounds = RectangleF.Union(bounds, layerBounds);
                    }
                }
            }

            return bounds;
        }

        private void DrawSelectionRect(Graphics graphics)
        {
            if (_selectionRect.IsEmpty)
                return;

            using var pen = new Pen(Color.Blue, 1) { DashPattern = new float[] { 5, 3 } };
            using var brush = new SolidBrush(Color.FromArgb(50, Color.LightBlue));

            graphics.DrawRectangle(pen, _selectionRect);
            graphics.FillRectangle(brush, _selectionRect);
        }

        #endregion

        #region 事件触发器

        protected virtual void OnCanvasPaint(CanvasPaintEventArgs e)
        {
            CanvasPaint?.Invoke(this, e);
        }

        protected virtual void OnZoomChanged(ZoomChangedEventArgs e)
        {
            ZoomChanged?.Invoke(this, e);
        }

        protected virtual void OnPanChanged(PanChangedEventArgs e)
        {
            PanChanged?.Invoke(this, e);
        }

        protected virtual void OnRenderQualityChanged(RenderQualityChangedEventArgs e)
        {
            RenderQualityChanged?.Invoke(this, e);
        }

        #endregion
    }

    #region 辅助类

    /// <summary>
    /// 画布层
    /// </summary>
    public class CanvasLayer : IDisposable
    {
        private readonly List<CanvasElement> _elements;
        private bool _visible = true;
        private float _opacity = 1.0f;
        private bool _cacheEnabled = false;
        private Matrix _transform;

        public string Name { get; }
        public int ZIndex { get; set; }

        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        public float Opacity
        {
            get => _opacity;
            set => _opacity = Math.Max(0f, Math.Min(1f, value));
        }

        public bool CacheEnabled
        {
            get => _cacheEnabled;
            set => _cacheEnabled = value;
        }

        public Matrix Transform
        {
            get => _transform;
            set
            {
                _transform?.Dispose();
                _transform = value?.Clone();
            }
        }

        public bool IsEmpty => _elements.Count == 0;

        public RectangleF Bounds
        {
            get
            {
                var bounds = RectangleF.Empty;
                foreach (var element in _elements)
                {
                    if (bounds.IsEmpty)
                        bounds = element.Bounds;
                    else
                        bounds = RectangleF.Union(bounds, element.Bounds);
                }
                return bounds;
            }
        }

        public CanvasLayer(string name, int zIndex = 0)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ZIndex = zIndex;
            _elements = new List<CanvasElement>();
        }

        public void AddElement(CanvasElement element)
        {
            if (element != null)
            {
                _elements.Add(element);
            }
        }

        public void RemoveElement(CanvasElement element)
        {
            _elements.Remove(element);
        }

        public void ClearElements()
        {
            _elements.Clear();
        }

        public void Render(Graphics graphics)
        {
            foreach (var element in _elements)
            {
                element.Render(graphics);
            }
        }

        public void Dispose()
        {
            _transform?.Dispose();
            foreach (var element in _elements)
            {
                element?.Dispose();
            }
            _elements.Clear();
        }
    }

    /// <summary>
    /// 画布元素（抽象基类）
    /// </summary>
    public abstract class CanvasElement : IDisposable
    {
        public abstract RectangleF Bounds { get; }
        public abstract void Render(Graphics graphics);
        public abstract void Dispose();
    }

    /// <summary>
    /// 线条元素
    /// </summary>
    public class LineElement : CanvasElement
    {
        public PointF Start { get; set; }
        public PointF End { get; set; }
        public Pen Pen { get; set; }

        public override RectangleF Bounds => new RectangleF(
            Math.Min(Start.X, End.X),
            Math.Min(Start.Y, End.Y),
            Math.Abs(End.X - Start.X),
            Math.Abs(End.Y - Start.Y)
        );

        public LineElement(PointF start, PointF end, Pen pen)
        {
            Start = start;
            End = end;
            Pen = pen;
        }

        public override void Render(Graphics graphics)
        {
            if (Pen != null)
            {
                graphics.DrawLine(Pen, Start, End);
            }
        }

        public override void Dispose()
        {
            Pen?.Dispose();
        }
    }

    /// <summary>
    /// 文本元素
    /// </summary>
    public class TextElement : CanvasElement
    {
        public string Text { get; set; }
        public PointF Position { get; set; }
        public Font Font { get; set; }
        public Brush Brush { get; set; }
        public StringFormat Format { get; set; }

        public override RectangleF Bounds
        {
            get
            {
                if (string.IsNullOrEmpty(Text) || Font == null)
                    return RectangleF.Empty;

                using var graphics = Graphics.FromHwnd(IntPtr.Zero);
                var size = graphics.MeasureString(Text, Font, new PointF(Position.X, Position.Y), Format);
                return new RectangleF(Position, size);
            }
        }

        public TextElement(string text, PointF position, Font font, Brush brush, StringFormat format = null)
        {
            Text = text;
            Position = position;
            Font = font;
            Brush = brush;
            Format = format ?? new StringFormat();
        }

        public override void Render(Graphics graphics)
        {
            if (!string.IsNullOrEmpty(Text) && Font != null && Brush != null)
            {
                graphics.DrawString(Text, Font, Brush, Position, Format);
            }
        }

        public override void Dispose()
        {
            Font?.Dispose();
            Brush?.Dispose();
            Format?.Dispose();
        }
    }

    #endregion

    #region 事件参数类

    /// <summary>
    /// 画布绘制事件参数
    /// </summary>
    public class CanvasPaintEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public RectangleF CanvasBounds { get; }

        public CanvasPaintEventArgs(Graphics graphics, RectangleF canvasBounds)
        {
            Graphics = graphics;
            CanvasBounds = canvasBounds;
        }
    }

    /// <summary>
    /// 缩放改变事件参数
    /// </summary>
    public class ZoomChangedEventArgs : EventArgs
    {
        public float OldZoom { get; }
        public float NewZoom { get; }

        public ZoomChangedEventArgs(float oldZoom, float newZoom)
        {
            OldZoom = oldZoom;
            NewZoom = newZoom;
        }
    }

    /// <summary>
    /// 平移改变事件参数
    /// </summary>
    public class PanChangedEventArgs : EventArgs
    {
        public PointF OldOffset { get; }
        public PointF NewOffset { get; }

        public PanChangedEventArgs(PointF oldOffset, PointF newOffset)
        {
            OldOffset = oldOffset;
            NewOffset = newOffset;
        }
    }

    /// <summary>
    /// 渲染质量改变事件参数
    /// </summary>
    public class RenderQualityChangedEventArgs : EventArgs
    {
        public LineCanvas.RenderQuality OldQuality { get; }
        public LineCanvas.RenderQuality NewQuality { get; }

        public RenderQualityChangedEventArgs(LineCanvas.RenderQuality oldQuality, LineCanvas.RenderQuality newQuality)
        {
            OldQuality = oldQuality;
            NewQuality = newQuality;
        }
    }

    #endregion
}