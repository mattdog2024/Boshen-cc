namespace BoshenCC.Models
{
    /// <summary>
    /// 应用程序设置
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 默认处理选项
        /// </summary>
        public ProcessingOptions DefaultProcessingOptions { get; set; } = new ProcessingOptions();

        /// <summary>
        /// 窗口设置
        /// </summary>
        public WindowSettings WindowSettings { get; set; } = new WindowSettings();

        /// <summary>
        /// 日志设置
        /// </summary>
        public LogSettings LogSettings { get; set; } = new LogSettings();

        /// <summary>
        /// 快捷键设置
        /// </summary>
        public HotkeySettings HotkeySettings { get; set; } = new HotkeySettings();
    }

    /// <summary>
    /// 窗口设置
    /// </summary>
    public class WindowSettings
    {
        /// <summary>
        /// 窗口宽度
        /// </summary>
        public int Width { get; set; } = 800;

        /// <summary>
        /// 窗口高度
        /// </summary>
        public int Height { get; set; } = 600;

        /// <summary>
        /// 窗口位置X
        /// </summary>
        public int Left { get; set; } = 100;

        /// <summary>
        /// 窗口位置Y
        /// </summary>
        public int Top { get; set; } = 100;

        /// <summary>
        /// 是否最大化
        /// </summary>
        public bool IsMaximized { get; set; } = false;

        /// <summary>
        /// 记住窗口状态
        /// </summary>
        public bool RememberWindowState { get; set; } = true;
    }

    /// <summary>
    /// 日志设置
    /// </summary>
    public class LogSettings
    {
        /// <summary>
        /// 日志级别
        /// </summary>
        public string LogLevel { get; set; } = "Info";

        /// <summary>
        /// 日志文件路径
        /// </summary>
        public string LogFilePath { get; set; } = "logs/boshencc.log";

        /// <summary>
        /// 最大日志文件大小（MB）
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 10;

        /// <summary>
        /// 保留日志文件数量
        /// </summary>
        public int MaxFileCount { get; set; } = 5;

        /// <summary>
        /// 是否启用文件日志
        /// </summary>
        public bool EnableFileLogging { get; set; } = true;

        /// <summary>
        /// 是否启用控制台日志
        /// </summary>
        public bool EnableConsoleLogging { get; set; } = true;
    }

    /// <summary>
    /// 快捷键设置
    /// </summary>
    public class HotkeySettings
    {
        /// <summary>
        /// 截图快捷键
        /// </summary>
        public string ScreenshotHotkey { get; set; } = "Ctrl+Shift+S";

        /// <summary>
        /// 识别快捷键
        /// </summary>
        public string RecognizeHotkey { get; set; } = "Ctrl+Shift+R";

        /// <summary>
        /// 设置快捷键
        /// </summary>
        public string SettingsHotkey { get; set; } = "Ctrl+Shift+,";
    }
}