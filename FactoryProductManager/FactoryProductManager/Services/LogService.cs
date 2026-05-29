using System;
using System.IO;
using System.Threading;

namespace FactoryProductManager.Services
{
    public static class LogService
    {
        private static readonly string _logDirectory = "Logs";
        private static readonly object _lock = new object();

        static LogService()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public static void LogWarning(string message)
        {
            WriteLog("WARNING", message);
        }

        public static void LogError(string message)
        {
            WriteLog("ERROR", message);
        }

        public static void LogError(string message, Exception ex)
        {
            string errorMessage = $"{message}\n异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}";
            WriteLog("ERROR", errorMessage);
        }

        public static void LogDebug(string message)
        {
#if DEBUG
            WriteLog("DEBUG", message);
#endif
        }

        private static void WriteLog(string level, string message)
        {
            lock (_lock)
            {
                try
                {
                    string logFileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
                    string logFilePath = Path.Combine(_logDirectory, logFileName);
                    
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}\n";
                    
                    File.AppendAllText(logFilePath, logEntry);
                }
                catch (Exception ex)
                {
                    try
                    {
                        string errorLogPath = Path.Combine(_logDirectory, "log_errors.txt");
                        File.AppendAllText(errorLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 日志写入失败: {ex.Message}\n");
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static void LogApplicationStart()
        {
            LogInfo("========== 应用程序启动 ==========");
            LogInfo($"操作系统: {Environment.OSVersion}");
            LogInfo($"CLR版本: {Environment.Version}");
            LogInfo($"进程ID: {Environment.ProcessId}");
            LogInfo($"启动时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogInfo("==================================");
        }

        public static void LogApplicationExit()
        {
            LogInfo("========== 应用程序退出 ==========");
            LogInfo($"退出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogInfo("==================================");
        }

        public static void LogMethodEntry(string methodName)
        {
            LogDebug($"进入方法: {methodName}");
        }

        public static void LogMethodExit(string methodName)
        {
            LogDebug($"退出方法: {methodName}");
        }
    }
}