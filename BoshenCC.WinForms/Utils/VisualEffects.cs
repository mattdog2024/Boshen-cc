using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace BoshenCC.WinForms.Utils
{
    /// <summary>
    /// 视觉效果工具类
    /// 提供各种视觉特效和动画效果的实现
    /// </summary>
    public static class VisualEffects
    {
        #region 颜色效果

        /// <summary>
        /// 应用渐变效果
        /// </summary>
        /// <param name="graphics">图形对象</param>
        /// <param name="rect">区域</param>
        /// <param name="startColor">起始颜色</param>
        /// <param name="endColor">结束颜色</param>
        /// <param name="mode">渐变模式</param>
        /// <param name="angle">渐变角度</param>
        public static void ApplyGradient(Graphics graphics, Rectangle rect, Color startColor, Color endColor,
            LinearGradientMode mode = LinearGradientMode.Vertical, float angle = 0f)
        {
            if (graphics == null || rect.IsEmpty)
                return;

            using var brush = new LinearGradientBrush(rect, startColor, endColor, mode)
            {
                WrapMode = WrapMode.TileFlipXY
            };

            if (angle != 0f)
            {
                brush.RotateTransform(angle);
            }

            graphics.FillRectangle(brush, rect);
        }

        /// <summary>
        /// 应用径向渐变效果
        /// </summary>
        /// <param name="graphics">图形对象</param>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <param name="centerColor">中心颜色</param>
        /// <param name="edgeColor">边缘颜色</param>
        public static void ApplyRadialGradient(Graphics graphics, PointF center, float radius, Color centerColor, Color edgeColor)
        {
            if (graphics == null || radius <= 0)
                return;

            var path = new GraphicsPath();
            path.AddEllipse(center.X - radius, center.Y - radius, radius * 2, radius * 2);

            using var brush = new PathGradientBrush(path)
            {
                CenterColor = centerColor,
                SurroundColors = new[] { edgeColor }
            };

            graphics.FillEllipse(brush, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            path.Dispose();
        }

        /// <summary>
        /// 调整颜色亮度
        /// </summary>
        /// <param name="color">原始颜色</param>
        /// <param name="factor">亮度因子（-1到1）</param>
        /// <returns>调整后的颜色</returns>
        public static Color AdjustBrightness(Color color, float factor)
        {
            factor = Math.Max(-1f, Math.Min(1f, factor));

            var r = Math.Max(0, Math.Min(255, color.R + (int)(255 * factor)));
            var g = Math.Max(0, Math.Min(255, color.G + (int)(255 * factor)));
            var b = Math.Max(0, Math.Min(255, color.B + (int)(255 * factor)));

            return Color.FromArgb(color.A, r, g, b);
        }

        /// <summary>
        /// 调整颜色饱和度
        /// </summary>
        /// <param name="color">原始颜色</param>
        /// <param name="factor">饱和度因子（0到2）</param>
        /// <returns>调整后的颜色</returns>
        public static Color AdjustSaturation(Color color, float factor)
        {
            factor = Math.Max(0f, Math.Min(2f, factor));

            var gray = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
            var r = Math.Max(0, Math.Min(255, gray + factor * (color.R - gray)));
            var g = Math.Max(0, Math.Min(255, gray + factor * (color.G - gray)));
            var b = Math.Max(0, Math.Min(255, gray + factor * (color.B - gray)));

            return Color.FromArgb(color.A, (int)r, (int)g, (int)b);
        }

        /// <summary>
        /// 创建颜色主题
        /// </summary>
        /// <param name="baseColor">基础颜色</param>
        /// <param name="count">颜色数量</param>
        /// <returns>颜色数组</returns>
        public static Color[] CreateColorTheme(Color baseColor, int count)
        {
            if (count <= 0)
                return new Color[0];

            var colors = new Color[count];
            var hue = GetHue(baseColor);
            var saturation = GetSaturation(baseColor);
            var brightness = GetBrightness(baseColor);

            for (int i = 0; i < count; i++)
            {
                var newHue = (hue + (i * 360f / count)) % 360;
                colors[i] = ColorFromHSV(newHue, saturation, brightness);
            }

            return colors;
        }

        #endregion

        #region 阴影和发光效果

        /// <summary>
        /// 绘制阴影效果
        /// </summary>
        /// <param name="graphics">图形对象</param>
        /// <param name="path">阴影路径</param>
        /// <param name="offset">偏移量</param>
        /// <param name="blurRadius">模糊半径</param>
        /// <param name="color">阴影颜色</param>
        public static void DrawShadow(Graphics graphics, GraphicsPath path, Point offset, int blurRadius, Color color)
        {
            if (graphics == null || path == null)
                return;

            // 创建阴影位图
            var bounds = path.GetBounds();
            var shadowRect = new Rectangle(
                (int)bounds.Left + offset.X - blurRadius,
                (int)bounds.Top + offset.Y - blurRadius,
                (int)bounds.Width + blurRadius * 2,
                (int)bounds.Height + blurRadius * 2
            );

            using var shadowBitmap = new Bitmap(shadowRect.Width, shadowRect.Height, PixelFormat.Format32bppArgb);
            using var shadowGraphics = Graphics.FromImage(shadowBitmap);

            // 绘制阴影形状
            shadowGraphics.TranslateTransform(-shadowRect.X, -shadowRect.Y);
            shadowGraphics.TranslateTransform(offset.X, offset.Y);
            shadowGraphics.FillPath(new SolidBrush(color), path);

            // 应用模糊效果
            ApplyGaussianBlur(shadowBitmap, blurRadius);

            // 绘制模糊后的阴影
            graphics.DrawImage(shadowBitmap, shadowRect.Location);
        }

        /// <summary>
        /// 绘制发光效果
        /// </summary>
        /// <param name="graphics">图形对象</param>
        /// <param name="path">发光路径</param>
        /// <param name="glowColor">发光颜色</param>
        /// <param name="glowSize">发光大小</param>
        /// <param name="intensity">发光强度（0-1）</param>
        public static void DrawGlow(Graphics graphics, GraphicsPath path, Color glowColor, int glowSize, float intensity = 1.0f)
        {
            if (graphics == null || path == null || glowSize <= 0)
                return;

            intensity = Math.Max(0f, Math.Min(1f, intensity));

            // 绘制多层发光效果
            for (int i = glowSize; i > 0; i--)
            {
                var alpha = (int)((1f - (float)i / glowSize) * intensity * glowColor.A);
                var layerColor = Color.FromArgb(alpha, glowColor.R, glowColor.G, glowColor.B);

                using var pen = new Pen(layerColor, i * 2);
                using var expandedPath = (GraphicsPath)path.Clone();

                // 膨胀路径
                using var matrix = new Matrix();
                matrix.Translate(i, i);
                expandedPath.Transform(matrix);

                graphics.DrawPath(pen, expandedPath);
                expandedPath.Dispose();
            }
        }

        /// <summary>
        /// 绘制内发光效果
        /// </summary>
        /// <param name="graphics">图形对象</param>
        /// <param name="path">内发光路径</param>
        /// <param name="glowColor">发光颜色</param>
        /// <param name="glowSize">发光大小</param>
        public static void DrawInnerGlow(Graphics graphics, GraphicsPath path, Color glowColor, int glowSize)
        {
            if (graphics == null || path == null || glowSize <= 0)
                return;

            var bounds = path.GetBounds();

            using var clipPath = (GraphicsPath)path.Clone();
            graphics.SetClip(clipPath);

            // 从外向内绘制渐变
            for (int i = glowSize; i > 0; i--)
            {
                var alpha = (int)((1f - (float)i / glowSize) * glowColor.A);
                var layerColor = Color.FromArgb(alpha, glowColor.R, glowColor.G, glowColor.B);

                using var pen = new Pen(layerColor, 2);
                using var contractedPath = (GraphicsPath)path.Clone();

                // 收缩路径
                using var matrix = new Matrix();
                matrix.Translate(-i, -i);
                contractedPath.Transform(matrix);

                graphics.DrawPath(pen, contractedPath);
                contractedPath.Dispose();
            }

            graphics.ResetClip();
            clipPath.Dispose();
        }

        #endregion

        #region 倒影和反射效果

        /// <summary>
        /// 创建倒影效果
        /// </summary>
        /// <param name="sourceImage">源图像</param>
        /// <param name="reflectionHeight">倒影高度比例（0-1）</param>
        /// <param name="opacity">倒影透明度（0-1）</param>
        /// <returns>带倒影的图像</returns>
        public static Bitmap CreateReflection(Bitmap sourceImage, float reflectionHeight = 0.5f, float opacity = 0.5f)
        {
            if (sourceImage == null)
                return null;

            reflectionHeight = Math.Max(0f, Math.Min(1f, reflectionHeight));
            opacity = Math.Max(0f, Math.Min(1f, opacity));

            var reflectionHeightPixels = (int)(sourceImage.Height * reflectionHeight);
            var resultHeight = sourceImage.Height + reflectionHeightPixels;

            using var resultImage = new Bitmap(sourceImage.Width, resultHeight, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(resultImage);

            // 绘制原始图像
            graphics.DrawImageUnscaled(sourceImage, 0, 0);

            // 创建倒影
            var reflectionRect = new Rectangle(0, sourceImage.Height, sourceImage.Width, reflectionHeightPixels);
            using var reflectedImage = new Bitmap(sourceImage.Width, reflectionHeightPixels);
            using var reflectedGraphics = Graphics.FromImage(reflectedImage);

            // 翻转图像
            reflectedGraphics.ScaleTransform(1, -1);
            reflectedGraphics.DrawImage(sourceImage, 0, -sourceImage.Height, sourceImage.Width, sourceImage.Height);

            // 应用渐变透明度
            var colors = new Color[reflectionHeightPixels];
            for (int i = 0; i < reflectionHeightPixels; i++)
            {
                var alpha = (int)((1f - (float)i / reflectionHeightPixels) * opacity * 255);
                colors[i] = Color.FromArgb(alpha, 255, 255, 255);
            }

            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(new ColorMatrix { Matrix33 = 1f });
            attributes.SetColorKey(colors[0], colors[colors.Length - 1]);

            graphics.DrawImage(reflectedImage, reflectionRect, 0, 0, reflectedImage.Width, reflectedImage.Height, GraphicsUnit.Pixel, attributes);

            return (Bitmap)resultImage.Clone();
        }

        #endregion

        #region 模糊和锐化效果

        /// <summary>
        /// 应用高斯模糊
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="radius">模糊半径</param>
        public static void ApplyGaussianBlur(Bitmap image, int radius)
        {
            if (image == null || radius <= 0)
                return;

            var kernel = CreateGaussianKernel(radius);
            ApplyConvolution(image, kernel);
        }

        /// <summary>
        /// 应用运动模糊
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="angle">运动角度（度）</param>
        /// <param name="distance">运动距离</param>
        public static void ApplyMotionBlur(Bitmap image, float angle, int distance)
        {
            if (image == null || distance <= 0)
                return;

            var radians = (float)(angle * Math.PI / 180.0);
            var kernel = CreateMotionBlurKernel(radians, distance);
            ApplyConvolution(image, kernel);
        }

        /// <summary>
        /// 应用锐化效果
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="strength">锐化强度（0-1）</param>
        public static void ApplySharpen(Bitmap image, float strength = 0.5f)
        {
            if (image == null)
                return;

            strength = Math.Max(0f, Math.Min(1f, strength));

            var kernel = new float[,]
            {
                { 0, -strength, 0 },
                { -strength, 1 + 4 * strength, -strength },
                { 0, -strength, 0 }
            };

            ApplyConvolution(image, kernel);
        }

        #endregion

        #region 动画效果

        /// <summary>
        /// 创建缓动函数
        /// </summary>
        /// <param name="type">缓动类型</param>
        /// <returns>缓动函数</returns>
        public static Func<double, double> CreateEasingFunction(EasingType type)
        {
            return type switch
            {
                EasingType.Linear => t => t,
                EasingType.EaseInQuad => t => t * t,
                EasingType.EaseOutQuad => t => t * (2 - t),
                EasingType.EaseInOutQuad => t => t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t,
                EasingType.EaseInCubic => t => t * t * t,
                EasingType.EaseOutCubic => t => (--t) * t * t + 1,
                EasingType.EaseInOutCubic => t => t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1,
                EasingType.EaseInElastic => ElasticIn(t),
                EasingType.EaseOutElastic => ElasticOut(t),
                EasingType.EaseInOutElastic => ElasticInOut(t),
                EasingType.EaseInBounce => BounceIn(t),
                EasingType.EaseOutBounce => BounceOut(t),
                EasingType.EaseInOutBounce => BounceInOut(t),
                _ => t => t
            };
        }

        /// <summary>
        /// 创建弹跳动画
        /// </summary>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <param name="duration">持续时间</param>
        /// <param name="bounces">弹跳次数</param>
        /// <param name="easing">缓动函数</param>
        /// <returns>动画值序列</returns>
        public static IEnumerable<float> CreateBounceAnimation(float startValue, float endValue, TimeSpan duration, int bounces = 3, Func<double, double> easing = null)
        {
            easing ??= CreateEasingFunction(EasingType.EaseOutBounce);
            var steps = (int)(duration.TotalMilliseconds / 16); // ~60 FPS

            for (int i = 0; i <= steps; i++)
            {
                var t = (double)i / steps;
                var easedT = easing(t);

                // 添加弹跳效果
                var bounce = Math.Abs(Math.Sin(easedT * Math.PI * bounces)) * Math.Pow(1 - easedT, 2);
                var value = startValue + (endValue - startValue) * easedT + (float)(bounce * (endValue - startValue) * 0.1);

                yield return value;
            }
        }

        /// <summary>
        /// 创建弹性动画
        /// </summary>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <param name="duration">持续时间</param>
        /// <param name="springiness">弹性系数</param>
        /// <param name="easing">缓动函数</param>
        /// <returns>动画值序列</returns>
        public static IEnumerable<float> CreateSpringAnimation(float startValue, float endValue, TimeSpan duration, float springiness = 0.3f, Func<double, double> easing = null)
        {
            easing ??= CreateEasingFunction(EasingType.EaseOutElastic);
            var steps = (int)(duration.TotalMilliseconds / 16);

            for (int i = 0; i <= steps; i++)
            {
                var t = (double)i / steps;
                var easedT = easing(t);

                // 添加弹簧效果
                var spring = Math.Sin(easedT * Math.PI * 8) * Math.Pow(1 - easedT, springiness);
                var value = startValue + (endValue - startValue) * easedT + (float)(spring * (endValue - startValue) * 0.05);

                yield return value;
            }
        }

        #endregion

        #region 粒子效果

        /// <summary>
        /// 粒子类
        /// </summary>
        public class Particle
        {
            public PointF Position { get; set; }
            public PointF Velocity { get; set; }
            public SizeF Size { get; set; }
            public Color Color { get; set; }
            public float Life { get; set; }
            public float MaxLife { get; set; }
            public float Rotation { get; set; }
            public float RotationSpeed { get; set; }

            public bool IsAlive => Life > 0;

            public void Update(float deltaTime)
            {
                Position = new PointF(
                    Position.X + Velocity.X * deltaTime,
                    Position.Y + Velocity.Y * deltaTime
                );
                Rotation += RotationSpeed * deltaTime;
                Life -= deltaTime;
            }

            public float GetOpacity()
            {
                return Math.Max(0, Life / MaxLife);
            }
        }

        /// <summary>
        /// 粒子系统
        /// </summary>
        public class ParticleSystem
        {
            private readonly List<Particle> _particles;
            private readonly Random _random;

            public ParticleSystem()
            {
                _particles = new List<Particle>();
                _random = new Random();
            }

            /// <summary>
            /// 发射粒子
            /// </summary>
            /// <param name="position">发射位置</param>
            /// <param name="count">粒子数量</param>
            /// <param name="config">粒子配置</param>
            public void Emit(PointF position, int count, ParticleConfig config = null)
            {
                config ??= ParticleConfig.Default;

                for (int i = 0; i < count; i++)
                {
                    var particle = new Particle
                    {
                        Position = position,
                        Velocity = new PointF(
                            (float)(_random.NextDouble() * 2 - 1) * config.VelocityRange,
                            (float)(_random.NextDouble() * 2 - 1) * config.VelocityRange
                        ),
                        Size = new SizeF(
                            (float)(_random.NextDouble() * (config.MaxSize - config.MinSize) + config.MinSize),
                            (float)(_random.NextDouble() * (config.MaxSize - config.MinSize) + config.MinSize)
                        ),
                        Color = config.Colors[_random.Next(config.Colors.Length)],
                        Life = config.Lifetime,
                        MaxLife = config.Lifetime,
                        Rotation = (float)(_random.NextDouble() * Math.PI * 2),
                        RotationSpeed = (float)(_random.NextDouble() * 2 - 1) * config.RotationSpeed
                    };

                    _particles.Add(particle);
                }
            }

            /// <summary>
            /// 更新粒子系统
            /// </summary>
            /// <param name="deltaTime">时间增量</param>
            public void Update(float deltaTime)
            {
                for (int i = _particles.Count - 1; i >= 0; i--)
                {
                    var particle = _particles[i];
                    particle.Update(deltaTime);

                    if (!particle.IsAlive)
                    {
                        _particles.RemoveAt(i);
                    }
                }
            }

            /// <summary>
            /// 绘制粒子
            /// </summary>
            /// <param name="graphics">图形对象</param>
            public void Draw(Graphics graphics)
            {
                foreach (var particle in _particles)
                {
                    var opacity = particle.GetOpacity();
                    var color = Color.FromArgb((int)(opacity * particle.Color.A), particle.Color);

                    using var brush = new SolidBrush(color);
                    graphics.FillEllipse(brush,
                        particle.Position.X - particle.Size.Width / 2,
                        particle.Position.Y - particle.Size.Height / 2,
                        particle.Size.Width,
                        particle.Size.Height);
                }
            }

            /// <summary>
            /// 清除所有粒子
            /// </summary>
            public void Clear()
            {
                _particles.Clear();
            }

            /// <summary>
            /// 获取活跃粒子数量
            /// </summary>
            public int ActiveCount => _particles.Count;
        }

        /// <summary>
        /// 粒子配置
        /// </summary>
        public class ParticleConfig
        {
            public static readonly ParticleConfig Default = new ParticleConfig();

            public Color[] Colors { get; set; } = { Color.White, Color.Yellow, Color.Orange };
            public float MinSize { get; set; } = 2f;
            public float MaxSize { get; set; } = 6f;
            public float VelocityRange { get; set; } = 100f;
            public float Lifetime { get; set; } = 2f;
            public float RotationSpeed { get; set; } = 5f;
        }

        #endregion

        #region 私有辅助方法

        private static float GetHue(Color color)
        {
            var r = color.R / 255f;
            var g = color.G / 255f;
            var b = color.B / 255f;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;

            if (delta == 0)
                return 0;

            float hue;
            if (max == r)
                hue = ((g - b) / delta) % 6;
            else if (max == g)
                hue = (b - r) / delta + 2;
            else
                hue = (r - g) / delta + 4;

            return hue * 60;
        }

        private static float GetSaturation(Color color)
        {
            var r = color.R / 255f;
            var g = color.G / 255f;
            var b = color.B / 255f;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;

            return max == 0 ? 0 : delta / max;
        }

        private static float GetBrightness(Color color)
        {
            return Math.Max(color.R, Math.Max(color.G, color.B)) / 255f;
        }

        private static Color ColorFromHSV(float hue, float saturation, float brightness)
        {
            hue = hue % 360;
            if (hue < 0) hue += 360;

            var c = brightness * saturation;
            var x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            var m = brightness - c;

            float r, g, b;
            if (hue < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (hue < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (hue < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (hue < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (hue < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return Color.FromArgb(
                255,
                (int)((r + m) * 255),
                (int)((g + m) * 255),
                (int)((b + m) * 255)
            );
        }

        private static float[,] CreateGaussianKernel(int radius)
        {
            var size = radius * 2 + 1;
            var kernel = new float[size, size];
            var sigma = radius / 3f;
            var sum = 0f;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    var value = (float)Math.Exp(-(x * x + y * y) / (2 * sigma * sigma));
                    kernel[x + radius, y + radius] = value;
                    sum += value;
                }
            }

            // 归一化
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[x, y] /= sum;
                }
            }

            return kernel;
        }

        private static float[,] CreateMotionBlurKernel(float angle, int distance)
        {
            var size = distance * 2 + 1;
            var kernel = new float[size, size];
            var center = distance;
            var dx = Math.Cos(angle);
            var dy = Math.Sin(angle);

            for (int i = 0; i < distance; i++)
            {
                var x = (int)(center + dx * i);
                var y = (int)(center + dy * i);

                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    kernel[x, y] = 1f / distance;
                }
            }

            return kernel;
        }

        private static void ApplyConvolution(Bitmap image, float[,] kernel)
        {
            var width = image.Width;
            var height = image.Height;
            var kSize = kernel.GetLength(0);
            var kRadius = kSize / 2;

            var bitmapData = image.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            );

            unsafe
            {
                var ptr = (byte*)bitmapData.Scan0;
                var stride = bitmapData.Stride;

                for (int y = kRadius; y < height - kRadius; y++)
                {
                    for (int x = kRadius; x < width - kRadius; x++)
                    {
                        var r = 0f;
                        var g = 0f;
                        var b = 0f;

                        for (int ky = 0; ky < kSize; ky++)
                        {
                            for (int kx = 0; kx < kSize; kx++)
                            {
                                var pixelX = x + kx - kRadius;
                                var pixelY = y + ky - kRadius;
                                var pixelPtr = ptr + pixelY * stride + pixelX * 4;

                                r += pixelPtr[2] * kernel[kx, ky];
                                g += pixelPtr[1] * kernel[kx, ky];
                                b += pixelPtr[0] * kernel[kx, ky];
                            }
                        }

                        var currentPtr = ptr + y * stride + x * 4;
                        currentPtr[2] = (byte)Math.Max(0, Math.Min(255, r));
                        currentPtr[1] = (byte)Math.Max(0, Math.Min(255, g));
                        currentPtr[0] = (byte)Math.Max(0, Math.Min(255, b));
                    }
                }
            }

            image.UnlockBits(bitmapData);
        }

        // 缓动函数实现
        private static double ElasticIn(double t)
        {
            if (t == 0 || t == 1) return t;
            return -Math.Pow(2, 10 * (t - 1)) * Math.Sin((t - 1.1) * 2 * Math.PI / 0.4);
        }

        private static double ElasticOut(double t)
        {
            if (t == 0 || t == 1) return t;
            return Math.Pow(2, -10 * t) * Math.Sin((t - 0.1) * 2 * Math.PI / 0.4) + 1;
        }

        private static double ElasticInOut(double t)
        {
            if (t == 0 || t == 1) return t;
            t *= 2;
            if (t < 1) return -0.5 * Math.Pow(2, 10 * (t - 1)) * Math.Sin((t - 1.1) * 2 * Math.PI / 0.4);
            return 0.5 * Math.Pow(2, -10 * (t - 1)) * Math.Sin((t - 1.1) * 2 * Math.PI / 0.4) + 1;
        }

        private static double BounceIn(double t)
        {
            return 1 - BounceOut(1 - t);
        }

        private static double BounceOut(double t)
        {
            if (t < 1 / 2.75)
            {
                return 7.5625 * t * t;
            }
            else if (t < 2 / 2.75)
            {
                t -= 1.5 / 2.75;
                return 7.5625 * t * t + 0.75;
            }
            else if (t < 2.5 / 2.75)
            {
                t -= 2.25 / 2.75;
                return 7.5625 * t * t + 0.9375;
            }
            else
            {
                t -= 2.625 / 2.75;
                return 7.5625 * t * t + 0.984375;
            }
        }

        private static double BounceInOut(double t)
        {
            if (t < 0.5) return BounceIn(t * 2) * 0.5;
            return BounceOut(t * 2 - 1) * 0.5 + 0.5;
        }

        #endregion
    }

    /// <summary>
    /// 缓动类型枚举
    /// </summary>
    public enum EasingType
    {
        Linear,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseInElastic,
        EaseOutElastic,
        EaseInOutElastic,
        EaseInBounce,
        EaseOutBounce,
        EaseInOutBounce
    }
}