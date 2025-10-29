using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 用户偏好设置控件
    /// 提供个人使用偏好和系统行为的配置选项
    /// </summary>
    public partial class UserPreferencesControl : UserControl
    {
        #region 字段

        private UserPreferences _settings = new UserPreferences();
        private bool _isUpdating = false;

        #endregion

        #region 事件

        /// <summary>
        /// 设置更改事件
        /// </summary>
        public event EventHandler<UserPreferences> SettingsChanged;

        #endregion

        #region 属性

        /// <summary>
        /// 当前设置
        /// </summary>
        public UserPreferences Settings
        {
            get => _settings;
            set
            {
                if (_settings != value)
                {
                    _settings = value ?? new UserPreferences();
                    UpdateControlsFromSettings();
                }
            }
        }

        #endregion

        #region 构造函数

        public UserPreferencesControl()
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
            this.Name = "UserPreferencesControl";
            this.Size = new Size(600, 450);
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
                RowCount = 10,
                ColumnCount = 3,
                Padding = new Padding(10),
                AutoSize = true
            };

            // 添加列样式
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // 标签
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, 200)); // 控件
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // 说明

            // 自动保存设置
            var lblAutoSave = CreateLabel("自动保存:");
            var chkAutoSave = CreateCheckBox("启用自动保存", _settings.AutoSave, "chkAutoSave");
            var lblAutoSaveDesc = CreateDescriptionLabel("定期自动保存当前工作状态");

            // 自动保存间隔
            var lblAutoSaveInterval = CreateLabel("保存间隔:");
            var nudAutoSaveInterval = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 60,
                Increment = 1,
                Value = _settings.AutoSaveInterval,
                Width = 80,
                Name = "nudAutoSaveInterval"
            };
            var lblAutoSaveIntervalDesc = CreateDescriptionLabel("自动保存间隔时间（分钟）");

            // 清除前确认
            var chkConfirmClear = CreateCheckBox("清除前确认", _settings.ConfirmBeforeClear, "chkConfirmClear");
            var lblConfirmClearDesc = CreateDescriptionLabel("清除预测线前显示确认对话框");

            // 启用声音
            var chkEnableSounds = CreateCheckBox("启用声音", _settings.EnableSounds, "chkEnableSounds");
            var lblEnableSoundsDesc = CreateDescriptionLabel("操作完成时播放提示音");

            // 随Windows启动
            var chkStartWithWindows = CreateCheckBox("随Windows启动", _settings.StartWithWindows, "chkStartWithWindows");
            var lblStartWithWindowsDesc = CreateDescriptionLabel("系统启动时自动运行波神计算器");

            // 检查更新
            var chkCheckUpdates = CreateCheckBox("检查更新", _settings.CheckForUpdates, "chkCheckUpdates");
            var lblCheckUpdatesDesc = CreateDescriptionLabel("定期检查应用程序更新");

            // 显示欢迎消息
            var chkShowWelcome = CreateCheckBox("显示欢迎消息", _settings.ShowWelcomeMessage, "chkShowWelcome");
            var lblShowWelcomeDesc = CreateDescriptionLabel("启动时显示欢迎和提示信息");

            // 最后使用路径
            var lblLastPath = CreateLabel("默认路径:");
            var txtLastPath = new TextBox
            {
                Text = _settings.LastUsedPath,
                Width = 180,
                Name = "txtLastPath"
            };
            var btnBrowsePath = new Button
            {
                Text = "浏览...",
                Width = 60,
                Height = 23,
                Name = "btnBrowsePath"
            };
            var pathPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };
            pathPanel.Controls.Add(txtLastPath);
            pathPanel.Controls.Add(btnBrowsePath);
            var lblLastPathDesc = CreateDescriptionLabel("默认的文件打开和保存路径");

            // 添加控件到面板
            mainPanel.Controls.Add(lblAutoSave, 0, 0);
            mainPanel.Controls.Add(chkAutoSave, 1, 0);
            mainPanel.Controls.Add(lblAutoSaveDesc, 2, 0);

            mainPanel.Controls.Add(lblAutoSaveInterval, 0, 1);
            var intervalPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };
            intervalPanel.Controls.Add(nudAutoSaveInterval);
            var lblMinutes = new Label { Text = "分钟", Margin = new Padding(5, 0, 0, 0) };
            intervalPanel.Controls.Add(lblMinutes);
            mainPanel.Controls.Add(intervalPanel, 1, 1);
            mainPanel.Controls.Add(lblAutoSaveIntervalDesc, 2, 1);

            mainPanel.Controls.Add(chkConfirmClear, 0, 2);
            mainPanel.Controls.Add(new Control(), 1, 2);
            mainPanel.Controls.Add(lblConfirmClearDesc, 2, 2);

            mainPanel.Controls.Add(chkEnableSounds, 0, 3);
            mainPanel.Controls.Add(new Control(), 1, 3);
            mainPanel.Controls.Add(lblEnableSoundsDesc, 2, 3);

            mainPanel.Controls.Add(chkStartWithWindows, 0, 4);
            mainPanel.Controls.Add(new Control(), 1, 4);
            mainPanel.Controls.Add(lblStartWithWindowsDesc, 2, 4);

            mainPanel.Controls.Add(chkCheckUpdates, 0, 5);
            mainPanel.Controls.Add(new Control(), 1, 5);
            mainPanel.Controls.Add(lblCheckUpdatesDesc, 2, 5);

            mainPanel.Controls.Add(chkShowWelcome, 0, 6);
            mainPanel.Controls.Add(new Control(), 1, 6);
            mainPanel.Controls.Add(lblShowWelcomeDesc, 2, 6);

            mainPanel.Controls.Add(lblLastPath, 0, 7);
            mainPanel.Controls.Add(pathPanel, 1, 7);
            mainPanel.Controls.Add(lblLastPathDesc, 2, 7);

            // 添加统计信息面板
            var statsPanel = CreateStatsPanel();
            mainPanel.Controls.Add(statsPanel, 0, 8);
            mainPanel.SetColumnSpan(statsPanel, 3);

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
                    case NumericUpDown nud:
                        nud.ValueChanged += (s, e) => UpdateSettingsFromControls();
                        break;
                    case CheckBox chk:
                        chk.CheckedChanged += (s, e) => UpdateSettingsFromControls();
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
            if (button?.Name == "btnBrowsePath")
            {
                BrowseForDefaultPath();
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

        private Panel CreateStatsPanel()
        {
            var panel = new Panel
            {
                Height = 80,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 250),
                Margin = new Padding(0, 10, 0, 10)
            };

            var lblStatsTitle = new Label
            {
                Text = "使用统计",
                Location = new Point(10, 5),
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                AutoSize = true
            };

            var lblStatsInfo = new Label
            {
                Text = GetUsageStatsText(),
                Location = new Point(10, 25),
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(108, 117, 125),
                AutoSize = true
            };

            var btnClearStats = new Button
            {
                Text = "清除统计",
                Location = new Point(panel.Width - 100, 45),
                Width = 80,
                Height = 25,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnClearStats.Click += (s, e) => ClearUsageStats();

            panel.Controls.Add(lblStatsTitle);
            panel.Controls.Add(lblStatsInfo);
            panel.Controls.Add(btnClearStats);

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
                // 更新CheckBox
                var chkAutoSave = FindControl("chkAutoSave") as CheckBox;
                if (chkAutoSave != null) chkAutoSave.Checked = _settings.AutoSave;

                var chkConfirmClear = FindControl("chkConfirmClear") as CheckBox;
                if (chkConfirmClear != null) chkConfirmClear.Checked = _settings.ConfirmBeforeClear;

                var chkEnableSounds = FindControl("chkEnableSounds") as CheckBox;
                if (chkEnableSounds != null) chkEnableSounds.Checked = _settings.EnableSounds;

                var chkStartWithWindows = FindControl("chkStartWithWindows") as CheckBox;
                if (chkStartWithWindows != null) chkStartWithWindows.Checked = _settings.StartWithWindows;

                var chkCheckUpdates = FindControl("chkCheckUpdates") as CheckBox;
                if (chkCheckUpdates != null) chkCheckUpdates.Checked = _settings.CheckForUpdates;

                var chkShowWelcome = FindControl("chkShowWelcome") as CheckBox;
                if (chkShowWelcome != null) chkShowWelcome.Checked = _settings.ShowWelcomeMessage;

                // 更新NumericUpDown
                var nudAutoSaveInterval = FindControl("nudAutoSaveInterval") as NumericUpDown;
                if (nudAutoSaveInterval != null) nudAutoSaveInterval.Value = _settings.AutoSaveInterval;

                // 更新TextBox
                var txtLastPath = FindControl("txtLastPath") as TextBox;
                if (txtLastPath != null) txtLastPath.Text = _settings.LastUsedPath;
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
                var chkAutoSave = FindControl("chkAutoSave") as CheckBox;
                if (chkAutoSave != null) _settings.AutoSave = chkAutoSave.Checked;

                var chkConfirmClear = FindControl("chkConfirmClear") as CheckBox;
                if (chkConfirmClear != null) _settings.ConfirmBeforeClear = chkConfirmClear.Checked;

                var chkEnableSounds = FindControl("chkEnableSounds") as CheckBox;
                if (chkEnableSounds != null) _settings.EnableSounds = chkEnableSounds.Checked;

                var chkStartWithWindows = FindControl("chkStartWithWindows") as CheckBox;
                if (chkStartWithWindows != null) _settings.StartWithWindows = chkStartWithWindows.Checked;

                var chkCheckUpdates = FindControl("chkCheckUpdates") as CheckBox;
                if (chkCheckUpdates != null) _settings.CheckForUpdates = chkCheckUpdates.Checked;

                var chkShowWelcome = FindControl("chkShowWelcome") as CheckBox;
                if (chkShowWelcome != null) _settings.ShowWelcomeMessage = chkShowWelcome.Checked;

                var nudAutoSaveInterval = FindControl("nudAutoSaveInterval") as NumericUpDown;
                if (nudAutoSaveInterval != null) _settings.AutoSaveInterval = (int)nudAutoSaveInterval.Value;

                var txtLastPath = FindControl("txtLastPath") as TextBox;
                if (txtLastPath != null) _settings.LastUsedPath = txtLastPath.Text;

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

        #region 功能方法

        /// <summary>
        /// 浏览默认路径
        /// </summary>
        private void BrowseForDefaultPath()
        {
            using (var folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "选择默认文件路径",
                ShowNewFolderButton = true,
                SelectedPath = _settings.LastUsedPath
            })
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    var txtLastPath = FindControl("txtLastPath") as TextBox;
                    if (txtLastPath != null)
                    {
                        txtLastPath.Text = folderBrowserDialog.SelectedPath;
                        UpdateSettingsFromControls();
                    }
                }
            }
        }

        /// <summary>
        /// 获取使用统计文本
        /// </summary>
        /// <returns>统计信息文本</returns>
        private string GetUsageStatsText()
        {
            // 这里可以实现真实的使用统计功能
            // 目前返回模拟数据
            return $"应用程序启动次数: 15\n" +
                   $"算法计算次数: 128\n" +
                   $"图像处理次数: 45\n" +
                   $"平均使用时长: 12分钟";
        }

        /// <summary>
        /// 清除使用统计
        /// </summary>
        private void ClearUsageStats()
        {
            var result = MessageBox.Show("确定要清除所有使用统计数据吗？", "确认清除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // 这里实现清除统计数据的逻辑
                MessageBox.Show("使用统计数据已清除", "清除完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        #region 按钮事件处理

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        private void ResetToDefaults()
        {
            var result = MessageBox.Show("确定要重置所有用户偏好设置为默认值吗？", "确认重置",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _settings = new UserPreferences();
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

            // 如果设置了随Windows启动，更新注册表
            if (_settings.StartWithWindows)
            {
                SetStartupWithWindows(true);
            }
            else
            {
                SetStartupWithWindows(false);
            }

            MessageBox.Show("用户偏好设置已应用", "设置已保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 设置开机启动
        /// </summary>
        /// <param name="enable">是否启用</param>
        private void SetStartupWithWindows(bool enable)
        {
            try
            {
                // 这里可以实现注册表操作来设置开机启动
                // 由于涉及系统注册表，需要管理员权限
                if (enable)
                {
                    // 添加到启动项
                }
                else
                {
                    // 从启动项移除
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置开机启动时发生错误: {ex.Message}", "设置错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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

            if (_settings.AutoSaveInterval < 1 || _settings.AutoSaveInterval > 60)
            {
                result.AddError("自动保存间隔必须在1-60分钟之间");
            }

            if (!string.IsNullOrEmpty(_settings.LastUsedPath) && !Directory.Exists(_settings.LastUsedPath))
            {
                result.AddWarning("默认路径不存在，将在首次使用时创建");
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
}