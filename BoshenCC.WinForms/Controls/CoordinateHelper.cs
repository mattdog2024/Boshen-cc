using System;
using System.Drawing;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 坐标转换和价格计算工具类
    /// 处理图像坐标与价格之间的转换
    /// </summary>
    public class CoordinateHelper
    {
        #region 私有字段

        private PriceRange _priceRange;
        private Rectangle _imageArea;
        private double _priceScale;
        private double _pixelsPerPrice;

        #endregion

        #region 构造函数

        public CoordinateHelper()
        {
            _priceRange = new PriceRange(0, 0);
            _imageArea = Rectangle.Empty;
            CalculateScale();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 价格范围
        /// </summary>
        public PriceRange PriceRange
        {
            get => _priceRange;
            set
            {
                _priceRange = value ?? new PriceRange(0, 0);
                CalculateScale();
            }
        }

        /// <summary>
        /// 图像显示区域
        /// </summary>
        public Rectangle ImageArea
        {
            get => _imageArea;
            set
            {
                _imageArea = value;
                CalculateScale();
            }
        }

        /// <summary>
        /// 价格缩放比例
        /// </summary>
        public double PriceScale => _priceScale;

        /// <summary>
        /// 每个价格单位对应的像素数
        /// </summary>
        public double PixelsPerPrice => _pixelsPerPrice;

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置价格范围和图像区域
        /// </summary>
        /// <param name="minPrice">最低价格</param>
        /// <param name="maxPrice">最高价格</param>
        /// <param name="imageArea">图像显示区域</param>
        public void SetPriceRange(double minPrice, double maxPrice, Rectangle imageArea)
        {
            _priceRange = new PriceRange(minPrice, maxPrice);
            _imageArea = imageArea;
            CalculateScale();
        }

        /// <summary>
        /// 将Y坐标转换为价格
        /// </summary>
        /// <param name="y">Y坐标</param>
        /// <returns>对应的价格</returns>
        public double YToPrice(int y)
        {
            if (_imageArea.Height == 0 || _priceRange.Range == 0)
                return _priceRange.MinPrice;

            // 计算相对位置 (0.0 - 1.0)
            double relativeY = (double)(y - _imageArea.Top) / _imageArea.Height;

            // 限制在有效范围内
            relativeY = Math.Max(0, Math.Min(1, relativeY));

            // Y坐标向下增加，价格向上减少，所以需要反转
            return _priceRange.MaxPrice - (relativeY * _priceRange.Range);
        }

        /// <summary>
        /// 将价格转换为Y坐标
        /// </summary>
        /// <param name="price">价格</param>
        /// <returns>对应的Y坐标</returns>
        public int PriceToY(double price)
        {
            if (_priceRange.Range == 0)
                return _imageArea.Top + _imageArea.Height / 2;

            // 计算价格的相对位置 (0.0 - 1.0)
            double relativePrice = (price - _priceRange.MinPrice) / _priceRange.Range;

            // 限制在有效范围内
            relativePrice = Math.Max(0, Math.Min(1, relativePrice));

            // 价格向上增加，Y坐标向下减少，所以需要反转
            return _imageArea.Top + (int)((1 - relativePrice) * _imageArea.Height);
        }

        /// <summary>
        /// 检查坐标是否在有效图像区域内
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>是否在有效区域内</returns>
        public bool IsValidCoordinate(int x, int y)
        {
            return _imageArea.Contains(x, y);
        }

        /// <summary>
        /// 检查价格是否在有效范围内
        /// </summary>
        /// <param name="price">价格</param>
        /// <returns>是否在有效范围内</returns>
        public bool IsValidPrice(double price)
        {
            return price >= _priceRange.MinPrice && price <= _priceRange.MaxPrice;
        }

        /// <summary>
        /// 格式化价格显示
        /// </summary>
        /// <param name="price">价格</param>
        /// <param name="decimalPlaces">小数位数</param>
        /// <returns>格式化的价格字符串</returns>
        public string FormatPrice(double price, int decimalPlaces = 2)
        {
            return price.ToString($"F{decimalPlaces}");
        }

        /// <summary>
        /// 计算两个价格之间的像素距离
        /// </summary>
        /// <param name="price1">价格1</param>
        /// <param name="price2">价格2</param>
        /// <returns>像素距离</returns>
        public double GetPixelDistance(double price1, double price2)
        {
            return Math.Abs(PriceToY(price1) - PriceToY(price2));
        }

        /// <summary>
        /// 获取指定Y坐标的价格信息（用于工具提示）
        /// </summary>
        /// <param name="y">Y坐标</param>
        /// <returns>价格信息字符串</returns>
        public string GetPriceTooltip(int y)
        {
            double price = YToPrice(y);
            return $"价格: {FormatPrice(price)}";
        }

        /// <summary>
        /// 重置坐标转换器
        /// </summary>
        public void Reset()
        {
            _priceRange = new PriceRange(0, 0);
            _imageArea = Rectangle.Empty;
            CalculateScale();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 计算缩放比例
        /// </summary>
        private void CalculateScale()
        {
            if (_imageArea.Height > 0 && _priceRange.Range > 0)
            {
                _pixelsPerPrice = _imageArea.Height / _priceRange.Range;
                _priceScale = _priceRange.Range / _imageArea.Height;
            }
            else
            {
                _pixelsPerPrice = 1.0;
                _priceScale = 1.0;
            }
        }

        #endregion
    }
}