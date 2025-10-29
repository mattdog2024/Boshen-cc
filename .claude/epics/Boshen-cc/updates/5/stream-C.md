---
issue: 5
stream: 窗口跟随和事件处理
agent: general-purpose
started: 2025-10-29T02:45:00Z
completed: 2025-10-29T03:30:00Z
status: completed
depends_on: stream-A
---

# Stream C: 窗口跟随和事件处理

## Scope
实现目标窗口的检测、跟随和事件处理，确保透明窗口能够准确跟踪文华行情软件窗口的位置变化。

## Files
- `BoshenCC.Core/Services/WindowTracker.cs` (new service)
- `BoshenCC.Core/Utils/WindowHookManager.cs` (new utility)
- `BoshenCC.Core/Utils/WindowEventProcessor.cs` (new processor)
- `BoshenCC.Core/Utils/ScreenCoordinateHelper.cs` (new helper)

## Dependencies
- Stream A must be completed (透明窗口创建和管理已完成)

## Progress
### ✅ 已完成

1. **WindowTracker服务** (`BoshenCC.Core/Services/WindowTracker.cs`)
   - 实现窗口检测和跟踪核心服务
   - 支持多窗口并发跟踪
   - 提供线程安全的操作接口
   - 实现窗口位置变化通知机制
   - 包含完整的Windows API集成
   - 支持窗口查找、枚举和状态监控

2. **WindowHookManager工具类** (`BoshenCC.Core/Utils/WindowHookManager.cs`)
   - 实现Windows事件钩子管理
   - 支持窗口位置变化、显示/隐藏、销毁事件
   - 提供自动钩子清理机制
   - 包含完整的错误处理和日志记录

3. **WindowEventProcessor工具类** (`BoshenCC.Core/Utils/WindowEventProcessor.cs`)
   - 实现高级事件处理和聚合
   - 支持事件过滤和防抖动
   - 提供位置变化和可见性变化分析
   - 包含智能的事件分发机制

4. **ScreenCoordinateHelper工具类** (`BoshenCC.Core/Utils/ScreenCoordinateHelper.cs`)
   - 实现多显示器环境下的坐标转换
   - 支持屏幕坐标和客户区坐标互转
   - 提供显示器信息查询和管理
   - 包含坐标约束和边界检查功能

5. **依赖注入集成**
   - 将WindowTracker服务注册到DI容器
   - 在Program.cs中完成服务配置
   - 确保服务的单例生命周期管理