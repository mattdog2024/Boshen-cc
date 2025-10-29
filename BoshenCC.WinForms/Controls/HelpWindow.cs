using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 帮助窗口
    /// 提供完整的帮助浏览和搜索功能
    /// </summary>
    public partial class HelpWindow : Form
    {
        #region 私有字段

        private readonly HelpSystem _helpSystem;
        private HelpTheme _currentTheme;
        private HelpTopic _currentTopic;
        private string _currentSearchTerm;
        private bool _disposed;

        // UI控件
        private TreeView _categoryTree;
        private WebBrowser _contentBrowser;
        private TextBox _searchTextBox;
        private Button _searchButton;
        private Button _homeButton;
        private Button _backButton;
        private Button _forwardButton;
        private Label _titleLabel;
        private SplitContainer _splitContainer;
        private Panel _searchPanel;
        private Panel _navigationPanel;

        // 导航历史
        private readonly Stack<HelpTopic> _navigationHistory;
        private readonly Stack<HelpTopic> _forwardHistory;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化HelpWindow类
        /// </summary>
        /// <param name="helpSystem">帮助系统</param>
        /// <param name="theme">主题</param>
        public HelpWindow(HelpSystem helpSystem, HelpTheme theme = HelpTheme.Default)
        {
            _helpSystem = helpSystem ?? throw new ArgumentNullException(nameof(helpSystem));
            _currentTheme = theme;
            _navigationHistory = new Stack<HelpTopic>();
            _forwardHistory = new Stack<HelpTopic>();

            InitializeComponent();
            InitializeTheme();
            LoadHelpContent();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 显示帮助主题
        /// </summary>
        /// <param name="topic">帮助主题</param>
        /// <param name="highlightText">要高亮的文本</param>
        public void DisplayTopic(HelpTopic topic, string highlightText = null)
        {
            if (topic == null) return;

            // 添加到导航历史
            if (_currentTopic != null)
            {
                _navigationHistory.Push(_currentTopic);
                _forwardHistory.Clear();
            }

            _currentTopic = topic;
            _titleLabel.Text = topic.Title;

            // 生成HTML内容
            var html = GenerateTopicHtml(topic, highlightText);
            _contentBrowser.DocumentText = html;

            UpdateNavigationButtons();
        }

        /// <summary>
        /// 显示主页
        /// </summary>
        public void ShowHome()
        {
            var homeTopic = new HelpTopic
            {
                Title = "波神K线测量工具 - 帮助中心",
                Content = GenerateHomeContent(),
                Category = "home"
            };

            DisplayTopic(homeTopic);
        }

        /// <summary>
        /// 显示索引
        /// </summary>
        public void ShowIndex()
        {
            var indexContent = GenerateIndexContent();
            var indexTopic = new HelpTopic
            {
                Title = "帮助索引",
                Content = indexContent,
                Category = "index"
            };

            DisplayTopic(indexTopic);
        }

        /// <summary>
        /// 显示搜索结果
        /// </summary>
        /// <param name="searchTerm">搜索词</param>
        /// <param name="results">搜索结果</param>
        public void ShowSearchResults(string searchTerm, List<HelpSearchResult> results)
        {
            _currentSearchTerm = searchTerm;
            var searchContent = GenerateSearchResultsContent(searchTerm, results);
            var searchTopic = new HelpTopic
            {
                Title = $"搜索结果: {searchTerm}",
                Content = searchContent,
                Category = "search"
            };

            DisplayTopic(searchTopic);
        }

        /// <summary>
        /// 更新主题
        /// </summary>
        /// <param name="theme">新主题</param>
        public void UpdateTheme(HelpTheme theme)
        {
            _currentTheme = theme;
            InitializeTheme();
            Invalidate();
        }

        /// <summary>
        /// 更新动画（用于支持动画效果）
        /// </summary>
        public void UpdateAnimation()
        {
            // 如果有动画效果，在这里更新
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponent()
        {
            // 窗体设置
            Text = "波神K线测量工具 - 帮助中心";
            Size = new Size(900, 600);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(800, 500);
            Icon = SystemIcons.Information;

            // 创建主要控件
            CreateControls();
            LayoutControls();
            WireEvents();
        }

        /// <summary>
        /// 创建控件
        /// </summary>
        private void CreateControls()
        {
            // 分割容器
            _splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 250,
                SplitterWidth = 5
            };

            // 左侧面板
            var leftPanel = _splitContainer.Panel1;

            // 分类树
            _categoryTree = new TreeView
            {
                Dock = DockStyle.Fill,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = false,
                FullRowSelect = true,
                HideSelection = false,
                Font = new Font("Microsoft YaHei", 9F)
            };

            // 搜索面板
            _searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(5)
            };

            _searchTextBox = new TextBox
            {
                Location = new Point(5, 5),
                Width = 180,
                Font = new Font("Microsoft YaHei", 9F)
            };

            _searchButton = new Button
            {
                Text = "搜索",
                Location = new Point(190, 3),
                Size = new Size(50, 24),
                Font = new Font("Microsoft YaHei", 9F)
            };

            _searchPanel.Controls.AddRange(new Control[] { _searchTextBox, _searchButton });

            // 导航面板
            _navigationPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            _homeButton = new Button
            {
                Text = "主页",
                Size = new Size(50, 30),
                Location = new Point(5, 5),
                Font = new Font("Microsoft YaHei", 9F)
            };

            _backButton = new Button
            {
                Text = "后退",
                Size = new Size(50, 30),
                Location = new Point(60, 5),
                Enabled = false,
                Font = new Font("Microsoft YaHei", 9F)
            };

            _forwardButton = new Button
            {
                Text = "前进",
                Size = new Size(50, 30),
                Location = new Point(115, 5),
                Enabled = false,
                Font = new Font("Microsoft YaHei", 9F)
            };

            _navigationPanel.Controls.AddRange(new Control[] { _homeButton, _backButton, _forwardButton });

            // 右侧面板
            var rightPanel = _splitContainer.Panel2;

            // 标题标签
            _titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Text = "帮助中心",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 5, 0, 0)
            };

            // 内容浏览器
            _contentBrowser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                AllowNavigation = true,
                ScriptErrorsSuppressed = true
            };

            rightPanel.Controls.AddRange(new Control[] { _titleLabel, _contentBrowser });
            leftPanel.Controls.AddRange(new Control[] { _navigationPanel, _searchPanel, _categoryTree });
        }

        /// <summary>
        /// 布局控件
        /// </summary>
        private void LayoutControls()
        {
            Controls.Add(_splitContainer);
        }

        /// <summary>
        /// 连接事件
        /// </summary>
        private void WireEvents()
        {
            _categoryTree.AfterSelect += OnCategoryTreeAfterSelect;
            _searchTextBox.KeyDown += OnSearchTextBoxKeyDown;
            _searchButton.Click += OnSearchButtonClick;
            _homeButton.Click += OnHomeButtonClick;
            _backButton.Click += OnBackButtonClick;
            _forwardButton.Click += OnForwardButtonClick;
            _contentBrowser.Navigating += OnContentBrowserNavigating;
            Load += OnHelpWindowLoad;
        }

        /// <summary>
        /// 初始化主题
        /// </summary>
        private void InitializeTheme()
        {
            switch (_currentTheme)
            {
                case HelpTheme.Dark:
                    BackColor = Color.FromArgb(45, 45, 48);
                    ForeColor = Color.White;
                    _categoryTree.BackColor = Color.FromArgb(37, 37, 38);
                    _categoryTree.ForeColor = Color.White;
                    _contentBrowser.BackColor = Color.FromArgb(45, 45, 48);
                    break;
                case HelpTheme.Light:
                    BackColor = Color.White;
                    ForeColor = Color.Black;
                    _categoryTree.BackColor = Color.White;
                    _categoryTree.ForeColor = Color.Black;
                    _contentBrowser.BackColor = Color.White;
                    break;
                default:
                    BackColor = Color.FromArgb(240, 240, 240);
                    ForeColor = Color.Black;
                    _categoryTree.BackColor = Color.White;
                    _categoryTree.ForeColor = Color.Black;
                    break;
            }
        }

        /// <summary>
        /// 加载帮助内容
        /// </summary>
        private void LoadHelpContent()
        {
            // 加载分类树
            LoadCategoryTree();

            // 显示主页
            ShowHome();
        }

        /// <summary>
        /// 加载分类树
        /// </summary>
        private void LoadCategoryTree()
        {
            _categoryTree.Nodes.Clear();

            var categories = _helpSystem.GetAllCategories();
            var topics = _helpSystem.GetAllTopics();

            // 按分类组织主题
            var categoryGroups = topics.GroupBy(t => t.Category ?? "其他").ToList();

            foreach (var group in categoryGroups)
            {
                var categoryNode = new TreeNode(group.Key)
                {
                    Tag = group.Key
                };

                foreach (var topic in group.OrderBy(t => t.Title))
                {
                    var topicNode = new TreeNode(topic.Title)
                    {
                        Tag = topic,
                        ToolTipText = topic.Title
                    };
                    categoryNode.Nodes.Add(topicNode);
                }

                _categoryTree.Nodes.Add(categoryNode);
            }

            // 展开第一个分类
            if (_categoryTree.Nodes.Count > 0)
            {
                _categoryTree.Nodes[0].Expand();
            }
        }

        /// <summary>
        /// 生成主题HTML
        /// </summary>
        /// <param name="topic">帮助主题</param>
        /// <param name="highlightText">要高亮的文本</param>
        /// <returns>HTML内容</returns>
        private string GenerateTopicHtml(HelpTopic topic, string highlightText = null)
        {
            var content = topic.Content;

            // 高亮搜索文本
            if (!string.IsNullOrEmpty(highlightText))
            {
                content = HighlightText(content, highlightText);
            }

            // 转换Markdown样式到HTML
            content = ConvertMarkdownToHtml(content);

            var themeStyles = GetThemeStyles();

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{topic.Title}</title>
    <style>
        {themeStyles}
        body {{ font-family: 'Microsoft YaHei', Arial, sans-serif; line-height: 1.6; margin: 20px; }}
        h1 {{ color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px; }}
        h2 {{ color: #34495e; margin-top: 30px; }}
        h3 {{ color: #7f8c8d; }}
        code {{ background-color: #f8f9fa; padding: 2px 4px; border-radius: 3px; }}
        pre {{ background-color: #f8f9fa; padding: 10px; border-radius: 5px; overflow-x: auto; }}
        table {{ border-collapse: collapse; width: 100%; margin: 20px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        .highlight {{ background-color: #fff3cd; padding: 2px 4px; border-radius: 3px; }}
        .metadata {{ font-size: 0.9em; color: #6c757d; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; }}
    </style>
</head>
<body>
    <h1>{topic.Title}</h1>
    <div class='content'>
        {content}
    </div>
    <div class='metadata'>
        <p><strong>分类:</strong> {topic.Category}</p>
        <p><strong>最后更新:</strong> {topic.LastModified:yyyy-MM-dd HH:mm}</p>
        {(!string.IsNullOrEmpty(topic.Author) ? $"<p><strong>作者:</strong> {topic.Author}</p>" : "")}
    </div>
</body>
</html>";

            return html;
        }

        /// <summary>
        /// 生成主页内容
        /// </summary>
        /// <returns>主页HTML内容</returns>
        private string GenerateHomeContent()
        {
            return $@"
# 欢迎使用波神K线测量工具帮助中心

## 快速导航

### 入门指南
- [软件简介](#introduction) - 了解软件基本功能
- [快速开始](#tutorial) - 学习基本操作流程
- [界面介绍](#interface) - 熟悉用户界面

### 功能说明
- [K线选择](#kline-selection) - K线选择和测量
- [影线测量](#shadow-measurement) - 影线分析功能
- [算法预测](#prediction) - 波神算法预测

### 操作技巧
- [快捷键指南](#shortcuts) - 提高操作效率
- [使用技巧](#tips) - 实用技巧分享
- [常见问题](#troubleshooting) - 问题解决方案

## 搜索帮助
使用左侧搜索框快速找到您需要的内容。

## 联系支持
如果您在使用过程中遇到问题，请：
1. 查看本帮助文档
2. 访问在线帮助中心
3. 联系技术支持

---
*最后更新: {DateTime.Now:yyyy年MM月dd日}*";
        }

        /// <summary>
        /// 生成索引内容
        /// </summary>
        /// <returns>索引HTML内容</returns>
        private string GenerateIndexContent()
        {
            var topics = _helpSystem.GetAllTopics();
            var indexHtml = new StringBuilder();

            indexHtml.AppendLine("# 帮助索引\n");

            foreach (var category in topics.GroupBy(t => t.Category ?? "其他").OrderBy(g => g.Key))
            {
                indexHtml.AppendLine($"## {category.Key}\n");

                foreach (var topic in category.OrderBy(t => t.Title))
                {
                    indexHtml.AppendLine($"- [{topic.Title}](javascript:openTopic('{topic.Id}'))");
                    if (!string.IsNullOrEmpty(topic.Keywords))
                    {
                        var keywords = string.Join(", ", topic.Keywords);
                        indexHtml.AppendLine($"  - 关键词: {keywords}");
                    }
                }

                indexHtml.AppendLine();
            }

            return indexHtml.ToString();
        }

        /// <summary>
        /// 生成搜索结果内容
        /// </summary>
        /// <param name="searchTerm">搜索词</param>
        /// <param name="results">搜索结果</param>
        /// <returns>搜索结果HTML内容</returns>
        private string GenerateSearchResultsContent(string searchTerm, List<HelpSearchResult> results)
        {
            var html = new StringBuilder();

            html.AppendLine($"# 搜索结果: \"{searchTerm}\"\n");
            html.AppendLine($"找到 {results.Count} 个结果\n");

            if (results.Count == 0)
            {
                html.AppendLine("没有找到相关内容。请尝试：");
                html.AppendLine("- 使用不同的关键词");
                html.AppendLine("- 检查拼写是否正确");
                html.AppendLine("- 浏览帮助索引");
            }
            else
            {
                foreach (var result in results)
                {
                    html.AppendLine($"## [{result.Topic.Title}](javascript:openTopic('{result.Topic.Id}'))");
                    html.AppendLine($"**匹配度:** {(result.Score * 100):F0}%");

                    if (!string.IsNullOrEmpty(result.MatchedText))
                    {
                        html.AppendLine($"**匹配内容:** {result.MatchedText}");
                    }

                    html.AppendLine($"**分类:** {result.Topic.Category}");
                    html.AppendLine();
                }
            }

            return html.ToString();
        }

        /// <summary>
        /// 获取主题样式
        /// </summary>
        /// <returns>CSS样式</returns>
        private string GetThemeStyles()
        {
            return _currentTheme switch
            {
                HelpTheme.Dark => @"
                    body { background-color: #2d2d30; color: #ffffff; }
                    h1 { color: #569cd6; border-bottom-color: #569cd6; }
                    h2 { color: #4ec9b0; }
                    h3 { color: #ce9178; }
                    code { background-color: #1e1e1e; color: #d4d4d4; }
                    pre { background-color: #1e1e1e; }
                    table { border-color: #3e3e42; }
                    th { background-color: #3e3e42; }
                    .highlight { background-color: #3a3d41; }
                    .metadata { color: #969696; border-top-color: #3e3e42; }",
                HelpTheme.Light => @"
                    body { background-color: #ffffff; color: #000000; }
                    h1 { color: #2c3e50; border-bottom-color: #3498db; }
                    h2 { color: #34495e; }
                    h3 { color: #7f8c8d; }
                    code { background-color: #f8f9fa; color: #e83e8c; }
                    pre { background-color: #f8f9fa; }
                    table { border-color: #dee2e6; }
                    th { background-color: #e9ecef; }
                    .highlight { background-color: #fff3cd; }
                    .metadata { color: #6c757d; border-top-color: #dee2e6; }",
                _ => @"
                    body { background-color: #f8f9fa; color: #212529; }
                    h1 { color: #2c3e50; border-bottom-color: #3498db; }
                    h2 { color: #34495e; }
                    h3 { color: #7f8c8d; }
                    code { background-color: #f8f9fa; color: #c7254e; }
                    pre { background-color: #f8f9fa; }
                    table { border-color: #dee2e6; }
                    th { background-color: #e9ecef; }
                    .highlight { background-color: #fff3cd; }
                    .metadata { color: #6c757d; border-top-color: #dee2e6; }"
            };
        }

        /// <summary>
        /// 高亮文本
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <param name="highlightText">要高亮的文本</param>
        /// <returns>高亮后的文本</returns>
        private string HighlightText(string text, string highlightText)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(highlightText))
                return text;

            var words = highlightText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = text;

            foreach (var word in words)
            {
                var pattern = $@"(?i){word}";
                var replacement = $@"<span class='highlight'>{word}</span>";
                result = System.Text.RegularExpressions.Regex.Replace(result, pattern, replacement);
            }

            return result;
        }

        /// <summary>
        /// 转换Markdown到HTML
        /// </summary>
        /// <param name="markdown">Markdown文本</param>
        /// <returns>HTML文本</returns>
        private string ConvertMarkdownToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            var html = markdown;

            // 标题转换
            html = System.Text.RegularExpressions.Regex.Replace(html, @"^### (.+)$", "<h3>$1</h3>", System.Text.RegularExpressions.RegexOptions.Multiline);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"^## (.+)$", "<h2>$1</h2>", System.Text.RegularExpressions.RegexOptions.Multiline);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"^# (.+)$", "<h1>$1</h1>", System.Text.RegularExpressions.RegexOptions.Multiline);

            // 粗体
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\*\*(.+?)\*\*", "<strong>$1</strong>");

            // 斜体
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\*(.+?)\*", "<em>$1</em>");

            // 链接
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\[(.+?)\]\((.+?)\)", "<a href='$2'>$1</a>");

            // 表格
            var lines = html.Split('\n');
            var inTable = false;
            var tableRows = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (line.StartsWith("|") && line.EndsWith("|"))
                {
                    if (!inTable)
                    {
                        inTable = true;
                        tableRows.Clear();
                    }

                    // 跳过表头分隔行
                    if (!line.Replace(" ", "").Replace("|", "").Replace("-", "").Equals(""))
                    {
                        var cells = line.Split('|').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToArray();
                        var isHeader = i > 0 && lines[i - 1].Trim().StartsWith("|") &&
                                     (lines[i - 1].Contains("---") || lines[i - 1].Contains("---"));
                        var tag = isHeader ? "th" : "td";
                        var row = $"<tr>{string.Join("", cells.Select(c => $"<{tag}>{c}</{tag}>"))}</tr>";
                        tableRows.Add(row);
                    }
                }
                else if (inTable)
                {
                    inTable = false;
                    if (tableRows.Count > 0)
                    {
                        var tableHtml = $"<table>{string.Join("", tableRows)}</table>";
                        lines[i - tableRows.Count - 1] = tableHtml;

                        // 移除原始表格行
                        for (int j = i - tableRows.Count; j < i; j++)
                        {
                            lines[j] = "";
                        }
                    }
                }
            }

            html = string.Join("\n", lines);

            // 段落
            var paragraphs = html.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            html = string.Join("\n\n", paragraphs.Select(p =>
            {
                p = p.Trim();
                if (p.StartsWith("<") || string.IsNullOrWhiteSpace(p))
                    return p;
                return $"<p>{p}</p>";
            }));

            return html;
        }

        /// <summary>
        /// 更新导航按钮状态
        /// </summary>
        private void UpdateNavigationButtons()
        {
            _backButton.Enabled = _navigationHistory.Count > 0;
            _forwardButton.Enabled = _forwardHistory.Count > 0;
        }

        #endregion

        #region 事件处理方法

        /// <summary>
        /// 帮助窗口加载事件
        /// </summary>
        private void OnHelpWindowLoad(object sender, EventArgs e)
        {
            // 设置浏览器文档文本，启用JavaScript函数
            _contentBrowser.DocumentText = @"
<!DOCTYPE html>
<html>
<head>
    <script>
        function openTopic(topicId) {
            window.external.openTopic(topicId);
        }
    </script>
</head>
<body>
    <p>正在加载帮助内容...</p>
</body>
</html>";
        }

        /// <summary>
        /// 分类树选择事件
        /// </summary>
        private void OnCategoryTreeAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is HelpTopic topic)
            {
                DisplayTopic(topic);
            }
        }

        /// <summary>
        /// 搜索文本框按键事件
        /// </summary>
        private void OnSearchTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                PerformSearch();
            }
        }

        /// <summary>
        /// 搜索按钮点击事件
        /// </summary>
        private void OnSearchButtonClick(object sender, EventArgs e)
        {
            PerformSearch();
        }

        /// <summary>
        /// 主页按钮点击事件
        /// </summary>
        private void OnHomeButtonClick(object sender, EventArgs e)
        {
            ShowHome();
        }

        /// <summary>
        /// 后退按钮点击事件
        /// </summary>
        private void OnBackButtonClick(object sender, EventArgs e)
        {
            if (_navigationHistory.Count > 0)
            {
                _forwardHistory.Push(_currentTopic);
                var previousTopic = _navigationHistory.Pop();
                DisplayTopic(previousTopic);
            }
        }

        /// <summary>
        /// 前进按钮点击事件
        /// </summary>
        private void OnForwardButtonClick(object sender, EventArgs e)
        {
            if (_forwardHistory.Count > 0)
            {
                _navigationHistory.Push(_currentTopic);
                var nextTopic = _forwardHistory.Pop();
                DisplayTopic(nextTopic);
            }
        }

        /// <summary>
        /// 内容浏览器导航事件
        /// </summary>
        private void OnContentBrowserNavigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url.Scheme == "javascript")
            {
                e.Cancel = true;

                // 处理JavaScript调用
                var script = e.Url.ToString();
                if (script.StartsWith("javascript:openTopic('"))
                {
                    var topicId = script.Substring(21, script.Length - 23); // 提取topicId
                    var topic = _helpSystem.GetHelpTopic(topicId);
                    if (topic != null)
                    {
                        DisplayTopic(topic);
                    }
                }
            }
        }

        /// <summary>
        /// 执行搜索
        /// </summary>
        private void PerformSearch()
        {
            var searchTerm = _searchTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var results = _helpSystem.SearchHelp(searchTerm);
                ShowSearchResults(searchTerm, results);
            }
        }

        #endregion
    }

    /// <summary>
    /// WebBrowser扩展，支持外部调用
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    public class HelpBrowserExternal
    {
        private readonly HelpWindow _helpWindow;

        public HelpBrowserExternal(HelpWindow helpWindow)
        {
            _helpWindow = helpWindow;
        }

        public void openTopic(string topicId)
        {
            var topic = _helpWindow._helpSystem.GetHelpTopic(topicId);
            if (topic != null)
            {
                _helpWindow.DisplayTopic(topic);
            }
        }
    }
}