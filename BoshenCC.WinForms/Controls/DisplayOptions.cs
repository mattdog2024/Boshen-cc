using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 显示选项控件
    /// 提供界面显示和视觉效果的配置选项
    /// </summary>
    public partial class DisplayOptions : UserControl
    {
        #region 字段

        private DisplaySettings _settings = new DisplaySettings();
        private bool _isUpdating = false;
        private Dictionary<string, Color> _themeColors = new Dictionary<string, Color>();

        #endregion

        #region 事件

        /// <summary>
        /// 设置更改事件
        /// </summary>
        public event EventHandler<DisplaySettings> SettingsChanged;

        /// <summary>
        /// 预览请求事件
        /// </summary>
        public event EventHandler PreviewRequested;

        #endregion

        #region 属性

        /// <summary>
        /// 当前设置
        /// </summary>
        public DisplaySettings Settings
        {
            get => _settings;
            set
            {
                if (_settings != value)
                {
                    _settings = value ?? new DisplaySettings();
                    UpdateControlsFromSettings();
                }
            }
        }

        #endregion

        #region 构造函数

        public DisplayOptions()
        {
            InitializeComponent();
            InitializeThemes();
            InitializeControls();
            SetupEventHandlers();
        }

        #endregion

        #region 初始化

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 控件基本属性
            this.Name = "DisplayOptions";
            this.Size = new Size(650, 450);
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;

            // 创建控件
            CreateControls();

            this.ResumeLayout(false);
        }

        private void InitializeThemes()
        {
            // 初始化主题颜色配置
            _themeColors.Add("Default", Color.FromArgb(33, 150, 243));
            _themeColors.Add("Dark", Color.FromArgb(33, 33, 33));
            _themeColors.Add("Light", Color.FromArgb(255, 255, 255));
            _themeColors.Add("Professional", Color.FromArgb(63, 81, 181));
            _themeColors.Add("Colorful", Color.FromArgb(156, 39, 176));
            _themeColors.Add("Nature", Color.FromArgb(76, 175, 80));
            _themeColors.Add("Ocean", Color.FromArgb(3, 169, 244));
            _themeColors.Add("Sunset", Color.FromArgb(255, 152, 0));
        }

        private void CreateControls()
        {
            // 创建主面板
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 12,
                ColumnCount = 3,
                Padding = new Padding(10),
                AutoSize = true
            };

            // 添加列样式
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // 标签
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, 200)); // 控件
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // 说明

            // 线条宽度
            var lblLineWidth = CreateLabel("线条宽度:");
            var trackLineWidth = CreateTrackBar(1, 10, _settings.LineWidth, "trackLineWidth");
            var lblLineWidthValue = CreateValueLabel(_settings.LineWidth.ToString());
            var lblLineWidthDesc = CreateDescriptionLabel("预测线的线条宽度");

            // 主题选择
            var lblTheme = CreateLabel("主题:");
            var cmbTheme = CreateComboBox(GetThemeNames(), _settings.Theme, "cmbTheme");
            var pnlThemePreview = CreateThemePreview(_settings.Theme, "pnlThemePreview");
            var lblThemeDesc = CreateDescriptionLabel("选择界面主题风格");

            // 字体大小
            var lblFontSize = CreateLabel("字体大小:");
            var trackFontSize = CreateTrackBar(8, 48, _settings.FontSize, "trackFontSize");
            var lblFontSizeValue = CreateValueLabel(_settings.FontSize.ToString());
            var lblFontSizeDesc = CreateDescriptionLabel("界面文字的字体大小");

            // 语言选择
            var lblLanguage = CreateLabel("语言:");
            var cmbLanguage = CreateComboBox(new[] { "zh-CN", "en-US" }, _settings.Language, "cmbLanguage");
            var lblLanguageDesc = CreateDescriptionLabel("选择界面显示语言");

            // 显示选项复选框
            var chkShowGrid = CreateCheckBox("显示网格", _settings.ShowGrid, "chkShowGrid");
            var lblShowGridDesc = CreateDescriptionLabel("在图表上显示网格线");

            var chkShowCoordinates = CreateCheckBox("显示坐标", _settings.ShowCoordinates, "chkShowCoordinates");
            var lblShowCoordinatesDesc = CreateDescriptionLabel("显示鼠标坐标信息");

            var chkEnableAnimations = CreateCheckBox("启用动画", _settings.EnableAnimations, "chkEnableAnimations");
            var lblEnableAnimationsDesc = CreateDescriptionLabel("启用界面动画效果");

            var chkShowTooltips = CreateCheckBox("显示工具提示", _settings.ShowTooltips, "chkShowTooltips");
            var lblShowTooltipsDesc = CreateDescriptionLabel("显示控件工具提示信息");

            // 添加控件到面板
            mainPanel.Controls.Add(lblTheme, 0, 0);
            mainPanel.Controls.Add(cmbTheme, 1, 0);
            mainPanel.Controls.Add(pnlThemePreview, 2, 0);

            mainPanel.Controls.Add(lblLineWidth, 0, 1);
            var lineWidthPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };
            lineWidthPanel.Controls.Add(trackLineWidth);
            lineWidthPanel.Controls.Add(lblLineWidthValue);
            mainPanel.Controls.Add(lineWidthPanel, 1, 1);
            mainPanel.Controls.Add(lblLineWidthDesc, 2, 1);

            mainPanel.Controls.Add(lblFontSize, 0, 2);
            var fontSizePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };
            fontSizePanel.Controls.Add(trackFontSize);
            fontSizePanel.Controls.Add(lblFontSizeValue);
            mainPanel.Controls.Add(fontSizePanel, 1, 2);
            mainPanel.Controls.Add(lblFontSizeDesc, 2, 2);

            mainPanel.Controls.Add(lblLanguage, 0, 3);
            mainPanel.Controls.Add(cmbLanguage, 1, 3);
            mainPanel.Controls.Add(lblLanguageDesc, 2, 3);

            mainPanel.Controls.Add(chkShowGrid, 0, 4);
            mainPanel.Controls.Add(new Control(), 1, 4);
            mainPanel.Controls.Add(lblShowGridDesc, 2, 4);

            mainPanel.Controls.Add(chkShowCoordinates, 0, 5);
            mainPanel.Controls.Add(new Control(), 1, 5);
            mainPanel.Controls.Add(lblShowCoordinatesDesc, 2, 5);

            mainPanel.Controls.Add(chkEnableAnimations, 0, 6);
            mainPanel.Controls.Add(new Control(), 1, 6);
            mainPanel.Controls.Add(lblEnableAnimationsDesc, 2, 6);

            mainPanel.Controls.Add(chkShowTooltips, 0, 7);
            mainPanel.Controls.Add(new Control(), 1, 7);
            mainPanel.Controls.Add(lblShowTooltipsDesc, 2, 7);

            // 添加预览区域
            var previewPanel = CreatePreviewPanel();
            mainPanel.Controls.Add(previewPanel, 0, 8);
            mainPanel.SetColumnSpan(previewPanel, 3);

            // 添加按钮面板
            var buttonPanel = CreateButtonPanel();
            mainPanel.Controls.Add(buttonPanel, 0, 9);
            mainPanel.SetColumnSpan(buttonPanel, 3);

            this.Controls.Add(mainPanel);
        }

        private void InitializeControls()
        {
            // 设置控件初始状态
            UpdateControlsFromSettings();
        }

        private void SetupEventHandlers()
        {
            // 为所有控件设置事件处理器
            foreach (Control control in this.Controls)
            {
                SetupControlEvents(control);
            }
        }

        private void SetupControlEvents(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                switch (control)
                {
                    case TrackBar track:
                        track.ValueChanged += TrackBar_ValueChanged;
                        break;
                    case ComboBox cmb:
                        cmb.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
                        break;
                    case CheckBox chk:
                        chk.CheckedChanged += CheckBox_CheckedChanged;
                        break;
                }

                if (control.HasChildren)
                {
                    SetupControlEvents(control);
                }
            }
        }

        #endregion

        #region 事件处理

        private void TrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;

            var trackBar = sender as TrackBar;
            if (trackBar == null) return;

            switch (trackBar.Name)
            {
                case "trackLineWidth":
                    _settings.LineWidth = trackBar.Value;
                    UpdateValueLabel("trackLineWidth", trackBar.Value.ToString());
                    break;
                case "trackFontSize":
                    _settings.FontSize = trackBar.Value;
                    UpdateValueLabel("trackFontSize", trackBar.Value.ToString());
                    break;
            }

            SettingsChanged?.Invoke(this, _settings);
            PreviewRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;

            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            switch (comboBox.Name)
            {
                case "cmbTheme":
                    _settings.Theme = comboBox.SelectedItem?.ToString() ?? "Default";
                    UpdateThemePreview(_settings.Theme);
                    break;
                case "cmbLanguage":
                    _settings.Language = comboBox.SelectedItem?.ToString() ?? "zh-CN";
                    break;
            }

            SettingsChanged?.Invoke(this, _settings);
            PreviewRequested?.Invoke(this, EventArgs.Empty);
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;

            var checkBox = sender as CheckBox;
            if (checkBox == null) return;

            switch (checkBox.Name)
            {
                case "chkShowGrid":
                    _settings.ShowGrid = checkBox.Checked;
                    break;
                case "chkShowCoordinates":
                    _settings.ShowCoordinates = checkBox.Checked;
                    break;
                case "chkEnableAnimations":
                    _settings.EnableAnimations = checkBox.Checked;
                    break;
                case "chkShowTooltips":
                    _settings.ShowTooltips = checkBox.Checked;
                    break;
            }

            SettingsChanged?.Invoke(this, _settings);
            PreviewRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 控件创建辅助方法

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 51, 51)
            };
        }

        private Label CreateDescriptionLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 8F, FontStyle.Regular),
                ForeColor = Color.Gray,
                Margin = new Padding(10, 0, 0, 0)
            };
        }

        private Label CreateValueLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                Width = 30,
                Margin = new Padding(5, 0, 0, 0)
            };
        }

        private TrackBar CreateTrackBar(int minimum, int maximum, int value, string name)
        {
            return new TrackBar
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = value,
                Name = name,
                Width = 120,
                TickFrequency = 1,
                SmallChange = 1,
                LargeChange = 1
            };
        }

        private ComboBox CreateComboBox(string[] items, string selectedItem, string name)
        {
            var comboBox = new ComboBox
            {
                Name = name,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular)
            };

            comboBox.Items.AddRange(items);
            comboBox.SelectedItem = selectedItem;

            return comboBox;
        }

        private CheckBox CreateCheckBox(string text, bool checkedState, string name)
        {
            return new CheckBox
            {
                Text = text,
                Checked = checkedState,
                Name = name,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 51, 51),
                AutoSize = true
            };
        }

        private Panel CreateThemePreview(string theme, string name)
        {
            var panel = new Panel
            {
                Name = name,
                Width = 60,
                Height = 20,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = _themeColors.ContainsKey(theme) ? _themeColors[theme] : Color.LightGray
            };

            return panel;
        }

        private Panel CreatePreviewPanel()
        {
            var panel = new Panel
            {
                Height = 100,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 250),
                Margin = new Padding(0, 10, 0, 10)
            };

            var lblPreviewTitle = new Label
            {
                Text = "预览效果",
                Location = new Point(10, 5),
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                AutoSize = true
            };

            var previewCanvas = new PictureBox
            {
                Location = new Point(10, 30),
                Size = new Size(200, 60),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            // 绘制预览内容
            previewCanvas.Paint += (s, e) => DrawPreviewContent(e.Graphics);

            panel.Controls.Add(lblPreviewTitle);
            panel.Controls.Add(previewCanvas);

            return panel;
        }

        private Panel CreateButtonPanel()
        {
            var panel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Bottom,
                Margin = new Padding(0, 10, 0, 0)
            };

            var btnReset = CreateButton("重置为默认", 80, Color.FromArgb(108, 117, 125));
            btnReset.Click += (s, e) => ResetToDefaults();

            var btnApply = CreateButton("应用", 80, Color.FromArgb(40, 167, 69));
            btnApply.Click += (s, e) => ApplySettings();

            var btnPreview = CreateButton("预览", 80, Color.FromArgb(23, 162, 184));
            btnPreview.Click += (s, e) => PreviewSettings();

            // 按钮布局
            btnReset.Location = new Point(panel.Width - 260, 5);
            btnPreview.Location = new Point(panel.Width - 170, 5);
            btnApply.Location = new Point(panel.Width - 80, 5);

            panel.Controls.Add(btnReset);
            panel.Controls.Add(btnPreview);
            panel.Controls.Add(btnApply);

            return panel;
        }

        private Button CreateButton(string text, int width, Color backColor)
        {
            return new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular),
                UseVisualStyleBackColor = false
            };
        }

        #endregion

        #region 预览绘制

        private void DrawPreviewContent(Graphics g)
        {
            var bgColor = _themeColors.ContainsKey(_settings.Theme) ? _themeColors[_settings.Theme] : Color.LightGray;

            // 清除背景
            g.Clear(Color.White);

            // 绘制示例线条
            var pen = new Pen(bgColor, _settings.LineWidth);

            if (_settings.LineWidth > 1)
            {
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            }

            // 绘制示例预测线
            for (int i = 0; i < 5; i++)
            {
                var y = 15 + i * 10;
                g.DrawLine(pen, 10, y, 190, y);

                // 绘制标签
                var font = new Font("Microsoft YaHei", Math.Max(8f, _settings.FontSize / 2), FontStyle.Regular);
                var brush = new SolidBrush(bgColor);
                g.DrawString($"Line {i + 1}", font, brush, 10, y - 7);
            }

            // 清理资源
            pen.Dispose();
        }

        #endregion

        #region 设置更新方法

        /// <summary>
        /// 从设置更新控件
        /// </summary>
        public void UpdateControlsFromSettings()
        {
            if (_isUpdating) return;

            _isUpdating = true;

            try
            {
                // 更新TrackBar
                var trackLineWidth = FindControl("trackLineWidth") as TrackBar;
                if (trackLineWidth != null) trackLineWidth.Value = _settings.LineWidth;

                var trackFontSize = FindControl("trackFontSize") as TrackBar;
                if (trackFontSize != null) trackFontSize.Value = _settings.FontSize;

                // 更新ComboBox
                var cmbTheme = FindControl("cmbTheme") as ComboBox;
                if (cmbTheme != null) cmbTheme.SelectedItem = _settings.Theme;

                var cmbLanguage = FindControl("cmbLanguage") as ComboBox;
                if (cmbLanguage != null) cmbLanguage.SelectedItem = _settings.Language;

                // 更新CheckBox
                var chkShowGrid = FindControl("chkShowGrid") as CheckBox;
                if (chkShowGrid != null) chkShowGrid.Checked = _settings.ShowGrid;

                var chkShowCoordinates = FindControl("chkShowCoordinates") as CheckBox;
                if (chkShowCoordinates != null) chkShowCoordinates.Checked = _settings.ShowCoordinates;

                var chkEnableAnimations = FindControl("chkEnableAnimations") as CheckBox;
                if (chkEnableAnimations != null) chkEnableAnimations.Checked = _settings.EnableAnimations;

                var chkShowTooltips = FindControl("chkShowTooltips") as CheckBox;
                if (chkShowTooltips != null) chkShowTooltips.Checked = _settings.ShowTooltips;

                // 更新值标签
                UpdateValueLabel("trackLineWidth", _settings.LineWidth.ToString());
                UpdateValueLabel("trackFontSize", _settings.FontSize.ToString());

                // 更新主题预览
                UpdateThemePreview(_settings.Theme);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        #endregion

        #region 辅助方法

        private string[] GetThemeNames()
        {
            return _themeColors.Keys.ToArray();
        }

        private void UpdateValueLabel(string trackBarName, string value)
        {
            // 查找对应的值标签并更新
            foreach (Control control in this.Controls)
            {
                var label = FindValueLabel(control, trackBarName);
                if (label != null)
                {
                    label.Text = value;
                    break;
                }
            }
        }

        private Label FindValueLabel(Control parent, string trackBarName)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is FlowLayoutPanel flowPanel)
                {
                    var trackBar = flowPanel.Controls.OfType<TrackBar>().FirstOrDefault(t => t.Name == trackBarName);
                    if (trackBar != null)
                    {
                        return flowPanel.Controls.OfType<Label>().FirstOrDefault();
                    }
                }

                if (control.HasChildren)
                {
                    var label = FindValueLabel(control, trackBarName);
                    if (label != null) return label;
                }
            }
            return null;
        }

        private void UpdateThemePreview(string theme)
        {
            var pnlThemePreview = FindControl("pnlThemePreview") as Panel;
            if (pnlThemePreview != null && _themeColors.ContainsKey(theme))
            {
                pnlThemePreview.BackColor = _themeColors[theme];
            }
        }

        /// <summary>
        /// 查找指定名称的控件
        /// </summary>
        /// <param name="name">控件名称</param>
        /// <returns>找到的控件，如果未找到返回null</returns>
        private Control FindControl(string name)
        {
            return FindControl(this, name);
        }

        private Control FindControl(Control parent, string name)
        {
            foreach (Control control in parent.Controls)
            {
                if (control.Name == name)
                    return control;

                if (control.HasChildren)
                {
                    var found = FindControl(control, name);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        #endregion

        #region 按钮事件处理

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        private void ResetToDefaults()
        {
            var result = MessageBox.Show("确定要重置所有显示设置为默认值吗？", "确认重置",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _settings = new DisplaySettings();
                UpdateControlsFromSettings();
                SettingsChanged?.Invoke(this, _settings);
                PreviewRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 应用设置
        /// </summary>
        private void ApplySettings()
        {
            UpdateControlsFromSettings();
            MessageBox.Show("显示设置已应用", "设置已保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 预览设置
        /// </summary>
        private void PreviewSettings()
        {
            PreviewRequested?.Invoke(this, EventArgs.Empty);
            MessageBox.Show("预览效果已更新", "预览", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 验证设置
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateSettings()
        {
            var result = new ValidationResult();

            if (_settings.LineWidth < 1 || _settings.LineWidth > 10)
            {
                result.AddError("线条宽度必须在1-10之间");
            }

            if (_settings.FontSize < 8 || _settings.FontSize > 48)
            {
                result.AddError("字体大小必须在8-48之间");
            }

            if (string.IsNullOrEmpty(_settings.Theme))
            {
                result.AddError("必须选择一个主题");
            }

            if (string.IsNullOrEmpty(_settings.Language))
            {
                result.AddError("必须选择一个语言");
            }

            return result;
        }

        /// <summary>
        /// 刷新预览
        /// </summary>
        public void RefreshPreview()
        {
            this.Invalidate();
            PreviewRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 刷新控件显示
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateControlsFromSettings();
            this.Invalidate();
        }

        #endregion

        #region 重写

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Invalidate();
        }

        #endregion
    }
}