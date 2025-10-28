using System;
using System.Windows.Forms;
using BoshenCC.WinForms.Views;
using BoshenCC.Core.Utils;
using BoshenCC.Services.Interfaces;
using BoshenCC.Services.Implementations;
using BoshenCC.Core.Interfaces;
using BoshenCC.Core;
using BoshenCC.WinForms.Services;

namespace BoshenCC.WinForms
{
    /// <summary>
    /// 应用程序主程序类
    /// </summary>
    static class Program
    {
        private static ExceptionHandlerService _exceptionHandler;

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // 启用应用程序框架
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 初始化服务
                InitializeServices();

                // 初始化全局异常处理
                InitializeExceptionHandling();

                // 启动主窗体
                Application.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                ShowFatalError(ex);
            }
            finally
            {
                // 清理资源
                CleanupServices();
            }
        }

        /// <summary>
        /// 初始化所有服务
        /// </summary>
        private static void InitializeServices()
        {
            try
            {
                // 注册日志服务
                var logService = new LogService();
                ServiceLocator.RegisterSingleton<ILogService>(logService);

                // 注册配置服务
                var configService = new ConfigService();
                ServiceLocator.RegisterSingleton<IConfigService>(configService);

                // 注册截图服务
                var screenshotService = new ScreenshotService();
                ServiceLocator.RegisterSingleton<IScreenshotService>(screenshotService);

                // 注册图像处理器
                var imageProcessor = new ImageProcessor(logService);
                ServiceLocator.RegisterSingleton<IImageProcessor>(imageProcessor);

                // 记录服务初始化完成
                logService.Info("所有服务初始化完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"服务初始化失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// 初始化异常处理框架
        /// </summary>
        private static void InitializeExceptionHandling()
        {
            try
            {
                var logService = ServiceLocator.GetService<ILogService>();
                _exceptionHandler = new ExceptionHandlerService(logService);
                _exceptionHandler.InitializeGlobalExceptionHandling();

                logService?.Info("异常处理框架初始化完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"异常处理框架初始化失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 清理服务资源
        /// </summary>
        private static void CleanupServices()
        {
            try
            {
                // 获取并关闭日志服务
                if (ServiceLocator.IsRegistered<ILogService>())
                {
                    var logService = ServiceLocator.GetService<ILogService>();
                    if (logService is LogService nlogService)
                    {
                        nlogService.Flush();
                        nlogService.Shutdown();
                    }
                }

                // 清理所有注册的服务
                ServiceLocator.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理服务失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示致命错误
        /// </summary>
        private static void ShowFatalError(Exception ex)
        {
            var message = $"应用程序发生致命错误，即将退出：\n\n{ex.Message}";

            // 如果有内部异常，也显示内部异常的信息
            var innerException = ex.InnerException;
            if (innerException != null)
            {
                message += $"\n\n详细信息: {innerException.Message}";
            }

            message += $"\n\n异常类型: {ex.GetType().Name}";

            MessageBox.Show(message, "致命错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 安全执行操作
        /// </summary>
        public static bool SafeExecute(Action action, string operationName = "操作")
        {
            return _exceptionHandler?.SafeExecute(action, operationName) ?? false;
        }

        /// <summary>
        /// 安全执行操作并返回结果
        /// </summary>
        public static T SafeExecute<T>(Func<T> func, string operationName = "操作", T defaultValue = default(T))
        {
            return _exceptionHandler?.SafeExecute(func, operationName, defaultValue) ?? defaultValue;
        }
    }
}