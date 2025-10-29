using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoshenCC.Core.Services;
using BoshenCC.Core.Utils;
using BoshenCC.Models;

namespace BoshenCC.Services.Implementations
{
    /// <summary>
    /// 预测线管理服务
    /// 提供预测线的完整生命周期管理，包括创建、存储、查询、更新等功能
    /// </summary>
    public class PredictionService : IPredictionService
    {
        #region 私有字段

        private readonly IBoshenAlgorithmService _boshenAlgorithmService;
        private readonly Dictionary<string, PredictionLineGroup> _predictionGroups;
        private readonly Dictionary<string, PredictionLine> _individualLines;
        private readonly object _lock = new object();

        // 事件
        public event EventHandler<PredictionLineAddedEventArgs> PredictionLineAdded;
        public event EventHandler<PredictionLineUpdatedEventArgs> PredictionLineUpdated;
        public event EventHandler<PredictionLineRemovedEventArgs> PredictionLineRemoved;
        public event EventHandler<PredictionGroupAddedEventArgs> PredictionGroupAdded;
        public event EventHandler<PredictionGroupUpdatedEventArgs> PredictionGroupUpdated;
        public event EventHandler<PredictionGroupRemovedEventArgs> PredictionGroupRemoved;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化预测线管理服务
        /// </summary>
        /// <param name="boshenAlgorithmService">波神算法服务</param>
        public PredictionService(IBoshenAlgorithmService boshenAlgorithmService)
        {
            _boshenAlgorithmService = boshenAlgorithmService ?? throw new ArgumentNullException(nameof(boshenAlgorithmService));
            _predictionGroups = new Dictionary<string, PredictionLineGroup>();
            _individualLines = new Dictionary<string, PredictionLine>();
        }

        #endregion

        #region 预测线创建

        /// <summary>
        /// 基于K线创建预测线组
        /// </summary>
        /// <param name="kline">K线信息</param>
        /// <param name="groupName">组名称（可选）</param>
        /// <returns>预测线组</returns>
        public async Task<PredictionLineGroup> CreatePredictionGroupAsync(KLineInfo kline, string groupName = null)
        {
            if (kline == null)
                throw new ArgumentNullException(nameof(kline));

            try
            {
                // 计算预测线
                var lines = await _boshenAlgorithmService.CalculateBoshenLinesAsync(kline);

                // 创建预测线组
                var groupId = GenerateGroupId();
                var group = new PredictionLineGroup
                {
                    Id = groupId,
                    Name = groupName ?? $"预测组_{DateTime.Now:yyyyMMdd_HHmmss}",
                    SourceKLine = kline,
                    Lines = lines,
                    CreatedTime = DateTime.Now,
                    UpdatedTime = DateTime.Now,
                    IsActive = true,
                    Symbol = kline.Symbol,
                    TimeFrame = kline.TimeFrame
                };

                // 存储预测线组
                lock (_lock)
                {
                    _predictionGroups[groupId] = group;
                    foreach (var line in lines)
                    {
                        line.GroupId = groupId;
                        _individualLines[line.GetUniqueId()] = line;
                    }
                }

                // 触发事件
                PredictionGroupAdded?.Invoke(this, new PredictionGroupAddedEventArgs(group));
                foreach (var line in lines)
                {
                    PredictionLineAdded?.Invoke(this, new PredictionLineAddedEventArgs(line, groupId));
                }

                return group;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"创建预测线组失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 基于价格创建预测线组
        /// </summary>
        /// <param name="pointAPrice">A点价格</param>
        /// <param name="pointBPrice">B点价格</param>
        /// <param name="symbol">交易品种</param>
        /// <param name="timeFrame">时间周期</param>
        /// <param name="groupName">组名称</param>
        /// <returns>预测线组</returns>
        public async Task<PredictionLineGroup> CreatePredictionGroupAsync(double pointAPrice, double pointBPrice,
            string symbol = null, string timeFrame = null, string groupName = null)
        {
            try
            {
                // 计算预测线
                var lines = await _boshenAlgorithmService.CalculateBoshenLinesAsync(pointAPrice, pointBPrice, symbol, timeFrame);

                // 创建虚拟K线信息
                var virtualKLine = new KLineInfo
                {
                    LowPrice = pointAPrice,
                    HighPrice = pointBPrice,
                    Symbol = symbol,
                    TimeFrame = timeFrame,
                    Timestamp = DateTime.Now
                };

                // 创建预测线组
                var groupId = GenerateGroupId();
                var group = new PredictionLineGroup
                {
                    Id = groupId,
                    Name = groupName ?? $"预测组_{DateTime.Now:yyyyMMdd_HHmmss}",
                    SourceKLine = virtualKLine,
                    Lines = lines,
                    CreatedTime = DateTime.Now,
                    UpdatedTime = DateTime.Now,
                    IsActive = true,
                    Symbol = symbol,
                    TimeFrame = timeFrame
                };

                // 存储预测线组
                lock (_lock)
                {
                    _predictionGroups[groupId] = group;
                    foreach (var line in lines)
                    {
                        line.GroupId = groupId;
                        _individualLines[line.GetUniqueId()] = line;
                    }
                }

                // 触发事件
                PredictionGroupAdded?.Invoke(this, new PredictionGroupAddedEventArgs(group));
                foreach (var line in lines)
                {
                    PredictionLineAdded?.Invoke(this, new PredictionLineAddedEventArgs(line, groupId));
                }

                return group;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"创建预测线组失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 预测线查询

        /// <summary>
        /// 获取所有预测线组
        /// </summary>
        /// <returns>预测线组列表</returns>
        public async Task<List<PredictionLineGroup>> GetAllPredictionGroupsAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return _predictionGroups.Values.ToList();
                }
            });
        }

