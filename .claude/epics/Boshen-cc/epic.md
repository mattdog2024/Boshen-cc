---
name: Boshen-cc
status: backlog
created: 2025-10-28T07:41:36Z
progress: 0%
prd: .claude/prds/Boshen-cc.md
github: https://github.com/mattdog2024/Boshen-cc/issues/1
---

# Epic: Boshen-cc

## Overview

Boshen-cc 是一个基于屏幕识别和图像处理技术的Windows桌面应用程序，通过监控文华行情软件窗口，实现K线自动识别和波神11线预测线的实时绘制。该系统采用C# + OpenCV技术栈，使用Windows API进行屏幕操作和窗口管理，实现非侵入式的技术分析工具。

## Architecture Decisions

### 核心技术选择
- **开发语言**: C# (.NET Framework 4.6.2)
  - 理由: Windows桌面应用开发效率高，成熟的WinForms/WPF框架，丰富的第三方库支持
  - 替代方案: C++ (性能更好但开发复杂度更高)

- **图像处理**: OpenCV 4.x + EmguCV
  - 理由: 成熟的计算机视觉库，强大的图像识别能力，C#封装良好
  - 用途: K线边界检测、颜色识别、形状分析

- **屏幕操作**: Windows API (User32.dll, GDI32.dll)
  - 理由: 原生Windows支持，性能最优，可实现精确的窗口操作
  - 用途: 窗口查找、屏幕截图、透明窗口绘制

### 架构模式
- **MVC模式**: 分离界面、业务逻辑和数据模型
- **事件驱动**: 基于Windows消息机制的用户交互
- **模块化设计**: 独立的K线识别、算法计算、图形绘制模块

### 设计原则
- **非侵入式**: 不修改文华行情软件，通过屏幕识别实现功能
- **高性能**: 优化图像处理算法，确保实时响应
- **高可靠性**: 异常处理机制，确保软件稳定运行
- **易维护**: 清晰的模块划分，便于后续扩展

## Technical Approach

### Frontend Components (用户界面层)

#### 主窗体 (MainWindow)
- **功能**: 简洁的操作界面，提供核心功能入口
- **技术**: WinForms/WPF
- **组件**:
  - 单体测量按钮 (btnSingleMeasure)
  - 影线测量按钮 (btnShadowMeasure)
  - 清除线条按钮 (btnClearLines)
  - 状态显示区域 (statusPanel)

#### 悬浮工具窗 (FloatingToolbar)
- **功能**: 半透明工具窗，快速访问测量功能
- **技术**: LayeredWindow (SetLayeredWindowAttributes API)
- **特性**:
  - 可拖拽定位
  - 总在最前显示
  - ESC键快速隐藏

#### 设置窗体 (SettingsWindow)
- **功能**: 线条样式、颜色、透明度等个性化设置
- **技术**: WinForms标准配置界面
- **存储**: 本地XML配置文件

### Backend Services (业务逻辑层)

#### K线识别服务 (KLineRecognitionService)
```csharp
public class KLineRecognitionService
{
    // 核心功能：从屏幕截图中识别K线
    public KLineInfo RecognizeKLine(Rectangle screenRegion)

    // 辅助功能：颜色检测、边界识别、实体/影线分离
    private bool DetectKLineColor(Bitmap image)
    private Rectangle DetectKLineBounds(Bitmap image)
    private KLineStructure AnalyzeKLineStructure(Bitmap image, Rectangle bounds)
}
```

#### 波神算法服务 (BoshenAlgorithmService)
```csharp
public class BoshenAlgorithmService
{
    // 核心功能：计算11条预测线
    public List<PredictionLine> CalculatePredictionLines(KLineInfo kLine)

    // 算法实现：基于精确比例计算
    private readonly double[] BOSHEN_RATIOS = {0.0, 1.0, 1.849, 2.397, 3.137, 3.401, 4.000, 4.726, 5.247, 6.027, 6.808};
}
```

