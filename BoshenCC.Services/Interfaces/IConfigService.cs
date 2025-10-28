using System;
using BoshenCC.Models;

namespace BoshenCC.Services.Interfaces
{
    /// <summary>
    /// 配置服务接口
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// 加载配置
        /// </summary>
        /// <returns>应用程序设置</returns>
        AppSettings LoadConfig();

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="settings">应用程序设置</param>
        void SaveConfig(AppSettings settings);

        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        /// <returns>配置文件路径</returns>
        string GetConfigPath();

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        /// <returns>默认设置</returns>
        AppSettings ResetToDefault();

        /// <summary>
        /// 验证配置有效性
        /// </summary>
        /// <param name="settings">设置</param>
        /// <returns>是否有效</returns>
        bool ValidateConfig(AppSettings settings);
    }
}