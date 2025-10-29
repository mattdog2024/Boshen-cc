using System;
using System.Collections.Generic;

namespace BoshenCC.Models
{
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 获取错误信息字符串
        /// </summary>
        /// <returns>错误信息</returns>
        public string GetErrorMessage()
        {
            if (Errors == null || Errors.Count == 0)
                return string.Empty;

            return string.Join("; ", Errors);
        }
    }
}