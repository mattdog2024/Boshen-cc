using System;

namespace BoshenCC.Models
{
    /// <summary>
    /// 字符识别结果
    /// </summary>
    public class RecognitionResult
    {
        /// <summary>
        /// 是否成功识别
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 识别出的文本
        /// </summary>
        public string RecognizedText { get; set; }

        /// <summary>
        /// 识别置信度 (0-1)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 处理耗时（毫秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// 识别时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 识别到的字符列表
        /// </summary>
        public RecognizedCharacter[] Characters { get; set; } = new RecognizedCharacter[0];
    }

    /// <summary>
    /// 识别到的单个字符
    /// </summary>
    public class RecognizedCharacter
    {
        /// <summary>
        /// 字符内容
        /// </summary>
        public string Character { get; set; }

        /// <summary>
        /// 置信度
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// X坐标
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Y坐标
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public int Height { get; set; }
    }
}