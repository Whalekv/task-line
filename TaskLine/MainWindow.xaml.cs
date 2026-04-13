using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TaskLine.Helpers;
using TaskLine.Models;
using TaskLine.ViewModels;
using TaskLine.Views;

namespace TaskLine;

/// <summary>
/// 应用程序主窗口，负责初始化 UI 组件并管理标题栏交互与对话框调用。
/// 业务逻辑由 <see cref="MainViewModel"/> 通过 DataContext 绑定提供。
/// </summary>
public partial class MainWindow : Window
{
    #region 常量

    /// <summary>标题栏背景色：白色（COLORREF 格式 0x00BBGGRR）。</summary>
    private const int TitleBarCaptionColor = 0x00FFFFFF;

    /// <summary>标题栏文字及图标色：深灰色 #424242（COLORREF 格式）。</summary>
    private const int TitleBarTextColor = 0x00424242;

    /// <summary>最大化图标（Segoe MDL2 Assets：ChromeMaximize）。</summary>
    private const string MaximizeGlyph = "\uE922";

    /// <summary>还原图标（Segoe MDL2 Assets：ChromeRestore）。</summary>
    private const string RestoreGlyph = "\uE923";

    #endregion

    #region 进度条拖拽状态

    /// <summary>当前是否正在拖拽底部进度条。</summary>
    private bool _isDraggingStrip;

    /// <summary>当前被拖拽进度条所属的任务项。</summary>
    private TaskItem? _draggingStripTask;

    #endregion

    #region 任务卡片拖拽状态

    /// <summary>用于区分拖拽源的自定义数据格式键。</summary>
    private const string DragDataFormat = "TaskLineTask";

    /// <summary>触发拖拽所需的最小鼠标位移阈值（像素）。</summary>
    private const double DragThreshold = 5.0;

    /// <summary>拖拽开始时记录的鼠标初始位置。</summary>
    private Point _dragStartPoint;

    /// <summary>是否处于等待判断是否触发拖拽的状态（鼠标已按下但尚未超过阈值）。</summary>
    private bool _isDraggingCard;

    /// <summary>当前被拖拽的任务项。</summary>
    private TaskItem? _draggedTask;

