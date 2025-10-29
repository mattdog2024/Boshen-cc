using System;
using System.Drawing;
using System.Windows.Forms;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 高级设置控件
    /// 提供应用程序的高级配置选项，包括窗口设置、日志设置、快捷键设置等
    /// </summary>
    public partial class AdvancedSettingsControl : UserControl
    {
        #region 字段

        private AppSettings _settings = new AppSettings();
        private bool _isUpdating = false;

        #endregion

        #region 事件

        /// <summary>
        /// 设置更改事件
        /// </summary>
        public event EventHandler<AppSettings> SettingsChanged;

        #endregion

        #region 属性

        /// <summary>
        /// 当前设置
        /// </summary>
        public AppSettings Settings
        {
            get => _settings;
            set
            {
                if (_settings != value)
                {
                    _settings = value ?? new AppSettings();
                    UpdateControlsFromSettings();
                }
            }
        }

        #endregion

        #region 构造函数

        public AdvancedSettingsControl()
        {
            InitializeComponent();
            InitializeControls();
            SetupEventHandlers();
        }

        #endregion

        #region 初始化

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 控件基本属性
            this.Name = "AdvancedSettingsControl";
            this.Size = new Size(650, 500);
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;

            // 创建控件
            CreateControls();

            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            // 创建主面板
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 15,
                ColumnCount = 3,
                Padding = new Padding(10),
                AutoSize = true
            };

            // 添加列样式
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // 标签
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, 200)); // 控件
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // 说明

            // 窗口设置分组
            var windowGroup = CreateGroupPanel("窗口设置", 0);
            AddWindowSettings(mainPanel, windowGroup);

            // 日志设置分组
            var logGroup = CreateGroupPanel("日志设置", 5);
            AddLogSettings(mainPanel, logGroup);

            // 快捷键设置分组
            var hotkeyGroup = CreateGroupPanel("快捷键设置", 10);
            AddHotkeySettings(mainPanel, hotkeyGroup);

            // 添加按钮面板
            var buttonPanel = CreateButtonPanel();
            mainPanel.Controls.Add(buttonPanel, 0, 14);
            mainPanel.SetColumnSpan(buttonPanel, 3);

            this.Controls.Add(mainPanel);
        }

        private void AddWindowSettings(TableLayoutPanel mainPanel, int startRow)
        {
            // 记住窗口状态
            var chkRememberState = CreateCheckBox("记住窗口状态", _settings.WindowSettings.RememberWindowState, "chkRememberState");
            var lblRememberStateDesc = CreateDescriptionLabel("保存并恢复窗口的位置和大小");

            mainPanel.Controls.Add(chkRememberState, 0, startRow + 1);
            mainPanel.Controls.Add(new Control(), 1, startRow + 1);
            mainPanel.Controls.Add(lblRememberStateDesc, 2, startRow + 1);

            // 窗口大小
            var lblWindowSize = CreateLabel("窗口大小:");
            var sizePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };
            var nudWidth = new NumericUpDown
            {
                Minimum = 400,
                Maximum = 4000,
                Increment = 50,
                Value = _settings.WindowSettings.Width,
                Width = 80,
                Name = "nudWindowWidth"
            };
            var lblX = new Label { Text = " × ", Margin = new Padding(5, 0, 5, 0) };
            var nudHeight = new NumericUpDown
            {
                Minimum = 300,
                Maximum = 3000,
                Increment = 50,
                Value = _settings.WindowSettings.Height,
                Width = 80,
                Name = "nudWindowHeight"
            };
            sizePanel.Controls.Add(nudWidth);
            sizePanel.Controls.Add(lblX);
            sizePanel.Controls.Add(nudHeight);
            var lblWindowSizeDesc = CreateDescriptionLabel("默认窗口尺寸（宽×高）");

            mainPanel.Controls.Add(lblWindowSize, 0, startRow + 2);
            mainPanel.Controls.Add(sizePanel, 1, startRow + 2);
            mainPanel.Controls.Add(lblWindowSizeDesc, 2, startRow + 2);

            // 窗口位置
            var lblWindowPosition = CreateLabel("窗口位置:");
            var positionPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };
            var nudLeft = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 5000,
                Increment = 10,
                Value = _settings.WindowSettings.Left,
                Width = 80,
                Name = "nudWindowLeft"
            };
            var lblComma = new Label { Text = " , ", Margin = new Padding(5, 0, 5, 0) };
            var nudTop = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 3000,
                Increment = 10,
                Value = _settings.WindowSettings.Top,
                Width = 80,
                Name = "nudWindowTop"
            };
            positionPanel.Controls.Add(nudLeft);
            positionPanel.Controls.Add(lblComma);
            positionPanel.Controls.Add(nudTop);
            var lblWindowPositionDesc = CreateDescriptionLabel("默认窗口位置（X, Y）");

            mainPanel.Controls.Add(lblWindowPosition, 0, startRow + 3);
            mainPanel.Controls.Add(positionPanel, 1, startRow + 3);
            mainPanel.Controls.Add(lblWindowPositionDesc, 2, startRow + 3);

            // 启动时最大化
            var chkStartMaximized = CreateCheckBox("启动时最大化", _settings.WindowSettings.IsMaximized, "chkStartMaximized");
            var lblStartMaximizedDesc = CreateDescriptionLabel("应用程序启动时窗口最大化");

            mainPanel.Controls.Add(chkStartMaximized, 0, startRow + 4);
            mainPanel.Controls.Add(new Control(), 1, startRow + 4);
            mainPanel.Controls.Add(lblStartMaximizedDesc, 2, startRow + 4);
        }

        private void AddLogSettings(TableLayoutPanel mainPanel, int startRow)
        {
            // 启用文件日志
            var chkEnableFileLog = CreateCheckBox("启用文件日志", _settings.LogSettings.EnableFileLogging, "chkEnableFileLog");
            var lblEnableFileLogDesc = CreateDescriptionLabel("将日志信息写入文件");

            mainPanel.Controls.Add(chkEnableFileLog, 0, startRow + 1);
            mainPanel.Controls.Add(new Control(), 1, startRow + 1);
            mainPanel.Controls.Add(lblEnableFileLogDesc, 2, startRow + 1);

            // 启用控制台日志
            var chkEnableConsoleLog = CreateCheckBox("启用控制台日志", _settings.LogSettings.EnableConsoleLogging, "chkEnableConsoleLog");
            var lblEnableConsoleLogDesc = CreateDescriptionLabel("在控制台显示日志信息");

            mainPanel.Controls.Add(chkEnableConsoleLog, 0, startRow + 2);
            mainPanel.Controls.Add(new Control(), 1, startRow + 2);
            mainPanel.Controls.Add(lblEnableConsoleLogDesc, 2, startRow + 2);

            // 日志级别
            var lblLogLevel = CreateLabel("日志级别:");
            var cmbLogLevel = CreateComboBox(new[] { "Debug", "Info", "Warning", "Error" }, _settings.LogSettings.LogLevel, "cmbLogLevel");
            var lblLogLevelDesc = CreateDescriptionLabel("记录的最低日志级别");

            mainPanel.Controls.Add(lblLogLevel, 0, startRow + 3);
            mainPanel.Controls.Add(cmbLogLevel, 1, startRow + 3);
            mainPanel.Controls.Add(lblLogLevelDesc, 2, startRow + 3);

            // 最大文件大小
            var lblMaxFileSize = CreateLabel("最大文件大小:");
            var fileSizePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };
            var nudMaxFileSize = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 1000,
                Increment = 1,
                Value = _settings.LogSettings.MaxFileSizeMB,
                Width = 80,
                Name = "nudMaxFileSize"
            };
            var lblMB = new Label { Text = "MB", Margin = new Padding(5, 0, 0, 0) };
            fileSizePanel.Controls.Add(nudMaxFileSize);
            fileSizePanel.Controls.Add(lblMB);
            var lblMaxFileSizeDesc = CreateDescriptionLabel("单个日志文件的最大大小");

            mainPanel.Controls.Add(lblMaxFileSize, 0, startRow + 4);
            mainPanel.Controls.Add(fileSizePanel, 1, startRow + 4);
            mainPanel.Controls.Add(lblMaxFileSizeDesc, 2, startRow + 4);

            // 保留文件数量
            var lblMaxFileCount = CreateLabel("保留文件数量:");
            var nudMaxFileCount = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 50,
                Increment = 1,
                Value = _settings.LogSettings.MaxFileCount,
                Width = 80,
                Name = "nudMaxFileCount"
            };
            var lblMaxFileCountDesc = CreateDescriptionLabel("保留的日志文件数量");

            mainPanel.Controls.Add(lblMaxFileCount, 0, startRow + 5);
            mainPanel.Controls.Add(nudMaxFileCount, 1, startRow + 5);
            mainPanel.Controls.Add(lblMaxFileCountDesc, 2, startRow + 5);
        }

        private void AddHotkeySettings(TableLayoutPanel mainPanel, int startRow)
        {
            // 截图快捷键
            var lblScreenshotHotkey = CreateLabel("截图快捷键:");
            var txtScreenshotHotkey = CreateHotkeyTextBox(_settings.HotkeySettings.ScreenshotHotkey, "txtScreenshotHotkey");
            var btnScreenshotHotkey = CreateHotkeyButton("btnScreenshotHotkey", txtScreenshotHotkey);
            var screenshotPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };
            screenshotPanel.Controls.Add(txtScreenshotHotkey);
            screenshotPanel.Controls.Add(btnScreenshotHotkey);
            var lblScreenshotHotkeyDesc = CreateDescriptionLabel("触发截图功能的快捷键");

            mainPanel.Controls.Add(lblScreenshotHotkey, 0, startRow + 1);
            mainPanel.Controls.Add(screenshotPanel, 1, startRow + 1);
            mainPanel.Controls.Add(lblScreenshotHotkeyDesc, 2, startRow + 1);

            // 识别快捷键
            var lblRecognizeHotkey = CreateLabel("识别快捷键:");
            var txtRecognizeHotkey = CreateHotkeyTextBox(_settings.HotkeySettings.RecognizeHotkey, "txtRecognizeHotkey");
            var btnRecognizeHotkey = CreateHotkeyButton("btnRecognizeHotkey", txtRecognizeHotkey);
            var recognizePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };
            recognizePanel.Controls.Add(txtRecognizeHotkey);
            recognizePanel.Controls.Add(btnRecognizeHotkey);
            var lblRecognizeHotkeyDesc = CreateDescriptionLabel("触发识别功能的快捷键");

            mainPanel.Controls.Add(lblRecognizeHotkey, 0, startRow + 2);
            mainPanel.Controls.Add(recognizePanel, 1, startRow + 2);
            mainPanel.Controls.Add(lblRecognizeHotkeyDesc, 2, startRow + 2);

            // 设置快捷键
            var lblSettingsHotkey = CreateLabel("设置快捷键:");
            var txtSettingsHotkey = CreateHotkeyTextBox(_settings.HotkeySettings.SettingsHotkey, "txtSettingsHotkey");
            var btnSettingsHotkey = CreateHotkeyButton("btnSettingsHotkey", txtSettingsHotkey);
            var settingsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };
            settingsPanel.Controls.Add(txtSettingsHotkey);
            settingsPanel.Controls.Add(btnSettingsHotkey);
            var lblSettingsHotkeyDesc = CreateDescriptionLabel("打开设置窗口的快捷键");

            mainPanel.Controls.Add(lblSettingsHotkey, 0, startRow + 3);
            mainPanel.Controls.Add(settingsPanel, 1, startRow + 3);
            mainPanel.Controls.Add(lblSettingsHotkeyDesc, 2, startRow + 3);
        }

        private Panel CreateGroupPanel(string title, int row)
        {
            var panel = new Panel
            {
                Height = 30,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(233, 236, 239),
                Margin = new Padding(0, 5, 0, 5)
            };

            var lblTitle = new Label
            {
                Text = title,
                Location = new Point(10, 5),
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 58, 64),
                AutoSize = true
            };

            panel.Controls.Add(lblTitle);
            return panel;
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
                    case NumericUpDown nud:
                        nud.ValueChanged += (s, e) => UpdateSettingsFromControls();
                        break;
                    case CheckBox chk:
                        chk.CheckedChanged += (s, e) => UpdateSettingsFromControls();
                        break;
                    case ComboBox cmb:
                        cmb.SelectedIndexChanged += (s, e) => UpdateSettingsFromControls();
                        break;
                    case TextBox txt:
                        txt.TextChanged += (s, e) => UpdateSettingsFromControls();
                        break;
                    case Button btn:
                        btn.Click += Button_Click;
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

        private void Button_Click(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button?.Name.StartsWith("btn") == true && button.Name.EndsWith("Hotkey"))
            {
                var textBoxName = button.Name.Replace("btn", "txt");
                var textBox = FindControl(textBoxName) as TextBox;
                if (textBox != null)
                {
                    CaptureHotkey(textBox);
                }
            }
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

        private TextBox CreateHotkeyTextBox(string text, string name)
        {
            return new TextBox
            {
                Text = text,
                Name = name,
                Width = 120,
                ReadOnly = true,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular)
            };
        }

        private Button CreateHotkeyButton(string name, TextBox targetTextBox)
        {
            var button = new Button
            {
                Text = "设置",
                Name = name,
                Width = 60,
                Height = 23,
                Margin = new Padding(5, 0, 0, 0),
                Tag = targetTextBox
            };
            return button;
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

            // 按钮布局
            btnReset.Location = new Point(panel.Width - 170, 5);
            btnApply.Location = new Point(panel.Width - 80, 5);

            panel.Controls.Add(btnReset);
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

        #region 快捷键捕获

        /// <summary>
        /// 捕获快捷键
        /// </summary>
        /// <param name="textBox">显示快捷键的文本框</param>
        private void CaptureHotkey(TextBox textBox)
        {
            using (var hotkeyDialog = new HotkeyCaptureDialog())
            {
                if (hotkeyDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = hotkeyDialog.Hotkey;
                    UpdateSettingsFromControls();
                }
            }
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
                // 更新窗口设置
                var chkRememberState = FindControl("chkRememberState") as CheckBox;
                if (chkRememberState != null) chkRememberState.Checked = _settings.WindowSettings.RememberWindowState;

                var chkStartMaximized = FindControl("chkStartMaximized") as CheckBox;
                if (chkStartMaximized != null) chkStartMaximized.Checked = _settings.WindowSettings.IsMaximized;

                var nudWindowWidth = FindControl("nudWindowWidth") as NumericUpDown;
                if (nudWindowWidth != null) nudWindowWidth.Value = _settings.WindowSettings.Width;

                var nudWindowHeight = FindControl("nudWindowHeight") as NumericUpDown;
                if (nudWindowHeight != null) nudWindowHeight.Value = _settings.WindowSettings.Height;

                var nudWindowLeft = FindControl("nudWindowLeft") as NumericUpDown;
                if (nudWindowLeft != null) nudWindowLeft.Value = _settings.WindowSettings.Left;

                var nudWindowTop = FindControl("nudWindowTop") as NumericUpDown;
                if (nudWindowTop != null) nudWindowTop.Value = _settings.WindowSettings.Top;

                // 更新日志设置
                var chkEnableFileLog = FindControl("chkEnableFileLog") as CheckBox;
                if (chkEnableFileLog != null) chkEnableFileLog.Checked = _settings.LogSettings.EnableFileLogging;

                var chkEnableConsoleLog = FindControl("chkEnableConsoleLog") as CheckBox;
                if (chkEnableConsoleLog != null) chkEnableConsoleLog.Checked = _settings.LogSettings.EnableConsoleLogging;

                var cmbLogLevel = FindControl("cmbLogLevel") as ComboBox;
                if (cmbLogLevel != null) cmbLogLevel.SelectedItem = _settings.LogSettings.LogLevel;

                var nudMaxFileSize = FindControl("nudMaxFileSize") as NumericUpDown;
                if (nudMaxFileSize != null) nudMaxFileSize.Value = _settings.LogSettings.MaxFileSizeMB;

                var nudMaxFileCount = FindControl("nudMaxFileCount") as NumericUpDown;
                if (nudMaxFileCount != null) nudMaxFileCount.Value = _settings.LogSettings.MaxFileCount;

                // 更新快捷键设置
                var txtScreenshotHotkey = FindControl("txtScreenshotHotkey") as TextBox;
                if (txtScreenshotHotkey != null) txtScreenshotHotkey.Text = _settings.HotkeySettings.ScreenshotHotkey;

                var txtRecognizeHotkey = FindControl("txtRecognizeHotkey") as TextBox;
                if (txtRecognizeHotkey != null) txtRecognizeHotkey.Text = _settings.HotkeySettings.RecognizeHotkey;

                var txtSettingsHotkey = FindControl("txtSettingsHotkey") as TextBox;
                if (txtSettingsHotkey != null) txtSettingsHotkey.Text = _settings.HotkeySettings.SettingsHotkey;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// 从控件更新设置
        /// </summary>
        private void UpdateSettingsFromControls()
        {
            if (_isUpdating) return;

            try
            {
                // 更新窗口设置
                var chkRememberState = FindControl("chkRememberState") as CheckBox;
                if (chkRememberState != null) _settings.WindowSettings.RememberWindowState = chkRememberState.Checked;

                var chkStartMaximized = FindControl("chkStartMaximized") as CheckBox;
                if (chkStartMaximized != null) _settings.WindowSettings.IsMaximized = chkStartMaximized.Checked;

                var nudWindowWidth = FindControl("nudWindowWidth") as NumericUpDown;
                if (nudWindowWidth != null) _settings.WindowSettings.Width = (int)nudWindowWidth.Value;

                var nudWindowHeight = FindControl("nudWindowHeight") as NumericUpDown;
                if (nudWindowHeight != null) _settings.WindowSettings.Height = (int)nudWindowHeight.Value;

                var nudWindowLeft = FindControl("nudWindowLeft") as NumericUpDown;
                if (nudWindowLeft != null) _settings.WindowSettings.Left = (int)nudWindowLeft.Value;

                var nudWindowTop = FindControl("nudWindowTop") as NumericUpDown;
                if (nudWindowTop != null) _settings.WindowSettings.Top = (int)nudWindowTop.Value;

                // 更新日志设置
                var chkEnableFileLog = FindControl("chkEnableFileLog") as CheckBox;
                if (chkEnableFileLog != null) _settings.LogSettings.EnableFileLogging = chkEnableFileLog.Checked;

                var chkEnableConsoleLog = FindControl("chkEnableConsoleLog") as CheckBox;
                if (chkEnableConsoleLog != null) _settings.LogSettings.EnableConsoleLogging = chkEnableConsoleLog.Checked;

                var cmbLogLevel = FindControl("cmbLogLevel") as ComboBox;
                if (cmbLogLevel != null) _settings.LogSettings.LogLevel = cmbLogLevel.SelectedItem?.ToString() ?? "Info";

                var nudMaxFileSize = FindControl("nudMaxFileSize") as NumericUpDown;
                if (nudMaxFileSize != null) _settings.LogSettings.MaxFileSizeMB = (int)nudMaxFileSize.Value;

                var nudMaxFileCount = FindControl("nudMaxFileCount") as NumericUpDown;
                if (nudMaxFileCount != null) _settings.LogSettings.MaxFileCount = (int)nudMaxFileCount.Value;

                // 更新快捷键设置
                var txtScreenshotHotkey = FindControl("txtScreenshotHotkey") as TextBox;
                if (txtScreenshotHotkey != null) _settings.HotkeySettings.ScreenshotHotkey = txtScreenshotHotkey.Text;

                var txtRecognizeHotkey = FindControl("txtRecognizeHotkey") as TextBox;
                if (txtRecognizeHotkey != null) _settings.HotkeySettings.RecognizeHotkey = txtRecognizeHotkey.Text;

                var txtSettingsHotkey = FindControl("txtSettingsHotkey") as TextBox;
                if (txtSettingsHotkey != null) _settings.HotkeySettings.SettingsHotkey = txtSettingsHotkey.Text;

                // 触发设置更改事件
                SettingsChanged?.Invoke(this, _settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新设置时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 控件查找

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
            var result = MessageBox.Show("确定要重置所有高级设置为默认值吗？", "确认重置",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _settings = new AppSettings();
                UpdateControlsFromSettings();
                SettingsChanged?.Invoke(this, _settings);
            }
        }

        /// <summary>
        /// 应用设置
        /// </summary>
        private void ApplySettings()
        {
            UpdateSettingsFromControls();
            MessageBox.Show("高级设置已应用", "设置已保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            if (_settings.WindowSettings.Width < 400 || _settings.WindowSettings.Width > 4000)
            {
                result.AddError("窗口宽度必须在400-4000之间");
            }

            if (_settings.WindowSettings.Height < 300 || _settings.WindowSettings.Height > 3000)
            {
                result.AddError("窗口高度必须在300-3000之间");
            }

            if (_settings.LogSettings.MaxFileSizeMB < 1 || _settings.LogSettings.MaxFileSizeMB > 1000)
            {
                result.AddError("日志文件大小限制必须在1-1000MB之间");
            }

            if (_settings.LogSettings.MaxFileCount < 1 || _settings.LogSettings.MaxFileCount > 50)
            {
                result.AddError("日志文件数量限制必须在1-50之间");
            }

            return result;
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

    #region 快捷键捕获对话框

    /// <summary>
    /// 快捷键捕获对话框
    /// </summary>
    public class HotkeyCaptureDialog : Form
    {
        private TextBox _textBox;
        private Button _btnOK;
        private Button _btnCancel;
        private string _hotkey = "";

        public string Hotkey => _hotkey;

        public HotkeyCaptureDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "设置快捷键";
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblInstruction = new Label
            {
                Text = "请按下要设置的快捷键组合:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular)
            };

            _textBox = new TextBox
            {
                Location = new Point(20, 50),
                Width = 240,
                ReadOnly = true,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold)
            };
            _textBox.KeyDown += TextBox_KeyDown;

            _btnOK = new Button
            {
                Text = "确定",
                Location = new Point(110, 80),
                Width = 80,
                Height = 30,
                DialogResult = DialogResult.OK
            };

            _btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(200, 80),
                Width = 60,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(lblInstruction);
            this.Controls.Add(_textBox);
            this.Controls.Add(_btnOK);
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOK;
            this.CancelButton = _btnCancel;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;

            var modifiers = "";
            if (e.Control) modifiers += "Ctrl+";
            if (e.Alt) modifiers += "Alt+";
            if (e.Shift) modifiers += "Shift+";

            if (e.KeyCode != Keys.ControlKey && e.KeyCode != Keys.Menu && e.KeyCode != Keys.ShiftKey)
            {
                _hotkey = modifiers + e.KeyCode;
                _textBox.Text = _hotkey;
            }
        }
    }

    #endregion
}