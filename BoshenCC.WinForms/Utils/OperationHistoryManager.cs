using System;
using System.Collections.Generic;
using System.Linq;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Utils
{
    /// <summary>
    /// 操作历史管理器
    /// 提供完整的撤销/重做功能，支持多种操作类型的记录和恢复
    /// </summary>
    public class OperationHistoryManager
    {
        #region 私有字段

        private readonly List<IOperation> _operations;
        private int _currentIndex;
        private int _maxHistorySize;
        private bool _isExecutingOperation;

        #endregion

        #region 构造函数

        public OperationHistoryManager(int maxHistorySize = 100)
        {
            _operations = new List<IOperation>();
            _currentIndex = -1;
            _maxHistorySize = maxHistorySize;
            _isExecutingOperation = false;
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        public bool CanUndo => _currentIndex >= 0;

        /// <summary>
        /// 是否可以重做
        /// </summary>
        public bool CanRedo => _currentIndex < _operations.Count - 1;

        /// <summary>
        /// 当前操作数量
        /// </summary>
        public int OperationCount => _operations.Count;

        /// <summary>
        /// 撤销历史中的操作数量
        /// </summary>
        public int UndoCount => _currentIndex + 1;

        /// <summary>
        /// 重做历史中的操作数量
        /// </summary>
        public int RedoCount => Math.Max(0, _operations.Count - _currentIndex - 1);

        /// <summary>
        /// 最大历史记录数量
        /// </summary>
        public int MaxHistorySize
        {
            get => _maxHistorySize;
            set
            {
                if (value > 0 && value != _maxHistorySize)
                {
                    _maxHistorySize = value;
                    TrimHistory();
                }
            }
        }

        #endregion

        #region 事件

        /// <summary>
        /// 操作执行事件
        /// </summary>
        public event EventHandler<OperationExecutedEventArgs> OperationExecuted;

        /// <summary>
        /// 撤销操作事件
        /// </summary>
        public event EventHandler<OperationUndoneEventArgs> OperationUndone;

        /// <summary>
        /// 重做操作事件
        /// </summary>
        public event EventHandler<OperationRedoneEventArgs> OperationRedone;

        /// <summary>
        /// 历史清空事件
        /// </summary>
        public event EventHandler HistoryCleared;

        #endregion

        #region 公共方法

        /// <summary>
        /// 执行操作并记录到历史
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <returns>操作是否执行成功</returns>
        public bool ExecuteOperation(IOperation operation)
        {
            if (operation == null) return false;

            try
            {
                _isExecutingOperation = true;

                // 如果当前位置不是在历史末尾，移除后续操作
                if (_currentIndex < _operations.Count - 1)
                {
                    var operationsToRemove = _operations.Count - _currentIndex - 1;
                    for (int i = 0; i < operationsToRemove; i++)
                    {
                        _operations.RemoveAt(_currentIndex + 1);
                    }
                }

                // 执行操作
                var result = operation.Execute();

                if (result)
                {
                    // 记录操作到历史
                    operation.Timestamp = DateTime.Now;
                    _operations.Add(operation);
                    _currentIndex++;

                    // 限制历史记录大小
                    TrimHistory();

                    // 触发事件
                    OnOperationExecuted(new OperationExecutedEventArgs(operation, OperationAction.Execute));
                }

                return result;
            }
            finally
            {
                _isExecutingOperation = false;
            }
        }

        /// <summary>
        /// 撤销上一个操作
        /// </summary>
        /// <returns>是否撤销成功</returns>
        public bool Undo()
        {
            if (!CanUndo || _isExecutingOperation) return false;

            try
            {
                _isExecutingOperation = true;

                var operation = _operations[_currentIndex];
                var result = operation.Undo();

                if (result)
                {
                    _currentIndex--;
                    OnOperationUndone(new OperationUndoneEventArgs(operation));
                }

                return result;
            }
            finally
            {
                _isExecutingOperation = false;
            }
        }

        /// <summary>
        /// 重做下一个操作
        /// </summary>
        /// <returns>是否重做成功</returns>
        public bool Redo()
        {
            if (!CanRedo || _isExecutingOperation) return false;

            try
            {
                _isExecutingOperation = true;

                var operation = _operations[_currentIndex + 1];
                var result = operation.Redo();

                if (result)
                {
                    _currentIndex++;
                    OnOperationRedone(new OperationRedoneEventArgs(operation));
                }

                return result;
            }
            finally
            {
                _isExecutingOperation = false;
            }
        }

        /// <summary>
        /// 撤销多个操作
        /// </summary>
        /// <param name="count">撤销数量</param>
        /// <returns>实际撤销数量</returns>
        public int UndoMultiple(int count)
        {
            int undoneCount = 0;
            for (int i = 0; i < count && CanUndo; i++)
            {
                if (Undo())
                    undoneCount++;
                else
                    break;
            }
            return undoneCount;
        }

        /// <summary>
        /// 重做多个操作
        /// </summary>
        /// <param name="count">重做数量</param>
        /// <returns>实际重做数量</returns>
        public int RedoMultiple(int count)
        {
            int redoneCount = 0;
            for (int i = 0; i < count && CanRedo; i++)
            {
                if (Redo())
                    redoneCount++;
                else
                    break;
            }
            return redoneCount;
        }

        /// <summary>
        /// 撤销到指定操作
        /// </summary>
        /// <param name="targetIndex">目标索引</param>
        /// <returns>是否成功</returns>
        public bool UndoTo(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _operations.Count) return false;

            while (_currentIndex > targetIndex && CanUndo)
            {
                if (!Undo())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 重做到指定操作
        /// </summary>
        /// <param name="targetIndex">目标索引</param>
        /// <returns>是否成功</returns>
        public bool RedoTo(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _operations.Count) return false;

            while (_currentIndex < targetIndex && CanRedo)
            {
                if (!Redo())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 清除所有历史记录
        /// </summary>
        public void ClearHistory()
        {
            _operations.Clear();
            _currentIndex = -1;
            OnHistoryCleared(EventArgs.Empty);
        }

        /// <summary>
        /// 获取操作描述
        /// </summary>
        /// <param name="index">操作索引</param>
        /// <returns>操作描述</returns>
        public string GetOperationDescription(int index)
        {
            if (index < 0 || index >= _operations.Count) return string.Empty;
            return _operations[index].Description;
        }

        /// <summary>
        /// 获取操作类型
        /// </summary>
        /// <param name="index">操作索引</param>
        /// <returns>操作类型</returns>
        public Type GetOperationType(int index)
        {
            if (index < 0 || index >= _operations.Count) return null;
            return _operations[index].GetType();
        }

        /// <summary>
        /// 获取操作时间戳
        /// </summary>
        /// <param name="index">操作索引</param>
        /// <returns>操作时间戳</returns>
        public DateTime GetOperationTimestamp(int index)
        {
            if (index < 0 || index >= _operations.Count) return DateTime.MinValue;
            return _operations[index].Timestamp;
        }

        /// <summary>
        /// 获取指定类型的操作数量
        /// </summary>
        /// <param name="operationType">操作类型</param>
        /// <returns>操作数量</returns>
        public int GetOperationCount<T>() where T : IOperation
        {
            return _operations.Count(op => op is T);
        }

        /// <summary>
        /// 获取操作历史摘要
        /// </summary>
        /// <returns>操作历史摘要</returns>
        public OperationHistorySummary GetHistorySummary()
        {
            var summary = new OperationHistorySummary
            {
                TotalOperations = _operations.Count,
                CurrentIndex = _currentIndex,
                CanUndo = CanUndo,
                CanRedo = CanRedo,
                OperationTypes = new Dictionary<string, int>()
            };

            // 统计各类型操作数量
            foreach (var operation in _operations)
            {
                var typeName = operation.GetType().Name;
                if (summary.OperationTypes.ContainsKey(typeName))
                    summary.OperationTypes[typeName]++;
                else
                    summary.OperationTypes[typeName] = 1;
            }

            return summary;
        }

        /// <summary>
        /// 创建操作组（用于批量操作）
        /// </summary>
        /// <param name="groupName">组名</param>
        /// <param name="operations">操作列表</param>
        /// <returns>是否成功</returns>
        public bool ExecuteOperationGroup(string groupName, params IOperation[] operations)
        {
            if (operations == null || operations.Length == 0) return false;

            var groupOperation = new GroupOperation(groupName, operations);
            return ExecuteOperation(groupOperation);
        }

        #endregion

        #region 私有方法

        private void TrimHistory()
        {
            if (_operations.Count > _maxHistorySize)
            {
                var removeCount = _operations.Count - _maxHistorySize;
                _operations.RemoveRange(0, removeCount);
                _currentIndex = Math.Max(-1, _currentIndex - removeCount);
            }
        }

        #endregion

        #region 事件触发器

        protected virtual void OnOperationExecuted(OperationExecutedEventArgs e)
        {
            OperationExecuted?.Invoke(this, e);
        }

        protected virtual void OnOperationUndone(OperationUndoneEventArgs e)
        {
            OperationUndone?.Invoke(this, e);
        }

        protected virtual void OnOperationRedone(OperationRedoneEventArgs e)
        {
            OperationRedone?.Invoke(this, e);
        }

        protected virtual void OnHistoryCleared(EventArgs e)
        {
            HistoryCleared?.Invoke(this, e);
        }

        #endregion
    }

    #region 接口定义

    /// <summary>
    /// 操作接口
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// 操作描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 操作时间戳
        /// </summary>
        DateTime Timestamp { get; set; }

        /// <summary>
        /// 执行操作
        /// </summary>
        /// <returns>是否执行成功</returns>
        bool Execute();

        /// <summary>
        /// 撤销操作
        /// </summary>
        /// <returns>是否撤销成功</returns>
        bool Undo();

        /// <summary>
        /// 重做操作
        /// </summary>
        /// <returns>是否重做成功</returns>
        bool Redo();
    }

    #endregion

    #region 具体操作实现

    /// <summary>
    /// K线选择操作
    /// </summary>
    public class KLineSelectionOperation : IOperation
    {
        private readonly Action<Point?> _setPointA;
        private readonly Action<Point?> _setPointB;
        private readonly Func<Point?> _getPointA;
        private readonly Func<Point?> _getPointB;
        private Point? _oldPointA;
        private Point? _oldPointB;
        private Point? _newPointA;
        private Point? _newPointB;

        public string Description { get; }
        public DateTime Timestamp { get; set; }

        public KLineSelectionOperation(
            Action<Point?> setPointA,
            Action<Point?> setPointB,
            Func<Point?> getPointA,
            Func<Point?> getPointB,
            Point? newPointA,
            Point? newPointB,
            string description = "K线选择")
        {
            _setPointA = setPointA;
            _setPointB = setPointB;
            _getPointA = getPointA;
            _getPointB = getPointB;
            _newPointA = newPointA;
            _newPointB = newPointB;
            Description = description;
            Timestamp = DateTime.Now;
        }

        public bool Execute()
        {
            _oldPointA = _getPointA();
            _oldPointB = _getPointB();

            _setPointA(_newPointA);
            _setPointB(_newPointB);

            return true;
        }

        public bool Undo()
        {
            _setPointA(_oldPointA);
            _setPointB(_oldPointB);
            return true;
        }

        public bool Redo()
        {
            return Execute();
        }
    }

    /// <summary>
    /// 影线测量操作
    /// </summary>
    public class ShadowMeasurementOperation : IOperation
    {
        private readonly Action<ShadowMeasurement> _addMeasurement;
        private readonly Action<ShadowMeasurement> _removeMeasurement;
        private ShadowMeasurement _measurement;

        public string Description { get; }
        public DateTime Timestamp { get; set; }

        public ShadowMeasurementOperation(
            Action<ShadowMeasurement> addMeasurement,
            Action<ShadowMeasurement> removeMeasurement,
            ShadowMeasurement measurement,
            string description = "影线测量")
        {
            _addMeasurement = addMeasurement;
            _removeMeasurement = removeMeasurement;
            _measurement = measurement;
            Description = description;
            Timestamp = DateTime.Now;
        }

        public bool Execute()
        {
            _addMeasurement(_measurement);
            return true;
        }

        public bool Undo()
        {
            _removeMeasurement(_measurement);
            return true;
        }

        public bool Redo()
        {
            return Execute();
        }
    }

    /// <summary>
    /// 设置变更操作
    /// </summary>
    public class SettingsChangeOperation<T> : IOperation
    {
        private readonly Action<T> _setter;
        private readonly Func<T> _getter;
        private T _oldValue;
        private readonly T _newValue;

        public string Description { get; }
        public DateTime Timestamp { get; set; }

        public SettingsChangeOperation(
            Action<T> setter,
            Func<T> getter,
            T newValue,
            string description = "设置变更")
        {
            _setter = setter;
            _getter = getter;
            _newValue = newValue;
            Description = description;
            Timestamp = DateTime.Now;
        }

        public bool Execute()
        {
            _oldValue = _getter();
            _setter(_newValue);
            return true;
        }

        public bool Undo()
        {
            _setter(_oldValue);
            return true;
        }

        public bool Redo()
        {
            return Execute();
        }
    }

    /// <summary>
    /// 组合操作
    /// </summary>
    public class GroupOperation : IOperation
    {
        private readonly List<IOperation> _operations;

        public string Description { get; }
        public DateTime Timestamp { get; set; }

        public GroupOperation(string description, params IOperation[] operations)
        {
            Description = description;
            _operations = operations?.ToList() ?? new List<IOperation>();
            Timestamp = DateTime.Now;
        }

        public bool Execute()
        {
            foreach (var operation in _operations)
            {
                if (!operation.Execute())
                    return false;
            }
            return true;
        }

        public bool Undo()
        {
            // 反向撤销
            for (int i = _operations.Count - 1; i >= 0; i--)
            {
                if (!_operations[i].Undo())
                    return false;
            }
            return true;
        }

        public bool Redo()
        {
            return Execute();
        }
    }

    #endregion

    #region 数据结构

    /// <summary>
    /// 操作动作类型
    /// </summary>
    public enum OperationAction
    {
        Execute,
        Undo,
        Redo
    }

    /// <summary>
    /// 操作历史摘要
    /// </summary>
    public class OperationHistorySummary
    {
        public int TotalOperations { get; set; }
        public int CurrentIndex { get; set; }
        public bool CanUndo { get; set; }
        public bool CanRedo { get; set; }
        public Dictionary<string, int> OperationTypes { get; set; }
    }

    #endregion

    #region 事件参数类

    /// <summary>
    /// 操作执行事件参数
    /// </summary>
    public class OperationExecutedEventArgs : EventArgs
    {
        public IOperation Operation { get; }
        public OperationAction Action { get; }

        public OperationExecutedEventArgs(IOperation operation, OperationAction action)
        {
            Operation = operation;
            Action = action;
        }
    }

    /// <summary>
    /// 操作撤销事件参数
    /// </summary>
    public class OperationUndoneEventArgs : EventArgs
    {
        public IOperation Operation { get; }

        public OperationUndoneEventArgs(IOperation operation)
        {
            Operation = operation;
        }
    }

    /// <summary>
    /// 操作重做事件参数
    /// </summary>
    public class OperationRedoneEventArgs : EventArgs
    {
        public IOperation Operation { get; }

        public OperationRedoneEventArgs(IOperation operation)
        {
            Operation = operation;
        }
    }

    #endregion
}