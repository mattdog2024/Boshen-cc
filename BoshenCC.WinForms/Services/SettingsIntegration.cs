using BoshenCC.Services.Interfaces;
using BoshenCC.Services.Implementations;
using BoshenCC.Core.Utils;

namespace BoshenCC.WinForms.Services
{
    /// <summary>
    /// 设置集成服务
    /// 负责初始化和注册所有设置相关的服务
    /// </summary>
    public static class SettingsIntegration
    {
        /// <summary>
        /// 初始化设置服务
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <returns>用户设置服务</returns>
        public static IUserSettingsService InitializeSettingsService(ILogService logService)
        {
            try
            {
                // 创建用户设置服务
                var userSettingsService = new UserSettingsService(logService);

                // 注册到服务定位器
                ServiceLocator.Register<IUserSettingsService>(userSettingsService);

                return userSettingsService;
            }
            catch (System.Exception ex)
            {
                logService?.LogError($"初始化设置服务失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 应用设置到应用程序
        /// </summary>
        /// <param name="userSettingsService">用户设置服务</param>
        /// <param name="logService">日志服务</param>
        public static async void ApplyAppSettings(IUserSettingsService userSettingsService, ILogService logService)
        {
            try
            {
                if (userSettingsService == null)
                {
                    logService?.LogWarning("用户设置服务不可用，无法应用设置");
                    return;
                }

                // 加载设置
                var settings = await userSettingsService.LoadSettingsAsync();

                if (settings?.AppSettings != null)
                {
                    // 应用窗口设置
                    ApplyWindowSettings(settings.AppSettings.WindowSettings);

                    // 应用日志设置
                    ApplyLogSettings(settings.AppSettings.LogSettings, logService);

                    logService?.LogInfo("应用程序设置已应用");
                }
            }
            catch (System.Exception ex)
            {
                logService?.LogError($"应用设置时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用窗口设置
        /// </summary>
        /// <param name="windowSettings">窗口设置</param>
        private static void ApplyWindowSettings(WindowSettings windowSettings)
        {
            if (windowSettings == null) return;

            // 这里可以应用到主窗口
            // 由于这是静态方法，主窗口的设置需要在主窗口中手动应用
        }

        /// <summary>
        /// 应用日志设置
        /// </summary>
        /// <param name="logSettings">日志设置</param>
        /// <param name="logService">日志服务</param>
        private static void ApplyLogSettings(LogSettings logSettings, ILogService logService)
        {
            if (logSettings == null || logService == null) return;

            try
            {
                // 如果日志服务支持动态配置，这里可以应用日志设置
                // 例如：logService.Configure(logSettings);

                // 目前仅记录日志
                logService.LogInfo($"日志设置: 文件日志={logSettings.EnableFileLogging}, 控制台日志={logSettings.EnableConsoleLogging}");
            }
            catch (System.Exception ex)
            {
                logService.LogError($"应用日志设置时发生错误: {ex.Message}");
            }
        }
    }
}