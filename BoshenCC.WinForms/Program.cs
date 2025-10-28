using System;
using System.Windows.Forms;
using BoshenCC.WinForms.Views;
using BoshenCC.Core.Utils;
using BoshenCC.Services.Interfaces;
using BoshenCC.Services.Implementations;
using BoshenCC.Core.Interfaces;
using BoshenCC.Core;

namespace BoshenCC.WinForms
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // 初始化服务
                InitializeServices();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用程序启动失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                var imageProcessor = new ImageProcessor();
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
        /// 清理服务资源
        /// </summary>
        private static void CleanupServices()
        {
            try
            {
                // 获取并清理日志服务
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
                Console.WriteLine($"服务清理失败: {ex.Message}");
            }
        }
    }
}
