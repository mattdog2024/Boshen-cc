using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BoshenCC.Core.Utils;

namespace BoshenCC.WinForms.Utils
{
    /// <summary>
    /// 响应式布局工具类 - 实现响应式设计和自适应布局
    /// Issue #6 Stream A: UI优化和响应式设计
    /// </summary>
    public static class ResponsiveLayout
    {
        #region 布局规则定义

        /// <summary>
        /// 响应式布局规则
        /// </summary>
        public class LayoutRule
        {
            public Size MinSize { get; set; }
            public Size MaxSize { get; set; }
            public Action<Control> ApplyRule { get; set; }
            public string Name { get; set; }

            public LayoutRule(string name, Size minSize, Size maxSize, Action<Control> applyRule)
            {
                Name = name;
                MinSize = minSize;
                MaxSize = maxSize;
                ApplyRule = applyRule;
            }
        }

        #endregion

        #region 私有字段

        private static Dictionary<Control, List<LayoutRule>> _layoutRules;
        private static Dictionary<Control, Size> _originalSizes;
        private static Dictionary<Control, Point> _originalPositions;
        private static bool _isInitialized = false;

        // 预定义断点
        public static readonly Size SmallScreen = new Size(800, 600);
        public static readonly Size MediumScreen = new Size(1200, 800);
        public static readonly Size LargeScreen = new Size(1600, 1000);
        public static readonly Size ExtraLargeScreen = new Size(2000, 1200);

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化响应式布局系统
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (_isInitialized)
                    return;

                _layoutRules = new Dictionary<Control, List<LayoutRule>>();
                _originalSizes = new Dictionary<Control, Size>();
                _originalPositions = new Dictionary<Control, Point>();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "初始化失败");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 为控件注册响应式布局规则
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="rule">布局规则</param>
        public static void RegisterLayoutRule(Control control, LayoutRule rule)
        {
            try
            {
                if (!_isInitialized)
                    Initialize();

                if (control == null || rule == null)
                    return;

                if (!_layoutRules.ContainsKey(control))
                {
                    _layoutRules[control] = new List<LayoutRule>();
                    SaveOriginalState(control);
                }

                _layoutRules[control].Add(rule);

                // 添加大小改变事件处理
                if (!control.HasResizeEvents())
                {
                    control.Resize += OnControlResize;
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "注册布局规则失败");
            }
        }

        /// <summary>
        /// 为控件添加多个响应式布局规则
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="rules">布局规则数组</param>
        public static void RegisterLayoutRules(Control control, params LayoutRule[] rules)
        {
            try
            {
                if (rules == null || rules.Length == 0)
                    return;

                foreach (var rule in rules)
                {
                    RegisterLayoutRule(control, rule);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "注册多个布局规则失败");
            }
        }