    /// <summary>拖拽源标识：<c>"List"</c> 表示从列表拖出，<c>"Console"</c> 表示从控制台拖出。</summary>
    private string _dragSource = string.Empty;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化主窗口，加载 XAML 组件并注册窗口事件。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        StateChanged += OnStateChanged;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 在 Win32 窗口句柄就绪后，通过 DWM API 应用自定义标题栏颜色。
    /// </summary>
    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        DwmHelper.SetTitleBarColors(hwnd, TitleBarCaptionColor, TitleBarTextColor);
    }

    /// <summary>
    /// 窗口状态变化时，切换最大化/还原按钮图标。
    /// </summary>
    private void OnStateChanged(object? sender, EventArgs e)
    {
        MaximizeRestoreButton.Content = WindowState == WindowState.Maximized
            ? RestoreGlyph
            : MaximizeGlyph;
    }

    /// <summary>最小化窗口。</summary>
    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    /// <summary>在最大化和还原之间切换窗口状态。</summary>
    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    /// <summary>关闭窗口。</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// 打开新建任务对话框；用户确认后调用 ViewModel 添加任务。
    /// </summary>
    private void OnAddTaskClick(object sender, RoutedEventArgs e)
    {
        var vm = (MainViewModel)DataContext;
        var dialog = new AddTaskDialog(vm.SelectedDate) { Owner = this };

        if (dialog.ShowDialog() == true)
            vm.AddTask(dialog.TaskTitle, dialog.TaskDate);
    }

    /// <summary>
    /// 进度条按下：捕获鼠标并立即更新到点击位置对应的进度值。
    /// </summary>
    private void OnStripMouseDown(object sender, MouseButtonEventArgs e)
    {
        var strip = (FrameworkElement)sender;
        _draggingStripTask = strip.DataContext as TaskItem;
        _isDraggingStrip = true;
        strip.CaptureMouse();
        UpdateStripProgress(strip, e.GetPosition(strip));
        e.Handled = true;
    }

    /// <summary>
    /// 进度条拖拽中：实时根据鼠标 X 坐标更新进度。
    /// </summary>
    private void OnStripMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingStrip || e.LeftButton != MouseButtonState.Pressed)
            return;

        UpdateStripProgress((FrameworkElement)sender, e.GetPosition((FrameworkElement)sender));
    }

    /// <summary>
    /// 进度条松开：结束拖拽并持久化进度。
    /// </summary>
    private void OnStripMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingStrip)
            return;

        _isDraggingStrip = false;
        _draggingStripTask = null;
        ((FrameworkElement)sender).ReleaseMouseCapture();
        ((MainViewModel)DataContext).SaveTasks();
    }

    /// <summary>
    /// 根据鼠标在进度条中的 X 坐标计算并更新任务进度值。
    /// </summary>
    /// <param name="strip">进度条元素，用于获取实际宽度。</param>
    /// <param name="position">鼠标相对于进度条的位置。</param>
    private void UpdateStripProgress(FrameworkElement strip, Point position)
    {
        if (_draggingStripTask == null || strip.ActualWidth <= 0)
            return;

        _draggingStripTask.Progress = (int)Math.Clamp(position.X / strip.ActualWidth * 100, 0, 100);
    }

    // ── 任务卡片拖拽（列表 → 控制台） ──────────────────────────────────────

    /// <summary>
    /// 列表卡片 PreviewMouseLeftButtonDown：记录起始位置。
    /// 若点击在 ProgressBar、Button 或 CheckBox 上则跳过，保留其原有交互。
    /// </summary>
    private void OnCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (IsSourceInteractive(e.OriginalSource as DependencyObject)) return;

        _dragStartPoint = e.GetPosition(null);
        _draggedTask = ((FrameworkElement)sender).DataContext as TaskItem;
        _dragSource = "List";
        _isDraggingCard = true;
    }

    /// <summary>
    /// 列表卡片 PreviewMouseMove：超过阈值后启动系统级 DragDrop。
    /// </summary>
    private void OnCardMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingCard || e.LeftButton != MouseButtonState.Pressed || _draggedTask == null)
            return;

        if (!ExceedsDragThreshold(e.GetPosition(null)))
            return;

        _isDraggingCard = false;
        DragDrop.DoDragDrop((DependencyObject)sender,
            new DataObject(DragDataFormat, _draggedTask), DragDropEffects.Move);
    }

    // ── 控制台卡片拖拽（控制台 → 列表） ────────────────────────────────────

    /// <summary>
    /// 控制台卡片 PreviewMouseLeftButtonDown：记录起始位置与激活任务。
    /// 若点击在 ProgressBar、Button 或 CheckBox 上则跳过。
    /// </summary>
    private void OnConsoleCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (IsSourceInteractive(e.OriginalSource as DependencyObject)) return;

        _dragStartPoint = e.GetPosition(null);
        _draggedTask = ((MainViewModel)DataContext).ActiveTask;
        _dragSource = "Console";
        _isDraggingCard = true;
    }

    /// <summary>
    /// 控制台卡片 PreviewMouseMove：超过阈值后启动系统级 DragDrop。
    /// </summary>
    private void OnConsoleCardMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingCard || e.LeftButton != MouseButtonState.Pressed || _draggedTask == null)
            return;

        if (!ExceedsDragThreshold(e.GetPosition(null)))
            return;

        _isDraggingCard = false;
        DragDrop.DoDragDrop((DependencyObject)sender,
            new DataObject(DragDataFormat, _draggedTask), DragDropEffects.Move);
    }

    // ── 控制台区域 Drop 目标 ────────────────────────────────────────────────

    /// <summary>
    /// 拖拽进入控制台区域：高亮反馈，并将效果设为 Move。
    /// </summary>
    private void OnConsoleDragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DragDataFormat) || _dragSource != "List") return;
        ConsoleZone.BorderBrush = (System.Windows.Media.Brush)FindResource("PrimaryBrush");
        ConsoleZone.BorderThickness = new Thickness(2);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    /// <summary>
    /// 拖拽离开控制台区域：检查是否真正离开了区域边界，避免移入子元素时误清除高亮。
    /// </summary>
    private void OnConsoleDragLeave(object sender, DragEventArgs e)
    {
        // 检查鼠标是否真的超出了 ConsoleZone 的边界范围
        var pos = e.GetPosition(ConsoleZone);
        if (pos.X >= 0 && pos.Y >= 0 &&
            pos.X <= ConsoleZone.ActualWidth && pos.Y <= ConsoleZone.ActualHeight)
            return;

        ConsoleZone.BorderBrush = null;
        ConsoleZone.BorderThickness = new Thickness(0);
    }

    /// <summary>
    /// PreviewDragOver（隧道事件）：在所有子元素之前拦截，持续维持 Move 效果，
    /// 确保鼠标在控制台内任意位置悬停时 Drop 均可响应。
    /// </summary>
    private void OnConsoleDragOver(object sender, DragEventArgs e)
    {
        e.Effects = (e.Data.GetDataPresent(DragDataFormat) && _dragSource == "List")
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    /// <summary>
    /// PreviewDrop（隧道事件）：若来源为列表，则将该任务设为激活。
    /// </summary>
    private void OnConsoleDrop(object sender, DragEventArgs e)
    {
        ConsoleZone.BorderBrush = null;
        ConsoleZone.BorderThickness = new Thickness(0);

        if (!e.Data.GetDataPresent(DragDataFormat) || _dragSource != "List") return;
        var task = e.Data.GetData(DragDataFormat) as TaskItem;
        if (task == null) return;

        ((MainViewModel)DataContext).SetActiveTask(task);
        e.Handled = true;
    }

    // ── 任务列表 Drop 目标（从控制台拖回） ─────────────────────────────────

    /// <summary>
    /// 拖拽进入任务列表：若来源为控制台则显示接收效果。
    /// </summary>
    private void OnListDragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DragDataFormat)) return;
        e.Effects = _dragSource == "Console" ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    /// <summary>拖拽离开任务列表：无额外处理。</summary>
    private void OnListDragLeave(object sender, DragEventArgs e) { }

    /// <summary>
    /// 拖拽在任务列表上方悬停：持续维持效果。
    /// </summary>
    private void OnListDragOver(object sender, DragEventArgs e)
    {
        e.Effects = (e.Data.GetDataPresent(DragDataFormat) && _dragSource == "Console")
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    /// <summary>
    /// 拖拽放入任务列表：若来源为控制台，则清除激活状态，任务回归列表原位。
    /// </summary>
    private void OnListDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DragDataFormat) || _dragSource != "Console") return;
        ((MainViewModel)DataContext).ClearActiveTask();
        e.Handled = true;
    }

    // ── 辅助方法 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 判断鼠标当前位置是否超过了触发拖拽的移动阈值。
    /// </summary>
    private bool ExceedsDragThreshold(Point current)
        => Math.Abs(current.X - _dragStartPoint.X) >= DragThreshold ||
           Math.Abs(current.Y - _dragStartPoint.Y) >= DragThreshold;

    /// <summary>
    /// 判断 <paramref name="source"/> 是否位于需要保留原有交互的控件（
    /// <see cref="ProgressBar"/>、<see cref="Button"/>、<see cref="CheckBox"/>）上或内部。
    /// 若是，则拖拽不应被触发，以免干扰进度调节、删除和勾选操作。
    /// </summary>
    private static bool IsSourceInteractive(DependencyObject? source)
    {
        var current = source;
        while (current != null)
        {
            if (current is System.Windows.Controls.ProgressBar
                       or System.Windows.Controls.Button
                       or System.Windows.Controls.CheckBox)
                return true;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    #endregion
}
