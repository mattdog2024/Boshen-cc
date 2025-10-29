using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 帮助信息系统
    /// 提供完整的帮助功能，包括上下文帮助、帮助浏览器、快捷键帮助等
    /// </summary>
    public class HelpSystem : IDisposable
    {
        #region 私有字段

        private readonly Dictionary<string, HelpTopic> _helpTopics;
        private readonly Dictionary<string, HelpCategory> _helpCategories;
        private readonly List<HelpSearchResult> _searchIndex;
        private readonly Timer _searchTimer;
        private readonly Timer _animationTimer;

        private HelpWindow _helpWindow;
        private QuickHelpForm _quickHelpForm;
        private Control _ownerControl;
        private bool _disposed;
        private string _currentSearchTerm;
        private HelpTheme _currentTheme;
        private int _currentDpi;

        #endregion

        #region 事件定义

        /// <summary>
        /// 帮助主题显示事件
        /// </summary>
        public event EventHandler<HelpTopicShownEventArgs> HelpTopicShown;

        /// <summary>
        /// 帮助搜索完成事件
        /// </summary>
        public event EventHandler<HelpSearchCompletedEventArgs> SearchCompleted;

        /// <summary>
        /// 帮助窗口关闭事件
        /// </summary>
        public event EventHandler<HelpWindowClosedEventArgs> HelpWindowClosed;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化HelpSystem类
        /// </summary>
        /// <param name="owner">拥有者控件</param>
        public HelpSystem(Control owner = null)
        {
            _ownerControl = owner;
            _helpTopics = new Dictionary<string, HelpTopic>();
            _helpCategories = new Dictionary<string, HelpCategory>();
            _searchIndex = new List<HelpSearchResult>();

            InitializeTimers();
            InitializeHelpContent();
            InitializeWindows();

            _currentTheme = HelpTheme.Default;
            _currentDpi = DPIHelper.GetCurrentDpi();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取或设置当前主题
        /// </summary>
        public HelpTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    UpdateHelpTheme();
                }
            }
        }

        /// <summary>
        /// 获取帮助主题数量
        /// </summary>
        public int TopicCount => _helpTopics.Count;

        /// <summary>
        /// 获取帮助分类数量
        /// </summary>
        public int CategoryCount => _helpCategories.Count;

        /// <summary>
        /// 获取帮助窗口是否可见
        /// </summary>
        public bool IsHelpWindowVisible => _helpWindow?.Visible == true;

        /// <summary>
        /// 获取快速帮助是否可见
        /// </summary>
        public bool IsQuickHelpVisible => _quickHelpForm?.Visible == true;

        #endregion

        #region 公共方法

        /// <summary>
        /// 显示帮助主题
        /// </summary>
        /// <param name="topicId">主题ID</param>
        /// <param name="highlightText">要高亮的文本</param>
        public void ShowHelpTopic(string topicId, string highlightText = null)
        {
            if (string.IsNullOrEmpty(topicId) || !_helpTopics.ContainsKey(topicId)) return;

            var topic = _helpTopics[topicId];
            ShowHelpWindow();
            _helpWindow.DisplayTopic(topic, highlightText);

            OnHelpTopicShown(topic, highlightText);
        }

        /// <summary>
        /// 显示上下文帮助
        /// </summary>
        /// <param name="context">上下文标识</param>
        /// <param name="position">显示位置</param>
        public void ShowContextHelp(string context, Point? position = null)
        {
            var topic = GetContextHelpTopic(context);
            if (topic != null)
            {
                if (position.HasValue)
                {
                    ShowQuickHelp(topic, position.Value);
                }
                else
                {
                    ShowHelpTopic(topic.Id);
                }
            }
        }

        /// <summary>
        /// 显示帮助主页
        /// </summary>
        public void ShowHelpHome()
        {
            ShowHelpWindow();
            _helpWindow.ShowHome();
        }

        /// <summary>
        /// 显示帮助索引
        /// </summary>
        public void ShowHelpIndex()
        {
            ShowHelpWindow();
            _helpWindow.ShowIndex();
        }

        /// <summary>
        /// 显示搜索结果
        /// </summary>
        /// <param name="searchTerm">搜索词</param>
        public void ShowSearchResults(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm)) return;

            var results = SearchHelp(searchTerm);
            ShowHelpWindow();
            _helpWindow.ShowSearchResults(searchTerm, results);

            OnSearchCompleted(searchTerm, results);
        }

        /// <summary>
        /// 显示快捷键帮助
        /// </summary>
        public void ShowShortcutsHelp()
        {
            ShowHelpTopic("shortcuts");
        }

        /// <summary>
        /// 显示使用教程
        /// </summary>
        public void ShowTutorial()
        {
            ShowHelpTopic("tutorial");
        }

        /// <summary>
        /// 显示快速帮助
        /// </summary>
        /// <param name="topic">帮助主题</param>
        /// <param name="position">显示位置</param>
        public void ShowQuickHelp(HelpTopic topic, Point position)
        {
            if (topic == null) return;

            _quickHelpForm?.Close();
            _quickHelpForm = new QuickHelpForm(topic, _currentTheme);
            _quickHelpForm.Closed += (s, e) => _quickHelpForm = null;
            _quickHelpForm.ShowAt(position);
        }

        /// <summary>
        /// 隐藏所有帮助窗口
        /// </summary>
        public void HideAllHelp()
        {
            _helpWindow?.Hide();
            _quickHelpForm?.Close();
        }

        /// <summary>
        /// 添加帮助主题
        /// </summary>
        /// <param name="topic">帮助主题</param>
        public void AddHelpTopic(HelpTopic topic)
        {
            if (topic != null && !string.IsNullOrEmpty(topic.Id))
            {
                _helpTopics[topic.Id] = topic;
                UpdateSearchIndex(topic);

                // 添加到分类
                if (!string.IsNullOrEmpty(topic.Category) && !_helpCategories.ContainsKey(topic.Category))
                {
                    _helpCategories[topic.Category] = new HelpCategory
                    {
                        Id = topic.Category,
                        Name = topic.Category,
                        Description = $"包含{topic.Category}相关的帮助主题"
                    };
                }
            }
        }

        /// <summary>
        /// 添加帮助分类
        /// </summary>
        /// <param name="category">帮助分类</param>
        public void AddHelpCategory(HelpCategory category)
        {
            if (category != null && !string.IsNullOrEmpty(category.Id))
            {
                _helpCategories[category.Id] = category;
            }
        }

        /// <summary>
        /// 搜索帮助内容
        /// </summary>
        /// <param name="searchTerm">搜索词</param>
        /// <param name="maxResults">最大结果数</param>
        /// <returns>搜索结果列表</returns>
        public List<HelpSearchResult> SearchHelp(string searchTerm, int maxResults = 50)
        {
            if (string.IsNullOrEmpty(searchTerm)) return new List<HelpSearchResult>();

            var results = new List<HelpSearchResult>();
            var searchLower = searchTerm.ToLowerInvariant();

            foreach (var topic in _helpTopics.Values)
            {
                var score = CalculateSearchScore(topic, searchLower);
                if (score > 0)
                {
                    results.Add(new HelpSearchResult
                    {
                        Topic = topic,
                        Score = score,
                        MatchedText = GetMatchedText(topic, searchLower)
                    });
                }
            }

            return results.OrderByDescending(r => r.Score).Take(maxResults).ToList();
        }

        /// <summary>
        /// 获取帮助主题
        /// </summary>
        /// <param name="topicId">主题ID</param>
        /// <returns>帮助主题</returns>
        public HelpTopic GetHelpTopic(string topicId)
        {
            return !string.IsNullOrEmpty(topicId) && _helpTopics.ContainsKey(topicId)
                ? _helpTopics[topicId]
                : null;
        }

        /// <summary>
        /// 获取帮助分类
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>帮助分类</returns>
        public HelpCategory GetHelpCategory(string categoryId)
        {
            return !string.IsNullOrEmpty(categoryId) && _helpCategories.ContainsKey(categoryId)
                ? _helpCategories[categoryId]
                : null;
        }

        /// <summary>
        /// 获取所有帮助主题
        /// </summary>
        /// <returns>帮助主题列表</returns>
        public List<HelpTopic> GetAllTopics()
        {
            return _helpTopics.Values.ToList();
        }

        /// <summary>
        /// 获取所有帮助分类
        /// </summary>
        /// <returns>帮助分类列表</returns>
        public List<HelpCategory> GetAllCategories()
        {
            return _helpCategories.Values.ToList();
        }

        /// <summary>
        /// 更新DPI设置
        /// </summary>
        public void UpdateDpi()
        {
            var newDpi = DPIHelper.GetCurrentDpi();
            if (newDpi != _currentDpi)
            {
                _currentDpi = newDpi;
                UpdateHelpTheme();
            }
        }

        /// <summary>
        /// 从文件加载帮助内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void LoadHelpFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                var json = File.ReadAllText(filePath, Encoding.UTF8);
                var helpData = Newtonsoft.Json.JsonConvert.DeserializeObject<HelpData>(json);

                if (helpData?.Topics != null)
                {
                    foreach (var topic in helpData.Topics)
                    {
                        AddHelpTopic(topic);
                    }
                }

                if (helpData?.Categories != null)
                {
                    foreach (var category in helpData.Categories)
                    {
                        AddHelpCategory(category);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载帮助文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 保存帮助内容到文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void SaveHelpToFile(string filePath)
        {
            try
            {
                var helpData = new HelpData
                {
                    Topics = _helpTopics.Values.ToList(),
                    Categories = _helpCategories.Values.ToList()
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(helpData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存帮助文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化定时器
        /// </summary>
        private void InitializeTimers()
        {
            _searchTimer = new Timer { Interval = 300 };
            _searchTimer.Tick += OnSearchTimerTick;

            _animationTimer = new Timer { Interval = 16 };
            _animationTimer.Tick += OnAnimationTimerTick;
        }

        /// <summary>
        /// 初始化帮助内容
        /// </summary>
        private void InitializeHelpContent()
        {
            // 添加基本帮助分类
            AddHelpCategory(new HelpCategory
            {
                Id = "getting-started",
                Name = "入门指南",
                Description = "帮助新用户快速了解软件功能"
            });

            AddHelpCategory(new HelpCategory
            {
                Id = "features",
                Name = "功能介绍",
                Description = "详细介绍软件的各项功能"
            });

            AddHelpCategory(new HelpCategory
            {
                Id = "shortcuts",
                Name = "快捷键",
                Description = "快捷键操作说明"
            });

            AddHelpCategory(new HelpCategory
            {
                Id = "troubleshooting",
                Name = "故障排除",
                Description = "常见问题解决方法"
            });

            // 添加基本帮助主题
            AddHelpTopic(new HelpTopic
            {
                Id = "introduction",
                Title = "波神K线测量工具简介",
                Category = "getting-started",
                Content = GetIntroductionContent(),
                Keywords = new[] { "简介", "介绍", "概述", "功能" }
            });

            AddHelpTopic(new HelpTopic
            {
                Id = "kline-selection",
                Title = "K线选择操作",
                Category = "features",
                Content = GetKLineSelectionContent(),
                Keywords = new[] { "K线", "选择", "测量", "点击" }
            });

            AddHelpTopic(new HelpTopic
            {
                Id = "shortcuts",
                Title = "快捷键操作指南",
                Category = "shortcuts",
                Content = GetShortcutsContent(),
                Keywords = new[] { "快捷键", "键盘", "操作", "快速" }
            });

            AddHelpTopic(new HelpTopic
            {
                Id = "shadow-measurement",
                Title = "影线测量功能",
                Category = "features",
                Content = GetShadowMeasurementContent(),
                Keywords = new[] { "影线", "测量", "上影线", "下影线" }
            });

            AddHelpTopic(new HelpTopic
            {
                Id = "tutorial",
                Title = "使用教程",
                Category = "getting-started",
                Content = GetTutorialContent(),
                Keywords = new[] { "教程", "使用", "步骤", "指南" }
            });

            AddHelpTopic(new HelpTopic
            {
                Id = "troubleshooting",
                Title = "常见问题",
                Category = "troubleshooting",
                Content = GetTroubleshootingContent(),
                Keywords = new[] { "问题", "故障", "错误", "解决" }
            });
        }

        /// <summary>
        /// 初始化帮助窗口
        /// </summary>
        private void InitializeWindows()
        {
            _helpWindow = new HelpWindow(this, _currentTheme);
            _helpWindow.FormClosed += (s, e) => OnHelpWindowClosed();
        }

        /// <summary>
        /// 显示帮助窗口
        /// </summary>
        private void ShowHelpWindow()
        {
            if (_helpWindow == null || _helpWindow.IsDisposed)
            {
                _helpWindow = new HelpWindow(this, _currentTheme);
                _helpWindow.FormClosed += (s, e) => OnHelpWindowClosed();
            }

            if (!_helpWindow.Visible)
            {
                _helpWindow.Show();
                _helpWindow.BringToFront();
            }
        }

        /// <summary>
        /// 获取上下文帮助主题
        /// </summary>
        /// <param name="context">上下文标识</param>
        /// <returns>帮助主题</returns>
        private HelpTopic GetContextHelpTopic(string context)
        {
            var contextMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["KLineSelector"] = "kline-selection",
                ["PriceDisplay"] = "kline-selection",
                ["SelectionPanel"] = "kline-selection",
                ["MainWindow"] = "introduction",
                ["SettingsWindow"] = "shortcuts",
                ["ShadowMeasurement"] = "shadow-measurement"
            };

            var topicId = contextMap.TryGetValue(context, out var id) ? id : "introduction";
            return GetHelpTopic(topicId);
        }

        /// <summary>
        /// 更新搜索索引
        /// </summary>
        /// <param name="topic">帮助主题</param>
        private void UpdateSearchIndex(HelpTopic topic)
        {
            // 移除旧的索引
            _searchIndex.RemoveAll(r => r.Topic.Id == topic.Id);

            // 添加新的索引项
            if (topic.Keywords != null)
            {
                foreach (var keyword in topic.Keywords)
                {
                    _searchIndex.Add(new HelpSearchResult
                    {
                        Topic = topic,
                        Score = 1.0f,
                        MatchedText = keyword
                    });
                }
            }

            // 添加标题索引
            _searchIndex.Add(new HelpSearchResult
            {
                Topic = topic,
                Score = 1.5f,
                MatchedText = topic.Title
            });
        }

        /// <summary>
        /// 计算搜索得分
        /// </summary>
        /// <param name="topic">帮助主题</param>
        /// <param name="searchTerm">搜索词</param>
        /// <returns>搜索得分</returns>
        private float CalculateSearchScore(HelpTopic topic, string searchTerm)
        {
            float score = 0;

            // 标题匹配
            if (topic.Title.ToLowerInvariant().Contains(searchTerm))
                score += 1.5f;

            // 内容匹配
            if (topic.Content.ToLowerInvariant().Contains(searchTerm))
                score += 1.0f;

            // 关键词匹配
            if (topic.Keywords != null)
            {
                foreach (var keyword in topic.Keywords)
                {
                    if (keyword.ToLowerInvariant().Contains(searchTerm))
                        score += 1.2f;
                }
            }

            // 分类匹配
            if (topic.Category?.ToLowerInvariant().Contains(searchTerm) == true)
                score += 0.5f;

            return score;
        }

        /// <summary>
        /// 获取匹配的文本片段
        /// </summary>
        /// <param name="topic">帮助主题</param>
        /// <param name="searchTerm">搜索词</param>
        /// <returns>匹配的文本</returns>
        private string GetMatchedText(HelpTopic topic, string searchTerm)
        {
            var content = topic.Content.ToLowerInvariant();
            var index = content.IndexOf(searchTerm);

            if (index >= 0)
            {
                var start = Math.Max(0, index - 50);
                var length = Math.Min(150, content.Length - start);
                return content.Substring(start, length) + "...";
            }

            return topic.Title;
        }

        /// <summary>
        /// 更新帮助主题
        /// </summary>
        private void UpdateHelpTheme()
        {
            _helpWindow?.UpdateTheme(_currentTheme);
            _quickHelpForm?.UpdateTheme(_currentTheme);
        }

        #endregion

        #region 帮助内容获取方法

        /// <summary>
        /// 获取简介内容
        /// </summary>
        /// <returns>简介内容</returns>
        private string GetIntroductionContent()
        {
            return @"# 波神K线测量工具简介

## 软件概述
波神K线测量工具是一款专业的股票K线分析软件，基于波神理论算法，为投资者提供精准的K线测量和预测功能。

## 主要功能
- **K线选择测量**：精确选择K线进行测量分析
- **影线测量**：支持上影线、下影线、完整影线测量
- **算法预测**：基于波神理论的智能预测分析
- **结果显示**：直观的价格和预测结果显示
- **快捷操作**：丰富的快捷键和鼠标操作支持

## 适用场景
- 股票技术分析
- K线形态研究
- 价格预测分析
- 投资决策支持

## 技术特点
- 高精度测量算法
- 流畅的用户交互体验
- 完整的快捷键支持
- 智能工具提示系统
- 多主题界面支持

## 开始使用
1. 打开K线图片文件
2. 点击K线选择测量点
3. 查看测量结果和预测
4. 导出分析结果

更多详细操作请参考相关教程。";
        }

        /// <summary>
        /// 获取K线选择内容
        /// </summary>
        /// <returns>K线选择内容</returns>
        private string GetKLineSelectionContent()
        {
            return @"# K线选择操作指南

## 基本操作
K线选择是波神测量工具的核心功能，通过点击K线图上的K线来选择测量点。

### 选择步骤
1. 打开包含K线的图片文件
2. 将鼠标移动到要选择的K线上
3. 点击鼠标左键选择A点（第一个测量点）
4. 继续点击选择B点（第二个测量点）
5. 系统自动计算并显示测量结果

### 选择模式
软件支持多种测量模式：

#### 1. 标准测量模式
- 测量K线实体之间的价格差
- 适用于基本的价格分析

#### 2. 上影线测量模式
- 测量K线上影线的长度
- 用于分析上方压力位

#### 3. 下影线测量模式
- 测量K线下影线的长度
- 用于分析下方支撑位

#### 4. 完整影线测量模式
- 测量整根K线的总长度
- 提供完整的价格区间分析

### 快捷键操作
- **数字键1-4**：快速切换测量模式
- **C键**：清除当前选择
- **U键**：撤销上一步操作
- **R键**：重做已撤销的操作
- **M键**：快速测量模式

### 注意事项
1. 确保K线图片清晰可见
2. 点击时要精确对准K线实体
3. 选择A点和B点时要有逻辑关联
4. 可以使用鼠标滚轮缩放图片便于精确选择
5. 支持拖拽移动查看大图片的不同区域

### 高级功能
- **十字准星**：帮助精确定位
- **网格显示**：辅助对齐和测量
- **历史记录**：查看之前的测量结果
- **批量操作**：一次选择多个K线进行分析

## 常见问题
**Q: 为什么无法选择K线？**
A: 请确保已正确打开K线图片文件，并且图片格式支持。

**Q: 如何提高选择精度？**
A: 可以放大图片，使用十字准星功能，或者在设置中调整鼠标灵敏度。

**Q: 选择错误如何修正？**
A: 使用撤销功能或重新选择即可。";
        }

        /// <summary>
        /// 获取快捷键内容
        /// </summary>
        /// <returns>快捷键内容</returns>
        private string GetShortcutsContent()
        {
            return @"# 快捷键操作指南

## 文件操作
| 快捷键 | 功能描述 |
|--------|----------|
| **Ctrl + O** | 打开图片文件 |
| **Ctrl + S** | 保存当前结果 |
| **Ctrl + Shift + S** | 另存为 |
| **Ctrl + E** | 导出分析结果 |
| **Ctrl + P** | 打印 |

## 编辑操作
| 快捷键 | 功能描述 |
|--------|----------|
| **Ctrl + Z** | 撤销上一步操作 |
| **Ctrl + Y** | 重做已撤销的操作 |
| **Ctrl + R** | 清除所有选择 |
| **Delete** | 删除选中项 |
| **Ctrl + A** | 全选 |

## 视图操作
| 快捷键 | 功能描述 |
|--------|----------|
| **Space** | 计算预测 |
| **F5** | 刷新视图 |
| **F11** | 全屏切换 |
| **Ctrl + 0** | 重置缩放 |
| **Ctrl + Plus** | 放大 |
| **Ctrl + Minus** | 缩小 |

## 工具操作
| 快捷键 | 功能描述 |
|--------|----------|
| **1** | 标准测量模式 |
| **2** | 上影线测量模式 |
| **3** | 下影线测量模式 |
| **4** | 完整影线测量模式 |
| **M** | 快速测量 |

## 导航操作
| 快捷键 | 功能描述 |
|--------|----------|
| **ESC** | 取消当前操作 |
| **Enter** | 确认操作 |
| **Tab** | 切换到下一个控件 |
| **Shift + Tab** | 切换到上一个控件 |

## 帮助操作
| 快捷键 | 功能描述 |
|--------|----------|
| **F1** | 显示帮助 |
| **F12** | 关于信息 |

## K线选择器特定快捷键
| 快捷键 | 功能描述 |
|--------|----------|
| **1** | 切换到标准测量模式 |
| **2** | 切换到上影线测量模式 |
| **3** | 切换到下影线测量模式 |
| **4** | 切换到完整影线测量模式 |
| **C** | 清除当前选择 |
| **U** | 撤销上一步 |
| **R** | 重做操作 |

## 鼠标操作
| 操作 | 功能描述 |
|------|----------|
| **左键点击** | 选择K线点 |
| **右键拖拽** | 手势识别 |
| **滚轮** | 缩放图片 |
| **双击** | 快速确认 |
| **中键** | 平移视图 |

## 使用技巧
1. **记忆常用快捷键**：熟练使用快捷键可以大幅提高操作效率
2. **组合键操作**：很多功能支持组合键，如Ctrl+Shift+S另存为
3. **上下文相关**：不同的界面可能有不同的快捷键支持
4. **自定义设置**：可以在设置中自定义部分快捷键

## 快捷键冲突处理
如果快捷键与系统或其他软件冲突，可以：
1. 修改软件中的快捷键设置
2. 使用其他组合键替代
3. 检查是否有其他程序占用相同的快捷键";
        }

        /// <summary>
        /// 获取影线测量内容
        /// </summary>
        /// <returns>影线测量内容</returns>
        private string GetShadowMeasurementContent()
        {
            return @"# 影线测量功能详解

## 影线测量概述
影线测量是波神K线测量工具的高级功能，专门用于测量K线的影线部分，包括上影线、下影线和完整影线。

## 影线类型
### 上影线（Upper Shadow）
- **定义**：K线实体上方的细线部分
- **意义**：表示当日最高价与实体上端的价格差
- **分析价值**：反映上方压力强度

### 下影线（Lower Shadow）
- **定义**：K线实体下方的细线部分
- **意义**：表示当日最低价与实体下端的价格差
- **分析价值**：反映下方支撑强度

### 完整影线（Complete Shadow）
- **定义**：从最高价到最低价的完整价格区间
- **意义**：反映当日价格波动范围
- **分析价值**：判断市场活跃度

## 测量模式详解

### 1. 上影线测量模式
**操作方法**：
1. 按数字键 **2** 或选择上影线测量模式
2. 点击K线的上影线顶端（最高价）
3. 点击K线实体的上端
4. 系统自动计算上影线长度

**应用场景**：
- 分析上方阻力位
- 判断突破可能性
- 评估卖方力量

### 2. 下影线测量模式
**操作方法**：
1. 按数字键 **3** 或选择下影线测量模式
2. 点击K线实体的下端
3. 点击K线的下影线底端（最低价）
4. 系统自动计算下影线长度

**应用场景**：
- 分析下方支撑位
- 判断反弹可能性
- 评估买方力量

### 3. 完整影线测量模式
**操作方法**：
1. 按数字键 **4** 或选择完整影线测量模式
2. 点击K线的最高点
3. 点击K线的最低点
4. 系统计算完整价格区间

**应用场景**：
- 分析价格波动幅度
- 评估市场活跃度
- 计算风险收益比

## 影线分析指标

### 影线长度比例
- **上影线/实体比例**：>50%表示卖方较强
- **下影线/实体比例**：>50%表示买方较强
- **影线/实体比例**：整体反映市场情绪

### 影线形态分析
- **长上影线**：上方压力明显，可能回调
- **长下影线**：下方支撑明显，可能反弹
- **十字星**：多空平衡，关注后续走势
- **光头光脚**：单边走势强烈

## 测量精度控制

### 精确定位技巧
1. **放大图片**：使用滚轮放大便于精确点击
2. **十字准星**：开启十字准星辅助定位
3. **网格显示**：显示网格帮助对齐
4. **慢速移动**：缓慢移动鼠标提高点击精度

### 误差控制
- **像素级精度**：支持单像素级别的精确测量
- **多次测量**：重要点位可多次测量取平均值
- **校准功能**：可对测量结果进行微调

## 实际应用案例

### 案例一：突破前高分析
1. 使用上影线测量前高压力位
2. 结合当前价格判断突破概率
3. 计算突破后的目标价位

### 案例二：支撑位确认
1. 使用下影线测量历史支撑位
2. 观察多次测试的支撑强度
3. 判断支撑位的有效性

### 案例三：趋势反转判断
1. 测量连续K线的影线变化
2. 分析影线长度的递增递减趋势
3. 结合成交量判断反转概率

## 高级功能

### 批量测量
- **选择多根K线**：按住Ctrl键选择多根K线
- **自动计算**：系统自动计算平均影线长度
- **统计对比**：生成影线统计对比图表

### 影线趋势分析
- **趋势线绘制**：根据影线端点绘制趋势线
- **斜率计算**：计算影线长度的变化斜率
- **周期分析**：分析影线长度的周期性变化

### 历史对比
- **历史数据对比**：与历史同期影线数据对比
- **相对强度分析**：计算影线的相对强弱指标
- **模式识别**：识别常见的影线形态模式

## 注意事项
1. 影线测量需要清晰的K线图片
2. 注意影线与实体的区分
3. 结合其他技术指标综合分析
4. 影线分析需要考虑市场环境
5. 建议与成交量数据结合使用

## 常见问题
**Q: 如何区分影线和实体？**
A: 实体是K线的粗体部分，影线是连接实体的细线部分。

**Q: 影线长度如何换算成价格？**
A: 系统会根据图表比例自动换算成实际价格。

**Q: 为什么测量结果有误差？**
A: 可能是图片分辨率或K线绘制不标准导致的，建议使用高清K线图。";
        }

        /// <summary>
        /// 获取教程内容
        /// </summary>
        /// <returns>教程内容</returns>
        private string GetTutorialContent()
        {
            return @"# 使用教程

## 第一章：快速入门

### 1.1 软件启动
1. 双击桌面图标启动软件
2. 等待主界面加载完成
3. 熟悉主界面布局

### 1.2 打开K线图
1. 点击 **文件 → 打开** 或按 **Ctrl+O**
2. 选择包含K线的图片文件
3. 点击 **打开** 加载图片

### 1.3 基本测量
1. 观察K线图，找到要测量的K线
2. 将鼠标移动到目标K线上
3. 点击左键选择第一个点（A点）
4. 点击左键选择第二个点（B点）
5. 查看右侧面板的测量结果

## 第二章：进阶操作

### 2.1 测量模式切换
- **标准模式**：测量K线实体间距离（快捷键：1）
- **上影线模式**：测量上影线长度（快捷键：2）
- **下影线模式**：测量下影线长度（快捷键：3）
- **完整影线模式**：测量整根K线长度（快捷键：4）

### 2.2 视图操作
- **缩放**：使用鼠标滚轮或Ctrl+Plus/Minus
- **平移**：按住鼠标右键拖拽
- **全屏**：按F11进入全屏模式
- **刷新**：按F5刷新视图

### 2.3 测量结果查看
1. 测量完成后，结果会显示在右侧面板
2. 包含价格差、百分比等详细信息
3. 可以查看历史测量记录
4. 支持导出测量结果

## 第三章：高级功能

### 3.1 批量测量
1. 按住Ctrl键选择多根K线
2. 系统自动进行批量测量
3. 生成测量统计报告

### 3.2 算法预测
1. 完成基本测量后按空格键
2. 系统使用波神算法进行预测
3. 显示预测的价格区间和时间点
4. 可调整算法参数优化预测结果

### 3.3 结果导出
1. 点击 **文件 → 导出结果**
2. 选择导出格式（Excel、PDF、图片）
3. 设置导出参数
4. 保存导出文件

## 第四章：技巧与窍门

### 4.1 提高测量精度
- **放大图片**：使用滚轮放大K线图
- **使用十字准星**：开启辅助线精确定位
- **慢速移动**：缓慢移动鼠标便于精确点击
- **多次测量**：重要点位可多次测量验证

### 4.2 快捷操作技巧
- **数字键1-4**：快速切换测量模式
- **C键**：快速清除选择
- **U键**：撤销操作
- **R键**：重做操作
- **M键**：快速测量模式

### 4.3 效率提升方法
- **熟练使用快捷键**：比鼠标操作更高效
- **建立工作流程**：标准化的分析流程
- **保存常用设置**：自定义工作环境
- **使用模板功能**：保存常用的分析模板

## 第五章：实战案例

### 5.1 趋势线分析案例
**目标**：分析股票上升趋势的支撑位
**步骤**：
1. 打开股票K线图
2. 选择上升趋势线的起点和终点
3. 使用标准测量模式测量趋势线角度
4. 结合影线测量分析支撑强度
5. 得出支撑位价格区间

### 5.2 形态识别案例
**目标**：识别头肩顶形态
**步骤**：
1. 找到可能的头肩顶形态
2. 测量左肩和右肩的高度
3. 测量头部的最高点
4. 分析颈线位置
5. 预测下跌目标位

### 5.3 压力支撑分析案例
**目标**：确定关键压力位和支撑位
**步骤**：
1. 使用上影线测量历史高点压力
2. 使用下影线测量历史低点支撑
3. 分析多次测试的有效性
4. 结合成交量确认关键位
5. 制定交易策略

## 第六章：常见问题解决

### 6.1 软件使用问题
**Q: 软件启动缓慢怎么办？**
A: 检查系统资源，关闭不必要的程序，或重启软件。

**Q: 图片无法打开？**
A: 检查图片格式是否支持，文件是否损坏。

**Q: 测量结果不准确？**
A: 确保K线图片清晰，放大图片进行精确选择。

### 6.2 操作技巧问题
**Q: 如何快速切换测量模式？**
A: 使用数字键1-4快速切换不同模式。

**Q: 选择错误如何修正？**
A: 使用Ctrl+Z撤销或按C键清除重新选择。

### 6.3 结果分析问题
**Q: 如何理解预测结果？**
A: 参考帮助文档中的波神理论说明，结合市场环境分析。

**Q: 导出结果格式不合适？**
A: 选择合适的导出格式，或使用Excel进一步处理数据。

## 总结
通过本教程的学习，您应该能够：
1. 熟练操作波神K线测量工具
2. 掌握各种测量模式的使用
3. 理解测量结果的意义
4. 应用到实际股票分析中

记住，技术分析工具只是辅助手段，还需要结合基本面分析和市场经验才能做出准确的投资决策。";
        }

        /// <summary>
        /// 获取故障排除内容
        /// </summary>
        /// <returns>故障排除内容</returns>
        private string GetTroubleshootingContent()
        {
            return @"# 常见问题与故障排除

## 软件启动问题

### 问题1：软件无法启动
**可能原因**：
- .NET Framework版本不兼容
- 系统权限不足
- 软件文件损坏

**解决方法**：
1. 检查是否安装了.NET Framework 4.7.2或更高版本
2. 以管理员身份运行软件
3. 重新下载安装软件

### 问题2：启动后闪退
**可能原因**：
- 配置文件损坏
- 依赖组件缺失
- 系统兼容性问题

**解决方法**：
1. 删除配置文件重新启动
2. 安装必要的运行库
3. 检查Windows更新状态

## 文件操作问题

### 问题3：无法打开图片文件
**可能原因**：
- 文件格式不支持
- 文件路径包含特殊字符
- 文件被其他程序占用

**解决方法**：
1. 使用支持的格式（JPG、PNG、BMP等）
2. 将文件移动到简单路径
3. 关闭占用文件的其他程序

### 问题4：保存失败
**可能原因**：
- 磁盘空间不足
- 没有写入权限
- 文件路径不存在

**解决方法**：
1. 检查磁盘剩余空间
2. 以管理员身份运行
3. 选择有效的保存路径

## 测量操作问题

### 问题5：无法点击K线
**可能原因**：
- 图片未正确加载
- 鼠标精度设置问题
- 软件响应延迟

**解决方法**：
1. 重新加载图片文件
2. 调整鼠标设置
3. 重启软件

### 问题6：测量结果不准确
**可能原因**：
- K线图片模糊
- 点击位置偏差
- 缩放比例问题

**解决方法**：
1. 使用高清K线图
2. 放大图片精确选择
3. 检查缩放设置

### 问题7：十字准星不显示
**可能原因**：
- 功能被禁用
- 显示设置问题
- 渲染错误

**解决方法**：
1. 在设置中启用十字准星
2. 检查显示选项
3. 更新显卡驱动

## 界面显示问题

### 问题8：界面显示异常
**可能原因**：
- DPI缩放问题
- 主题兼容性问题
- 系统显示设置

**解决方法**：
1. 调整DPI设置
2. 切换到默认主题
3. 检查系统显示设置

### 问题9：字体显示模糊
**可能原因**：
- 系统字体渲染问题
- DPI缩放比例不合适
- 字体文件损坏

**解决方法**：
1. 调整ClearType设置
2. 修改DPI缩放比例
3. 重新安装字体

## 性能问题

### 问题10：软件运行缓慢
**可能原因**：
- 系统资源不足
- 图片文件过大
- 后台程序占用

**解决方法**：
1. 关闭不必要的程序
2. 压缩图片文件大小
3. 增加系统内存

### 问题11：内存占用过高
**可能原因**：
- 内存泄漏
- 大量历史记录
- 缓存文件过多

**解决方法**：
1. 定期重启软件
2. 清理历史记录
3. 清理缓存文件

## 算法计算问题

### 问题12：预测结果异常
**可能原因**：
- 输入数据错误
- 算法参数设置不当
- 市场异常波动

**解决方法**：
1. 检查测量数据准确性
2. 调整算法参数
3. 结合其他分析工具

### 问题13：计算速度慢
**可能原因**：
- 数据量过大
- 算法复杂度高
- 系统性能不足

**解决方法**：
1. 减少数据量
2. 优化算法参数
3. 升级硬件配置

## 网络相关

### 问题14：在线帮助无法访问
**可能原因**：
- 网络连接问题
- 防火墙阻止
- 服务器维护

**解决方法**：
1. 检查网络连接
2. 配置防火墙规则
3. 使用本地帮助文件

## 数据导入导出

### 问题15：导入数据失败
**可能原因**：
- 数据格式不正确
- 文件编码问题
- 数据完整性问题

**解决方法**：
1. 检查数据格式要求
2. 使用UTF-8编码
3. 验证数据完整性

### 问题16：导出结果为空
**可能原因**：
- 没有有效数据
- 导出设置错误
- 权限不足

**解决方法**：
1. 确保有测量数据
2. 检查导出设置
3. 以管理员身份运行

## 兼容性问题

### 问题17：与杀毒软件冲突
**解决方法**：
1. 将软件添加到白名单
2. 临时关闭实时防护
3. 联系杀毒软件厂商

### 问题18：与其他软件冲突
**解决方法**：
1. 关闭冲突软件
2. 修改软件设置
3. 使用兼容模式运行

## 联系技术支持

如果以上方法无法解决您的问题，请联系技术支持：

**邮箱支持**：support@boshen.com
**在线客服**：访问官方网站获取帮助
**用户论坛**：与其他用户交流经验

**报告问题时请提供以下信息**：
1. 操作系统版本
2. 软件版本号
3. 详细的错误描述
4. 错误截图或日志文件
5. 复现问题的具体步骤

## 预防措施

### 定期维护
1. 定期清理缓存文件
2. 及时更新软件版本
3. 备份重要数据
4. 定期检查系统健康状态

### 最佳实践
1. 使用推荐的系统配置
2. 保持软件更新
3. 定期查看帮助文档
4. 参与用户社区交流

通过遵循这些故障排除指南，大部分常见问题都可以得到有效解决。如果遇到特殊问题，建议及时联系技术支持获取专业帮助。";
        }

        #endregion

        #region 事件处理方法

        /// <summary>
        /// 搜索定时器事件处理
        /// </summary>
        private void OnSearchTimerTick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            if (!string.IsNullOrEmpty(_currentSearchTerm))
            {
                var results = SearchHelp(_currentSearchTerm);
                OnSearchCompleted(_currentSearchTerm, results);
            }
        }

        /// <summary>
        /// 动画定时器事件处理
        /// </summary>
        private void OnAnimationTimerTick(object sender, EventArgs e)
        {
            // 处理帮助窗口的动画效果
            _helpWindow?.UpdateAnimation();
        }

        #endregion

        #region 事件触发方法

        /// <summary>
        /// 触发帮助主题显示事件
        /// </summary>
        /// <param name="topic">帮助主题</param>
        /// <param name="highlightText">高亮文本</param>
        protected virtual void OnHelpTopicShown(HelpTopic topic, string highlightText)
        {
            HelpTopicShown?.Invoke(this, new HelpTopicShownEventArgs
            {
                Topic = topic,
                HighlightText = highlightText,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 触发搜索完成事件
        /// </summary>
        /// <param name="searchTerm">搜索词</param>
        /// <param name="results">搜索结果</param>
        protected virtual void OnSearchCompleted(string searchTerm, List<HelpSearchResult> results)
        {
            SearchCompleted?.Invoke(this, new HelpSearchCompletedEventArgs
            {
                SearchTerm = searchTerm,
                Results = results,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 触发帮助窗口关闭事件
        /// </summary>
        protected virtual void OnHelpWindowClosed()
        {
            HelpWindowClosed?.Invoke(this, new HelpWindowClosedEventArgs
            {
                Timestamp = DateTime.Now
            });
        }

        #endregion

        #region IDisposable实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 停止定时器
                _searchTimer?.Stop();
                _animationTimer?.Stop();
                _searchTimer?.Dispose();
                _animationTimer?.Dispose();

                // 关闭窗口
                _helpWindow?.Close();
                _helpWindow?.Dispose();
                _quickHelpForm?.Close();
                _quickHelpForm?.Dispose();

                // 清理资源
                _helpTopics.Clear();
                _helpCategories.Clear();
                _searchIndex.Clear();

                _disposed = true;
            }
        }

        #endregion
    }

    #region 辅助类和枚举

    /// <summary>
    /// 帮助主题
    /// </summary>
    public class HelpTopic
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Content { get; set; }
        public string[] Keywords { get; set; }
        public DateTime LastModified { get; set; } = DateTime.Now;
        public int ViewCount { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public object Tag { get; set; }
    }

    /// <summary>
    /// 帮助分类
    /// </summary>
    public class HelpCategory
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// 帮助搜索结果
    /// </summary>
    public class HelpSearchResult
    {
        public HelpTopic Topic { get; set; }
        public float Score { get; set; }
        public string MatchedText { get; set; }
        public DateTime SearchTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 帮助数据
    /// </summary>
    public class HelpData
    {
        public List<HelpTopic> Topics { get; set; }
        public List<HelpCategory> Categories { get; set; }
        public string Version { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 帮助主题显示事件参数
    /// </summary>
    public class HelpTopicShownEventArgs : EventArgs
    {
        public HelpTopic Topic { get; set; }
        public string HighlightText { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 帮助搜索完成事件参数
    /// </summary>
    public class HelpSearchCompletedEventArgs : EventArgs
    {
        public string SearchTerm { get; set; }
        public List<HelpSearchResult> Results { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 帮助窗口关闭事件参数
    /// </summary>
    public class HelpWindowClosedEventArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 帮助主题
    /// </summary>
    public enum HelpTheme
    {
        Default,
        Dark,
        Light,
        Blue,
        Green,
        Custom
    }

    #endregion
}