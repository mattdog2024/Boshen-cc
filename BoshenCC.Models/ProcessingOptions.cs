namespace BoshenCC.Models
{
    /// <summary>
    /// 图像处理选项
    /// </summary>
    public class ProcessingOptions
    {
        /// <summary>
        /// 是否启用灰度化
        /// </summary>
        public bool EnableGrayscale { get; set; } = true;

        /// <summary>
        /// 是否启用二值化
        /// </summary>
        public bool EnableThreshold { get; set; } = true;

        /// <summary>
        /// 二值化阈值
        /// </summary>
        public int ThresholdValue { get; set; } = 128;

        /// <summary>
        /// 是否启用降噪
        /// </summary>
        public bool EnableDenoising { get; set; } = true;

        /// <summary>
        /// 是否启用边缘检测
        /// </summary>
        public bool EnableEdgeDetection { get; set; } = false;

        /// <summary>
        /// 缩放比例
        /// </summary>
        public double ScaleFactor { get; set; } = 1.0;

        /// <summary>
        /// OCR引擎类型
        /// </summary>
        public OcrEngineType OcrEngine { get; set; } = OcrEngineType.Tesseract;

        /// <summary>
        /// OCR语言
        /// </summary>
        public string OcrLanguage { get; set; } = "chi_sim+eng";

        /// <summary>
        /// 预处理质量
        /// </summary>
        public ProcessingQuality Quality { get; set; } = ProcessingQuality.Medium;
    }

    /// <summary>
    /// OCR引擎类型
    /// </summary>
    public enum OcrEngineType
    {
        /// <summary>
        /// Tesseract引擎
        /// </summary>
        Tesseract,

        /// <summary>
        /// 自定义引擎
        /// </summary>
        Custom
    }

    /// <summary>
    /// 处理质量
    /// </summary>
    public enum ProcessingQuality
    {
        /// <summary>
        /// 低质量（快速）
        /// </summary>
        Low,

        /// <summary>
        /// 中等质量（平衡）
        /// </summary>
        Medium,

        /// <summary>
        /// 高质量（精确）
        /// </summary>
        High
    }
}