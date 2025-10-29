---
issue: 5
stream: 窗口跟随和事件处理
agent: general-purpose
started: 2025-10-29T02:30:00Z
status: in_progress
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
- Starting window tracking and event handling implementation