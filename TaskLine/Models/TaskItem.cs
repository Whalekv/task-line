using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskLine.Models;

/// <summary>
/// 表示 TaskLine 中的单个任务项。
/// 实现 <see cref="INotifyPropertyChanged"/>，以便 UI 绑定自动更新。
/// </summary>
public class TaskItem : INotifyPropertyChanged
{
    #region 私有字段

    private string _title = string.Empty;
    private string _description = string.Empty;
    private bool _isCompleted;
    private bool _isActive;
    private DateTime? _dueDate;
    private int _progress;

    #endregion

    #region 属性

    /// <summary>获取任务的唯一标识符，创建时初始化，不可修改。</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>获取或设置任务的标题（简短摘要）。</summary>
    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    /// <summary>获取或设置任务的可选详细描述。</summary>
    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    /// <summary>获取或设置一个值，指示该任务是否已完成。</summary>
    public bool IsCompleted
    {
        get => _isCompleted;
        set { _isCompleted = value; OnPropertyChanged(); }
    }

    /// <summary>获取或设置该任务是否正在控制台卡槽中执行。同一时刻最多一个任务为 <c>true</c>。</summary>
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }

    /// <summary>获取或设置任务的可选截止日期。</summary>
    public DateTime? DueDate
    {
        get => _dueDate;
        set { _dueDate = value; OnPropertyChanged(); }
    }

    /// <summary>获取或设置任务的完成进度（0–100）。进度达到 100 时自动将任务标记为已完成。</summary>
    public int Progress
    {
        get => _progress;
        set
        {
            _progress = Math.Clamp(value, 0, 100);
            OnPropertyChanged();
            if (_progress == 100 && !_isCompleted)
            {
                _isCompleted = true;
                OnPropertyChanged(nameof(IsCompleted));
            }
        }
    }

    /// <summary>获取任务的创建时间，创建时初始化，不可修改。</summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    #endregion

    #region INotifyPropertyChanged

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 触发 <see cref="PropertyChanged"/> 事件，通知 UI 指定属性已变更。
    /// </summary>
    /// <param name="name">已变更的属性名称，由编译器自动填充。</param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    #endregion
}
