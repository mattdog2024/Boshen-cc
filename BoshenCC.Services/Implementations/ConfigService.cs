using System;
using System.IO;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;
using Newtonsoft.Json;

namespace BoshenCC.Services.Implementations
{
    /// <summary>
    /// 配置服务实现
    /// </summary>
    public class ConfigService : IConfigService
    {
        private readonly string _configPath;

        public ConfigService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "BoshenCC");

            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _configPath = Path.Combine(appFolder, "settings.json");
        }

        public AppSettings LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null && ValidateConfig(settings))
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                // 配置文件损坏或读取失败，使用默认配置
                Console.WriteLine($"加载配置失败，使用默认配置: {ex.Message}");
            }

            return ResetToDefault();
        }

        public void SaveConfig(AppSettings settings)
        {
            try
            {
                if (!ValidateConfig(settings))
                {
                    throw new ArgumentException("配置无效");
                }

                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存配置失败: {ex.Message}", ex);
            }
        }

        public string GetConfigPath()
        {
            return _configPath;
        }

        public AppSettings ResetToDefault()
        {
            var defaultSettings = new AppSettings();
            SaveConfig(defaultSettings);
            return defaultSettings;
        }

        public bool ValidateConfig(AppSettings settings)
        {
            if (settings == null)
                return false;

            if (settings.DefaultProcessingOptions == null)
                return false;

            if (settings.WindowSettings == null)
                return false;

            if (settings.LogSettings == null)
                return false;

            if (settings.HotkeySettings == null)
                return false;

            // 验证窗口设置
            if (settings.WindowSettings.Width <= 0 || settings.WindowSettings.Height <= 0)
                return false;

            // 验证日志设置
            if (string.IsNullOrEmpty(settings.LogSettings.LogFilePath))
                return false;

            if (settings.LogSettings.MaxFileSizeMB <= 0 || settings.LogSettings.MaxFileCount <= 0)
                return false;

            // 验证快捷键设置
            if (string.IsNullOrEmpty(settings.HotkeySettings.ScreenshotHotkey) ||
                string.IsNullOrEmpty(settings.HotkeySettings.RecognizeHotkey))
                return false;

            return true;
        }
    }
}