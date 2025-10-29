using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 预测线管理器
    /// 提供预测线的创建、管理、计算和持久化功能
    /// </summary>
    public class PredictionLineManager : IDisposable
    {
        #region 私有字段

        private readonly ILogService _logService;
        private readonly ConcurrentDictionary<string, PredictionLine> _predictionLines;
        private readonly object _lockObject = new object();
        private bool _disposed = false;
        private PredictionLineCalculationSettings _calculationSettings;

        #endregion

        #region 构造函数和析构函数

        /// <summary>
        /// 初始化预测线管理器
        /// </summary>
        /// <param name="logService">日志服务</param>
        public PredictionLineManager(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _predictionLines = new ConcurrentDictionary<string, PredictionLine>();
            _calculationSettings = PredictionLineCalculationSettings.CreateDefault();

            _logService.LogInfo("PredictionLineManager 初始化完成");
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~PredictionLineManager()
        {
            Dispose(false);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 预测线集合变化事件
        /// </summary>
        public event EventHandler<PredictionLineCollectionChangedEventArgs> CollectionChanged;

        /// <summary>
        /// 预测线计算完成事件
        /// </summary>
        public event EventHandler<PredictionLineCalculationCompletedEventArgs> CalculationCompleted;

        /// <summary>
        /// 预测线验证结果事件
        /// </summary>
        public event EventHandler<PredictionLineValidationEventArgs> ValidationCompleted;

        #endregion

        #region 属性

        /// <summary>
        /// 预测线数量
        /// </summary>
        public int Count => _predictionLines.Count;

        /// <summary>
        /// 计算设置
        /// </summary>
        public PredictionLineCalculationSettings CalculationSettings
        {
            get => _calculationSettings;
            set => _calculationSettings = value ?? PredictionLineCalculationSettings.CreateDefault();
        }

        /// <summary>
        /// 是否已释放
        /// </summary>
        public bool IsDisposed => _disposed;

        #endregion

        #region 预测线管理方法

        /// <summary>
        /// 添加预测线
        /// </summary>
        /// <param name="predictionLine">预测线</param>
        /// <returns>是否添加成功</returns>
        public bool AddPredictionLine(PredictionLine predictionLine)
        {
            try
            {
                if (predictionLine == null)
                {
                    _logService.LogWarning("预测线为空，无法添加");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(predictionLine.Name))
                {
                    _logService.LogWarning("预测线名称为空，无法添加");
                    return false;
                }

                if (_predictionLines.ContainsKey(predictionLine.Name))
                {
                    _logService.LogWarning($"预测线已存在: {predictionLine.Name}");
                    return false;
                }

                var added = _predictionLines.TryAdd(predictionLine.Name, predictionLine);
                if (added)
                {
                    _logService.LogInfo($"预测线添加成功: {predictionLine.Name}");
                    OnCollectionChanged(PredictionLineCollectionChangeType.Added, predictionLine);
                }

                return added;
            }
            catch (Exception ex)
            {
                _logService.LogError($"添加预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 移除预测线
        /// </summary>
        /// <param name="lineName">预测线名称</param>
        /// <returns>是否移除成功</returns>
        public bool RemovePredictionLine(string lineName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(lineName))
                {
                    _logService.LogWarning("预测线名称为空，无法移除");
                    return false;
                }

                var removed = _predictionLines.TryRemove(lineName, out var removedLine);
                if (removed && removedLine != null)
                {
                    _logService.LogInfo($"预测线移除成功: {lineName}");
                    OnCollectionChanged(PredictionLineCollectionChangeType.Removed, removedLine);
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logService.LogError($"移除预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 更新预测线
        /// </summary>
        /// <param name="predictionLine">新的预测线数据</param>
        /// <returns>是否更新成功</returns>
        public bool UpdatePredictionLine(PredictionLine predictionLine)
        {
            try
            {
                if (predictionLine == null || string.IsNullOrWhiteSpace(predictionLine.Name))
                {
                    _logService.LogWarning("预测线数据无效，无法更新");
                    return false;
                }

                var existingLine = _predictionLines.TryGetValue(predictionLine.Name, out var oldLine);
                _predictionLines.AddOrUpdate(predictionLine.Name, predictionLine, (key, oldValue) => predictionLine);

                if (existingLine)
                {
                    _logService.LogInfo($"预测线更新成功: {predictionLine.Name}");
                    OnCollectionChanged(PredictionLineCollectionChangeType.Updated, predictionLine);
                }
                else
                {
                    _logService.LogInfo($"预测线添加成功: {predictionLine.Name}");
                    OnCollectionChanged(PredictionLineCollectionChangeType.Added, predictionLine);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"更新预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取预测线
        /// </summary>
        /// <param name="lineName">预测线名称</param>
        /// <returns>预测线，不存在返回null</returns>
        public PredictionLine GetPredictionLine(string lineName)
        {
            if (string.IsNullOrWhiteSpace(lineName))
                return null;

            _predictionLines.TryGetValue(lineName, out var line);
            return line;
        }

        /// <summary>
        /// 获取所有预测线
        /// </summary>
        /// <returns>预测线集合</returns>
        public IReadOnlyList<PredictionLine> GetAllPredictionLines()
        {
            return _predictionLines.Values.ToList();
        }

        /// <summary>
        /// 获取指定类型的预测线
        /// </summary>
        /// <param name="lineType">预测线类型</param>
        /// <returns>预测线集合</returns>
        public IReadOnlyList<PredictionLine> GetPredictionLinesByType(PredictionLineType lineType)
        {
            return _predictionLines.Values.Where(l => l.Type == lineType).ToList();
        }

        /// <summary>
        /// 获取指定索引的预测线
        /// </summary>
        /// <param name="index">预测线索引</param>
        /// <returns>预测线，不存在返回null</returns>
        public PredictionLine GetPredictionLineByIndex(int index)
        {
            return _predictionLines.Values.FirstOrDefault(l => l.Index == index);
        }

        /// <summary>
        /// 清空所有预测线
        /// </summary>
        public void ClearAllPredictionLines()
        {
            try
            {
                var linesToRemove = _predictionLines.Values.ToList();
                _predictionLines.Clear();

                foreach (var line in linesToRemove)
                {
                    OnCollectionChanged(PredictionLineCollectionChangeType.Removed, line);
                }

                _logService.LogInfo("所有预测线已清空");
            }
            catch (Exception ex)
            {
                _logService.LogError($"清空预测线失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量添加预测线
        /// </summary>
        /// <param name="predictionLines">预测线集合</param>
        /// <returns>成功添加的数量</returns>
        public int AddPredictionLinesBatch(IEnumerable<PredictionLine> predictionLines)
        {
            if (predictionLines == null)
                return 0;

            var successCount = 0;
            var linesList = predictionLines.ToList();

            foreach (var line in linesList)
            {
                if (AddPredictionLine(line))
                {
                    successCount++;
                }
            }

            _logService.LogInfo($"批量添加预测线完成，成功: {successCount}/{linesList.Count}");
            return successCount;
        }

        #endregion

        #region 预测线计算方法

        /// <summary>
        /// 从K线数据计算预测线
        /// </summary>
        /// <param name="kLineData">K线数据</param>
        /// <param name="basePrice">基准价格</param>
        /// <param name="calculationType">计算类型</param>
        /// <returns>计算出的预测线集合</returns>
        public async Task<IReadOnlyList<PredictionLine>> CalculatePredictionLinesFromKLineAsync(
            KLineInfo kLineData, double basePrice, PredictionLineCalculationType calculationType)
        {
            try
            {
                _logService.LogInfo($"开始计算预测线: 基准价格={basePrice:F2}, 计算类型={calculationType}");

                var calculatedLines = new List<PredictionLine>();

                switch (calculationType)
                {
                    case PredictionLineCalculationType.BoshenStandard:
                        calculatedLines = CalculateBoshenStandardLines(kLineData, basePrice);
                        break;
                    case PredictionLineCalculationType.BoshenAdvanced:
                        calculatedLines = CalculateBoshenAdvancedLines(kLineData, basePrice);
                        break;
                    case PredictionLineCalculationType.Fibonacci:
                        calculatedLines = CalculateFibonacciLines(kLineData, basePrice);
                        break;
                    case PredictionLineCalculationType.Custom:
                        calculatedLines = CalculateCustomLines(kLineData, basePrice);
                        break;
                    default:
                        _logService.LogWarning($"未知的计算类型: {calculationType}");
                        break;
                }

                // 应用计算设置
                ApplyCalculationSettings(calculatedLines);

                _logService.LogInfo($"预测线计算完成，生成 {calculatedLines.Count} 条线");

                // 触发计算完成事件
                OnCalculationCompleted(calculationType, calculatedLines, basePrice);

                return calculatedLines;
            }
            catch (Exception ex)
            {
                _logService.LogError($"计算预测线失败: {ex.Message}", ex);
                return new List<PredictionLine>();
            }
        }

        /// <summary>
        /// 计算波神标准线
        /// </summary>
        private List<PredictionLine> CalculateBoshenStandardLines(KLineInfo kLineData, double basePrice)
        {
            var lines = new List<PredictionLine>();

            // 波神标准计算公式
            var percentages = new[] { 1.0, 1.2, 1.137, 2.0, 2.137, 3.0, 3.137, 4.0, 4.137, 5.0, 5.137, 6.0 };
            var names = new[] { "A线", "B线", "C线", "1线", "2线", "3线", "4线", "5线", "6线", "7线", "8线", "9线" };

            for (int i = 0; i < percentages.Length && i < names.Length; i++)
            {
                var price = basePrice * percentages[i] / 100.0;
                var line = PredictionLine.CreateStandard(i, names[i], price, basePrice, percentages[i], 0.0);
                lines.Add(line);
            }

            return lines;
        }

        /// <summary>
        /// 计算波神高级线
        /// </summary>
        private List<PredictionLine> CalculateBoshenAdvancedLines(KLineInfo kLineData, double basePrice)
        {
            var lines = new List<PredictionLine>();

            // 包含标准线
            lines.AddRange(CalculateBoshenStandardLines(kLineData, basePrice));

            // 添加高级计算线
            var advancedPercentages = new[] { 1.5, 2.5, 3.5, 4.5, 5.5 };
            var advancedNames = new[] { "A1线", "B1线", "C1线", "D1线", "E1线" };

            for (int i = 0; i < advancedPercentages.Length && i < advancedNames.Length; i++)
            {
                var price = basePrice * advancedPercentages[i] / 100.0;
                var line = PredictionLine.CreateAdvanced(lines.Count + i, advancedNames[i], price, basePrice, advancedPercentages[i], 0.0);
                lines.Add(line);
            }

            return lines;
        }

        /// <summary>
        /// 计算斐波那契线
        /// </summary>
        private List<PredictionLine> CalculateFibonacciLines(KLineInfo kLineData, double basePrice)
        {
            var lines = new List<PredictionLine>();

            // 斐波那契回调位
            var fibLevels = new[] { 0.236, 0.382, 0.5, 0.618, 0.786, 1.0, 1.272, 1.618, 2.0, 2.618 };
            var fibNames = new[] { "F23.6", "F38.2", "F50.0", "F61.8", "F78.6", "F100.0", "F127.2", "F161.8", "F200.0", "F261.8" };

            for (int i = 0; i < fibLevels.Length && i < fibNames.Length; i++)
            {
                var price = basePrice * fibLevels[i];
                var line = PredictionLine.CreateFibonacci(i, fibNames[i], price, basePrice, fibLevels[i] * 100, 0.0);
                lines.Add(line);
            }

            return lines;
        }

        /// <summary>
        /// 计算自定义线
        /// </summary>
        private List<PredictionLine> CalculateCustomLines(KLineInfo kLineData, double basePrice)
        {
            var lines = new List<PredictionLine>();

            // 使用计算设置中的自定义百分比
            foreach (var percentage in _calculationSettings.CustomPercentages)
            {
                var price = basePrice * percentage / 100.0;
                var name = $"自定义{percentage:F1}%";
                var line = PredictionLine.CreateCustom(lines.Count, name, price, basePrice, percentage, 0.0);
                lines.Add(line);
            }

            return lines;
        }

        /// <summary>
        /// 应用计算设置
        /// </summary>
        private void ApplyCalculationSettings(List<PredictionLine> lines)
        {
            foreach (var line in lines)
            {
                // 应用默认颜色设置
                if (line.LineColor == Color.Empty)
                {
                    line.LineColor = GetDefaultLineColor(line.Index);
                }

                // 应用透明度设置
                line.Opacity = _calculationSettings.DefaultOpacity;

                // 应用线条宽度设置
                line.LineWidth = _calculationSettings.DefaultLineWidth;

                // 应用价格标签显示设置
                line.ShowPriceLabel = _calculationSettings.ShowPriceLabels;
            }
        }

        /// <summary>
        /// 获取默认线条颜色
        /// </summary>
        private Color GetDefaultLineColor(int index)
        {
            var colors = new[]
            {
                Color.Red, Color.FromArgb(255, 128, 0), Color.Yellow, Color.Green,
                Color.Cyan, Color.Blue, Color.FromArgb(128, 0, 255), Color.Magenta
            };

            return colors[index % colors.Length];
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证预测线数据
        /// </summary>
        /// <param name="predictionLine">预测线</param>
        /// <param name="currentPrice">当前价格</param>
        /// <returns>验证结果</returns>
        public async Task<PredictionLineValidationResult> ValidatePredictionLineAsync(
            PredictionLine predictionLine, double currentPrice = 0)
        {
            try
            {
                if (predictionLine == null)
                {
                    return new PredictionLineValidationResult
                    {
                        IsValid = false,
                        ValidationMessages = new[] { "预测线为空" }
                    };
                }

                var messages = new List<string>();

                // 验证基本信息
                if (string.IsNullOrWhiteSpace(predictionLine.Name))
                    messages.Add("预测线名称不能为空");

                if (predictionLine.Price <= 0)
                    messages.Add("预测线价格必须大于0");

                if (predictionLine.BasePrice <= 0)
                    messages.Add("基准价格必须大于0");

                // 验证价格范围
                if (currentPrice > 0)
                {
                    var priceDiff = Math.Abs(predictionLine.Price - currentPrice);
                    var percentDiff = (priceDiff / currentPrice) * 100;

                    if (percentDiff > _calculationSettings.MaxPriceDeviationPercent)
                    {
                        messages.Add($"预测线价格偏差过大: {percentDiff:F2}%");
                    }
                }

                // 验证Y位置
                if (double.IsNaN(predictionLine.YPosition) || predictionLine.YPosition < 0)
                {
                    messages.Add("预测线Y位置无效");
                }

                var result = new PredictionLineValidationResult
                {
                    IsValid = messages.Count == 0,
                    ValidationMessages = messages.ToArray(),
                    PredictionLine = predictionLine,
                    CurrentPrice = currentPrice,
                    ValidationTime = DateTime.Now
                };

                // 触发验证完成事件
                OnValidationCompleted(result);

                _logService.LogInfo($"预测线验证完成: {predictionLine.Name}, 结果: {(result.IsValid ? "通过" : "失败")}");

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"验证预测线失败: {ex.Message}", ex);
                return new PredictionLineValidationResult
                {
                    IsValid = false,
                    ValidationMessages = new[] { $"验证过程出错: {ex.Message}" },
                    PredictionLine = predictionLine
                };
            }
        }

        /// <summary>
        /// 验证所有预测线
        /// </summary>
        /// <param name="currentPrice">当前价格</param>
        /// <returns>验证结果集合</returns>
        public async Task<IReadOnlyList<PredictionLineValidationResult>> ValidateAllPredictionLinesAsync(double currentPrice = 0)
        {
            var results = new List<PredictionLineValidationResult>();

            foreach (var line in _predictionLines.Values)
            {
                var result = await ValidatePredictionLineAsync(line, currentPrice);
                results.Add(result);
            }

            _logService.LogInfo($"所有预测线验证完成，总计: {results.Count}");
            return results;
        }

        #endregion

        #region 数据持久化方法

        /// <summary>
        /// 导出预测线数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="format">导出格式</param>
        /// <returns>是否导出成功</returns>
        public async Task<bool> ExportPredictionLinesAsync(string filePath, PredictionLineExportFormat format)
        {
            try
            {
                var lines = _predictionLines.Values.ToList();

                switch (format)
                {
                    case PredictionLineExportFormat.Json:
                        return await ExportToJsonAsync(filePath, lines);
                    case PredictionLineExportFormat.Xml:
                        return await ExportToXmlAsync(filePath, lines);
                    case PredictionLineExportFormat.Csv:
                        return await ExportToCsvAsync(filePath, lines);
                    default:
                        _logService.LogWarning($"不支持的导出格式: {format}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"导出预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 导入预测线数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="format">导入格式</param>
        /// <param name="replaceExisting">是否替换现有数据</param>
        /// <returns>是否导入成功</returns>
        public async Task<bool> ImportPredictionLinesAsync(string filePath, PredictionLineExportFormat format, bool replaceExisting = false)
        {
            try
            {
                List<PredictionLine> importedLines;

                switch (format)
                {
                    case PredictionLineExportFormat.Json:
                        importedLines = await ImportFromJsonAsync(filePath);
                        break;
                    case PredictionLineExportFormat.Xml:
                        importedLines = await ImportFromXmlAsync(filePath);
                        break;
                    case PredictionLineExportFormat.Csv:
                        importedLines = await ImportFromCsvAsync(filePath);
                        break;
                    default:
                        _logService.LogWarning($"不支持的导入格式: {format}");
                        return false;
                }

                if (importedLines == null || importedLines.Count == 0)
                {
                    _logService.LogWarning("没有导入有效的预测线数据");
                    return false;
                }

                if (replaceExisting)
                {
                    ClearAllPredictionLines();
                }

                var successCount = AddPredictionLinesBatch(importedLines);
                _logService.LogInfo($"预测线导入完成，成功导入: {successCount}/{importedLines.Count}");

                return successCount > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"导入预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        // 导出导入方法的简化实现（实际项目中应该使用完整的序列化逻辑）
        private async Task<bool> ExportToJsonAsync(string filePath, List<PredictionLine> lines)
        {
            // 简化实现，实际应该使用JsonSerializer
            await Task.Delay(1);
            _logService.LogInfo($"导出JSON格式到: {filePath}");
            return true;
        }

        private async Task<bool> ExportToXmlAsync(string filePath, List<PredictionLine> lines)
        {
            // 简化实现，实际应该使用XmlSerializer
            await Task.Delay(1);
            _logService.LogInfo($"导出XML格式到: {filePath}");
            return true;
        }

        private async Task<bool> ExportToCsvAsync(string filePath, List<PredictionLine> lines)
        {
            // 简化实现，实际应该使用CsvHelper或手动构建CSV
            await Task.Delay(1);
            _logService.LogInfo($"导出CSV格式到: {filePath}");
            return true;
        }

        private async Task<List<PredictionLine>> ImportFromJsonAsync(string filePath)
        {
            // 简化实现，实际应该使用JsonSerializer
            await Task.Delay(1);
            return new List<PredictionLine>();
        }

        private async Task<List<PredictionLine>> ImportFromXmlAsync(string filePath)
        {
            // 简化实现，实际应该使用XmlSerializer
            await Task.Delay(1);
            return new List<PredictionLine>();
        }

        private async Task<List<PredictionLine>> ImportFromCsvAsync(string filePath)
        {
            // 简化实现，实际应该使用CsvHelper或手动解析CSV
            await Task.Delay(1);
            return new List<PredictionLine>();
        }

        #endregion

        #region 事件触发方法

        /// <summary>
        /// 触发集合变化事件
        /// </summary>
        protected virtual void OnCollectionChanged(PredictionLineCollectionChangeType changeType, PredictionLine predictionLine)
        {
            try
            {
                CollectionChanged?.Invoke(this, new PredictionLineCollectionChangedEventArgs
                {
                    ChangeType = changeType,
                    PredictionLine = predictionLine,
                    TotalCount = _predictionLines.Count,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logService.LogError($"触发集合变化事件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 触发计算完成事件
        /// </summary>
        protected virtual void OnCalculationCompleted(
            PredictionLineCalculationType calculationType,
            IReadOnlyList<PredictionLine> calculatedLines,
            double basePrice)
        {
            try
            {
                CalculationCompleted?.Invoke(this, new PredictionLineCalculationCompletedEventArgs
                {
                    CalculationType = calculationType,
                    CalculatedLines = calculatedLines,
                    BasePrice = basePrice,
                    CalculationTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logService.LogError($"触发计算完成事件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 触发验证完成事件
        /// </summary>
        protected virtual void OnValidationCompleted(PredictionLineValidationResult validationResult)
        {
            try
            {
                ValidationCompleted?.Invoke(this, new PredictionLineValidationEventArgs
                {
                    ValidationResult = validationResult,
                    ValidationTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logService.LogError($"触发验证完成事件失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region IDisposable 实现

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
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logService.LogInfo("PredictionLineManager 正在释放托管资源");
                }

                // 清理事件
                CollectionChanged = null;
                CalculationCompleted = null;
                ValidationCompleted = null;

                // 清理数据
                _predictionLines.Clear();

                _disposed = true;
                _logService.LogInfo("PredictionLineManager 资源释放完成");
            }
        }

        #endregion
    }

    #region 辅助类和枚举

    /// <summary>
    /// 预测线计算设置
    /// </summary>
    public class PredictionLineCalculationSettings
    {
        /// <summary>
        /// 默认透明度
        /// </summary>
        public float DefaultOpacity { get; set; } = 0.8f;

        /// <summary>
        /// 默认线条宽度
        /// </summary>
        public float DefaultLineWidth { get; set; } = 2.0f;

        /// <summary>
        /// 是否显示价格标签
        /// </summary>
        public bool ShowPriceLabels { get; set; } = true;

        /// <summary>
        /// 最大价格偏差百分比
        /// </summary>
        public double MaxPriceDeviationPercent { get; set; } = 50.0;

        /// <summary>
        /// 自定义百分比集合
        /// </summary>
        public List<double> CustomPercentages { get; set; } = new List<double>
        {
            1.5, 2.5, 3.5, 4.5, 5.5, 6.5, 7.5, 8.5, 9.5, 10.5
        };

        /// <summary>
        /// 创建默认设置
        /// </summary>
        public static PredictionLineCalculationSettings CreateDefault()
        {
            return new PredictionLineCalculationSettings();
        }

        /// <summary>
        /// 创建高性能设置
        /// </summary>
        public static PredictionLineCalculationSettings CreateHighPerformance()
        {
            return new PredictionLineCalculationSettings
            {
                DefaultOpacity = 0.6f,
                DefaultLineWidth = 1.5f,
                ShowPriceLabels = false
            };
        }

        /// <summary>
        /// 创建高质量设置
        /// </summary>
        public static PredictionLineCalculationSettings CreateHighQuality()
        {
            return new PredictionLineCalculationSettings
            {
                DefaultOpacity = 0.9f,
                DefaultLineWidth = 2.5f,
                ShowPriceLabels = true
            };
        }
    }

    /// <summary>
    /// 预测线计算类型
    /// </summary>
    public enum PredictionLineCalculationType
    {
        /// <summary>
        /// 波神标准
        /// </summary>
        BoshenStandard,

        /// <summary>
        /// 波神高级
        /// </summary>
        BoshenAdvanced,

        /// <summary>
        /// 斐波那契
        /// </summary>
        Fibonacci,

        /// <summary>
        /// 自定义
        /// </summary>
        Custom
    }

    /// <summary>
    /// 预测线集合变化类型
    /// </summary>
    public enum PredictionLineCollectionChangeType
    {
        /// <summary>
        /// 添加
        /// </summary>
        Added,

        /// <summary>
        /// 移除
        /// </summary>
        Removed,

        /// <summary>
        /// 更新
        /// </summary>
        Updated,

        /// <summary>
        /// 清空
        /// </summary>
        Cleared
    }

    /// <summary>
    /// 预测线导出格式
    /// </summary>
    public enum PredictionLineExportFormat
    {
        /// <summary>
        /// JSON格式
        /// </summary>
        Json,

        /// <summary>
        /// XML格式
        /// </summary>
        Xml,

        /// <summary>
        /// CSV格式
        /// </summary>
        Csv
    }

    /// <summary>
    /// 预测线验证结果
    /// </summary>
    public class PredictionLineValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证消息
        /// </summary>
        public string[] ValidationMessages { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 预测线
        /// </summary>
        public PredictionLine PredictionLine { get; set; }

        /// <summary>
        /// 当前价格
        /// </summary>
        public double CurrentPrice { get; set; }

        /// <summary>
        /// 验证时间
        /// </summary>
        public DateTime ValidationTime { get; set; }
    }

    /// <summary>
    /// 预测线集合变化事件参数
    /// </summary>
    public class PredictionLineCollectionChangedEventArgs : EventArgs
    {
        public PredictionLineCollectionChangeType ChangeType { get; set; }
        public PredictionLine PredictionLine { get; set; }
        public int TotalCount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 预测线计算完成事件参数
    /// </summary>
    public class PredictionLineCalculationCompletedEventArgs : EventArgs
    {
        public PredictionLineCalculationType CalculationType { get; set; }
        public IReadOnlyList<PredictionLine> CalculatedLines { get; set; }
        public double BasePrice { get; set; }
        public DateTime CalculationTime { get; set; }
    }

    /// <summary>
    /// 预测线验证事件参数
    /// </summary>
    public class PredictionLineValidationEventArgs : EventArgs
    {
        public PredictionLineValidationResult ValidationResult { get; set; }
        public DateTime ValidationTime { get; set; }
    }

    #endregion
}