#### 图形绘制服务 (DrawingService)
```csharp
public class DrawingService
{
    // 核心功能：在目标窗口上绘制预测线
    public void DrawPredictionLines(IntPtr targetWindow, List<PredictionLine> lines)

    // 绘制技术：透明窗口叠加、GDI+绘图
    private void CreateTransparentOverlay(IntPtr targetWindow)
    private void DrawLineOnOverlay(Graphics g, PredictionLine line)
}
```

#### 窗口管理服务 (WindowManagementService)
```csharp
public class WindowManagementService
{
    // 核心功能：查找和管理文华行情软件窗口
    public IntPtr FindWenhuaWindow()
    public Rectangle GetWindowClientRect(IntPtr window)
    public Bitmap CaptureWindowRegion(IntPtr window, Rectangle region)
}
```

### Infrastructure (基础设施层)

#### 配置管理 (ConfigurationManager)
- **存储格式**: XML文件
- **配置项**: 线条颜色、粗细、透明度、热键设置
- **位置**: %APPDATA%/Boshen-cc/config.xml

#### 日志系统 (LoggingService)
- **框架**: NLog 或 Serilog
- **级别**: Debug, Info, Warning, Error
- **存储**: 本地文件 + 滚动日志

#### 异常处理 (ExceptionHandler)
- **全局异常捕获**: AppDomain.CurrentDomain.UnhandledException
- **错误报告**: 自动记录错误详情
- **恢复机制**: 自动重启、状态恢复

## Implementation Strategy

### 开发阶段规划

#### Phase 1: 核心功能开发 (Week 1-2)
1. **基础框架搭建**
   - WinForms主界面创建
   - 依赖库集成 (OpenCV, Windows API)
   - 基础服务框架搭建

2. **K线识别算法开发**
   - 屏幕截图功能
   - K线边界检测算法
   - 颜色和形状识别

3. **波神算法实现**
   - 11线比例计算
   - 精度验证和测试
   - 价格标签生成

#### Phase 2: 图形绘制集成 (Week 3)
1. **透明窗口绘制**
   - Windows API透明窗口创建
   - GDI+线条绘制
   - 窗口吸附和跟随

2. **用户交互完善**
   - 鼠标事件处理
   - 状态指示和反馈
   - 错误处理和提示

#### Phase 3: 优化和测试 (Week 4)
1. **性能优化**
   - 图像处理算法优化
   - 内存使用优化
   - 响应速度提升

2. **全面测试**
   - 功能测试
   - 兼容性测试
   - 性能压力测试

### 风险缓解策略

#### 技术风险
1. **K线识别准确率**
   - 风险: 复杂背景下识别失败
   - 缓解: 多种算法结合，人工校正机制

2. **文华软件兼容性**
   - 风险: 软件更新导致接口变化
   - 缓解: 版本检测机制，多版本适配

3. **性能瓶颈**
   - 风险: 实时图像处理消耗资源
   - 缓解: 异步处理，算法优化

#### 业务风险
1. **用户体验**
   - 风险: 操作复杂，学习成本高
   - 缓解: 简化界面，引导式设计

2. **算法准确性**
   - 风险: 计算结果与理论不符
   - 缓解: 充分测试，用户反馈机制

### 测试方法

#### 单元测试
- K线识别算法测试
- 波神算法计算验证
- 工具函数正确性验证

#### 集成测试
- 完整流程测试（识别→计算→绘制）
- 多分辨率兼容性测试
- 长时间稳定性测试

#### 用户验收测试
- 真实交易环境测试
- 用户操作流程验证
- 性能指标达成验证

## Task Breakdown Preview

