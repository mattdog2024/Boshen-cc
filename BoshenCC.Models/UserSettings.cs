using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BoshenCC.Models
{
    /// <summary>
    /// 用户设置模型类
    /// 管理波神算法应用的所有用户配置
    /// </summary>
    [Serializable]
    public class UserSettings : INotifyPropertyChanged
    {
        #region 算法设置

        private AlgorithmSettings _algorithmSettings = new AlgorithmSettings();

        /// <summary>
        /// 算法设置
        /// </summary>
        public AlgorithmSettings AlgorithmSettings
        {
            get => _algorithmSettings;
            set
            {
                if (_algorithmSettings != value)
                {
                    _algorithmSettings = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region 显示设置

        private DisplaySettings _displaySettings = new DisplaySettings();

        /// <summary>
        /// 显示设置
        /// </summary>
        public DisplaySettings DisplaySettings
        {
            get => _displaySettings;
            set
            {
                if (_displaySettings != value)
                {
                    _displaySettings = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region 用户偏好设置

        private UserPreferences _userPreferences = new UserPreferences();

        /// <summary>
        /// 用户偏好设置
        /// </summary>
        public UserPreferences UserPreferences
        {
            get => _userPreferences;
            set
            {
                if (_userPreferences != value)
                {
                    _userPreferences = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region 应用设置

        private AppSettings _appSettings = new AppSettings();

        /// <summary>
        /// 应用设置
        /// </summary>
        public AppSettings AppSettings
        {
            get => _appSettings;
            set
            {
                if (_appSettings != value)
                {
                    _appSettings = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region 构造函数

        public UserSettings()
        {
            // 初始化设置
            InitializeDefaults();
        }

        #endregion

        #region 方法

        /// <summary>
        /// 初始化默认设置
        /// </summary>
        public void InitializeDefaults()
        {
            AlgorithmSettings = new AlgorithmSettings();
            DisplaySettings = new DisplaySettings();
            UserPreferences = new UserPreferences();
            AppSettings = new AppSettings();
        }

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        public void ResetToDefaults()
        {
            InitializeDefaults();
            OnPropertyChanged(nameof(AlgorithmSettings));
            OnPropertyChanged(nameof(DisplaySettings));
            OnPropertyChanged(nameof(UserPreferences));
            OnPropertyChanged(nameof(AppSettings));
        }

        /// <summary>
        /// 验证设置是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateSettings()
        {
            var result = new ValidationResult();

            // 验证算法设置
            if (AlgorithmSettings.PricePrecision < 0 || AlgorithmSettings.PricePrecision > 8)
            {
                result.AddError("价格精度必须在0-8之间");
            }

            if (AlgorithmSettings.MaxPredictionLines <= 0 || AlgorithmSettings.MaxPredictionLines > 20)
            {
                result.AddError("预测线数量必须在1-20之间");
            }

            // 验证显示设置
            if (DisplaySettings.LineWidth < 1 || DisplaySettings.LineWidth > 10)
            {
                result.AddError("线条宽度必须在1-10之间");
            }

            if (DisplaySettings.FontSize < 8 || DisplaySettings.FontSize > 48)
            {
                result.AddError("字体大小必须在8-48之间");
            }

            // 验证应用设置
            if (AppSettings.WindowSettings.Width < 400 || AppSettings.WindowSettings.Width > 4000)
            {
                result.AddError("窗口宽度必须在400-4000之间");
            }

            if (AppSettings.WindowSettings.Height < 300 || AppSettings.WindowSettings.Height > 3000)
            {
                result.AddError("窗口高度必须在300-3000之间");
            }

            return result;
        }

        /// <summary>
        /// 复制设置到另一个实例
        /// </summary>
        /// <param name="target">目标设置实例</param>
        public void CopyTo(UserSettings target)
        {
            if (target == null) return;

            // 深度复制算法设置
            AlgorithmSettings.CopyTo(target.AlgorithmSettings);

            // 深度复制显示设置
            DisplaySettings.CopyTo(target.DisplaySettings);

            // 深度复制用户偏好设置
            UserPreferences.CopyTo(target.UserPreferences);

            // 深度复制应用设置
            CopyAppSettings(AppSettings, target.AppSettings);
        }

        /// <summary>
        /// 复制应用设置
        /// </summary>
        /// <param name="source">源设置</param>
        /// <param name="target">目标设置</param>
        private void CopyAppSettings(AppSettings source, AppSettings target)
        {
            if (source?.DefaultProcessingOptions != null && target?.DefaultProcessingOptions != null)
            {
                // 复制处理选项
                target.DefaultProcessingOptions = new ProcessingOptions
                {
                    // 复制所有ProcessingOptions属性
                    ImageFilter = source.DefaultProcessingOptions.ImageFilter,
                    Threshold = source.DefaultProcessingOptions.Threshold,
                    MinKLineSize = source.DefaultProcessingOptions.MinKLineSize,
                    MaxKLineSize = source.DefaultProcessingOptions.MaxKLineSize
                };
            }

            // 复制窗口设置
            if (source?.WindowSettings != null && target?.WindowSettings != null)
            {
                target.WindowSettings = new WindowSettings
                {
                    Width = source.WindowSettings.Width,
                    Height = source.WindowSettings.Height,
                    Left = source.WindowSettings.Left,
                    Top = source.WindowSettings.Top,
                    IsMaximized = source.WindowSettings.IsMaximized,
                    RememberWindowState = source.WindowSettings.RememberWindowState
                };
            }

            // 复制日志设置
            if (source?.LogSettings != null && target?.LogSettings != null)
            {
                target.LogSettings = new LogSettings
                {
                    LogLevel = source.LogSettings.LogLevel,
                    LogFilePath = source.LogSettings.LogFilePath,
                    MaxFileSizeMB = source.LogSettings.MaxFileSizeMB,
                    MaxFileCount = source.LogSettings.MaxFileCount,
                    EnableFileLogging = source.LogSettings.EnableFileLogging,
                    EnableConsoleLogging = source.LogSettings.EnableConsoleLogging
                };
            }

            // 复制快捷键设置
            if (source?.HotkeySettings != null && target?.HotkeySettings != null)
            {
                target.HotkeySettings = new HotkeySettings
                {
                    ScreenshotHotkey = source.HotkeySettings.ScreenshotHotkey,
                    RecognizeHotkey = source.HotkeySettings.RecognizeHotkey,
                    SettingsHotkey = source.HotkeySettings.SettingsHotkey
                };
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 算法设置类
    /// </summary>
    [Serializable]
    public class AlgorithmSettings : INotifyPropertyChanged
    {
        private decimal _priceThreshold = 0.01m;
        private int _pricePrecision = 2;
        private bool _enableAutoCalculate = true;
        private bool _showCalculationLog = false;
        private int _maxPredictionLines = 11;
        private bool _enableAdvancedMode = false;
        private decimal _tolerance = 0.001m;

        /// <summary>
        /// 价格阈值
        /// </summary>
        public decimal PriceThreshold
        {
            get => _priceThreshold;
            set
            {
                if (_priceThreshold != value)
                {
                    _priceThreshold = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 价格精度
        /// </summary>
        public int PricePrecision
        {
            get => _pricePrecision;
            set
            {
                if (_pricePrecision != value)
                {
                    _pricePrecision = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 启用自动计算
        /// </summary>
        public bool EnableAutoCalculate
        {
            get => _enableAutoCalculate;
            set
            {
                if (_enableAutoCalculate != value)
                {
                    _enableAutoCalculate = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 显示计算日志
        /// </summary>
        public bool ShowCalculationLog
        {
            get => _showCalculationLog;
            set
            {
                if (_showCalculationLog != value)
                {
                    _showCalculationLog = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 最大预测线数量
        /// </summary>
        public int MaxPredictionLines
        {
            get => _maxPredictionLines;
            set
            {
                if (_maxPredictionLines != value)
                {
                    _maxPredictionLines = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 启用高级模式
        /// </summary>
        public bool EnableAdvancedMode
        {
            get => _enableAdvancedMode;
            set
            {
                if (_enableAdvancedMode != value)
                {
                    _enableAdvancedMode = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 计算容差
        /// </summary>
        public decimal Tolerance
        {
            get => _tolerance;
            set
            {
                if (_tolerance != value)
                {
                    _tolerance = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 复制到另一个实例
        /// </summary>
        /// <param name="target">目标实例</param>
        public void CopyTo(AlgorithmSettings target)
        {
            if (target == null) return;

            target.PriceThreshold = this.PriceThreshold;
            target.PricePrecision = this.PricePrecision;
            target.EnableAutoCalculate = this.EnableAutoCalculate;
            target.ShowCalculationLog = this.ShowCalculationLog;
            target.MaxPredictionLines = this.MaxPredictionLines;
            target.EnableAdvancedMode = this.EnableAdvancedMode;
            target.Tolerance = this.Tolerance;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 显示设置类
    /// </summary>
    [Serializable]
    public class DisplaySettings : INotifyPropertyChanged
    {
        private int _lineWidth = 1;
        private string _theme = "Default";
        private int _fontSize = 12;
        private bool _showGrid = true;
        private bool _showCoordinates = true;
        private bool _enableAnimations = true;
        private bool _showTooltips = true;
        private string _language = "zh-CN";

        /// <summary>
        /// 线条宽度
        /// </summary>
        public int LineWidth
        {
            get => _lineWidth;
            set
            {
                if (_lineWidth != value)
                {
                    _lineWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 主题
        /// </summary>
        public string Theme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 字体大小
        /// </summary>
        public int FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 显示网格
        /// </summary>
        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                if (_showGrid != value)
                {
                    _showGrid = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 显示坐标
        /// </summary>
        public bool ShowCoordinates
        {
            get => _showCoordinates;
            set
            {
                if (_showCoordinates != value)
                {
                    _showCoordinates = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 启用动画
        /// </summary>
        public bool EnableAnimations
        {
            get => _enableAnimations;
            set
            {
                if (_enableAnimations != value)
                {
                    _enableAnimations = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 显示工具提示
        /// </summary>
        public bool ShowTooltips
        {
            get => _showTooltips;
            set
            {
                if (_showTooltips != value)
                {
                    _showTooltips = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 语言设置
        /// </summary>
        public string Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 复制到另一个实例
        /// </summary>
        /// <param name="target">目标实例</param>
        public void CopyTo(DisplaySettings target)
        {
            if (target == null) return;

            target.LineWidth = this.LineWidth;
            target.Theme = this.Theme;
            target.FontSize = this.FontSize;
            target.ShowGrid = this.ShowGrid;
            target.ShowCoordinates = this.ShowCoordinates;
            target.EnableAnimations = this.EnableAnimations;
            target.ShowTooltips = this.ShowTooltips;
            target.Language = this.Language;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 用户偏好设置类
    /// </summary>
    [Serializable]
    public class UserPreferences : INotifyPropertyChanged
    {
        private bool _autoSave = true;
        private int _autoSaveInterval = 5;
        private bool _confirmBeforeClear = true;
        private bool _enableSounds = false;
        private bool _startWithWindows = false;
        private bool _checkForUpdates = true;
        private string _lastUsedPath = "";
        private bool _showWelcomeMessage = true;

        /// <summary>
        /// 自动保存
        /// </summary>
        public bool AutoSave
        {
            get => _autoSave;
            set
            {
                if (_autoSave != value)
                {
                    _autoSave = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 自动保存间隔（分钟）
        /// </summary>
        public int AutoSaveInterval
        {
            get => _autoSaveInterval;
            set
            {
                if (_autoSaveInterval != value)
                {
                    _autoSaveInterval = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 清除前确认
        /// </summary>
        public bool ConfirmBeforeClear
        {
            get => _confirmBeforeClear;
            set
            {
                if (_confirmBeforeClear != value)
                {
                    _confirmBeforeClear = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 启用声音
        /// </summary>
        public bool EnableSounds
        {
            get => _enableSounds;
            set
            {
                if (_enableSounds != value)
                {
                    _enableSounds = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 随Windows启动
        /// </summary>
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (_startWithWindows != value)
                {
                    _startWithWindows = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 检查更新
        /// </summary>
        public bool CheckForUpdates
        {
            get => _checkForUpdates;
            set
            {
                if (_checkForUpdates != value)
                {
                    _checkForUpdates = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 最后使用的路径
        /// </summary>
        public string LastUsedPath
        {
            get => _lastUsedPath;
            set
            {
                if (_lastUsedPath != value)
                {
                    _lastUsedPath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 显示欢迎消息
        /// </summary>
        public bool ShowWelcomeMessage
        {
            get => _showWelcomeMessage;
            set
            {
                if (_showWelcomeMessage != value)
                {
                    _showWelcomeMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 复制到另一个实例
        /// </summary>
        /// <param name="target">目标实例</param>
        public void CopyTo(UserPreferences target)
        {
            if (target == null) return;

            target.AutoSave = this.AutoSave;
            target.AutoSaveInterval = this.AutoSaveInterval;
            target.ConfirmBeforeClear = this.ConfirmBeforeClear;
            target.EnableSounds = this.EnableSounds;
            target.StartWithWindows = this.StartWithWindows;
            target.CheckForUpdates = this.CheckForUpdates;
            target.LastUsedPath = this.LastUsedPath;
            target.ShowWelcomeMessage = this.ShowWelcomeMessage;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}