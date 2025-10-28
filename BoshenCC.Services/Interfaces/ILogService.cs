using System;

namespace BoshenCC.Services.Interfaces
{
    /// <summary>
    /// 日志服务接口
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">格式化参数</param>
        void Debug(string message, params object[] args);

        /// <summary>
        /// 记录信息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">格式化参数</param>
        void Info(string message, params object[] args);

        /// <summary>
        /// 记录警告
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">格式化参数</param>
        void Warn(string message, params object[] args);

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">格式化参数</param>
        void Error(string message, params object[] args);

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="exception">异常</param>
        /// <param name="message">消息</param>
        /// <param name="args">格式化参数</param>
        void Error(Exception exception, string message, params object[] args);

        /// <summary>
        /// 记录致命错误
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">格式化参数</param>
        void Fatal(string message, params object[] args);

        /// <summary>
        /// 记录致命异常
        /// </summary>
        /// <param name="exception">异常</param>
        /// <param name="message">消息</param>
        /// <param name="args">格式化参数</param>
        void Fatal(Exception exception, string message, params object[] args);
    }
}