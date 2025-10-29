using System;
using System.Threading;
using BoshenCC.Core.Services;
using Microsoft.Extensions.Logging;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// WindowTracker测试工具类
    /// 用于测试和验证窗口跟踪功能
    /// </summary>
    public static class WindowTrackerTest
    {
        /// <summary>
        /// 运行基本的窗口跟踪测试
        /// </summary>
        public static void RunBasicTest()
        {
            Console.WriteLine("=== WindowTracker 基本功能测试 ===");

            try
            {
                // 创建WindowTracker实例（使用空logger）
                using var tracker = new WindowTracker(null);

                // 订阅事件
                tracker.WindowPositionChanged += (sender, e) =>
                {
                    Console.WriteLine($"窗口位置变化: {e.WindowHandle} -> 新位置: ({e.NewPosition.X},{e.NewPosition.Y})");
                };

                tracker.WindowSizeChanged += (sender, e) =>
                {
                    Console.WriteLine($"窗口大小变化: {e.WindowHandle} -> 新尺寸: {e.NewSize.Width}x{e.NewSize.Height}");
                };

                tracker.WindowVisibilityChanged += (sender, e) =>
                {
                    Console.WriteLine($"窗口可见性变化: {e.WindowHandle} -> {(e.IsVisible ? "显示" : "隐藏")}");
                };

                // 启动跟踪
                tracker.StartTracking();
                Console.WriteLine("窗口跟踪已启动...");

                // 尝试查找并跟踪记事本窗口
                IntPtr notepadHandle = Win32Api.FindWindow("Notepad", null);
                if (notepadHandle != IntPtr.Zero)
                {
                    if (tracker.TrackWindow(notepadHandle, "记事本"))
                    {
                        Console.WriteLine($"成功跟踪记事本窗口: {notepadHandle}");
                    }
                }
                else
                {
                    Console.WriteLine("未找到记事本窗口，尝试查找其他窗口...");

                    // 查找包含特定标题模式的窗口
                    var windows = tracker.FindWindowsByTitlePattern("资源管理器");
                    if (windows.Count > 0)
                    {
                        tracker.TrackWindow(windows[0], "资源管理器");
                        Console.WriteLine($"成功跟踪资源管理器窗口: {windows[0]}");
                    }
                    else
                    {
                        Console.WriteLine("未找到合适的测试窗口");
                    }
                }

                // 显示当前跟踪的窗口
                var trackedWindows = tracker.GetTrackedWindows();
                Console.WriteLine($"\n当前跟踪的窗口数量: {trackedWindows.Count}");
                foreach (var window in trackedWindows)
                {
                    Console.WriteLine($"- {window.Name}: 位置({window.Position.X},{window.Position.Y}) 尺寸({window.Position.Width}x{window.Position.Height}) 可见:{window.IsVisible}");
                }

                Console.WriteLine("\n请移动、缩放或最小化被跟踪的窗口以测试事件响应...");
                Console.WriteLine("按任意键停止测试");

                // 等待用户输入或超时
                Thread.Sleep(10000); // 等待10秒

                // 停止跟踪
                tracker.StopTracking();
                Console.WriteLine("窗口跟踪已停止");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中发生错误: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }

            Console.WriteLine("=== 测试完成 ===");
        }

        /// <summary>
        /// 测试屏幕坐标转换功能
        /// </summary>
        public static void TestCoordinateConversion()
        {
            Console.WriteLine("=== 屏幕坐标转换功能测试 ===");

            try
            {
                // 获取所有屏幕信息
                var screens = ScreenCoordinateHelper.GetAllScreens();
                Console.WriteLine($"检测到 {screens.Count} 个显示器:");

                foreach (var screen in screens)
                {
                    Console.WriteLine($"- {screen.DeviceName}: {screen.Bounds.Width}x{screen.Bounds.Height} {(screen.IsPrimary ? "[主显示器]" : "")}");
                }

                // 测试虚拟屏幕
                var virtualScreen = ScreenCoordinateHelper.GetVirtualScreen();
                Console.WriteLine($"\n虚拟屏幕区域: {virtualScreen.Width}x{virtualScreen.Height} 位置({virtualScreen.X},{virtualScreen.Y})");

                // 测试坐标约束
                var testPoint = new System.Drawing.Point(5000, 3000); // 超出屏幕范围的点
                var constrainedPoint = ScreenCoordinateHelper.ConstrainPointToScreen(testPoint);
                Console.WriteLine($"坐标约束测试: {testPoint} -> {constrainedPoint}");

                // 测试距离计算
                var point1 = new System.Drawing.Point(0, 0);
                var point2 = new System.Drawing.Point(100, 100);
                var distance = ScreenCoordinateHelper.GetDistance(point1, point2);
                Console.WriteLine($"距离计算测试: {point1} 到 {point2} = {distance:F2}");

                Console.WriteLine("\n=== 坐标转换测试完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"坐标转换测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行完整的测试套件
        /// </summary>
        public static void RunFullTestSuite()
        {
            Console.WriteLine("开始运行WindowTracker完整测试套件...\n");

            TestCoordinateConversion();
            Console.WriteLine();

            RunBasicTest();

            Console.WriteLine("\n所有测试已完成！");
        }
    }
}