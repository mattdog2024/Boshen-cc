---
issue: 4
stream: K线选择和交互控件
agent: general-purpose
started: 2025-10-29T01:40:00Z
status: in_progress
depends_on: stream-A
---

# Stream B: K线选择和交互控件

## Scope
创建专用的K线选择控件和价格显示组件，优化用户交互体验，实现更精确的K线点选择和价格计算。

## Files
- `BoshenCC.WinForms/Controls/KLineSelector.cs` (new control)
- `BoshenCC.WinForms/Controls/PriceDisplay.cs` (new control)
- `BoshenCC.WinForms/Controls/SelectionPanel.cs` (new control)
- `BoshenCC.WinForms/Controls/CoordinateHelper.cs` (new utility)

## Dependencies
- Stream A must be completed (主界面算法集成已完成)

## Progress
- Starting K-line selection and interaction controls implementation