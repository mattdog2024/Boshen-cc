using System;
using System.Drawing;
using System.Drawing.Imaging;
using BoshenCC.Models;

namespace BoshenCC.Core.Interfaces
{
    /// <summary>
    /// ͼ�������ӿ�
    /// </summary>
    public interface IImageProcessor
    {
        /// <summary>
        /// ����ͼ��
        /// </summary>
        /// <param name="image">����ͼ��</param>
        /// <param name="options">����ѡ��</param>
        /// <returns>������ͼ��</returns>
        Bitmap ProcessImage(Bitmap image, ProcessingOptions options = null);

        /// <summary>
        /// ʶ��ͼ���е��ַ�
        /// </summary>
        /// <param name="image">����ͼ��</param>
        /// <returns>ʶ����</returns>
        RecognitionResult RecognizeCharacters(Bitmap image);

        /// <summary>
        /// Ԥ����ͼ��
        /// </summary>
        /// <param name="image">����ͼ��</param>
        /// <param name="options">Ԥ����ѡ��</param>
        /// <returns>Ԥ������ͼ��</returns>
        Bitmap PreprocessImage(Bitmap image, ProcessingOptions options = null);

        /// <summary>
        /// �ҶȻ�ͼ��
        /// </summary>
        /// <param name="image">����ͼ��</param>
        /// <returns>�Ҷ�ͼ��</returns>
        Bitmap ConvertToGrayscale(Bitmap image);

        /// <summary>
        /// ��ֵ��ͼ��
        /// </summary>
        /// <param name="image">����ͼ��</param>
        /// <param name="threshold">��ֵ</param>
        /// <returns>��ֵ��ͼ��</returns>
        Bitmap ThresholdImage(Bitmap image, int threshold = 128);

        /// <summary>
        /// ���봦��
        /// </summary>
        /// <param name="image">����ͼ��</param>
        /// <returns>������ͼ��</returns>
        Bitmap DenoiseImage(Bitmap image);

        /// <summary>
        /// ��Ե���
        /// </summary>
        /// <param name="image">����ͼ��</param>
        /// <returns>��Եͼ��</returns>
        Bitmap DetectEdges(Bitmap image);

        /// <summary>
        /// ͼ������
        /// </summary>
        /// <param name="image">����ͼ��</param>
        /// <param name="scale">���ű���</param>
        /// <returns>���ź��ͼ��</returns>
        Bitmap ScaleImage(Bitmap image, double scale);

        /// <summary>
        /// �ü�ͼ��
        /// </summary>
        /// <param name="image">����ͼ��</param>
        /// <param name="rectangle">�ü�����</param>
        /// <returns>�ü����ͼ��</returns>
        Bitmap CropImage(Bitmap image, Rectangle rectangle);

        /// <summary>
        /// ��תͼ��
        /// </summary>
        /// <param name="image">����ͼ��</param>
        /// <param name="angle">��ת�Ƕ�</param>
        /// <returns>��ת���ͼ��</returns>
        Bitmap RotateImage(Bitmap image, float angle);

        /// <summary>
        /// ���ͼ���е�K����̬
        /// </summary>
        /// <param name="image">����ͼ��</param>
        /// <returns>K��ʶ����</returns>
        RecognitionResult DetectCandlestickPatterns(Bitmap image);
    }
}
