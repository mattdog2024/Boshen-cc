using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BoshenCC.Core.Utils;
using BoshenCC.Core.Interfaces;
using BoshenCC.Services.Interfaces;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Views
{
    /// <summary>
    /// 主窗体
    /// </summary>
    public partial class MainWindow : Form
    {
        #region 私有字段

        private readonly ILogService _logService;
        private readonly IImageProcessor _imageProcessor;
        private readonly IConfigService _configService;
        private readonly IScreenshotService _screenshotService;
        private Bitmap _currentImage;
        private bool _isModified = false;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // 从服务定位器获取服务
            _logService = ServiceLocator.GetService<ILogService>();
            _imageProcessor = ServiceLocator.GetService<IImageProcessor>();
            _configService = ServiceLocator.GetService<IConfigService>();
            _screenshotService = ServiceLocator.GetService<IScreenshotService>();

            // 初始化UI
            InitializeUI();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化UI
        /// </summary>
        private void InitializeUI()
        {
            try
            {
                // 设置窗体属性
                this.Text = "Boshen CC - 股票图表识别工具";

                // 初始化状态栏
                UpdateStatus("就绪");

                // 初始化日志
                if (_logService != null)
                {
                    _logService.Info("主窗体初始化完成");
                    LogMessage("应用程序启动成功");
                }

                // 加载配置
                LoadSettings();
            }
            catch (Exception ex)
            {
                LogMessage($"初始化UI失败: {ex.Message}", true);
                MessageBox.Show($"初始化UI失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 更新状态栏
        /// </summary>
        /// <param name="status">状态文本</param>
        private void UpdateStatus(string status)
        {
            if (toolStripStatusLabel.InvokeRequired)
            {
                toolStripStatusLabel.Invoke(new Action<string>(UpdateStatus), status);
                return;
            }

            toolStripStatusLabel.Text = $"状态：{status}";
        }

        /// <summary>
        /// 记录日志消息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="isError">是否错误</param>
        private void LogMessage(string message, bool isError = false)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

                if (richTextBoxLog.InvokeRequired)
                {
                    richTextBoxLog.Invoke(new Action<string, bool>(LogMessage), message, isError);
                    return;
                }

                richTextBoxLog.AppendText(logEntry + Environment.NewLine);
                richTextBoxLog.ScrollToCaret();

                // 同时记录到日志服务
                if (_logService != null)
                {
                    if (isError)
                        _logService.Error(message);
                    else
                        _logService.Info(message);
                }
            }
            catch (Exception ex)
            {
                // 日志记录失败时的静默处理
                System.Diagnostics.Debug.WriteLine($"日志记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示进度
        /// </summary>
        /// <param name="progress">进度值 (0-100)</param>
        private void ShowProgress(int progress)
        {
            if (toolStripProgressBar.InvokeRequired)
            {
                toolStripProgressBar.Invoke(new Action<int>(ShowProgress), progress);
                return;
            }

            toolStripProgressBar.Value = Math.Max(0, Math.Min(100, progress));
            toolStripProgressBar.Visible = progress > 0 && progress < 100;
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (_configService != null)
                {
                    var settings = _configService.LoadSettings();
                    // 应用设置到UI
                    LogMessage("配置加载完成");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"加载配置失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                if (_configService != null)
                {
                    // 从UI收集设置并保存
                    _configService.SaveSettings(new AppSettings());
                    LogMessage("配置保存完成");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"保存配置失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 打开图像文件
        /// </summary>
        private void OpenImageFile()
        {
            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ShowProgress(10);
                    UpdateStatus("正在加载图像...");

                    using (var fileStream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                    {
                        _currentImage = new Bitmap(fileStream);
                    }

                    ShowProgress(50);
                    UpdateStatus("图像加载完成");

                    pictureBoxMain.Image = _currentImage;
                    _isModified = false;

                    ShowProgress(100);
                    UpdateStatus($"已加载图像: {Path.GetFileName(openFileDialog.FileName)}");
                    LogMessage($"成功加载图像: {openFileDialog.FileName}");

                    ShowProgress(0);
                }
            }
            catch (Exception ex)
            {
                ShowProgress(0);
                UpdateStatus("图像加载失败");
                LogMessage($"加载图像失败: {ex.Message}", true);
                MessageBox.Show($"加载图像失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 保存图像文件
        /// </summary>
        private void SaveImageFile()
        {
            try
            {
                if (_currentImage == null)
                {
                    MessageBox.Show("没有可保存的图像", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ShowProgress(10);
                    UpdateStatus("正在保存图像...");

                    var format = GetImageFormat(saveFileDialog.FileName);
                    _currentImage.Save(saveFileDialog.FileName, format);

                    ShowProgress(100);
                    UpdateStatus($"图像保存完成: {Path.GetFileName(saveFileDialog.FileName)}");
                    LogMessage($"成功保存图像: {saveFileDialog.FileName}");
                    _isModified = false;

                    ShowProgress(0);
                }
            }
            catch (Exception ex)
            {
                ShowProgress(0);
                UpdateStatus("图像保存失败");
                LogMessage($"保存图像失败: {ex.Message}", true);
                MessageBox.Show($"保存图像失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 根据文件扩展名获取图像格式
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>图像格式</returns>
        private System.Drawing.Imaging.ImageFormat GetImageFormat(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return System.Drawing.Imaging.ImageFormat.Jpeg;
                case ".png":
                    return System.Drawing.Imaging.ImageFormat.Png;
                case ".bmp":
                    return System.Drawing.Imaging.ImageFormat.Bmp;
                case ".gif":
                    return System.Drawing.Imaging.ImageFormat.Gif;
                case ".tif":
                case ".tiff":
                    return System.Drawing.Imaging.ImageFormat.Tiff;
                default:
                    return System.Drawing.Imaging.ImageFormat.Jpeg;
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        private void MainWindow_Load(object sender, EventArgs e)
        {
            LogMessage("主窗体加载完成");
        }

        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // 检查是否有未保存的更改
                if (_isModified)
                {
                    var result = MessageBox.Show("有未保存的更改，确定要退出吗？", "确认退出",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                    if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                // 保存设置
                SaveSettings();

                // 清理资源
                _currentImage?.Dispose();

                LogMessage("主窗体关闭，应用程序退出");
            }
            catch (Exception ex)
            {
                LogMessage($"窗体关闭处理失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 键盘按下事件
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // 快捷键处理
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.O:
                        OpenImageFile();
                        e.Handled = true;
                        break;
                    case Keys.S:
                        SaveImageFile();
                        e.Handled = true;
                        break;
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 状态更新定时器
        /// </summary>
        private void timerStatus_Tick(object sender, EventArgs e)
        {
            // 定期更新状态信息（如内存使用情况等）
            // 这里可以添加实时状态监控逻辑
        }

        #endregion

        #region 菜单事件处理

        private void openImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenImageFile();
        }

        private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveImageFile();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void singleMeasureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("单体测量功能将在后续版本中实现", "功能开发中",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void lineMeasureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("影线测量功能将在后续版本中实现", "功能开发中",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void clearLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("清除线条功能将在后续版本中实现", "功能开发中",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("设置功能将在后续版本中实现", "功能开发中",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Boshen CC - 股票图表识别工具\n\n版本: 1.0.0\n\n© 2025 Boshen Technology",
                "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region 工具栏事件处理

        private void toolStripButtonOpen_Click(object sender, EventArgs e)
        {
            OpenImageFile();
        }

        private void toolStripButtonSingleMeasure_Click(object sender, EventArgs e)
        {
            singleMeasureToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonLineMeasure_Click(object sender, EventArgs e)
        {
            lineMeasureToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonClear_Click(object sender, EventArgs e)
        {
            clearLinesToolStripMenuItem_Click(sender, e);
        }

        #endregion
    }
}