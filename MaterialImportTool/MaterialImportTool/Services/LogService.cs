using System;
using System.IO;
using System.Text;

namespace MaterialImportTool.Services
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    public static class LogService
    {
        private static readonly string _logDirectory;
        private static readonly object _lock = new object();

        static LogService()
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public static void Debug(string message, string source = "Unknown")
        {
            WriteLog(LogLevel.Debug, message, source);
        }

        public static void Info(string message, string source = "Unknown")
        {
            WriteLog(LogLevel.Info, message, source);
        }

        public static void Warning(string message, string source = "Unknown")
        {
            WriteLog(LogLevel.Warning, message, source);
        }

        public static void Error(string message, string source = "Unknown")
        {
            WriteLog(LogLevel.Error, message, source);
        }

        public static void Error(Exception ex, string source = "Unknown")
        {
            WriteLog(LogLevel.Error, $"{ex.Message}\n{ex.StackTrace}", source);
        }

        public static void Fatal(string message, string source = "Unknown")
        {
            WriteLog(LogLevel.Fatal, message, source);
        }

        public static void Fatal(Exception ex, string source = "Unknown")
        {
            WriteLog(LogLevel.Fatal, $"{ex.Message}\n{ex.StackTrace}", source);
        }

        private static void WriteLog(LogLevel level, string message, string source)
        {
            lock (_lock)
            {
                try
                {
                    string fileName = $"Log_{DateTime.Now:yyyyMMdd}.txt";
                    string filePath = Path.Combine(_logDirectory, fileName);

                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level.ToString().PadRight(7)}] [{source}] {message}\n";

                    using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
                    {
                        writer.Write(logEntry);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"日志写入失败: {ex.Message}");
                }
            }
        }

        public static string GetLogDirectory()
        {
            return _logDirectory;
        }

        public static string GetLatestLogFilePath()
        {
            string fileName = $"Log_{DateTime.Now:yyyyMMdd}.txt";
            return Path.Combine(_logDirectory, fileName);
        }

        public static void LogMethodEntry(string methodName, string source = "Unknown")
        {
            Debug($"进入方法: {methodName}", source);
        }

        public static void LogMethodExit(string methodName, string source = "Unknown")
        {
            Debug($"退出方法: {methodName}", source);
        }

        public static void LogDatabaseOperation(string operation, string tableName, int affectedRows = 0, string source = "DbService")
        {
            Info($"数据库操作: {operation} 表:{tableName} 影响行数:{affectedRows}", source);
        }

        public static void LogExportOperation(string exportType, string filePath, int recordCount, string source = "ExportService")
        {
            Info($"导出操作: {exportType} 路径:{filePath} 记录数:{recordCount}", source);
        }

        public static void LogImportOperation(string importType, string filePath, int recordCount, string source = "ImportService")
        {
            Info($"导入操作: {importType} 路径:{filePath} 记录数:{recordCount}", source);
        }
    }
}