- [ ] **核心架构搭建**: 项目初始化、依赖库集成、基础框架
- [ ] **K线识别引擎**: 屏幕捕获、图像处理、K线检测算法
- [ ] **波神算法实现**: 11线计算逻辑、精度验证、价格标签
- [ ] **图形绘制系统**: 透明窗口、GDI+绘制、窗口跟随
- [ ] **用户界面开发**: 主界面、工具栏、设置面板
- [ ] **系统集成测试**: 端到端功能测试、性能优化
- [ ] **部署打包**: 安装程序制作、依赖库打包

## Dependencies

### 外部服务依赖
- **文华行情软件**: 核心依赖，需要用户预先安装
- **Windows操作系统**: Windows 7 SP1及以上版本
- **.NET Framework 4.6.2**: 运行时环境

### 第三方库依赖
- **EmguCV/OpenCV**: 图像处理核心库
- **Windows API**: 系统级窗口操作
- **可能需要的库**:
  - Newtonsoft.Json (配置文件处理)
  - NLog/Serilog (日志记录)
  - AutoHotkey.Interop (热键支持)

### 开发工具依赖
- **Visual Studio 2019+**: C#开发环境
- **NuGet Package Manager**: 依赖包管理
- **Git**: 版本控制
- **测试工具**: 单元测试框架、性能分析工具

## Success Criteria (Technical)

### 性能指标
- **识别准确率**: K线识别准确率 ≥ 99%
- **响应时间**: 完整测量流程 ≤ 2秒
- **资源占用**: 内存使用 ≤ 100MB，CPU占用 ≤ 15%
- **稳定性**: 连续运行24小时无崩溃

### 质量指标
- **代码覆盖率**: 核心模块测试覆盖率 ≥ 80%
- **缺陷密度**: ≤ 0.5个缺陷/千行代码
- **兼容性**: 支持Windows 7/8/10/11，主流分辨率
- **用户体验**: 新用户上手时间 ≤ 10分钟

### 功能验收标准
- **单体测量功能**: 点击K线后2秒内完成11条线绘制
- **影线测量功能**: 准确区分实体和影线，计算结果正确
- **预测线管理**: 清除操作响应时间 ≤ 0.5秒，跟随缩放误差 ≤ 1像素
- **界面交互**: 所有按钮功能正常，状态显示准确

## Tasks Created
- [ ] 001.md - 项目初始化和基础架构搭建 (parallel: false)
- [ ] 002.md - K线识别算法核心开发 (parallel: false)
- [ ] 003.md - 波神11线算法实现 (parallel: true)
- [ ] 004.md - Windows透明窗口绘制系统 (parallel: true)
- [ ] 005.md - 主界面和用户交互系统 (parallel: true)
- [ ] 006.md - 窗口管理和系统集成 (parallel: true)
- [ ] 007.md - 集成测试和系统联调 (parallel: false)

**Total tasks: 7**
**Parallel tasks: 4** (003, 004, 005, 006)
**Sequential tasks: 3** (001 → 002/003/004/005/006 → 007)
**Estimated total effort: 188 hours**

## Estimated Effort

### 时间估算
- **总开发周期**: 4周 (MVP版本)
- **Phase 1 (架构+算法)**: 2周
- **Phase 2 (绘制+界面)**: 1周
- **Phase 3 (测试+优化)**: 1周

### 资源需求
- **开发人员**: 1-2名C#桌面应用开发工程师
- **测试人员**: 1名（可兼职）
- **硬件环境**: Windows开发机器，多显示器配置用于测试

### 关键路径
1. **K线识别算法开发** (技术难度最高，风险最大)
2. **透明窗口绘制实现** (核心技术挑战)
3. **文华软件兼容性适配** (外部依赖，需要持续测试)

### 里程碑节点
- **Week 1 End**: 完成基础框架和K线识别算法原型
- **Week 2 End**: 完成波神算法和基础绘制功能
- **Week 3 End**: 完成用户界面和核心功能集成
- **Week 4 End**: 完成测试、优化和发布准备

---

*此Epic将作为Boshen-cc项目的技术实施指南，所有开发活动应严格按照此计划执行*