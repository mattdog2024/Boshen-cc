using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace BoshenCC.Models
{
    /// <summary>
    /// �ַ�ʶ����
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class RecognitionResult
    {
        /// <summary>
        /// �Ƿ�ɹ�ʶ��
        /// </summary>
        [JsonProperty]
        public bool Success { get; set; }

        /// <summary>
        /// ʶ������ı�
        /// </summary>
        [JsonProperty]
        [StringLength(10000, ErrorMessage = "ʶ���ı����Ȳ��ܳ���10000���ַ�")]
        public string RecognizedText { get; set; }

        /// <summary>
        /// ʶ�����Ŷ� (0-1)
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "���Ŷȱ�����0-1֮��")]
        public double Confidence { get; set; }

        /// <summary>
        /// ������Ϣ
        /// </summary>
        [JsonProperty]
        [StringLength(500, ErrorMessage = "������Ϣ���Ȳ��ܳ���500���ַ�")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// �����ʱ�����룩
        /// </summary>
        [JsonProperty]
        [Range(0, long.MaxValue, ErrorMessage = "����ʱ�䲻��Ϊ����")]
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// ʶ��ʱ���
        /// </summary>
        [JsonProperty]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// ʶ�𵽵��ַ��б�
        /// </summary>
        [JsonProperty]
        public List<RecognizedCharacter> Characters { get; set; } = new List<RecognizedCharacter>();

        /// <summary>
        /// ͼ����·��
        /// </summary>
        [JsonProperty]
        public string ImagePath { get; set; }

        /// <summary>
        /// ʶ����������
        /// </summary>
        [JsonProperty]
        public string EngineType { get; set; }

        /// <summary>
        /// ʶ������
        /// </summary>
        [JsonProperty]
        public string Language { get; set; }

        /// <summary>
        /// ��֤ģ������
        /// </summary>
        /// <returns>��֤���</returns>
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
        /// ת��ΪJSON�ַ���
        /// </summary>
        /// <returns>JSON�ַ���</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// ��JSON�ַ�������ʵ��
        /// </summary>
        /// <param name="json">JSON�ַ���</param>
        /// <returns>RecognitionResultʵ��</returns>
        public static RecognitionResult FromJson(string json)
        {
            return JsonConvert.DeserializeObject<RecognitionResult>(json);
        }

        /// <summary>
        /// �����ɹ���ʶ����
        /// </summary>
        /// <param name="text">ʶ���ı�</param>
        /// <param name="confidence">���Ŷ�</param>
        /// <param name="processingTime">����ʱ��</param>
        /// <returns>�ɹ��Ľ��</returns>
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
        /// ����ʧ�ܵ�ʶ����
        /// </summary>
        /// <param name="error">������Ϣ</param>
        /// <param name="processingTime">����ʱ��</param>
        /// <returns>ʧ�ܵĽ��</returns>
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
    /// ʶ�𵽵ĵ����ַ�
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class RecognizedCharacter
    {
        /// <summary>
        /// �ַ�����
        /// </summary>
        [JsonProperty]
        [StringLength(1, ErrorMessage = "�����ַ����Ȳ��ܳ���1")]
        public string Character { get; set; }

        /// <summary>
        /// ���Ŷ�
        /// </summary>
        [JsonProperty]
        [Range(0.0, 1.0, ErrorMessage = "���Ŷȱ�����0-1֮��")]
        public double Confidence { get; set; }

        /// <summary>
        /// X����
        /// </summary>
        [JsonProperty]
        [Range(0, int.MaxValue, ErrorMessage = "X���겻��Ϊ����")]
        public int X { get; set; }

        /// <summary>
        /// Y����
        /// </summary>
        [JsonProperty]
        [Range(0, int.MaxValue, ErrorMessage = "Y���겻��Ϊ����")]
        public int Y { get; set; }

        /// <summary>
        /// ���
        /// </summary>
        [JsonProperty]
        [Range(1, int.MaxValue, ErrorMessage = "��ȱ������0")]
        public int Width { get; set; }

        /// <summary>
        /// �߶�
        /// </summary>
        [JsonProperty]
        [Range(1, int.MaxValue, ErrorMessage = "�߶ȱ������0")]
        public int Height { get; set; }

        /// <summary>
        /// ��ȡ�ַ��ı߽����
        /// </summary>
        /// <returns>�߽����</returns>
        public System.Drawing.Rectangle GetBounds()
        {
            return new System.Drawing.Rectangle(X, Y, Width, Height);
        }

        /// <summary>
        /// ��ȡ�ַ������ĵ�
        /// </summary>
        /// <returns>���ĵ�����</returns>
        public System.Drawing.Point GetCenter()
        {
            return new System.Drawing.Point(X + Width / 2, Y + Height / 2);
        }
    }

    /// <summary>
    /// ��֤���
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// �Ƿ���Ч
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// �����б�
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }
}
