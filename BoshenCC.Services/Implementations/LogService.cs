using System;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Services.Implementations
{
    /// <summary>
    /// 日志服务实现
    /// </summary>
    public class LogService : ILogService
    {
        private readonly string _logFilePath;

        public LogService()
        {
            // 确保日志目录存在
            var logDir = "logs";
            if (!System.IO.Directory.Exists(logDir))
            {
                System.IO.Directory.CreateDirectory(logDir);
            }

            _logFilePath = System.IO.Path.Combine(logDir, $"boshencc_{DateTime.Now:yyyyMMdd}.log");
        }

        public void Debug(string message, params object[] args)
        {
            WriteLog("DEBUG", message, args);
        }

        public void Info(string message, params object[] args)
        {
            WriteLog("INFO", message, args);
        }

        public void Warn(string message, params object[] args)
        {
            WriteLog("WARN", message, args);
        }

        public void Error(string message, params object[] args)
        {
            WriteLog("ERROR", message, args);
        }

        public void Error(Exception exception, string message, params object[] args)
        {
            WriteLog("ERROR", $"{message}\nException: {exception}", args);
        }

        public void Fatal(string message, params object[] args)
        {
            WriteLog("FATAL", message, args);
        }

        public void Fatal(Exception exception, string message, params object[] args)
        {
            WriteLog("FATAL", $"{message}\nException: {exception}", args);
        }

        private void WriteLog(string level, string message, params object[] args)
        {
            try
            {
                var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {formattedMessage}{Environment.NewLine}";

                // 写入文件
                System.IO.File.AppendAllText(_logFilePath, logEntry);

                // 同时输出到控制台（如果在调试模式下）
                #if DEBUG
                Console.Write(logEntry);
                #endif
            }
            catch (Exception ex)
            {
                // 避免日志记录本身出错导致程序崩溃
                Console.WriteLine($"日志记录失败: {ex.Message}");
            }
        }
    }
}