using System;
using System.IO;
using System.Threading.Tasks;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;
using Newtonsoft.Json;

namespace BoshenCC.Services.Implementations
{
    /// <summary>
    /// 用户设置服务实现
    /// 提供配置的保存、加载、导入、导出等功能
    /// </summary>
    public class UserSettingsService : IUserSettingsService
    {
        #region 字段

        private readonly ILogService _logService;
        private readonly string _defaultSettingsPath;
        private readonly string _backupSettingsPath;
        private UserSettings _cachedSettings;
        private readonly object _lockObject = new object();

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        public UserSettingsService(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));

            // 设置默认文件路径
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BoshenCC"
            );

            _defaultSettingsPath = Path.Combine(appDataPath, "settings.json");
            _backupSettingsPath = Path.Combine(appDataPath, "settings.backup.json");

            // 确保目录存在
            EnsureDirectoryExists(appDataPath);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载用户设置
        /// </summary>
        /// <param name="filePath">设置文件路径，为null时使用默认路径</param>
        /// <returns>用户设置</returns>
        public async Task<UserSettings> LoadSettingsAsync(string filePath = null)
        {
            var settingsPath = filePath ?? _defaultSettingsPath;

            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        _logService.LogInfo($"正在加载设置文件: {settingsPath}");

                        if (!File.Exists(settingsPath))
                        {
                            _logService.LogInfo("设置文件不存在，使用默认设置");
                            var defaultSettings = CreateDefaultSettings();
                            SaveSettingsSync(defaultSettings, settingsPath);
                            return defaultSettings;
                        }

                        // 尝试读取主要设置文件
                        var json = File.ReadAllText(settingsPath);
                        var settings = JsonConvert.DeserializeObject<UserSettings>(json);

                        if (settings == null)
                        {
                            _logService.LogWarning("设置文件格式错误，尝试使用备份文件");
                            return LoadBackupSettings();
                        }

                        // 验证设置
                        var validationResult = ValidateSettings(settings);
                        if (!validationResult.IsValid)
                        {
                            _logService.LogWarning($"设置验证失败: {string.Join(", ", validationResult.Errors)}");
                            _logService.LogInfo("使用备份设置或默认设置");
                            return LoadBackupSettings();
                        }

                        _cachedSettings = settings;
                        _logService.LogInfo("设置加载成功");
                        return settings;
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"加载设置时发生错误: {ex.Message}");
                        _logService.LogInfo("使用默认设置");
                        return CreateDefaultSettings();
                    }
                }
            });
        }

        /// <summary>
        /// 保存用户设置
        /// </summary>
        /// <param name="settings">用户设置</param>
        /// <param name="filePath">设置文件路径，为null时使用默认路径</param>
        /// <returns>保存是否成功</returns>
        public async Task<bool> SaveSettingsAsync(UserSettings settings, string filePath = null)
        {
            var settingsPath = filePath ?? _defaultSettingsPath;

            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        _logService.LogInfo($"正在保存设置到文件: {settingsPath}");

                        // 验证设置
                        var validationResult = ValidateSettings(settings);
                        if (!validationResult.IsValid)
                        {
                            _logService.LogError($"设置验证失败，无法保存: {string.Join(", ", validationResult.Errors)}");
                            return false;
                        }

                        // 创建备份
                        CreateBackup();

                        // 保存设置
                        SaveSettingsSync(settings, settingsPath);

                        _cachedSettings = settings;
                        _logService.LogInfo("设置保存成功");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"保存设置时发生错误: {ex.Message}");
                        return false;
                    }
                }
            });
        }

        /// <summary>
        /// 导入设置
        /// </summary>
        /// <param name="filePath">导入文件路径</param>
        /// <returns>导入的用户设置</returns>
        public async Task<UserSettings> ImportSettingsAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("设置文件不存在", filePath);

            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        _logService.LogInfo($"正在从文件导入设置: {filePath}");

                        var json = File.ReadAllText(filePath);
                        var settings = JsonConvert.DeserializeObject<UserSettings>(json);

                        if (settings == null)
                        {
                            throw new InvalidOperationException("导入的设置文件格式无效");
                        }

                        // 验证导入的设置
                        var validationResult = ValidateSettings(settings);
                        if (!validationResult.IsValid)
                        {
                            throw new InvalidOperationException($"导入的设置验证失败: {string.Join(", ", validationResult.Errors)}");
                        }

                        _logService.LogInfo("设置导入成功");
                        return settings;
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"导入设置时发生错误: {ex.Message}");
                        throw;
                    }
                }
            });
        }

        /// <summary>
        /// 导出设置
        /// </summary>
        /// <param name="settings">要导出的用户设置</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出是否成功</returns>
        public async Task<bool> ExportSettingsAsync(UserSettings settings, string filePath)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        _logService.LogInfo($"正在导出设置到文件: {filePath}");

                        // 验证设置
                        var validationResult = ValidateSettings(settings);
                        if (!validationResult.IsValid)
                        {
                            throw new InvalidOperationException($"导出的设置验证失败: {string.Join(", ", validationResult.Errors)}");
                        }

                        // 确保目录存在
                        var directory = Path.GetDirectoryName(filePath);
                        EnsureDirectoryExists(directory);

                        // 创建导出信息
                        var exportData = new
                        {
                            Settings = settings,
                            ExportInfo = new
                            {
                                ExportTime = DateTime.Now,
                                Version = "1.0",
                                Application = "BoshenCC",
                                Description = "波神算法计算器设置文件"
                            }
                        };

                        // 保存到文件
                        var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                        File.WriteAllText(filePath, json);

                        _logService.LogInfo("设置导出成功");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"导出设置时发生错误: {ex.Message}");
                        return false;
                    }
                }
            });
        }

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        /// <returns>重置后的默认设置</returns>
        public async Task<UserSettings> ResetToDefaultsAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        _logService.LogInfo("正在重置设置为默认值");

                        var defaultSettings = CreateDefaultSettings();

                        // 保存默认设置
                        SaveSettingsSync(defaultSettings, _defaultSettingsPath);

                        _cachedSettings = defaultSettings;
                        _logService.LogInfo("设置重置成功");
                        return defaultSettings;
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"重置设置时发生错误: {ex.Message}");
                        throw;
                    }
                }
            });
        }

        /// <summary>
        /// 获取缓存的设置
        /// </summary>
        /// <returns>缓存的用户设置，如果没有缓存则返回null</returns>
        public UserSettings GetCachedSettings()
        {
            lock (_lockObject)
            {
                return _cachedSettings;
            }
        }

        /// <summary>
        /// 验证设置
        /// </summary>
        /// <param name="settings">要验证的用户设置</param>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateSettings(UserSettings settings)
        {
            if (settings == null)
            {
                return new ValidationResult { Errors = { "设置对象不能为null" } };
            }

            var result = new ValidationResult();

            // 验证算法设置
            if (settings.AlgorithmSettings == null)
            {
                result.AddError("算法设置不能为null");
            }
            else
            {
                ValidateAlgorithmSettings(settings.AlgorithmSettings, result);
            }

            // 验证显示设置
            if (settings.DisplaySettings == null)
            {
                result.AddError("显示设置不能为null");
            }
            else
            {
                ValidateDisplaySettings(settings.DisplaySettings, result);
            }

            // 验证用户偏好设置
            if (settings.UserPreferences == null)
            {
                result.AddError("用户偏好设置不能为null");
            }
            else
            {
                ValidateUserPreferences(settings.UserPreferences, result);
            }

            // 验证应用设置
            if (settings.AppSettings == null)
            {
                result.AddError("应用设置不能为null");
            }
            else
            {
                ValidateAppSettings(settings.AppSettings, result);
            }

            return result;
        }

        /// <summary>
        /// 获取默认设置文件路径
        /// </summary>
        /// <returns>默认设置文件路径</returns>
        public string GetDefaultSettingsPath()
        {
            return _defaultSettingsPath;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 创建默认设置
        /// </summary>
        /// <returns>默认用户设置</returns>
        private UserSettings CreateDefaultSettings()
        {
            return new UserSettings();
        }

        /// <summary>
        /// 同步保存设置
        /// </summary>
        /// <param name="settings">用户设置</param>
        /// <param name="filePath">文件路径</param>
        private void SaveSettingsSync(UserSettings settings, string filePath)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 创建备份
        /// </summary>
        private void CreateBackup()
        {
            try
            {
                if (File.Exists(_defaultSettingsPath))
                {
                    File.Copy(_defaultSettingsPath, _backupSettingsPath, true);
                    _logService.LogDebug("设置备份已创建");
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"创建设置备份时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载备份设置
        /// </summary>
        /// <returns>备份设置或默认设置</returns>
        private UserSettings LoadBackupSettings()
        {
            try
            {
                if (File.Exists(_backupSettingsPath))
                {
                    var json = File.ReadAllText(_backupSettingsPath);
                    var settings = JsonConvert.DeserializeObject<UserSettings>(json);

                    if (settings != null)
                    {
                        _logService.LogInfo("成功加载备份设置");
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"加载备份设置时发生错误: {ex.Message}");
            }

            // 如果备份也无法加载，返回默认设置
            _logService.LogInfo("备份设置无效，使用默认设置");
            return CreateDefaultSettings();
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        /// <param name="path">目录路径</param>
        private void EnsureDirectoryExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 验证算法设置
        /// </summary>
        /// <param name="settings">算法设置</param>
        /// <param name="result">验证结果</param>
        private void ValidateAlgorithmSettings(AlgorithmSettings settings, ValidationResult result)
        {
            if (settings.PriceThreshold < 0.001m || settings.PriceThreshold > 1.0m)
            {
                result.AddError("价格阈值必须在0.001-1.0之间");
            }

            if (settings.PricePrecision < 0 || settings.PricePrecision > 8)
            {
                result.AddError("价格精度必须在0-8之间");
            }

            if (settings.MaxPredictionLines <= 0 || settings.MaxPredictionLines > 20)
            {
                result.AddError("预测线数量必须在1-20之间");
            }

            if (settings.Tolerance < 0.0001m || settings.Tolerance > 0.1m)
            {
                result.AddError("计算容差必须在0.0001-0.1之间");
            }
        }

        /// <summary>
        /// 验证显示设置
        /// </summary>
        /// <param name="settings">显示设置</param>
        /// <param name="result">验证结果</param>
        private void ValidateDisplaySettings(DisplaySettings settings, ValidationResult result)
        {
            if (settings.LineWidth < 1 || settings.LineWidth > 10)
            {
                result.AddError("线条宽度必须在1-10之间");
            }

            if (settings.FontSize < 8 || settings.FontSize > 48)
            {
                result.AddError("字体大小必须在8-48之间");
            }

            if (string.IsNullOrEmpty(settings.Theme))
            {
                result.AddError("主题不能为空");
            }

            if (string.IsNullOrEmpty(settings.Language))
            {
                result.AddError("语言不能为空");
            }
        }

        /// <summary>
        /// 验证用户偏好设置
        /// </summary>
        /// <param name="settings">用户偏好设置</param>
        /// <param name="result">验证结果</param>
        private void ValidateUserPreferences(UserPreferences settings, ValidationResult result)
        {
            if (settings.AutoSaveInterval < 1 || settings.AutoSaveInterval > 60)
            {
                result.AddError("自动保存间隔必须在1-60分钟之间");
            }
        }

        /// <summary>
        /// 验证应用设置
        /// </summary>
        /// <param name="settings">应用设置</param>
        /// <param name="result">验证结果</param>
        private void ValidateAppSettings(AppSettings settings, ValidationResult result)
        {
            if (settings.WindowSettings == null)
            {
                result.AddError("窗口设置不能为null");
                return;
            }

            if (settings.WindowSettings.Width < 400 || settings.WindowSettings.Width > 4000)
            {
                result.AddError("窗口宽度必须在400-4000之间");
            }

            if (settings.WindowSettings.Height < 300 || settings.WindowSettings.Height > 3000)
            {
                result.AddError("窗口高度必须在300-3000之间");
            }

            if (settings.LogSettings == null)
            {
                result.AddError("日志设置不能为null");
                return;
            }

            if (settings.LogSettings.MaxFileSizeMB < 1 || settings.LogSettings.MaxFileSizeMB > 1000)
            {
                result.AddError("日志文件大小限制必须在1-1000MB之间");
            }

            if (settings.LogSettings.MaxFileCount < 1 || settings.LogSettings.MaxFileCount > 50)
            {
                result.AddError("日志文件数量限制必须在1-50之间");
            }
        }

        #endregion
    }

    #region 接口定义

    /// <summary>
    /// 用户设置服务接口
    /// </summary>
    public interface IUserSettingsService
    {
        /// <summary>
        /// 加载用户设置
        /// </summary>
        /// <param name="filePath">设置文件路径，为null时使用默认路径</param>
        /// <returns>用户设置</returns>
        Task<UserSettings> LoadSettingsAsync(string filePath = null);

        /// <summary>
        /// 保存用户设置
        /// </summary>
        /// <param name="settings">用户设置</param>
        /// <param name="filePath">设置文件路径，为null时使用默认路径</param>
        /// <returns>保存是否成功</returns>
        Task<bool> SaveSettingsAsync(UserSettings settings, string filePath = null);

        /// <summary>
        /// 导入设置
        /// </summary>
        /// <param name="filePath">导入文件路径</param>
        /// <returns>导入的用户设置</returns>
        Task<UserSettings> ImportSettingsAsync(string filePath);

        /// <summary>
        /// 导出设置
        /// </summary>
        /// <param name="settings">要导出的用户设置</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出是否成功</returns>
        Task<bool> ExportSettingsAsync(UserSettings settings, string filePath);

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        /// <returns>重置后的默认设置</returns>
        Task<UserSettings> ResetToDefaultsAsync();

        /// <summary>
        /// 获取缓存的设置
        /// </summary>
        /// <returns>缓存的用户设置，如果没有缓存则返回null</returns>
        UserSettings GetCachedSettings();

        /// <summary>
        /// 验证设置
        /// </summary>
        /// <param name="settings">要验证的用户设置</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateSettings(UserSettings settings);

        /// <summary>
        /// 获取默认设置文件路径
        /// </summary>
        /// <returns>默认设置文件路径</returns>
        string GetDefaultSettingsPath();
    }

    #endregion
}