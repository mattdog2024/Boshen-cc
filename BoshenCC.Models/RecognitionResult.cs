using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace BoshenCC.Models
{
    /// <summary>
    /// 字符识别结果
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class RecognitionResult
    {
        /// <summary>
        /// 是否成功识别
        /// </summary>
        [JsonProperty]
        public bool Success { get; set; }

        /// <summary>
        /// 识别出的文本
        /// </summary>
        [JsonProperty]
        [StringLength(10000, ErrorMessage = "识别文本长度不能超过10000个字符")]
        public string RecognizedText { get; set; }

        /// <summary>
        /// 识别置信度 (0-1)
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "置信度必须在0-1之间")]
        public double Confidence { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        [JsonProperty]
        [StringLength(500, ErrorMessage = "错误信息长度不能超过500个字符")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 处理耗时（毫秒）
        /// </summary>
        [JsonProperty]
        [Range(0, long.MaxValue, ErrorMessage = "处理时间不能为负数")]
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// 识别时间戳
        /// </summary>
        [JsonProperty]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 识别到的字符列表
        /// </summary>
        [JsonProperty]
        public List<RecognizedCharacter> Characters { get; set; } = new List<RecognizedCharacter>();

        /// <summary>
        /// 图像处理路径
        /// </summary>
        [JsonProperty]
        public string ImagePath { get; set; }

        /// <summary>
        /// 识别引擎类型
        /// </summary>
        [JsonProperty]
        public string EngineType { get; set; }

        /// <summary>
        /// 识别语言
        /// </summary>
        [JsonProperty]
        public string Language { get; set; }

        /// <summary>
        /// 验证模型数据
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateModel()
        {
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(this, context, results, true);

            return new ValidationResult
            {
                IsValid = isValid,
                Errors = results.ConvertAll(r => r.ErrorMessage)
            };
        }

        /// <summary>
        /// 转换为JSON字符串
        /// </summary>
        /// <returns>JSON字符串</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// 从JSON字符串创建实例
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>RecognitionResult实例</returns>
        public static RecognitionResult FromJson(string json)
        {
            return JsonConvert.DeserializeObject<RecognitionResult>(json);
        }

        /// <summary>
        /// 创建成功的识别结果
        /// </summary>
        /// <param name="text">识别文本</param>
        /// <param name="confidence">置信度</param>
        /// <param name="processingTime">处理时间</param>
        /// <returns>成功的结果</returns>
        public static RecognitionResult CreateSuccess(string text, double confidence, long processingTime)
        {
            return new RecognitionResult
            {
                Success = true,
                RecognizedText = text,
                Confidence = confidence,
                ProcessingTimeMs = processingTime,
                Timestamp = DateTime.Now
            };
        }

        /// <summary>
        /// 创建失败的识别结果
        /// </summary>
        /// <param name="error">错误信息</param>
        /// <param name="processingTime">处理时间</param>
        /// <returns>失败的结果</returns>
        public static RecognitionResult CreateFailure(string error, long processingTime)
        {
            return new RecognitionResult
            {
                Success = false,
                ErrorMessage = error,
                Confidence = 0.0,
                ProcessingTimeMs = processingTime,
                Timestamp = DateTime.Now
            };
        }
    }
}

    /// <summary>
    /// 识别到的单个字符
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class RecognizedCharacter
    {
        /// <summary>
        /// 字符内容
        /// </summary>
        [JsonProperty]
        [StringLength(1, ErrorMessage = "单个字符长度不能超过1")]
        public string Character { get; set; }

        /// <summary>
        /// 置信度
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "置信度必须在0-1之间")]
        public double Confidence { get; set; }

        /// <summary>
        /// X坐标
        /// </summary>
        [JsonProperty]
        [Range(0, int.MaxValue, ErrorMessage = "X坐标不能为负数")]
        public int X { get; set; }

        /// <summary>
        /// Y坐标
        /// </summary>
        [JsonProperty]
        [Range(0, int.MaxValue, ErrorMessage = "Y坐标不能为负数")]
        public int Y { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        [JsonProperty]
        [Range(1, int.MaxValue, ErrorMessage = "宽度必须大于0")]
        public int Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        [JsonProperty]
        [Range(1, int.MaxValue, ErrorMessage = "高度必须大于0")]
        public int Height { get; set; }

        /// <summary>
        /// 获取字符的边界矩形
        /// </summary>
        /// <returns>边界矩形</returns>
        public System.Drawing.Rectangle GetBounds()
        {
            return new System.Drawing.Rectangle(X, Y, Width, Height);
        }

        /// <summary>
        /// 获取字符的中心点
        /// </summary>
        /// <returns>中心点坐标</returns>
        public System.Drawing.Point GetCenter()
        {
            return new System.Drawing.Point(X + Width / 2, Y + Height / 2);
        }
    }

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
    }
}
