using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BoshenCC.Models;
using BoshenCC.WinForms.Controls;
using Newtonsoft.Json;

namespace BoshenCC.WinForms.Views
{
    /// <summary>
    /// 设置窗口
    /// 提供完整的应用程序设置和配置界面
    /// </summary>
    public partial class SettingsWindow : Form
    {
        #region 字段

        private UserSettings _currentSettings = new UserSettings();
        private UserSettings _originalSettings = new UserSettings();
        private string _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BoshenCC",
            "settings.json"
        );

        // 控件
        private TabControl _tabControl;
        private AlgorithmSettings _algorithmSettingsControl;
        private DisplayOptions _displayOptionsControl;
        private UserPreferencesControl _userPreferencesControl;
        private AdvancedSettingsControl _advancedSettingsControl;

        // 按钮
        private Button _btnOK;
        private Button _btnCancel;
        private Button _btnApply;
        private Button _btnReset;
        private Button _btnImport;
        private Button _btnExport;

        #endregion

        #region 事件

        /// <summary>
        /// 设置应用事件
        /// </summary>
        public event EventHandler<UserSettings> SettingsApplied;

        #endregion

        #region 属性

        /// <summary>
        /// 当前设置
        /// </summary>
        public UserSettings CurrentSettings
        {
            get => _currentSettings;
            set
            {
                if (_currentSettings != value)
                {
                    _currentSettings = value ?? new UserSettings();
                    _originalSettings = new UserSettings();
                    _currentSettings.CopyTo(_originalSettings);
                    UpdateControlsFromSettings();
                }
            }
        }

        #endregion

        #region 构造函数

        public SettingsWindow()
        {
            InitializeComponent();
            InitializeControls();
            SetupEventHandlers();
            LoadSettings();
        }

        public SettingsWindow(UserSettings initialSettings) : this()
        {
            CurrentSettings = initialSettings;
        }

        #endregion

