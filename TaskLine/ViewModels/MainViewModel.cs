using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TaskLine.Commands;
using TaskLine.Models;
using TaskLine.Services;

namespace TaskLine.ViewModels;

/// <summary>
/// 主窗口的 ViewModel，负责日期侧边栏与当日任务列表的管理。
/// 继承自 <see cref="ViewModelBase"/>，通过 <see cref="TaskService"/> 进行数据持久化。
/// </summary>
public class MainViewModel : ViewModelBase
{
    #region 私有字段

    private readonly TaskService _taskService = new();

    /// <summary>所有任务的内部列表，作为数据唯一来源。</summary>
    private readonly List<TaskItem> _allTasks = [];

    private TaskItem? _selectedTask;
    private DateTime _selectedDate = DateTime.Today;

    /// <summary>控制台区域拖入时的高亮状态，用于驱动 DragEnter/DragLeave 视觉反馈。</summary>
    private bool _isConsoleDragOver;

    #endregion

    #region 属性

    /// <summary>获取侧边栏的日期列表（今天置顶，其余有任务的日期倒序排列）。</summary>
    public ObservableCollection<DateTime> DateList { get; } = [];

    /// <summary>获取当前选中日期下的任务列表，绑定到右侧任务面板。</summary>
    public ObservableCollection<TaskItem> TasksForSelectedDate { get; } = [];

