using System;
using System.IO;
using System.Reflection;

namespace FactoryProductManager.Services
{
    public static class LogService
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly object LockObj = new object();
        private static bool _isInitialized = false;

        static LogService()
        {
            Initialize();
        }

        private static void Initialize()
        {
            if (_isInitialized) return;
            
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
                
                string todayLogFile = Path.Combine(LogDirectory, $"Log_{DateTime.Now:yyyyMMdd}.txt");
                if (File.Exists(todayLogFile))
                {
                    File.Delete(todayLogFile);
                }
                
                _isInitialized = true;
                WriteLog("INFO", "日志服务初始化完成，日志文件已重置");
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText("Log_Error.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] 日志服务初始化失败: {ex.Message}\n", System.Text.Encoding.UTF8);
                }
                catch { }
            }
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Warning(string message)
        {
            WriteLog("WARNING", message);
        }

        public static void Error(string message)
        {
            WriteLog("ERROR", message);
        }

        public static void Error(Exception ex)
        {
            WriteLog("ERROR", $"Exception Type: {ex.GetType().Name}\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}\nSource: {ex.Source}");
        }

        public static void Error(string message, Exception ex)
        {
            WriteLog("ERROR", $"{message}\nException Type: {ex.GetType().Name}\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}\nSource: {ex.Source}");
        }

        public static void Debug(string message)
        {
            WriteLog("DEBUG", message);
        }

        public static void LogApplicationStart()
        {
            WriteLog("INFO", "========================================");
            WriteLog("INFO", "应用程序启动");
            WriteLog("INFO", $"启动时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteLog("INFO", $"操作系统: {Environment.OSVersion}");
            WriteLog("INFO", $"CLR版本: {Environment.Version}");
            WriteLog("INFO", $"应用程序目录: {AppDomain.CurrentDomain.BaseDirectory}");
            WriteLog("INFO", $"程序集版本: {Assembly.GetExecutingAssembly().GetName().Version}");
            WriteLog("INFO", $"处理器数量: {Environment.ProcessorCount}");
            WriteLog("INFO", $"系统内存: {GetMemoryInfo()}");
            WriteLog("INFO", "========================================");
        }

        public static void LogApplicationExit()
        {
            WriteLog("INFO", "========================================");
            WriteLog("INFO", "应用程序退出");
            WriteLog("INFO", $"退出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteLog("INFO", "========================================");
        }

        public static void LogServiceInitialization(string serviceName)
        {
            WriteLog("INFO", $"正在初始化服务: {serviceName}");
        }

        public static void LogServiceInitialized(string serviceName)
        {
            WriteLog("INFO", $"服务初始化完成: {serviceName}");
        }

        public static void LogDatabaseConnection(string connectionString)
        {
            WriteLog("INFO", $"数据库连接字符串: {connectionString}");
        }

        public static void LogDatabaseQuery(string queryName, int rowCount, long executionTimeMs)
        {
            WriteLog("INFO", $"查询[{queryName}]: 返回 {rowCount} 条记录, 耗时 {executionTimeMs}ms");
        }

        public static void LogMethodEnter(string className, string methodName)
        {
            WriteLog("DEBUG", $"进入方法: {className}.{methodName}");
        }

        public static void LogMethodExit(string className, string methodName, long executionTimeMs)
        {
            WriteLog("DEBUG", $"退出方法: {className}.{methodName}, 耗时 {executionTimeMs}ms");
        }

        public static void LogViewModelCreation(string viewModelName)
        {
            WriteLog("INFO", $"创建ViewModel: {viewModelName}");
        }

        public static void LogViewLoading(string viewName)
        {
            WriteLog("INFO", $"正在加载视图: {viewName}");
        }

        public static void LogViewLoaded(string viewName)
        {
            WriteLog("INFO", $"视图加载完成: {viewName}");
        }

        private static string GetMemoryInfo()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                return $"已使用: {process.WorkingSet64 / 1024 / 1024} MB, 峰值: {process.PeakWorkingSet64 / 1024 / 1024} MB";
            }
            catch
            {
                return "无法获取内存信息";
            }
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                lock (LockObj)
                {
                    string logFileName = $"Log_{DateTime.Now:yyyyMMdd}.txt";
                    string logFilePath = Path.Combine(LogDirectory, logFileName);
                    
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}\n";
                    
                    File.AppendAllText(logFilePath, logEntry, System.Text.Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText("Log_Error_Fallback.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] 写入日志失败: {ex.Message}\n原始消息: {message}\n", System.Text.Encoding.UTF8);
                }
                catch { }
            }
        }
    }
}