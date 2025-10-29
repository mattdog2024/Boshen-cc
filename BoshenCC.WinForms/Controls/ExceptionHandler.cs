using System;
using System.Diagnostics;
using System.Windows.Forms;
using BoshenCC.Core.Interfaces;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 异常处理工具类
    /// 提供统一的异常处理和日志记录功能
    /// </summary>
    public static class ExceptionHandler
    {
        #region 公共方法

        /// <summary>
        /// 安全执行操作，处理异常并记录日志
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="logService">日志服务</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="showErrorDialog">是否显示错误对话框</param>
        /// <param name="context">上下文信息</param>
        /// <returns>操作是否成功</returns>
        public static bool SafeExecute(Action action, ILogService logService,
            string operationName, bool showErrorDialog = true, string context = null)
        {
            if (action == null)
                return false;

            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                LogException(ex, logService, operationName, context);

                if (showErrorDialog)
                {
                    ShowErrorDialog(ex, operationName);
                }

                return false;
            }
        }

        /// <summary>
        /// 安全执行操作，处理异常并记录日志（带返回值）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="logService">日志服务</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="defaultValue">异常时的默认返回值</param>
        /// <param name="showErrorDialog">是否显示错误对话框</param>
        /// <param name="context">上下文信息</param>
        /// <returns>操作结果或默认值</returns>
        public static T SafeExecute<T>(Func<T> func, ILogService logService,
            string operationName, T defaultValue = default(T), bool showErrorDialog = false, string context = null)
        {
            if (func == null)
                return defaultValue;

            try
            {
                return func();
            }
            catch (Exception ex)
            {
                LogException(ex, logService, operationName, context);

                if (showErrorDialog)
                {
                    ShowErrorDialog(ex, operationName);
                }

                return defaultValue;
            }
        }

        /// <summary>
        /// 安全执行异步操作，处理异常并记录日志
        /// </summary>
        /// <param name="asyncAction">要执行的异步操作</param>
        /// <param name="logService">日志服务</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="showErrorDialog">是否显示错误对话框</param>
        /// <param name="context">上下文信息</param>
        /// <returns>表示异步操作的任务，包含操作是否成功</returns>
        public static async Task<bool> SafeExecuteAsync(Func<Task> asyncAction, ILogService logService,
            string operationName, bool showErrorDialog = true, string context = null)
        {
            if (asyncAction == null)
                return false;

            try
            {
                await asyncAction();
                return true;
            }
            catch (Exception ex)
            {
                LogException(ex, logService, operationName, context);

                if (showErrorDialog)
                {
                    ShowErrorDialog(ex, operationName);
                }

                return false;
            }
        }

        /// <summary>
        /// 安全执行异步操作，处理异常并记录日志（带返回值）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="asyncFunc">要执行的异步函数</param>
        /// <param name="logService">日志服务</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="defaultValue">异常时的默认返回值</param>
        /// <param name="showErrorDialog">是否显示错误对话框</param>
        /// <param name="context">上下文信息</param>
        /// <returns>表示异步操作的任务，包含操作结果或默认值</returns>
        public static async Task<T> SafeExecuteAsync<T>(Func<Task<T>> asyncFunc, ILogService logService,
            string operationName, T defaultValue = default(T), bool showErrorDialog = false, string context = null)
        {
            if (asyncFunc == null)
                return defaultValue;

            try
            {
                return await asyncFunc();
            }
            catch (Exception ex)
            {
                LogException(ex, logService, operationName, context);

                if (showErrorDialog)
                {
                    ShowErrorDialog(ex, operationName);
                }

                return defaultValue;
            }
        }

        /// <summary>
        /// 记录异常信息
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="logService">日志服务</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="context">上下文信息</param>
        public static void LogException(Exception ex, ILogService logService, string operationName, string context = null)
        {
            try
            {
                var message = FormatExceptionMessage(ex, operationName, context);

                // 记录到日志服务
                logService?.Error(message);

                // 记录到系统调试输出
                Debug.WriteLine($"[ExceptionHandler] {message}");

                // 记录到事件日志（Windows）
                try
                {
                    if (!EventLog.SourceExists("BoshenCC"))
                    {
                        EventLog.CreateEventSource("BoshenCC", "Application");
                    }
                    EventLog.WriteEntry("BoshenCC", message, EventLogEntryType.Error);
                }
                catch
                {
                    // 事件日志写入失败时静默处理
                }
            }
            catch
            {
                // 日志记录失败时的最后保障
                Debug.WriteLine($"[ExceptionHandler] 日志记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="operationName">操作名称</param>
        public static void ShowErrorDialog(Exception ex, string operationName)
        {
            try
            {
                var message = GetUserFriendlyMessage(ex, operationName);
                var detailedMessage = GetDetailedMessage(ex);

                // 如果是在UI线程，直接显示对话框
                if (Application.OpenForms.Count > 0)
                {
                    var result = MessageBox.Show(
                        $"{message}\n\n是否查看详细错误信息？",
                        "操作失败",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error);

                    if (result == DialogResult.Yes)
                    {
                        MessageBox.Show(detailedMessage, "详细错误信息",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch
            {
                // 显示对话框失败时的静默处理
                Debug.WriteLine($"[ExceptionHandler] 无法显示错误对话框: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 格式化异常消息
        /// </summary>
        private static string FormatExceptionMessage(Exception ex, string operationName, string context)
        {
            var message = $"操作失败: {operationName}";

            if (!string.IsNullOrEmpty(context))
            {
                message += $" (上下文: {context})";
            }

            message += $"\n异常类型: {ex.GetType().Name}";
            message += $"\n异常消息: {ex.Message}";
            message += $"\n堆栈跟踪: {ex.StackTrace}";

            if (ex.InnerException != null)
            {
                message += $"\n内部异常: {ex.InnerException.Message}";
            }

            message += $"\n发生时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            return message;
        }

        /// <summary>
        /// 获取用户友好的错误消息
        /// </summary>
        private static string GetUserFriendlyMessage(Exception ex, string operationName)
        {
            // 根据异常类型返回友好的错误消息
            switch (ex)
            {
                case ArgumentException _:
                    return $"操作参数错误：{ex.Message}";
                case InvalidOperationException _:
                    return $"操作无效：{ex.Message}";
                case System.IO.FileNotFoundException _:
                    return "文件未找到，请检查文件路径是否正确";
                case System.UnauthorizedAccessException _:
                    return "访问被拒绝，请检查文件权限";
                case TimeoutException _:
                    return "操作超时，请稍后重试";
                case OutOfMemoryException _:
                    return "内存不足，请关闭其他应用程序后重试";
                default:
                    return $"执行操作 '{operationName}' 时发生错误：{ex.Message}";
            }
        }

        /// <summary>
        /// 获取详细的错误信息
        /// </summary>
        private static string GetDetailedMessage(Exception ex)
        {
            var details = $"异常类型: {ex.GetType().FullName}\n";
            details += $"异常消息: {ex.Message}\n";
            details += $"发生时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                details += "堆栈跟踪:\n";
                details += ex.StackTrace;
            }

            if (ex.InnerException != null)
            {
                details += "\n\n内部异常:\n";
                details += $"类型: {ex.InnerException.GetType().Name}\n";
                details += $"消息: {ex.InnerException.Message}\n";

                if (!string.IsNullOrEmpty(ex.InnerException.StackTrace))
                {
                    details += "堆栈跟踪:\n";
                    details += ex.InnerException.StackTrace;
                }
            }

            return details;
        }

        #endregion

        #region 控件扩展方法

        /// <summary>
        /// 安全执行控件操作
        /// </summary>
        public static bool SafeInvoke(this Control control, Action action, ILogService logService, string operationName)
        {
            if (control == null || action == null)
                return false;

            if (control.InvokeRequired)
            {
                try
                {
                    control.Invoke(new Action(() => SafeExecute(action, logService, operationName)));
                    return true;
                }
                catch (Exception ex)
                {
                    LogException(ex, logService, $"{operationName} (Invoke)");
                    return false;
                }
            }
            else
            {
                return SafeExecute(action, logService, operationName);
            }
        }

        #endregion
    }
}