    /// <summary>获取或设置当前选中的日期；变更后自动刷新任务列表及可新增状态。</summary>
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetField(ref _selectedDate, value))
            {
                RefreshTasksForDate();
                OnPropertyChanged(nameof(IsAddableDate));
                OnPropertyChanged(nameof(IsToday));
            }
        }
    }

    /// <summary>
    /// 获取当前选中日期是否允许新增任务（今天或未来日期返回 <c>true</c>）。
    /// </summary>
    public bool IsAddableDate => _selectedDate.Date >= DateTime.Today;

    /// <summary>获取当前选中日期是否为今天，用于控制控制台区域的可见性。</summary>
    public bool IsToday => _selectedDate.Date == DateTime.Today;

    /// <summary>获取当前控制台卡槽中正在执行的任务；无激活任务时返回 <c>null</c>。</summary>
    public TaskItem? ActiveTask => _allTasks.FirstOrDefault(t => t.IsActive);

    /// <summary>获取控制台卡槽当前是否已有任务（占用态），用于切换空态/占用态视觉。</summary>
    public bool IsConsoleFull => ActiveTask != null;

    /// <summary>获取控制台卡槽当前是否为空，用于切换空态提示文字可见性。</summary>
    public bool IsConsoleEmpty => ActiveTask == null;

    /// <summary>获取或设置控制台区域是否处于拖拽悬停高亮状态。</summary>
    public bool IsConsoleDragOver
    {
        get => _isConsoleDragOver;
        set => SetField(ref _isConsoleDragOver, value);
    }

    /// <summary>获取或设置当前在列表中选中的任务项。</summary>
    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set => SetField(ref _selectedTask, value);
    }

    #endregion

    #region 命令

    /// <summary>删除指定任务命令，接收 <see cref="TaskItem"/> 作为参数。</summary>
    public ICommand DeleteTaskCommand { get; }

    /// <summary>切换指定任务完成状态的命令，接收 <see cref="TaskItem"/> 作为参数。</summary>
    public ICommand ToggleCompleteCommand { get; }

    /// <summary>将控制台中的激活任务释放回任务列表的命令（无参数）。</summary>
    public ICommand ClearActiveTaskCommand { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 <see cref="MainViewModel"/>，绑定命令并从本地存储加载任务列表。
    /// </summary>
    public MainViewModel()
    {
        DeleteTaskCommand = new RelayCommand<TaskItem>(DeleteTask);
        ToggleCompleteCommand = new RelayCommand<TaskItem>(ToggleComplete);
        ClearActiveTaskCommand = new RelayCommand(ClearActiveTask);

        _allTasks.AddRange(_taskService.Load());
        RefreshDateList();
        RefreshTasksForDate();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 将当前全部任务持久化到本地存储。供视图层在拖拽进度结束后调用。
    /// </summary>
    public void SaveTasks() => _taskService.Save(_allTasks);

    /// <summary>
    /// 将指定任务设为控制台激活状态，同时清除其他已激活任务。
    /// 任务从今天的列表中移走，出现在控制台卡槽。
    /// </summary>
    /// <param name="task">要激活的任务项；不可为 <c>null</c>。</param>
    public void SetActiveTask(TaskItem task)
    {
        // 先清除已有激活任务，保证卡槽同一时刻只有一个
        var prev = _allTasks.FirstOrDefault(t => t.IsActive);
        if (prev != null) prev.IsActive = false;

        task.IsActive = true;
        RefreshTasksForDate();
        NotifyConsoleChanged();
        _taskService.Save(_allTasks);
    }

    /// <summary>
    /// 将控制台中的激活任务释放回任务列表原位置。
    /// </summary>
    public void ClearActiveTask()
    {
        var task = _allTasks.FirstOrDefault(t => t.IsActive);
        if (task == null) return;
        task.IsActive = false;
        RefreshTasksForDate();
        NotifyConsoleChanged();
        _taskService.Save(_allTasks);
    }

    /// <summary>
    /// 在指定日期下新建任务并持久化，完成后切换视图到该日期。
    /// </summary>
    /// <param name="title">任务标题，不可为空或空白。</param>
    /// <param name="date">任务所属日期，须为今天或未来日期。</param>
    public void AddTask(string title, DateTime date)
    {
        var task = new TaskItem { Title = title.Trim(), DueDate = date.Date };
        _allTasks.Add(task);
        RefreshDateList();
        // 切换到新任务所在日期（若与当前相同则仅刷新列表）
        _selectedDate = date.Date;
        OnPropertyChanged(nameof(SelectedDate));
        OnPropertyChanged(nameof(IsAddableDate));
        RefreshTasksForDate();
        _taskService.Save(_allTasks);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 重建日期侧边栏列表：今天始终置顶，其余有任务的日期倒序追加。
    /// </summary>
    private void RefreshDateList()
    {
        var dates = new HashSet<DateTime> { DateTime.Today };

        foreach (var task in _allTasks)
            dates.Add((task.DueDate ?? DateTime.Today).Date);

        DateList.Clear();
        foreach (var date in dates.OrderByDescending(d => d))
            DateList.Add(date);
    }

    /// <summary>
    /// 根据 <see cref="SelectedDate"/> 筛选 <see cref="_allTasks"/>，刷新右侧任务列表。
    /// 当查看今天时，正在控制台执行的任务（<see cref="TaskItem.IsActive"/> 为 <c>true</c>）不出现在列表中。
    /// </summary>
    private void RefreshTasksForDate()
    {
        var isToday = _selectedDate.Date == DateTime.Today;
        TasksForSelectedDate.Clear();
        foreach (var task in _allTasks.Where(t =>
            (t.DueDate ?? DateTime.Today).Date == _selectedDate.Date
            && !(isToday && t.IsActive)))
            TasksForSelectedDate.Add(task);
    }

    /// <summary>
    /// 触发所有控制台相关属性的变更通知，驱动 UI 刷新控制台区域。
    /// </summary>
    private void NotifyConsoleChanged()
    {
        OnPropertyChanged(nameof(ActiveTask));
        OnPropertyChanged(nameof(IsConsoleFull));
        OnPropertyChanged(nameof(IsConsoleEmpty));
    }

    /// <summary>
    /// 从任务列表中移除指定任务并持久化。
    /// </summary>
    /// <param name="task">要删除的任务项；为 <c>null</c> 时不执行任何操作。</param>
    private void DeleteTask(TaskItem? task)
    {
        if (task is null) return;
        _allTasks.Remove(task);
        RefreshDateList();
        RefreshTasksForDate();
        _taskService.Save(_allTasks);
    }

    /// <summary>
    /// 切换指定任务的完成状态并持久化。
    /// </summary>
    /// <param name="task">要切换状态的任务项；为 <c>null</c> 时不执行任何操作。</param>
    private void ToggleComplete(TaskItem? task)
    {
        if (task is null) return;
        task.IsCompleted = !task.IsCompleted;
        _taskService.Save(_allTasks);
    }

    #endregion
}
