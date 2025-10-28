using System;
using System.Threading;
using System.Windows.Forms;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.WinForms.Services
{
    /// <summary>
    /// WinForms异常处理服务
    /// </summary>
    public class ExceptionHandlerService
    {
        private readonly ILogService _logService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        public ExceptionHandlerService(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// 初始化全局异常处理
        /// </summary>
        public void InitializeGlobalExceptionHandling()
        {
            // 设置UI线程异常处理
            Application.ThreadException += OnUiThreadException;

            // 设置非UI线程异常处理
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// UI线程异常处理
        /// </summary>
        private void OnUiThreadException(object sender, ThreadExceptionEventArgs e)
        {
            try
            {
                LogException(e.Exception, "UI线程异常");
                ShowExceptionDialog(e.Exception, "应用程序异常");
            }
            catch (Exception ex)
            {
                // 异常处理本身出错时的备用处理
                MessageBox.Show($"发生严重错误，异常处理也失败了：\n{ex.Message}",
                    "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 非UI线程异常处理
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                if (exception != null)
                {
                    LogException(exception, "非UI线程未处理异常");

                    if (e.IsTerminating)
                    {
                        MessageBox.Show($"应用程序即将终止，发生严重错误：\n{exception.Message}",
                            "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        ShowExceptionDialog(exception, "后台任务异常");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生严重错误，异常处理也失败了：\n{ex.Message}",
                    "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 记录异常信息
        /// </summary>
        private void LogException(Exception exception, string context)
        {
            try
            {
                _logService?.Error($"{context}: {exception.Message}", exception);
                _logService?.Error($"异常堆栈: {exception.StackTrace}");

                // 记录内部异常
                var innerException = exception.InnerException;
                while (innerException != null)
                {
                    _logService?.Error($"内部异常: {innerException.Message}", innerException);
                    innerException = innerException.InnerException;
                }
            }
            catch
            {
                // 日志记录失败时的静默处理
            }
        }

        /// <summary>
        /// 显示异常对话框
        /// </summary>
        private void ShowExceptionDialog(Exception exception, string title)
        {
            var message = FormatExceptionMessage(exception);

            var result = MessageBox.Show(
                $"{message}\n\n是否要继续运行应用程序？",
                title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// 格式化异常信息
        /// </summary>
        private string FormatExceptionMessage(Exception exception)
        {
            var message = exception.Message;

            // 如果有内部异常，也显示内部异常的信息
            var innerException = exception.InnerException;
            if (innerException != null)
            {
                message += $"\n\n详细信息: {innerException.Message}";
            }

            return message;
        }

        /// <summary>
        /// 安全执行操作
        /// </summary>
        public bool SafeExecute(Action action, string operationName = "操作")
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                LogException(ex, operationName);
                MessageBox.Show($"{operationName}失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 安全执行操作并返回结果
        /// </summary>
        public T SafeExecute<T>(Func<T> func, string operationName = "操作", T defaultValue = default(T))
        {
            try
            {
                return func != null ? func() : defaultValue;
            }
            catch (Exception ex)
            {
                LogException(ex, operationName);
                MessageBox.Show($"{operationName}失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return defaultValue;
            }
        }
    }
}