        /// <summary>
        /// 根据ID获取预测线组
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <returns>预测线组</returns>
        public async Task<PredictionLineGroup> GetPredictionGroupAsync(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
                return null;

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return _predictionGroups.TryGetValue(groupId, out var group) ? group : null;
                }
            });
        }

        /// <summary>
        /// 根据条件查询预测线组
        /// </summary>
        /// <param name="filter">查询过滤器</param>
        /// <returns>预测线组列表</returns>
        public async Task<List<PredictionLineGroup>> QueryPredictionGroupsAsync(PredictionGroupFilter filter)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    var query = _predictionGroups.Values.AsEnumerable();

                    if (filter != null)
                    {
                        if (!string.IsNullOrEmpty(filter.Symbol))
                            query = query.Where(g => g.Symbol == filter.Symbol);

                        if (!string.IsNullOrEmpty(filter.TimeFrame))
                            query = query.Where(g => g.TimeFrame == filter.TimeFrame);

                        if (filter.IsActive.HasValue)
                            query = query.Where(g => g.IsActive == filter.IsActive.Value);

                        if (filter.CreatedAfter.HasValue)
                            query = query.Where(g => g.CreatedTime >= filter.CreatedAfter.Value);

                        if (filter.CreatedBefore.HasValue)
                            query = query.Where(g => g.CreatedTime <= filter.CreatedBefore.Value);

                        if (!string.IsNullOrEmpty(filter.NameContains))
                            query = query.Where(g => g.Name.Contains(filter.NameContains));
                    }

                    return query.ToList();
                }
            });
        }

        /// <summary>
        /// 获取单条预测线
        /// </summary>
        /// <param name="lineId">线索引ID</param>
        /// <returns>预测线</returns>
        public async Task<PredictionLine> GetPredictionLineAsync(string lineId)
        {
            if (string.IsNullOrEmpty(lineId))
                return null;

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    return _individualLines.TryGetValue(lineId, out var line) ? line : null;
                }
            });
        }

        /// <summary>
        /// 根据价格查找接近的预测线
        /// </summary>
        /// <param name="currentPrice">当前价格</param>
        /// <param name="tolerancePercent">容差百分比</param>
        /// <param name="symbol">交易品种（可选）</param>
        /// <returns>接近的预测线列表</returns>
        public async Task<List<PredictionLine>> FindNearbyLinesAsync(double currentPrice,
            double tolerancePercent = 0.1, string symbol = null)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    var lines = _individualLines.Values.AsEnumerable();

                    if (!string.IsNullOrEmpty(symbol))
                        lines = lines.Where(l => l.Symbol == symbol);

                    return _boshenAlgorithmService.FindNearbyLines(lines.ToList(), currentPrice, tolerancePercent);
                }
            });
        }

        #endregion

        #region 预测线更新

        /// <summary>
        /// 更新预测线
        /// </summary>
        /// <param name="lineId">线索引ID</param>
        /// <param name="updates">更新数据</param>
        /// <returns>更新后的预测线</returns>
        public async Task<PredictionLine> UpdatePredictionLineAsync(string lineId, PredictionLineUpdate updates)
        {
            if (string.IsNullOrEmpty(lineId))
                throw new ArgumentException("线索引ID不能为空", nameof(lineId));

            if (updates == null)
                throw new ArgumentNullException(nameof(updates));

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!_individualLines.TryGetValue(lineId, out var line))
                        throw new KeyNotFoundException($"未找到ID为 {lineId} 的预测线");

                    var originalLine = line.Clone();

                    // 应用更新
                    if (updates.Color.HasValue)
                        line.Color = updates.Color.Value;

                    if (updates.Width.HasValue)
                        line.Width = updates.Width.Value;

                    if (updates.Style.HasValue)
                        line.Style = updates.Style.Value;

                    if (updates.Opacity.HasValue)
                        line.Opacity = updates.Opacity.Value;

                    if (updates.ShowPriceLabel.HasValue)
                        line.ShowPriceLabel = updates.ShowPriceLabel.Value;

                    if (updates.Confidence.HasValue)
                        line.Confidence = updates.Confidence.Value;

                    if (updates.AccuracyScore.HasValue)
                        line.AccuracyScore = updates.AccuracyScore.Value;

                    if (updates.TriggerCount.HasValue)
                        line.TriggerCount = updates.TriggerCount.Value;

                    if (updates.SuccessRate.HasValue)
                        line.SuccessRate = updates.SuccessRate.Value;

                    line.CalculationTime = DateTime.Now;

                    // 更新所属组的时间戳
                    if (!string.IsNullOrEmpty(line.GroupId) && _predictionGroups.TryGetValue(line.GroupId, out var group))
                    {
                        group.UpdatedTime = DateTime.Now;
                    }

                    // 触发事件
                    PredictionLineUpdated?.Invoke(this, new PredictionLineUpdatedEventArgs(originalLine, line));

                    return line;
                }
            });
        }

        /// <summary>
        /// 更新预测线组
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <param name="updates">更新数据</param>
        /// <returns>更新后的预测线组</returns>
        public async Task<PredictionLineGroup> UpdatePredictionGroupAsync(string groupId, PredictionGroupUpdate updates)
        {
            if (string.IsNullOrEmpty(groupId))
                throw new ArgumentException("组ID不能为空", nameof(groupId));

            if (updates == null)
                throw new ArgumentNullException(nameof(updates));

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!_predictionGroups.TryGetValue(groupId, out var group))
                        throw new KeyNotFoundException($"未找到ID为 {groupId} 的预测线组");

                    var originalGroup = ClonePredictionGroup(group);

                    // 应用更新
                    if (!string.IsNullOrEmpty(updates.Name))
                        group.Name = updates.Name;

                    if (updates.IsActive.HasValue)
                        group.IsActive = updates.IsActive.Value;

                    if (!string.IsNullOrEmpty(updates.Description))
                        group.Description = updates.Description;

                    if (updates.Tags != null)
                        group.Tags = updates.Tags.ToList();

                    group.UpdatedTime = DateTime.Now;

                    // 触发事件
                    PredictionGroupUpdated?.Invoke(this, new PredictionGroupUpdatedEventArgs(originalGroup, group));

                    return group;
                }
            });
        }

        #endregion

        #region 预测线删除

        /// <summary>
        /// 删除预测线
        /// </summary>
        /// <param name="lineId">线索引ID</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeletePredictionLineAsync(string lineId)
        {
            if (string.IsNullOrEmpty(lineId))
                return false;

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!_individualLines.TryGetValue(lineId, out var line))
                        return false;

                    _individualLines.Remove(lineId);

                    // 从所属组中移除
                    if (!string.IsNullOrEmpty(line.GroupId) && _predictionGroups.TryGetValue(line.GroupId, out var group))
                    {
                        group.Lines.Remove(line);
                        group.UpdatedTime = DateTime.Now;
                    }

                    // 触发事件
                    PredictionLineRemoved?.Invoke(this, new PredictionLineRemovedEventArgs(line));

                    return true;
                }
            });
        }

        /// <summary>
        /// 删除预测线组
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeletePredictionGroupAsync(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
                return false;

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!_predictionGroups.TryGetValue(groupId, out var group))
                        return false;

                    // 移除组内的所有预测线
                    foreach (var line in group.Lines)
                    {
                        _individualLines.Remove(line.GetUniqueId());
                    }

                    _predictionGroups.Remove(groupId);

                    // 触发事件
                    PredictionGroupRemoved?.Invoke(this, new PredictionGroupRemovedEventArgs(group));

                    return true;
                }
            });
        }

        /// <summary>
        /// 清空所有预测线
        /// </summary>
        /// <returns>清空的数量</returns>
        public async Task<int> ClearAllPredictionsAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    var count = _predictionGroups.Count;

                    _predictionGroups.Clear();
                    _individualLines.Clear();

                    return count;
                }
            });
        }

        #endregion

        #region 统计和分析

        /// <summary>
        /// 获取预测线统计信息
        /// </summary>
        /// <param name="symbol">交易品种（可选）</param>
        /// <returns>统计信息</returns>
        public async Task<PredictionStatistics> GetStatisticsAsync(string symbol = null)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    var groups = _predictionGroups.Values.AsEnumerable();
                    var lines = _individualLines.Values.AsEnumerable();

                    if (!string.IsNullOrEmpty(symbol))
                    {
                        groups = groups.Where(g => g.Symbol == symbol);
                        lines = lines.Where(l => l.Symbol == symbol);
                    }

                    var groupList = groups.ToList();
                    var lineList = lines.ToList();

                    return new PredictionStatistics
                    {
                        TotalGroups = groupList.Count,
                        ActiveGroups = groupList.Count(g => g.IsActive),
                        TotalLines = lineList.Count,
                        KeyLines = lineList.Count(l => l.IsKeyLine),
                        AverageConfidence = lineList.Any() ? lineList.Average(l => l.Confidence) : 0,
                        AverageAccuracy = lineList.Where(l => l.AccuracyScore > 0).Any() ?
                            lineList.Where(l => l.AccuracyScore > 0).Average(l => l.AccuracyScore) : 0,
                        SymbolCount = groupList.Select(g => g.Symbol).Distinct().Count(),
                        LastUpdateTime = lineList.Any() ? lineList.Max(l => l.CalculationTime) : DateTime.MinValue
                    };
                }
            });
        }

        /// <summary>
        /// 获取预测线性能分析
        /// </summary>
        /// <param name="groupId">组ID（可选）</param>
        /// <returns>性能分析结果</returns>
        public async Task<PredictionPerformanceAnalysis> AnalyzePerformanceAsync(string groupId = null)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    var lines = _individualLines.Values.AsEnumerable();

                    if (!string.IsNullOrEmpty(groupId))
                    {
                        lines = lines.Where(l => l.GroupId == groupId);
                    }

                    var lineList = lines.ToList();
                    var triggeredLines = lineList.Where(l => l.TriggerCount > 0).ToList();

                    return new PredictionPerformanceAnalysis
                    {
                        TotalLines = lineList.Count,
                        TriggeredLines = triggeredLines.Count,
                        TriggerRate = lineList.Any() ? (double)triggeredLines.Count / lineList.Count : 0,
                        AverageTriggerCount = triggeredLines.Any() ? triggeredLines.Average(l => l.TriggerCount) : 0,
                        AverageSuccessRate = triggeredLines.Any() ? triggeredLines.Average(l => l.SuccessRate) : 0,
                        HighAccuracyLines = triggeredLines.Count(l => l.SuccessRate >= 0.8),
                        KeyLinePerformance = AnalyzeKeyLinePerformance(triggeredLines)
                    };
                }
            });
        }

        #endregion

        #region 实用工具方法

        /// <summary>
        /// 导出预测线组为JSON
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <returns>JSON字符串</returns>
        public async Task<string> ExportGroupToJsonAsync(string groupId)
        {
            var group = await GetPredictionGroupAsync(groupId);
            if (group == null)
                throw new KeyNotFoundException($"未找到ID为 {groupId} 的预测线组");

            return group.ToJson();
        }

        /// <summary>
        /// 从JSON导入预测线组
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>导入的预测线组</returns>
        public async Task<PredictionLineGroup> ImportGroupFromJsonAsync(string json)
        {
            try
            {
                var group = PredictionLineGroup.FromJson(json);

                // 生成新的ID避免冲突
                var newGroupId = GenerateGroupId();
                var oldGroupId = group.Id;
                group.Id = newGroupId;

                // 更新预测线的组ID
                foreach (var line in group.Lines)
                {
                    line.GroupId = newGroupId;
                }

                // 存储到内存
                lock (_lock)
                {
                    _predictionGroups[newGroupId] = group;
                    foreach (var line in group.Lines)
                    {
                        _individualLines[line.GetUniqueId()] = line;
                    }
                }

                // 触发事件
                PredictionGroupAdded?.Invoke(this, new PredictionGroupAddedEventArgs(group));
                foreach (var line in group.Lines)
                {
                    PredictionLineAdded?.Invoke(this, new PredictionLineAddedEventArgs(line, newGroupId));
                }

                return group;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"导入预测线组失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 生成组ID
        /// </summary>
        /// <returns>组ID</returns>
        private string GenerateGroupId()
        {
            return $"group_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        /// <summary>
        /// 克隆预测线组
        /// </summary>
        /// <param name="original">原始组</param>
        /// <returns>克隆的组</returns>
        private PredictionLineGroup ClonePredictionGroup(PredictionLineGroup original)
        {
            return new PredictionLineGroup
            {
                Id = original.Id,
                Name = original.Name,
                Description = original.Description,
                SourceKLine = original.SourceKLine,
                Lines = original.Lines.Select(l => l.Clone()).ToList(),
                CreatedTime = original.CreatedTime,
                UpdatedTime = original.UpdatedTime,
                IsActive = original.IsActive,
                Symbol = original.Symbol,
                TimeFrame = original.TimeFrame,
                Tags = original.Tags?.ToList()
            };
        }

        /// <summary>
        /// 分析重点线性能
        /// </summary>
        /// <param name="triggeredLines">已触发的预测线</param>
        /// <returns>重点线性能</returns>
        private KeyLinePerformance AnalyzeKeyLinePerformance(List<PredictionLine> triggeredLines)
        {
            var keyLines = triggeredLines.Where(l => l.IsKeyLine).ToList();

            return new KeyLinePerformance
            {
                TotalKeyLines = keyLines.Count,
                TriggeredKeyLines = keyLines.Count,
                AverageSuccessRate = keyLines.Any() ? keyLines.Average(l => l.SuccessRate) : 0,
                Line3Performance = keyLines.FirstOrDefault(l => l.Index == 3)?.SuccessRate ?? 0,
                Line6Performance = keyLines.FirstOrDefault(l => l.Index == 6)?.SuccessRate ?? 0,
                Line8Performance = keyLines.FirstOrDefault(l => l.Index == 8)?.SuccessRate ?? 0
            };
        }

        #endregion
    }

    #region 接口定义

    /// <summary>
    /// 预测线管理服务接口
    /// </summary>
    public interface IPredictionService
    {
        // 事件
        event EventHandler<PredictionLineAddedEventArgs> PredictionLineAdded;
        event EventHandler<PredictionLineUpdatedEventArgs> PredictionLineUpdated;
        event EventHandler<PredictionLineRemovedEventArgs> PredictionLineRemoved;
        event EventHandler<PredictionGroupAddedEventArgs> PredictionGroupAdded;
        event EventHandler<PredictionGroupUpdatedEventArgs> PredictionGroupUpdated;
        event EventHandler<PredictionGroupRemovedEventArgs> PredictionGroupRemoved;

        // 创建
        Task<PredictionLineGroup> CreatePredictionGroupAsync(KLineInfo kline, string groupName = null);
        Task<PredictionLineGroup> CreatePredictionGroupAsync(double pointAPrice, double pointBPrice,
            string symbol = null, string timeFrame = null, string groupName = null);

        // 查询
        Task<List<PredictionLineGroup>> GetAllPredictionGroupsAsync();
        Task<PredictionLineGroup> GetPredictionGroupAsync(string groupId);
        Task<List<PredictionLineGroup>> QueryPredictionGroupsAsync(PredictionGroupFilter filter);
        Task<PredictionLine> GetPredictionLineAsync(string lineId);
        Task<List<PredictionLine>> FindNearbyLinesAsync(double currentPrice,
            double tolerancePercent = 0.1, string symbol = null);

        // 更新
        Task<PredictionLine> UpdatePredictionLineAsync(string lineId, PredictionLineUpdate updates);
        Task<PredictionLineGroup> UpdatePredictionGroupAsync(string groupId, PredictionGroupUpdate updates);

        // 删除
        Task<bool> DeletePredictionLineAsync(string lineId);
        Task<bool> DeletePredictionGroupAsync(string groupId);
        Task<int> ClearAllPredictionsAsync();

        // 统计
        Task<PredictionStatistics> GetStatisticsAsync(string symbol = null);
        Task<PredictionPerformanceAnalysis> AnalyzePerformanceAsync(string groupId = null);

        // 工具
        Task<string> ExportGroupToJsonAsync(string groupId);
        Task<PredictionLineGroup> ImportGroupFromJsonAsync(string json);
    }

    #endregion

    #region 数据模型

    /// <summary>
    /// 预测线组
    /// </summary>
    public class PredictionLineGroup
    {
        /// <summary>
        /// 组ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 组名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 组描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 源K线信息
        /// </summary>
        public KLineInfo SourceKLine { get; set; }

        /// <summary>
        /// 预测线列表
        /// </summary>
        public List<PredictionLine> Lines { get; set; } = new List<PredictionLine>();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 交易品种
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// 时间周期
        /// </summary>
        public string TimeFrame { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 转换为JSON字符串
        /// </summary>
        public string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// 从JSON字符串创建实例
        /// </summary>
        public static PredictionLineGroup FromJson(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<PredictionLineGroup>(json);
        }
    }

    /// <summary>
    /// 预测线更新数据
    /// </summary>
    public class PredictionLineUpdate
    {
        public Color? Color { get; set; }
        public int? Width { get; set; }
        public PredictionLineStyle? Style { get; set; }
        public double? Opacity { get; set; }
        public bool? ShowPriceLabel { get; set; }
        public double? Confidence { get; set; }
        public double? AccuracyScore { get; set; }
        public int? TriggerCount { get; set; }
        public double? SuccessRate { get; set; }
    }

    /// <summary>
    /// 预测线组更新数据
    /// </summary>
    public class PredictionGroupUpdate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool? IsActive { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }

    /// <summary>
    /// 预测线组过滤器
    /// </summary>
    public class PredictionGroupFilter
    {
        public string Symbol { get; set; }
        public string TimeFrame { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public string NameContains { get; set; }
    }

    /// <summary>
    /// 预测线统计信息
    /// </summary>
    public class PredictionStatistics
    {
        public int TotalGroups { get; set; }
        public int ActiveGroups { get; set; }
        public int TotalLines { get; set; }
        public int KeyLines { get; set; }
        public double AverageConfidence { get; set; }
        public double AverageAccuracy { get; set; }
        public int SymbolCount { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    /// <summary>
    /// 预测线性能分析
    /// </summary>
    public class PredictionPerformanceAnalysis
    {
        public int TotalLines { get; set; }
        public int TriggeredLines { get; set; }
        public double TriggerRate { get; set; }
        public double AverageTriggerCount { get; set; }
        public double AverageSuccessRate { get; set; }
        public int HighAccuracyLines { get; set; }
        public KeyLinePerformance KeyLinePerformance { get; set; }
    }

    /// <summary>
    /// 重点线性能
    /// </summary>
    public class KeyLinePerformance
    {
        public int TotalKeyLines { get; set; }
        public int TriggeredKeyLines { get; set; }
        public double AverageSuccessRate { get; set; }
        public double Line3Performance { get; set; }
        public double Line6Performance { get; set; }
        public double Line8Performance { get; set; }
    }

    #endregion

    #region 事件参数

    /// <summary>
    /// 预测线添加事件参数
    /// </summary>
    public class PredictionLineAddedEventArgs : EventArgs
    {
        public PredictionLine Line { get; }
        public string GroupId { get; }

        public PredictionLineAddedEventArgs(PredictionLine line, string groupId)
        {
            Line = line;
            GroupId = groupId;
        }
    }

    /// <summary>
    /// 预测线更新事件参数
    /// </summary>
    public class PredictionLineUpdatedEventArgs : EventArgs
    {
        public PredictionLine OriginalLine { get; }
        public PredictionLine UpdatedLine { get; }

        public PredictionLineUpdatedEventArgs(PredictionLine originalLine, PredictionLine updatedLine)
        {
            OriginalLine = originalLine;
            UpdatedLine = updatedLine;
        }
    }

    /// <summary>
    /// 预测线删除事件参数
    /// </summary>
    public class PredictionLineRemovedEventArgs : EventArgs
    {
        public PredictionLine Line { get; }

        public PredictionLineRemovedEventArgs(PredictionLine line)
        {
            Line = line;
        }
    }

    /// <summary>
    /// 预测线组添加事件参数
    /// </summary>
    public class PredictionGroupAddedEventArgs : EventArgs
    {
        public PredictionLineGroup Group { get; }

        public PredictionGroupAddedEventArgs(PredictionLineGroup group)
        {
            Group = group;
        }
    }

    /// <summary>
    /// 预测线组更新事件参数
    /// </summary>
    public class PredictionGroupUpdatedEventArgs : EventArgs
    {
        public PredictionLineGroup OriginalGroup { get; }
        public PredictionLineGroup UpdatedGroup { get; }

        public PredictionGroupUpdatedEventArgs(PredictionLineGroup originalGroup, PredictionLineGroup updatedGroup)
        {
            OriginalGroup = originalGroup;
            UpdatedGroup = updatedGroup;
        }
    }

    /// <summary>
    /// 预测线组删除事件参数
    /// </summary>
    public class PredictionGroupRemovedEventArgs : EventArgs
    {
        public PredictionLineGroup Group { get; }

        public PredictionGroupRemovedEventArgs(PredictionLineGroup group)
        {
            Group = group;
        }
    }

    #endregion
}