        /// <summary>
        /// 为窗体设置响应式布局
        /// </summary>
        /// <param name="form">目标窗体</param>
        public static void MakeResponsive(Form form)
        {
            try
            {
                if (form == null)
                    return;

                if (!_isInitialized)
                    Initialize();

                // 应用DPI缩放
                DPIHelper.ApplyDpiScaling(form);

                // 保存原始状态
                SaveOriginalState(form);

                // 添加默认的响应式规则
                AddDefaultResponsiveRules(form);

                // 添加大小改变事件
                form.Resize += OnFormResize;
                form.Load += OnFormLoad;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "设置响应式窗体失败");
            }
        }

        /// <summary>
        /// 为面板设置响应式布局
        /// </summary>
        /// <param name="panel">目标面板</param>
        public static void MakeResponsive(Panel panel)
        {
            try
            {
                if (panel == null)
                    return;

                if (!_isInitialized)
                    Initialize();

                SaveOriginalState(panel);
                AddDefaultPanelRules(panel);
                panel.Resize += OnControlResize;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "设置响应式面板失败");
            }
        }

        /// <summary>
        /// 为SplitContainer设置响应式布局
        /// </summary>
        /// <param name="splitContainer">目标SplitContainer</param>
        public static void MakeResponsive(SplitContainer splitContainer)
        {
            try
            {
                if (splitContainer == null)
                    return;

                if (!_isInitialized)
                    Initialize();

                SaveOriginalState(splitContainer);
                AddDefaultSplitContainerRules(splitContainer);
                splitContainer.Resize += OnControlResize;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "设置响应式SplitContainer失败");
            }
        }

        /// <summary>
        /// 手动触发布局更新
        /// </summary>
        /// <param name="control">目标控件</param>
        public static void UpdateLayout(Control control)
        {
            try
            {
                if (control == null)
                    return;

                var currentSize = control.Size;
                ApplyResponsiveRules(control, currentSize);
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "更新布局失败");
            }
        }

        /// <summary>
        /// 检查控件是否为小屏幕模式
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <returns>是否为小屏幕</returns>
        public static bool IsSmallScreen(Control control)
        {
            return control != null && IsInSizeRange(control.Size, Size.Empty, SmallScreen);
        }

        /// <summary>
        /// 检查控件是否为中等屏幕模式
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <returns>是否为中等屏幕</returns>
        public static bool IsMediumScreen(Control control)
        {
            return control != null && IsInSizeRange(control.Size, SmallScreen, MediumScreen);
        }

        /// <summary>
        /// 检查控件是否为大屏幕模式
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <returns>是否为大屏幕</returns>
        public static bool IsLargeScreen(Control control)
        {
            return control != null && IsInSizeRange(control.Size, MediumScreen, LargeScreen);
        }

        /// <summary>
        /// 检查控件是否为超大屏幕模式
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <returns>是否为超大屏幕</returns>
        public static bool IsExtraLargeScreen(Control control)
        {
            return control != null && IsInSizeRange(control.Size, LargeScreen, Size.Empty);
        }

        /// <summary>
        /// 获取屏幕尺寸类型
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <returns>屏幕尺寸类型</returns>
        public static ScreenSizeType GetScreenSizeType(Control control)
        {
            if (control == null)
                return ScreenSizeType.Unknown;

            if (IsSmallScreen(control))
                return ScreenSizeType.Small;
            else if (IsMediumScreen(control))
                return ScreenSizeType.Medium;
            else if (IsLargeScreen(control))
                return ScreenSizeType.Large;
            else if (IsExtraLargeScreen(control))
                return ScreenSizeType.ExtraLarge;
            else
                return ScreenSizeType.Unknown;
        }

        #endregion

        #region 默认规则

        /// <summary>
        /// 添加默认的窗体响应式规则
        /// </summary>
        private static void AddDefaultResponsiveRules(Form form)
        {
            try
            {
                // 小屏幕规则
                RegisterLayoutRule(form, new LayoutRule(
                    "SmallScreen",
                    Size.Empty,
                    SmallScreen,
                    control => {
                        var frm = control as Form;
                        if (frm != null)
                        {
                            // 隐藏部分不重要的控件
                            HideNonEssentialControls(frm);
                            // 调整字体大小
                            AdjustFontSize(frm, 0.9f);
                        }
                    }
                ));

                // 中等屏幕规则
                RegisterLayoutRule(form, new LayoutRule(
                    "MediumScreen",
                    SmallScreen,
                    MediumScreen,
                    control => {
                        var frm = control as Form;
                        if (frm != null)
                        {
                            // 显示所有控件
                            ShowAllControls(frm);
                            // 正常字体大小
                            AdjustFontSize(frm, 1.0f);
                        }
                    }
                ));

                // 大屏幕规则
                RegisterLayoutRule(form, new LayoutRule(
                    "LargeScreen",
                    MediumScreen,
                    LargeScreen,
                    control => {
                        var frm = control as Form;
                        if (frm != null)
                        {
                            // 增大字体和间距
                            AdjustFontSize(frm, 1.1f);
                            AdjustSpacing(frm, 1.2f);
                        }
                    }
                ));

                // 超大屏幕规则
                RegisterLayoutRule(form, new LayoutRule(
                    "ExtraLargeScreen",
                    LargeScreen,
                    Size.Empty,
                    control => {
                        var frm = control as Form;
                        if (frm != null)
                        {
                            // 更大的字体和间距
                            AdjustFontSize(frm, 1.2f);
                            AdjustSpacing(frm, 1.4f);
                        }
                    }
                ));
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "添加默认窗体规则失败");
            }
        }

        /// <summary>
        /// 添加默认的面板响应式规则
        /// </summary>
        private static void AddDefaultPanelRules(Panel panel)
        {
            try
            {
                RegisterLayoutRule(panel, new LayoutRule(
                    "PanelResponsive",
                    Size.Empty,
                    Size.Empty,
                    control => {
                        var pnl = control as Panel;
                        if (pnl != null)
                        {
                            // 自动调整子控件布局
                            AutoResizeChildControls(pnl);
                        }
                    }
                ));
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "添加默认面板规则失败");
            }
        }

        /// <summary>
        /// 添加默认的SplitContainer响应式规则
        /// </summary>
        private static void AddDefaultSplitContainerRules(SplitContainer splitContainer)
        {
            try
            {
                RegisterLayoutRule(splitContainer, new LayoutRule(
                    "SplitContainerResponsive",
                    Size.Empty,
                    SmallScreen,
                    control => {
                        var sc = control as SplitContainer;
                        if (sc != null)
                        {
                            // 小屏幕时垂直分割
                            if (sc.Orientation == Orientation.Horizontal && IsSmallScreen(sc))
                            {
                                sc.Orientation = Orientation.Vertical;
                            }
                        }
                    }
                ));
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "添加默认SplitContainer规则失败");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 保存控件原始状态
        /// </summary>
        private static void SaveOriginalState(Control control)
        {
            try
            {
                if (!_originalSizes.ContainsKey(control))
                {
                    _originalSizes[control] = control.Size;
                    _originalPositions[control] = control.Location;
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "保存原始状态失败");
            }
        }

        /// <summary>
        /// 应用响应式规则
        /// </summary>
        private static void ApplyResponsiveRules(Control control, Size currentSize)
        {
            try
            {
                if (!_layoutRules.ContainsKey(control))
                    return;

                var rules = _layoutRules[control];
                foreach (var rule in rules)
                {
                    if (IsInSizeRange(currentSize, rule.MinSize, rule.MaxSize))
                    {
                        rule.ApplyRule?.Invoke(control);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "应用响应式规则失败");
            }
        }

        /// <summary>
        /// 检查尺寸是否在指定范围内
        /// </summary>
        private static bool IsInSizeRange(Size currentSize, Size minSize, Size maxSize)
        {
            // 最小尺寸检查
            if (minSize != Size.Empty)
            {
                if (currentSize.Width < minSize.Width || currentSize.Height < minSize.Height)
                    return false;
            }

            // 最大尺寸检查
            if (maxSize != Size.Empty)
            {
                if (currentSize.Width > maxSize.Width || currentSize.Height > maxSize.Height)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 隐藏非必要控件
        /// </summary>
        private static void HideNonEssentialControls(Control parent)
        {
            try
            {
                foreach (Control control in parent.Controls)
                {
                    // 根据控件类型或Tag判断是否隐藏
                    if (ShouldHideOnSmallScreen(control))
                    {
                        control.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "隐藏非必要控件失败");
            }
        }

        /// <summary>
        /// 显示所有控件
        /// </summary>
        private static void ShowAllControls(Control parent)
        {
            try
            {
                foreach (Control control in parent.Controls)
                {
                    control.Visible = true;
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "显示所有控件失败");
            }
        }

        /// <summary>
        /// 判断是否应该在小屏幕时隐藏
        /// </summary>
        private static bool ShouldHideOnSmallScreen(Control control)
        {
            // 可以根据控件的Tag、Name或类型来判断
            return control.Tag?.ToString() == "optional" ||
                   control.Name.Contains("Optional") ||
                   control is Label && string.IsNullOrWhiteSpace(control.Text);
        }

        /// <summary>
        /// 调整字体大小
        /// </summary>
        private static void AdjustFontSize(Control parent, float scaleFactor)
        {
            try
            {
                if (parent.Font == null)
                    return;

                var originalSize = parent.Font.Size;
                var newSize = originalSize * scaleFactor;
                var newFont = new Font(parent.Font.FontFamily, newSize, parent.Font.Style);
                parent.Font = newFont;

                // 递归调整子控件字体
                foreach (Control child in parent.Controls)
                {
                    AdjustFontSize(child, scaleFactor);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "调整字体大小失败");
            }
        }

        /// <summary>
        /// 调整间距
        /// </summary>
        private static void AdjustSpacing(Control parent, float scaleFactor)
        {
            try
            {
                // 调整Margin和Padding
                if (parent.Margin != Padding.Empty)
                {
                    parent.Margin = ScalePadding(parent.Margin, scaleFactor);
                }

                if (parent.Padding != Padding.Empty)
                {
                    parent.Padding = ScalePadding(parent.Padding, scaleFactor);
                }

                // 递归调整子控件间距
                foreach (Control child in parent.Controls)
                {
                    AdjustSpacing(child, scaleFactor);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "调整间距失败");
            }
        }

        /// <summary>
        /// 缩放边距
        /// </summary>
        private static Padding ScalePadding(Padding padding, float scaleFactor)
        {
            return new Padding(
                (int)(padding.Left * scaleFactor),
                (int)(padding.Top * scaleFactor),
                (int)(padding.Right * scaleFactor),
                (int)(padding.Bottom * scaleFactor)
            );
        }

        /// <summary>
        /// 自动调整子控件大小
        /// </summary>
        private static void AutoResizeChildControls(Control parent)
        {
            try
            {
                // 这里可以实现各种自动布局逻辑
                // 例如：流式布局、网格布局等
                foreach (Control child in parent.Controls)
                {
                    if (child.Dock == DockStyle.None)
                    {
                        // 按比例调整大小
                        var parentSize = parent.Size;
                        var childSize = child.Size;

                        // 示例：保持宽高比的同时适应容器
                        var scaleX = (float)parentSize.Width / _originalSizes[parent].Width;
                        var scaleY = (float)parentSize.Height / _originalSizes[parent].Height;
                        var scale = Math.Min(scaleX, scaleY);

                        child.Size = new Size(
                            (int)(childSize.Width * scale),
                            (int)(childSize.Height * scale)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "自动调整子控件失败");
            }
        }

        /// <summary>
        /// 检查控件是否已有Resize事件
        /// </summary>
        private static bool HasResizeEvents(this Control control)
        {
            try
            {
                return control.GetResizeEvents().Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取控件的Resize事件
        /// </summary>
        private static System.Reflection.EventInfo GetResizeEvents(this Control control)
        {
            return control.GetType().GetEvent("Resize");
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 窗体大小改变事件处理
        /// </summary>
        private static void OnFormResize(object sender, EventArgs e)
        {
            try
            {
                if (sender is Form form)
                {
                    UpdateLayout(form);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "窗体大小改变处理失败");
            }
        }

        /// <summary>
        /// 控件大小改变事件处理
        /// </summary>
        private static void OnControlResize(object sender, EventArgs e)
        {
            try
            {
                if (sender is Control control)
                {
                    UpdateLayout(control);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "控件大小改变处理失败");
            }
        }

        /// <summary>
        /// 窗体加载事件处理
        /// </summary>
        private static void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
                if (sender is Form form)
                {
                    // 窗体加载时立即应用布局
                    UpdateLayout(form);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "窗体加载处理失败");
            }
        }

        #endregion

        #region 清理和重置

        /// <summary>
        /// 清除控件的所有响应式规则
        /// </summary>
        /// <param name="control">目标控件</param>
        public static void ClearLayoutRules(Control control)
        {
            try
            {
                if (_layoutRules.ContainsKey(control))
                {
                    _layoutRules.Remove(control);
                }

                if (_originalSizes.ContainsKey(control))
                {
                    _originalSizes.Remove(control);
                }

                if (_originalPositions.ContainsKey(control))
                {
                    _originalPositions.Remove(control);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "清除布局规则失败");
            }
        }

        /// <summary>
        /// 重置所有响应式布局设置
        /// </summary>
        public static void Reset()
        {
            try
            {
                _layoutRules?.Clear();
                _originalSizes?.Clear();
                _originalPositions?.Clear();
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "响应式布局", "重置失败");
            }
        }

        #endregion
    }

    #region 枚举定义

    /// <summary>
    /// 屏幕尺寸类型
    /// </summary>
    public enum ScreenSizeType
    {
        Unknown,
        Small,
        Medium,
        Large,
        ExtraLarge
    }

    #endregion
}