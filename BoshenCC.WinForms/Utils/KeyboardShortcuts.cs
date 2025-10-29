using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BoshenCC.Core;

namespace BoshenCC.WinForms.Utils
{
    /// <summary>
    /// 快捷键管理器
    /// 提供完整的键盘快捷键系统，支持全局快捷键、上下文相关快捷键和自定义快捷键
    /// </summary>
    public class KeyboardShortcuts : IDisposable
    {
        #region 私有字段

        private readonly Dictionary<Keys, ShortcutAction> _globalShortcuts;
        private readonly Dictionary<string, Dictionary<Keys, ShortcutAction>> _contextualShortcuts;
        private readonly List<Keys> _pressedKeys;
        private string _currentContext;
        private bool _disposed;
        private readonly object _lockObject = new object();

        #endregion

        #region 事件

        /// <summary>
        /// 快捷键执行事件
        /// </summary>
        public event EventHandler<ShortcutExecutedEventArgs> ShortcutExecuted;

        /// <summary>
        /// 快捷键冲突事件
        /// </summary>
        public event EventHandler<ShortcutConflictEventArgs> ShortcutConflict;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化KeyboardShortcuts类
        /// </summary>
        public KeyboardShortcuts()
        {
            _globalShortcuts = new Dictionary<Keys, ShortcutAction>();
            _contextualShortcuts = new Dictionary<string, Dictionary<Keys, ShortcutAction>>();
            _pressedKeys = new List<Keys>();
            _currentContext = "Global";

            InitializeDefaultShortcuts();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 注册全局快捷键
        /// </summary>
        /// <param name="key">快捷键组合</param>
        /// <param name="action">快捷键动作</param>
        /// <param name="description">描述</param>
        public void RegisterGlobalShortcut(Keys key, Action action, string description = "")
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (_lockObject)
            {
                if (_globalShortcuts.ContainsKey(key))
                {
                    OnShortcutConflict(new ShortcutConflictEventArgs(key, _globalShortcuts[key].Description, description));
                    return;
                }

                _globalShortcuts[key] = new ShortcutAction(action, description, ShortcutScope.Global);
            }
        }

        /// <summary>
        /// 注册上下文快捷键
        /// </summary>
        /// <param name="context">上下文名称</param>
        /// <param name="key">快捷键组合</param>
        /// <param name="action">快捷键动作</param>
        /// <param name="description">描述</param>
        public void RegisterContextualShortcut(string context, Keys key, Action action, string description = "")
        {
            if (string.IsNullOrEmpty(context))
                throw new ArgumentException("Context cannot be null or empty", nameof(context));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (_lockObject)
            {
                if (!_contextualShortcuts.ContainsKey(context))
                {
                    _contextualShortcuts[context] = new Dictionary<Keys, ShortcutAction>();
                }

                var contextShortcuts = _contextualShortcuts[context];
                if (contextShortcuts.ContainsKey(key))
                {
                    OnShortcutConflict(new ShortcutConflictEventArgs(key, contextShortcuts[key].Description, description));
                    return;
                }

                contextShortcuts[key] = new ShortcutAction(action, description, ShortcutScope.Contextual);
            }
        }

        /// <summary>
        /// 取消注册快捷键
        /// </summary>
        /// <param name="key">快捷键组合</param>
        /// <param name="context">上下文，null表示全局</param>
        public void UnregisterShortcut(Keys key, string context = null)
        {
            lock (_lockObject)
            {
                if (string.IsNullOrEmpty(context))
                {
                    _globalShortcuts.Remove(key);
                }
                else if (_contextualShortcuts.ContainsKey(context))
                {
                    _contextualShortcuts[context].Remove(key);
                }
            }
        }

        /// <summary>
        /// 设置当前上下文
        /// </summary>
        /// <param name="context">上下文名称</param>
        public void SetContext(string context)
        {
            if (string.IsNullOrEmpty(context))
                context = "Global";

            lock (_lockObject)
            {
                _currentContext = context;
            }
        }

