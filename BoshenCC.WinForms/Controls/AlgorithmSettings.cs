using System;
using System.Drawing;
using System.Windows.Forms;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 算法设置控件
    /// 提供波神算法参数的调整界面
    /// </summary>
    public partial class AlgorithmSettings : UserControl
    {
        #region 字段

        private AlgorithmSettings _settings = new AlgorithmSettings();
        private bool _isUpdating = false;

        #endregion

        #region 事件

        /// <summary>
        /// 设置更改事件
        /// </summary>
        public event EventHandler<AlgorithmSettings> SettingsChanged;

        #endregion

        #region 属性

        /// <summary>
        /// 当前设置
        /// </summary>
        public AlgorithmSettings Settings
        {
            get => _settings;
            set
            {
                if (_settings != value)
                {
                    _settings = value ?? new AlgorithmSettings();
                    UpdateControlsFromSettings();
                }
            }
        }

        #endregion

        #region 构造函数

        public AlgorithmSettings()
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
            this.Name = "AlgorithmSettings";
            this.Size = new Size(600, 400);
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
                RowCount = 8,
                ColumnCount = 3,
                Padding = new Padding(10),
                AutoSize = true
            };

            // 添加列样式
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // 标签
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, 200)); // 控件
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // 说明

            // 价格阈值
            var lblPriceThreshold = CreateLabel("价格阈值:");
            var nudPriceThreshold = new NumericUpDown
            {
                Minimum = 0.001m,
                Maximum = 1.0m,
                DecimalPlaces = 3,
                Increment = 0.001m,
                Value = _settings.PriceThreshold,
                Width = 100,
                Name = "nudPriceThreshold"
            };
            var lblPriceThresholdDesc = CreateDescriptionLabel("算法计算的价格精度阈值");

            // 价格精度
            var lblPricePrecision = CreateLabel("价格精度:");
            var nudPricePrecision = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 8,
                Increment = 1,
                Value = _settings.PricePrecision,
                Width = 100,
                Name = "nudPricePrecision"
            };
            var lblPricePrecisionDesc = CreateDescriptionLabel("价格显示的小数位数");

            // 最大预测线数量
            var lblMaxLines = CreateLabel("最大预测线数:");
            var nudMaxLines = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 20,
                Increment = 1,
                Value = _settings.MaxPredictionLines,
                Width = 100,
                Name = "nudMaxLines"
            };
            var lblMaxLinesDesc = CreateDescriptionLabel("最多显示的预测线数量");

            // 计算容差
            var lblTolerance = CreateLabel("计算容差:");
            var nudTolerance = new NumericUpDown
            {
                Minimum = 0.0001m,
                Maximum = 0.1m,
                DecimalPlaces = 4,
                Increment = 0.0001m,
                Value = _settings.Tolerance,
                Width = 100,
                Name = "nudTolerance"
            };
            var lblToleranceDesc = CreateDescriptionLabel("算法计算的容差范围");

            // 自动计算
            var chkAutoCalculate = CreateCheckBox("启用自动计算", _settings.EnableAutoCalculate, "chkAutoCalculate");
            var lblAutoCalculateDesc = CreateDescriptionLabel("选择K线后自动计算预测线");

            // 显示计算日志
            var chkShowLog = CreateCheckBox("显示计算日志", _settings.ShowCalculationLog, "chkShowLog");
            var lblShowLogDesc = CreateDescriptionLabel("在界面中显示详细的计算过程");

            // 高级模式
            var chkAdvancedMode = CreateCheckBox("启用高级模式", _settings.EnableAdvancedMode, "chkAdvancedMode");
            var lblAdvancedModeDesc = CreateDescriptionLabel("启用高级算法功能和参数调整");

            // 添加控件到面板
            mainPanel.Controls.Add(lblPriceThreshold, 0, 0);
            mainPanel.Controls.Add(nudPriceThreshold, 1, 0);
            mainPanel.Controls.Add(lblPriceThresholdDesc, 2, 0);

            mainPanel.Controls.Add(lblPricePrecision, 0, 1);
            mainPanel.Controls.Add(nudPricePrecision, 1, 1);
            mainPanel.Controls.Add(lblPricePrecisionDesc, 2, 1);

            mainPanel.Controls.Add(lblMaxLines, 0, 2);
            mainPanel.Controls.Add(nudMaxLines, 1, 2);
            mainPanel.Controls.Add(lblMaxLinesDesc, 2, 2);

            mainPanel.Controls.Add(lblTolerance, 0, 3);
            mainPanel.Controls.Add(nudTolerance, 1, 3);
            mainPanel.Controls.Add(lblToleranceDesc, 2, 3);

            mainPanel.Controls.Add(chkAutoCalculate, 0, 4);
            mainPanel.Controls.Add(new Control(), 1, 4);
            mainPanel.Controls.Add(lblAutoCalculateDesc, 2, 4);

            mainPanel.Controls.Add(chkShowLog, 0, 5);
            mainPanel.Controls.Add(new Control(), 1, 5);
            mainPanel.Controls.Add(lblShowLogDesc, 2, 5);

            mainPanel.Controls.Add(chkAdvancedMode, 0, 6);
            mainPanel.Controls.Add(new Control(), 1, 6);
            mainPanel.Controls.Add(lblAdvancedModeDesc, 2, 6);

            // 添加按钮面板
            var buttonPanel = CreateButtonPanel();
            mainPanel.Controls.Add(buttonPanel, 0, 7);
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
                }

                if (control.HasChildren)
                {
                    SetupControlEvents(control);
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
                // 查找并更新所有控件
                var nudPriceThreshold = FindControl("nudPriceThreshold") as NumericUpDown;
                if (nudPriceThreshold != null) nudPriceThreshold.Value = _settings.PriceThreshold;

                var nudPricePrecision = FindControl("nudPricePrecision") as NumericUpDown;
                if (nudPricePrecision != null) nudPricePrecision.Value = _settings.PricePrecision;

                var nudMaxLines = FindControl("nudMaxLines") as NumericUpDown;
                if (nudMaxLines != null) nudMaxLines.Value = _settings.MaxPredictionLines;

                var nudTolerance = FindControl("nudTolerance") as NumericUpDown;
                if (nudTolerance != null) nudTolerance.Value = _settings.Tolerance;

                var chkAutoCalculate = FindControl("chkAutoCalculate") as CheckBox;
                if (chkAutoCalculate != null) chkAutoCalculate.Checked = _settings.EnableAutoCalculate;

                var chkShowLog = FindControl("chkShowLog") as CheckBox;
                if (chkShowLog != null) chkShowLog.Checked = _settings.ShowCalculationLog;

                var chkAdvancedMode = FindControl("chkAdvancedMode") as CheckBox;
                if (chkAdvancedMode != null) chkAdvancedMode.Checked = _settings.EnableAdvancedMode;
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
                var nudPriceThreshold = FindControl("nudPriceThreshold") as NumericUpDown;
                if (nudPriceThreshold != null) _settings.PriceThreshold = nudPriceThreshold.Value;

                var nudPricePrecision = FindControl("nudPricePrecision") as NumericUpDown;
                if (nudPricePrecision != null) _settings.PricePrecision = (int)nudPricePrecision.Value;

                var nudMaxLines = FindControl("nudMaxLines") as NumericUpDown;
                if (nudMaxLines != null) _settings.MaxPredictionLines = (int)nudMaxLines.Value;

                var nudTolerance = FindControl("nudTolerance") as NumericUpDown;
                if (nudTolerance != null) _settings.Tolerance = nudTolerance.Value;

                var chkAutoCalculate = FindControl("chkAutoCalculate") as CheckBox;
                if (chkAutoCalculate != null) _settings.EnableAutoCalculate = chkAutoCalculate.Checked;

                var chkShowLog = FindControl("chkShowLog") as CheckBox;
                if (chkShowLog != null) _settings.ShowCalculationLog = chkShowLog.Checked;

                var chkAdvancedMode = FindControl("chkAdvancedMode") as CheckBox;
                if (chkAdvancedMode != null) _settings.EnableAdvancedMode = chkAdvancedMode.Checked;

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
            var result = MessageBox.Show("确定要重置所有算法设置为默认值吗？", "确认重置",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _settings = new AlgorithmSettings();
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
            MessageBox.Show("算法设置已应用", "设置已保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            if (_settings.PriceThreshold < 0.001m || _settings.PriceThreshold > 1.0m)
            {
                result.AddError("价格阈值必须在0.001-1.0之间");
            }

            if (_settings.PricePrecision < 0 || _settings.PricePrecision > 8)
            {
                result.AddError("价格精度必须在0-8之间");
            }

            if (_settings.MaxPredictionLines <= 0 || _settings.MaxPredictionLines > 20)
            {
                result.AddError("预测线数量必须在1-20之间");
            }

            if (_settings.Tolerance < 0.0001m || _settings.Tolerance > 0.1m)
            {
                result.AddError("计算容差必须在0.0001-0.1之间");
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