using System;
using System.IO;

namespace FactoryProductManager.Services
{
    /// <summary>
    /// 数据库管理器 - 支持全局物料库和项目数据库
    /// </summary>
    public static class DatabaseManager
    {
        private static string? _globalMaterialDbPath;
        private static string? _projectDbPath;
        private static bool _initialized = false;

        /// <summary>
        /// 全局物料数据库路径
        /// </summary>
        public static string GlobalMaterialDbPath
        {
            get
            {
                EnsureInitialized();
                return _globalMaterialDbPath;
            }
            set => _globalMaterialDbPath = value;
        }

        /// <summary>
        /// 项目数据库路径
        /// </summary>
        public static string ProjectDbPath
        {
            get
            {
                EnsureInitialized();
                return _projectDbPath;
            }
            set => _projectDbPath = value;
        }

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        public static string GetConnectionString(string dbPath)
        {
            return $"Data Source={dbPath};Version=3;BusyTimeout=3000;";
        }

        /// <summary>
        /// 初始化数据库路径
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized) return;

            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", "..", ".."));
            string dbFolder = Path.Combine(projectDirectory, "FactoryProductManager");

            // 优先使用分库后的数据库
            string globalDb = Path.Combine(dbFolder, "GlobalMaterialDB.db");
            string projectDb = Path.Combine(dbFolder, "ProjectDB.db");

            // 如果分库后的数据库不存在，回退到原始数据库
            if (File.Exists(globalDb))
            {
                _globalMaterialDbPath = globalDb;
            }
            else
            {
                _globalMaterialDbPath = Path.Combine(dbFolder, "FactoryProductDB.db");
            }

            if (File.Exists(projectDb))
            {
                _projectDbPath = projectDb;
            }
            else
            {
                _projectDbPath = Path.Combine(dbFolder, "FactoryProductDB.db");
            }

            _initialized = true;
        }

        /// <summary>
        /// 重新初始化（用于测试或切换数据库）
        /// </summary>
        public static void ReInitialize()
        {
            _initialized = false;
            EnsureInitialized();
        }
    }

    /// <summary>
    /// 数据库类型枚举
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// 全局物料库
        /// </summary>
        GlobalMaterial,

        /// <summary>
        /// 项目数据库
        /// </summary>
        Project
    }
}