        #region 初始化

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 窗口基本属性
            this.Text = "设置 - 波神算法计算器";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Icon = SystemIcons.Application;

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
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(10)
            };

            // 标题栏
            var titlePanel = CreateTitlePanel();

            // Tab控件
            _tabControl = CreateTabControl();

            // 按钮面板
            var buttonPanel = CreateButtonPanel();

            // 添加到主面板
            mainPanel.Controls.Add(titlePanel, 0, 0);
            mainPanel.Controls.Add(_tabControl, 0, 1);
            mainPanel.Controls.Add(buttonPanel, 0, 2);

            // 设置行高
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            this.Controls.Add(mainPanel);
        }

        private Panel CreateTitlePanel()
        {
            var panel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(33, 150, 243)
            };

            var lblTitle = new Label
            {
                Text = "应用程序设置",
                Location = new Point(20, 15),
                Font = new Font("Microsoft YaHei", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "配置波神算法参数、显示选项和用户偏好",
                Location = new Point(250, 20),
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 220, 220),
                AutoSize = true
            };

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblSubtitle);

            return panel;
        }

        private TabControl CreateTabControl()
        {
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular),
                Appearance = TabAppearance.FlatButtons,
                Margin = new Padding(0, 10, 0, 10)
            };

            // 算法设置页
            var algorithmPage = new TabPage("算法设置");
            _algorithmSettingsControl = new AlgorithmSettings
            {
                Dock = DockStyle.Fill
            };
            algorithmPage.Controls.Add(_algorithmSettingsControl);

            // 显示选项页
            var displayPage = new TabPage("显示选项");
            _displayOptionsControl = new DisplayOptions
            {
                Dock = DockStyle.Fill
            };
            displayPage.Controls.Add(_displayOptionsControl);

            // 用户偏好页
            var preferencesPage = new TabPage("用户偏好");
            _userPreferencesControl = new UserPreferencesControl
            {
                Dock = DockStyle.Fill
            };
            preferencesPage.Controls.Add(_userPreferencesControl);

            // 高级设置页
            var advancedPage = new TabPage("高级设置");
            _advancedSettingsControl = new AdvancedSettingsControl
            {
                Dock = DockStyle.Fill
            };
            advancedPage.Controls.Add(_advancedSettingsControl);

            // 添加页面
            tabControl.TabPages.Add(algorithmPage);
            tabControl.TabPages.Add(displayPage);
            tabControl.TabPages.Add(preferencesPage);
            tabControl.TabPages.Add(advancedPage);

            return tabControl;
        }

        private Panel CreateButtonPanel()
        {
            var panel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            // 创建按钮
            _btnOK = CreateButton("确定", Color.FromArgb(40, 167, 69));
            _btnOK.Click += BtnOK_Click;

            _btnCancel = CreateButton("取消", Color.FromArgb(108, 117, 125));
            _btnCancel.Click += BtnCancel_Click;

            _btnApply = CreateButton("应用", Color.FromArgb(23, 162, 184));
            _btnApply.Click += BtnApply_Click;

            _btnReset = CreateButton("重置", Color.FromArgb(255, 193, 7));
            _btnReset.Click += BtnReset_Click;

            _btnImport = CreateButton("导入", Color.FromArgb(6, 82, 221));
            _btnImport.Click += BtnImport_Click;

            _btnExport = CreateButton("导出", Color.FromArgb(13, 110, 253));
            _btnExport.Click += BtnExport_Click;

            // 按钮布局
            _btnOK.Location = new Point(panel.Width - 350, 5);
            _btnCancel.Location = new Point(panel.Width - 260, 5);
            _btnApply.Location = new Point(panel.Width - 170, 5);
            _btnReset.Location = new Point(panel.Width - 80, 5);
            _btnImport.Location = new Point(20, 5);
            _btnExport.Location = new Point(110, 5);

            panel.Controls.Add(_btnOK);
            panel.Controls.Add(_btnCancel);
            panel.Controls.Add(_btnApply);
            panel.Controls.Add(_btnReset);
            panel.Controls.Add(_btnImport);
            panel.Controls.Add(_btnExport);

            return panel;
        }

        private Button CreateButton(string text, Color backColor)
        {
            return new Button
            {
                Text = text,
                Width = 70,
                Height = 30,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular),
                UseVisualStyleBackColor = false
            };
        }

        private void InitializeControls()
        {
            // 设置控件初始状态
            UpdateControlsFromSettings();
        }

        private void SetupEventHandlers()
        {
            // 设置控件事件处理
            _algorithmSettingsControl.SettingsChanged += AlgorithmSettings_SettingsChanged;
            _displayOptionsControl.SettingsChanged += DisplayOptions_SettingsChanged;
            _displayOptionsControl.PreviewRequested += DisplayOptions_PreviewRequested;

            // 窗口事件
            this.FormClosing += SettingsWindow_FormClosing;
            this.Load += SettingsWindow_Load;
        }

        #endregion

        #region 事件处理

        private void AlgorithmSettings_SettingsChanged(object sender, AlgorithmSettings settings)
        {
            _currentSettings.AlgorithmSettings = settings;
            _btnApply.Enabled = HasUnsavedChanges();
        }

        private void DisplayOptions_SettingsChanged(object sender, DisplaySettings settings)
        {
            _currentSettings.DisplaySettings = settings;
            _btnApply.Enabled = HasUnsavedChanges();
        }

        private void DisplayOptions_PreviewRequested(object sender, EventArgs e)
        {
            // 预览显示效果
            ApplyDisplayPreview();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (ValidateAndApply())
            {
                SaveSettings();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            // 恢复原始设置
            _originalSettings.CopyTo(_currentSettings);
            UpdateControlsFromSettings();
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (ValidateAndApply())
            {
                SaveSettings();
                _originalSettings = new UserSettings();
                _currentSettings.CopyTo(_originalSettings);
                _btnApply.Enabled = false;
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("确定要重置所有设置为默认值吗？\n此操作将撤销所有未保存的更改。",
                "确认重置", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _currentSettings.ResetToDefaults();
                UpdateControlsFromSettings();
                _btnApply.Enabled = HasUnsavedChanges();
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            ImportSettings();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            ExportSettings();
        }

        private void SettingsWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (HasUnsavedChanges() && this.DialogResult == DialogResult.None)
            {
                var result = MessageBox.Show("您有未保存的更改，确定要关闭吗？", "未保存的更改",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void SettingsWindow_Load(object sender, EventArgs e)
        {
            _btnApply.Enabled = false;
        }

        #endregion

        #region 设置管理

        /// <summary>
        /// 从设置更新控件
        /// </summary>
        private void UpdateControlsFromSettings()
        {
            _algorithmSettingsControl.Settings = _currentSettings.AlgorithmSettings;
            _displayOptionsControl.Settings = _currentSettings.DisplaySettings;
            _userPreferencesControl.Settings = _currentSettings.UserPreferences;
            _advancedSettingsControl.Settings = _currentSettings.AppSettings;
        }

        /// <summary>
        /// 验证并应用设置
        /// </summary>
        /// <returns>验证是否通过</returns>
        private bool ValidateAndApply()
        {
            // 验证算法设置
            var algorithmValidation = _algorithmSettingsControl.ValidateSettings();
            if (!algorithmValidation.IsValid)
            {
                ShowValidationErrors(algorithmValidation);
                _tabControl.SelectedTab = _tabControl.TabPages[0];
                return false;
            }

            // 验证显示设置
            var displayValidation = _displayOptionsControl.ValidateSettings();
            if (!displayValidation.IsValid)
            {
                ShowValidationErrors(displayValidation);
                _tabControl.SelectedTab = _tabControl.TabPages[1];
                return false;
            }

            // 应用设置
            try
            {
                SettingsApplied?.Invoke(this, _currentSettings);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用设置时发生错误: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 应用显示预览
        /// </summary>
        private void ApplyDisplayPreview()
        {
            // 这里可以实现实时预览效果
            // 例如更新主窗口的主题、字体等
            this.Invalidate();
        }

        /// <summary>
        /// 检查是否有未保存的更改
        /// </summary>
        /// <returns>是否有未保存的更改</returns>
        private bool HasUnsavedChanges()
        {
            // 比较当前设置和原始设置
            return !AreSettingsEqual(_currentSettings, _originalSettings);
        }

        /// <summary>
        /// 比较两个设置对象是否相等
        /// </summary>
        /// <param name="settings1">设置1</param>
        /// <param name="settings2">设置2</param>
        /// <returns>是否相等</returns>
        private bool AreSettingsEqual(UserSettings settings1, UserSettings settings2)
        {
            if (settings1 == null && settings2 == null) return true;
            if (settings1 == null || settings2 == null) return false;

            // 简化的比较逻辑，实际应用中可能需要更详细的比较
            return settings1.AlgorithmSettings.PriceThreshold == settings2.AlgorithmSettings.PriceThreshold &&
                   settings1.AlgorithmSettings.PricePrecision == settings2.AlgorithmSettings.PricePrecision &&
                   settings1.DisplaySettings.LineWidth == settings2.DisplaySettings.LineWidth &&
                   settings1.DisplaySettings.Theme == settings2.DisplaySettings.Theme &&
                   settings1.DisplaySettings.FontSize == settings2.DisplaySettings.FontSize;
        }

        #endregion

        #region 文件操作

        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonConvert.DeserializeObject<UserSettings>(json);
                    if (settings != null)
                    {
                        CurrentSettings = settings;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载设置文件时发生错误: {ex.Message}", "加载错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(_currentSettings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置文件时发生错误: {ex.Message}", "保存错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 导入设置
        /// </summary>
        private void ImportSettings()
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Title = "导入设置文件",
                Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var json = File.ReadAllText(openFileDialog.FileName);
                        var settings = JsonConvert.DeserializeObject<UserSettings>(json);
                        if (settings != null)
                        {
                            CurrentSettings = settings;
                            MessageBox.Show("设置导入成功！", "导入完成",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导入设置时发生错误: {ex.Message}", "导入错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// 导出设置
        /// </summary>
        private void ExportSettings()
        {
            using (var saveFileDialog = new SaveFileDialog
            {
                Title = "导出设置文件",
                Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FilterIndex = 1,
                FileName = $"BoshenCC_Settings_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                RestoreDirectory = true
            })
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var json = JsonConvert.SerializeObject(_currentSettings, Formatting.Indented);
                        File.WriteAllText(saveFileDialog.FileName, json);
                        MessageBox.Show("设置导出成功！", "导出完成",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出设置时发生错误: {ex.Message}", "导出错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 显示验证错误
        /// </summary>
        /// <param name="validationResult">验证结果</param>
        private void ShowValidationErrors(ValidationResult validationResult)
        {
            var errorMessage = "设置验证失败:\n\n" + string.Join("\n", validationResult.Errors);
            MessageBox.Show(errorMessage, "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 显示设置窗口
        /// </summary>
        /// <param name="owner">父窗口</param>
        /// <param name="initialSettings">初始设置</param>
        /// <returns>对话框结果</returns>
        public static DialogResult ShowSettings(IWin32Window owner, UserSettings initialSettings = null)
        {
            using (var settingsWindow = new SettingsWindow(initialSettings))
            {
                return settingsWindow.ShowDialog(owner);
            }
        }

        /// <summary>
        /// 刷新窗口显示
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

    #region 临时控件类（将在后续创建）

    /// <summary>
    /// 用户偏好设置控件（临时占位，后续需要完整实现）
    /// </summary>
    public class UserPreferencesControl : UserControl
    {
        public UserPreferences Settings { get; set; }

        public UserPreferencesControl()
        {
            this.BackColor = Color.White;
            this.Size = new Size(600, 400);
        }
    }

    /// <summary>
    /// 高级设置控件（临时占位，后续需要完整实现）
    /// </summary>
    public class AdvancedSettingsControl : UserControl
    {
        public AppSettings Settings { get; set; }

        public AdvancedSettingsControl()
        {
            this.BackColor = Color.White;
            this.Size = new Size(600, 400);
        }
    }

    #endregion
}