#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace FactoryProductManager.Services
{
    public static class LogService
    {
        private const int RetentionDays = 30;
        private static readonly object LockObj = new object();
        private static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name ?? "FactoryProductManager";
        private static readonly string FallbackLogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string SessionId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        private static string _logDirectory = FallbackLogDirectory;
        private static bool _isInitialized = false;
        private static bool _debugEnabled = true;

        public static string LogDirectory => _logDirectory;
        public static string CurrentLogFilePath => Path.Combine(LogDirectory, $"Log_{DateTime.Now:yyyyMMdd}.txt");
        public static string RootDebugLogPath
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 5 && !string.IsNullOrEmpty(baseDir); i++)
                {
                    baseDir = Path.GetDirectoryName(baseDir) ?? string.Empty;
                }
                return Path.Combine(baseDir, "debug.log");
            }
        }
        public static string CurrentSessionId => SessionId;
        public static bool DebugEnabled => _debugEnabled;

        static LogService()
        {
            Initialize();
        }

        private static void Initialize()
        {
            if (_isInitialized) return;

            lock (LockObj)
            {
                if (_isInitialized) return;

                try
                {
                    _logDirectory = ResolveLogDirectory();
                    Directory.CreateDirectory(_logDirectory);
                    _debugEnabled = ResolveDebugEnabled();

                    ClearRootDebugLog();
                    CleanupOldLogs();

                    _isInitialized = true;
                    WriteLogCore("INFO", $"日志服务初始化完成，日志目录: {_logDirectory}");
                    WriteLogCore("INFO", $"日志保留天数: {RetentionDays}");
                    WriteLogCore("INFO", $"调试日志开关: {_debugEnabled}");
                }
                catch (Exception ex)
                {
                    _logDirectory = FallbackLogDirectory;
                    try
                    {
                        Directory.CreateDirectory(_logDirectory);
                        File.AppendAllText(
                            Path.Combine(_logDirectory, "Log_Error.txt"),
                            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [ERROR] 日志服务初始化失败: {ex.Message}{Environment.NewLine}",
                            Encoding.UTF8);
                    }
                    catch
                    {
                    }
                }
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
            WriteLog("ERROR", BuildExceptionMessage(ex));
        }

        public static void Error(string message, Exception ex)
        {
            WriteLog("ERROR", $"{message}{Environment.NewLine}{BuildExceptionMessage(ex)}");
        }

        public static void Debug(string message)
        {
            if (!_debugEnabled)
            {
                return;
            }

            WriteLog("DEBUG", message);
        }

        public static void LogApplicationStart()
        {
            WriteLog("INFO", "========================================");
            WriteLog("INFO", "应用程序启动");
            WriteLog("INFO", $"会话标识: {SessionId}");
            WriteLog("INFO", $"启动时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteLog("INFO", $"操作系统: {Environment.OSVersion}");
            WriteLog("INFO", $"CLR版本: {Environment.Version}");
            WriteLog("INFO", $"应用程序目录: {AppDomain.CurrentDomain.BaseDirectory}");
            WriteLog("INFO", $"日志目录: {LogDirectory}");
            WriteLog("INFO", $"日志文件: {CurrentLogFilePath}");
            WriteLog("INFO", $"程序集版本: {Assembly.GetExecutingAssembly().GetName().Version}");
            WriteLog("INFO", $"进程ID: {Environment.ProcessId}");
            WriteLog("INFO", $"处理器数量: {Environment.ProcessorCount}");
            WriteLog("INFO", $"系统内存: {GetMemoryInfo()}");
            WriteLog("INFO", "========================================");
        }

        public static void LogApplicationExit()
        {
            WriteLog("INFO", "========================================");
            WriteLog("INFO", "应用程序退出");
            WriteLog("INFO", $"会话标识: {SessionId}");
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
            WriteLog("INFO", $"数据库连接已初始化: {SanitizeConnectionString(connectionString)}");
        }

        public static void LogDatabaseQuery(string queryName, int rowCount, long executionTimeMs)
        {
            WriteLog("INFO", $"查询[{queryName}]: 返回 {rowCount} 条记录, 耗时 {executionTimeMs}ms");
        }

        public static void LogMethodEnter(string className, string methodName)
        {
            Debug($"进入方法: {className}.{methodName}");
        }

        public static void LogMethodExit(string className, string methodName, long executionTimeMs)
        {
            Debug($"退出方法: {className}.{methodName}, 耗时 {executionTimeMs}ms");
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

        public static void OpenLogDirectory()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{LogDirectory}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                WriteLog("WARNING", $"打开日志目录失败: {ex.Message}");
            }
        }

        private static string ResolveLogDirectory()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrWhiteSpace(localAppData))
                {
                    return Path.Combine(localAppData, "YuchenInfoCenter", AppName, "Logs");
                }
            }
            catch
            {
            }

            return FallbackLogDirectory;
        }

        private static bool ResolveDebugEnabled()
        {
            string? value = Environment.GetEnvironmentVariable("FACTORY_PRODUCT_MANAGER_DEBUG_LOG");
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return !value.Equals("0", StringComparison.OrdinalIgnoreCase)
                && !value.Equals("false", StringComparison.OrdinalIgnoreCase)
                && !value.Equals("off", StringComparison.OrdinalIgnoreCase);
        }

        private static void ClearRootDebugLog()
        {
            try
            {
                string path = RootDebugLogPath;
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var directoryInfo = new DirectoryInfo(_logDirectory);
                if (!directoryInfo.Exists)
                {
                    return;
                }

                foreach (var file in directoryInfo.GetFiles("Log_*.txt"))
                {
                    if (file.LastWriteTime < DateTime.Now.AddDays(-RetentionDays))
                    {
                        file.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(
                        Path.Combine(_logDirectory, "Log_Cleanup_Error.txt"),
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [WARNING] 清理旧日志失败: {ex.Message}{Environment.NewLine}",
                        Encoding.UTF8);
                }
                catch
                {
                }
            }
        }

        private static string GetMemoryInfo()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                return $"已使用: {process.WorkingSet64 / 1024 / 1024} MB, 峰值: {process.PeakWorkingSet64 / 1024 / 1024} MB";
            }
            catch
            {
                return "无法获取内存信息";
            }
        }

        private static string BuildExceptionMessage(Exception ex)
        {
            string stackTrace = ex.StackTrace ?? "<null>";
            string source = ex.Source ?? "<null>";

            return $"Exception Type: {ex.GetType().FullName}{Environment.NewLine}" +
                   $"Message: {ex.Message}{Environment.NewLine}" +
                   $"Stack Trace: {stackTrace}{Environment.NewLine}" +
                   $"Source: {source}";
        }

        private static string SanitizeConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return "<empty>";
            }

            const string dataSourceKey = "Data Source=";
            int startIndex = connectionString.IndexOf(dataSourceKey, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
            {
                return "<redacted>";
            }

            int valueStart = startIndex + dataSourceKey.Length;
            int valueEnd = connectionString.IndexOf(';', valueStart);
            if (valueEnd < 0)
            {
                valueEnd = connectionString.Length;
            }

            string dataSource = connectionString.Substring(valueStart, valueEnd - valueStart).Trim();
            string fileName = string.IsNullOrWhiteSpace(dataSource) ? "<empty>" : Path.GetFileName(dataSource);
            return $"Data Source=<redacted:{fileName}>";
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                if (!_isInitialized)
                {
                    Initialize();
                }

                lock (LockObj)
                {
                    Directory.CreateDirectory(_logDirectory);
                    WriteLogCore(level, message);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Directory.CreateDirectory(FallbackLogDirectory);
                    File.AppendAllText(
                        Path.Combine(FallbackLogDirectory, "Log_Error_Fallback.txt"),
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [ERROR] 写入日志失败: {ex.Message}{Environment.NewLine}原始消息: {message}{Environment.NewLine}",
                        Encoding.UTF8);
                }
                catch
                {
                }
            }
        }

        private static void WriteLogCore(string level, string message)
        {
            string logEntry =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [Session:{SessionId}] [PID:{Environment.ProcessId}] [TID:{Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}";
            File.AppendAllText(CurrentLogFilePath, logEntry, Encoding.UTF8);

            string rootEntry =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
            try
            {
                File.AppendAllText(RootDebugLogPath, rootEntry, Encoding.UTF8);
            }
            catch
            {
            }
        }
    }
}