        /// <summary>
        /// 处理键盘按下事件
        /// </summary>
        /// <param name="keyData">键盘数据</param>
        /// <returns>是否处理了该快捷键</returns>
        public bool ProcessKeyDown(Keys keyData)
        {
            lock (_lockObject)
            {
                if (_pressedKeys.Contains(keyData))
                    return false;

                _pressedKeys.Add(keyData);
                var combination = GetCurrentKeyCombination();

                // 优先检查当前上下文的快捷键
                if (_contextualShortcuts.ContainsKey(_currentContext))
                {
                    var contextShortcuts = _contextualShortcuts[_currentContext];
                    if (contextShortcuts.TryGetValue(combination, out var contextAction))
                    {
                        ExecuteShortcut(contextAction, combination);
                        return true;
                    }
                }

                // 检查全局快捷键
                if (_globalShortcuts.TryGetValue(combination, out var globalAction))
                {
                    ExecuteShortcut(globalAction, combination);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 处理键盘释放事件
        /// </summary>
        /// <param name="keyData">键盘数据</param>
        public void ProcessKeyUp(Keys keyData)
        {
            lock (_lockObject)
            {
                _pressedKeys.Remove(keyData);
            }
        }

        /// <summary>
        /// 获取所有快捷键列表
        /// </summary>
        /// <returns>快捷键信息列表</returns>
        public List<ShortcutInfo> GetAllShortcuts()
        {
            var shortcuts = new List<ShortcutInfo>();

            lock (_lockObject)
            {
                // 添加全局快捷键
                foreach (var kvp in _globalShortcuts)
                {
                    shortcuts.Add(new ShortcutInfo(kvp.Key, kvp.Value.Description, "Global", kvp.Value.Scope));
                }

                // 添加上下文快捷键
                foreach (var contextKvp in _contextualShortcuts)
                {
                    foreach (var shortcutKvp in contextKvp.Value)
                    {
                        shortcuts.Add(new ShortcutInfo(shortcutKvp.Key, shortcutKvp.Value.Description, contextKvp.Key, shortcutKvp.Value.Scope));
                    }
                }
            }

            return shortcuts.OrderBy(s => s.Context).ThenBy(s => s.Keys).ToList();
        }

        /// <summary>
        /// 获取快捷键描述
        /// </summary>
        /// <param name="key">快捷键组合</param>
        /// <param name="context">上下文</param>
        /// <returns>描述文本</returns>
        public string GetShortcutDescription(Keys key, string context = null)
        {
            lock (_lockObject)
            {
                if (!string.IsNullOrEmpty(context) && _contextualShortcuts.ContainsKey(context))
                {
                    var contextShortcuts = _contextualShortcuts[context];
                    if (contextShortcuts.TryGetValue(key, out var contextAction))
                    {
                        return contextAction.Description;
                    }
                }

                if (_globalShortcuts.TryGetValue(key, out var globalAction))
                {
                    return globalAction.Description;
                }

                return null;
            }
        }

        /// <summary>
        /// 检查快捷键是否已注册
        /// </summary>
        /// <param name="key">快捷键组合</param>
        /// <param name="context">上下文</param>
        /// <returns>是否已注册</returns>
        public bool IsShortcutRegistered(Keys key, string context = null)
        {
            lock (_lockObject)
            {
                if (!string.IsNullOrEmpty(context) && _contextualShortcuts.ContainsKey(context))
                {
                    return _contextualShortcuts[context].ContainsKey(key);
                }

                return _globalShortcuts.ContainsKey(key);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化默认快捷键
        /// </summary>
        private void InitializeDefaultShortcuts()
        {
            // 文件操作快捷键
            RegisterGlobalShortcut(Keys.Control | Keys.O, () => OnFileOpen?.Invoke(this, EventArgs.Empty), "打开图片文件");
            RegisterGlobalShortcut(Keys.Control | Keys.S, () => OnFileSave?.Invoke(this, EventArgs.Empty), "保存图片文件");
            RegisterGlobalShortcut(Keys.Control | Keys.Shift | Keys.S, () => OnFileSaveAs?.Invoke(this, EventArgs.Empty), "另存为");
            RegisterGlobalShortcut(Keys.Control | Keys.E, () => OnFileExport?.Invoke(this, EventArgs.Empty), "导出结果");
            RegisterGlobalShortcut(Keys.Control | Keys.P, () => OnFilePrint?.Invoke(this, EventArgs.Empty), "打印");

            // 编辑操作快捷键
            RegisterGlobalShortcut(Keys.Control | Keys.Z, () => OnEditUndo?.Invoke(this, EventArgs.Empty), "撤销");
            RegisterGlobalShortcut(Keys.Control | Keys.Y, () => OnEditRedo?.Invoke(this, EventArgs.Empty), "重做");
            RegisterGlobalShortcut(Keys.Control | Keys.R, () => OnEditClear?.Invoke(this, EventArgs.Empty), "清除选择");
            RegisterGlobalShortcut(Keys.Delete, () => OnEditDelete?.Invoke(this, EventArgs.Empty), "删除选中项");
            RegisterGlobalShortcut(Keys.Control | Keys.A, () => OnEditSelectAll?.Invoke(this, EventArgs.Empty), "全选");

            // 视图操作快捷键
            RegisterGlobalShortcut(Keys.Space, () => OnViewCalculate?.Invoke(this, EventArgs.Empty), "计算预测");
            RegisterGlobalShortcut(Keys.F5, () => OnViewRefresh?.Invoke(this, EventArgs.Empty), "刷新视图");
            RegisterGlobalShortcut(Keys.F11, () => OnViewFullscreen?.Invoke(this, EventArgs.Empty), "全屏切换");
            RegisterGlobalShortcut(Keys.Control | Keys.D0, () => OnViewZoomReset?.Invoke(this, EventArgs.Empty), "重置缩放");
            RegisterGlobalShortcut(Keys.Control | Keys.Oemplus, () => OnViewZoomIn?.Invoke(this, EventArgs.Empty), "放大");
            RegisterGlobalShortcut(Keys.Control | Keys.OemMinus, () => OnViewZoomOut?.Invoke(this, EventArgs.Empty), "缩小");

            // 工具操作快捷键
            RegisterGlobalShortcut(Keys.D1, () => OnToolSelectMode?.Invoke(this, EventArgs.Empty), "选择模式");
            RegisterGlobalShortcut(Keys.D2, () => OnToolMeasureMode?.Invoke(this, EventArgs.Empty), "测量模式");
            RegisterGlobalShortcut(Keys.D3, () => OnToolShadowMode?.Invoke(this, EventArgs.Empty), "影线测量模式");
            RegisterGlobalShortcut(Keys.D4, () => OnToolStandardMode?.Invoke(this, EventArgs.Empty), "标准测量模式");
            RegisterGlobalShortcut(Keys.M, () => OnToolMeasurement?.Invoke(this, EventArgs.Empty), "快速测量");

            // 导航快捷键
            RegisterGlobalShortcut(Keys.Escape, () => OnNavigationEscape?.Invoke(this, EventArgs.Empty), "取消/退出");
            RegisterGlobalShortcut(Keys.Enter, () => OnNavigationConfirm?.Invoke(this, EventArgs.Empty), "确认/执行");
            RegisterGlobalShortcut(Keys.Tab, () => OnNavigationNext?.Invoke(this, EventArgs.Empty), "切换到下一个");
            RegisterGlobalShortcut(Keys.Shift | Keys.Tab, () => OnNavigationPrevious?.Invoke(this, EventArgs.Empty), "切换到上一个");

            // 帮助快捷键
            RegisterGlobalShortcut(Keys.F1, () => OnHelpHelp?.Invoke(this, EventArgs.Empty), "显示帮助");
            RegisterGlobalShortcut(Keys.F12, () => OnHelpAbout?.Invoke(this, EventArgs.Empty), "关于");

            // K线选择器上下文快捷键
            RegisterContextualShortcut("KLineSelector", Keys.D1, () => OnKLineStandardMode?.Invoke(this, EventArgs.Empty), "标准测量模式");
            RegisterContextualShortcut("KLineSelector", Keys.D2, () => OnKLineUpperShadowMode?.Invoke(this, EventArgs.Empty), "上影线测量模式");
            RegisterContextualShortcut("KLineSelector", Keys.D3, () => OnKLineLowerShadowMode?.Invoke(this, EventArgs.Empty), "下影线测量模式");
            RegisterContextualShortcut("KLineSelector", Keys.D4, () => OnKLineFullShadowMode?.Invoke(this, EventArgs.Empty), "完整影线测量模式");
            RegisterContextualShortcut("KLineSelector", Keys.C, () => OnKLineClearSelection?.Invoke(this, EventArgs.Empty), "清除当前选择");
            RegisterContextualShortcut("KLineSelector", Keys.U, () => OnKLineUndo?.Invoke(this, EventArgs.Empty), "撤销上一步");
            RegisterContextualShortcut("KLineSelector", Keys.R, () => OnKLineRedo?.Invoke(this, EventArgs.Empty), "重做");
        }

        /// <summary>
        /// 获取当前按键组合
        /// </summary>
        /// <returns>按键组合</returns>
        private Keys GetCurrentKeyCombination()
        {
            Keys combination = Keys.None;
            foreach (var key in _pressedKeys)
            {
                combination |= key;
            }
            return combination;
        }

        /// <summary>
        /// 执行快捷键动作
        /// </summary>
        /// <param name="action">快捷键动作</param>
        /// <param name="key">按键组合</param>
        private void ExecuteShortcut(ShortcutAction action, Keys key)
        {
            try
            {
                action.Action?.Invoke();
                OnShortcutExecuted(new ShortcutExecutedEventArgs(key, action.Description, _currentContext));
            }
            catch (Exception ex)
            {
                // 记录异常但不中断程序
                System.Diagnostics.Debug.WriteLine($"快捷键执行异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 触发快捷键执行事件
        /// </summary>
        /// <param name="e">事件参数</param>
        protected virtual void OnShortcutExecuted(ShortcutExecutedEventArgs e)
        {
            ShortcutExecuted?.Invoke(this, e);
        }

        /// <summary>
        /// 触发快捷键冲突事件
        /// </summary>
        /// <param name="e">事件参数</param>
        protected virtual void OnShortcutConflict(ShortcutConflictEventArgs e)
        {
            ShortcutConflict?.Invoke(this, e);
        }

        #endregion

        #region 事件定义

        /// <summary>
        /// 文件打开事件
        /// </summary>
        public event EventHandler OnFileOpen;

        /// <summary>
        /// 文件保存事件
        /// </summary>
        public event EventHandler OnFileSave;

        /// <summary>
        /// 文件另存为事件
        /// </summary>
        public event EventHandler OnFileSaveAs;

        /// <summary>
        /// 文件导出事件
        /// </summary>
        public event EventHandler OnFileExport;

        /// <summary>
        /// 文件打印事件
        /// </summary>
        public event EventHandler OnFilePrint;

        /// <summary>
        /// 编辑撤销事件
        /// </summary>
        public event EventHandler OnEditUndo;

        /// <summary>
        /// 编辑重做事件
        /// </summary>
        public event EventHandler OnEditRedo;

        /// <summary>
        /// 编辑清除事件
        /// </summary>
        public event EventHandler OnEditClear;

        /// <summary>
        /// 编辑删除事件
        /// </summary>
        public event EventHandler OnEditDelete;

        /// <summary>
        /// 编辑全选事件
        /// </summary>
        public event EventHandler OnEditSelectAll;

        /// <summary>
        /// 视图计算事件
        /// </summary>
        public event EventHandler OnViewCalculate;

        /// <summary>
        /// 视图刷新事件
        /// </summary>
        public event EventHandler OnViewRefresh;

        /// <summary>
        /// 视图全屏事件
        /// </summary>
        public event EventHandler OnViewFullscreen;

        /// <summary>
        /// 视图重置缩放事件
        /// </summary>
        public event EventHandler OnViewZoomReset;

        /// <summary>
        /// 视图放大事件
        /// </summary>
        public event EventHandler OnViewZoomIn;

        /// <summary>
        /// 视图缩小事件
        /// </summary>
        public event EventHandler OnViewZoomOut;

        /// <summary>
        /// 工具选择模式事件
        /// </summary>
        public event EventHandler OnToolSelectMode;

        /// <summary>
        /// 工具测量模式事件
        /// </summary>
        public event EventHandler OnToolMeasureMode;

        /// <summary>
        /// 工具影线模式事件
        /// </summary>
        public event EventHandler OnToolShadowMode;

        /// <summary>
        /// 工具标准模式事件
        /// </summary>
        public event EventHandler OnToolStandardMode;

        /// <summary>
        /// 工具测量事件
        /// </summary>
        public event EventHandler OnToolMeasurement;

        /// <summary>
        /// 导航取消事件
        /// </summary>
        public event EventHandler OnNavigationEscape;

        /// <summary>
        /// 导航确认事件
        /// </summary>
        public event EventHandler OnNavigationConfirm;

        /// <summary>
        /// 导航下一个事件
        /// </summary>
        public event EventHandler OnNavigationNext;

        /// <summary>
        /// 导航上一个事件
        /// </summary>
        public event EventHandler OnNavigationPrevious;

        /// <summary>
        /// 帮助事件
        /// </summary>
        public event EventHandler OnHelpHelp;

        /// <summary>
        /// 关于事件
        /// </summary>
        public event EventHandler OnHelpAbout;

        /// <summary>
        /// K线标准模式事件
        /// </summary>
        public event EventHandler OnKLineStandardMode;

        /// <summary>
        /// K线上影线模式事件
        /// </summary>
        public event EventHandler OnKLineUpperShadowMode;

        /// <summary>
        /// K线下影线模式事件
        /// </summary>
        public event EventHandler OnKLineLowerShadowMode;

        /// <summary>
        /// K线完整影线模式事件
        /// </summary>
        public event EventHandler OnKLineFullShadowMode;

        /// <summary>
        /// K线清除选择事件
        /// </summary>
        public event EventHandler OnKLineClearSelection;

        /// <summary>
        /// K线撤销事件
        /// </summary>
        public event EventHandler OnKLineUndo;

        /// <summary>
        /// K线重做事件
        /// </summary>
        public event EventHandler OnKLineRedo;

        #endregion

        #region IDisposable实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_lockObject)
                {
                    _globalShortcuts.Clear();
                    _contextualShortcuts.Clear();
                    _pressedKeys.Clear();
                }

                _disposed = true;
            }
        }

        #endregion
    }

    #region 辅助类

    /// <summary>
    /// 快捷键动作
    /// </summary>
    internal class ShortcutAction
    {
        public Action Action { get; }
        public string Description { get; }
        public ShortcutScope Scope { get; }

        public ShortcutAction(Action action, string description, ShortcutScope scope)
        {
            Action = action;
            Description = description ?? "";
            Scope = scope;
        }
    }

    /// <summary>
    /// 快捷键信息
    /// </summary>
    public class ShortcutInfo
    {
        public Keys Keys { get; }
        public string Description { get; }
        public string Context { get; }
        public ShortcutScope Scope { get; }

        public ShortcutInfo(Keys keys, string description, string context, ShortcutScope scope)
        {
            Keys = keys;
            Description = description ?? "";
            Context = context ?? "";
            Scope = scope;
        }

        public override string ToString()
        {
            return $"{Keys} - {Description} ({Context})";
        }
    }

    /// <summary>
    /// 快捷键执行事件参数
    /// </summary>
    public class ShortcutExecutedEventArgs : EventArgs
    {
        public Keys Keys { get; }
        public string Description { get; }
        public string Context { get; }
        public DateTime ExecutedAt { get; }

        public ShortcutExecutedEventArgs(Keys keys, string description, string context)
        {
            Keys = keys;
            Description = description ?? "";
            Context = context ?? "";
            ExecutedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 快捷键冲突事件参数
    /// </summary>
    public class ShortcutConflictEventArgs : EventArgs
    {
        public Keys Keys { get; }
        public string ExistingDescription { get; }
        public string NewDescription { get; }
        public DateTime ConflictAt { get; }

        public ShortcutConflictEventArgs(Keys keys, string existingDescription, string newDescription)
        {
            Keys = keys;
            ExistingDescription = existingDescription ?? "";
            NewDescription = newDescription ?? "";
            ConflictAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 快捷键作用域
    /// </summary>
    public enum ShortcutScope
    {
        Global,
        Contextual
    }

    #endregion
}