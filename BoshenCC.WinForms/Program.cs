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
        /// Ӧ�ó��������ڵ㡣
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // ��ʼ������
                InitializeServices();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ӧ�ó�������ʧ��: {ex.Message}", "����",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // ������Դ
                CleanupServices();
            }
        }

        /// <summary>
        /// ��ʼ�����з���
        /// </summary>
        private static void InitializeServices()
        {
            try
            {
                // ע����־����
                var logService = new LogService();
                ServiceLocator.RegisterSingleton<ILogService>(logService);

                // ע�����÷���
                var configService = new ConfigService();
                ServiceLocator.RegisterSingleton<IConfigService>(configService);

                // ע���ͼ����
                var screenshotService = new ScreenshotService();
                ServiceLocator.RegisterSingleton<IScreenshotService>(screenshotService);

                // ע��ͼ������
                var imageProcessor = new ImageProcessor();
                ServiceLocator.RegisterSingleton<IImageProcessor>(imageProcessor);

                // ��¼�����ʼ�����
                logService.Info("���з����ʼ�����");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�����ʼ��ʧ��: {ex.Message}", "����",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// ���������Դ
        /// </summary>
        private static void CleanupServices()
        {
            try
            {
                // ��ȡ��������־����
                if (ServiceLocator.IsRegistered<ILogService>())
                {
                    var logService = ServiceLocator.GetService<ILogService>();
                    if (logService is LogService nlogService)
                    {
                        nlogService.Flush();
                        nlogService.Shutdown();
                    }
                }

                // ��������ע��ķ���
                ServiceLocator.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"��������ʧ��: {ex.Message}");
            }
        }
    }
}
