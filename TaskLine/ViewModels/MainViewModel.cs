using System.Collections.ObjectModel;
using System.Windows.Input;
using TaskLine.Commands;
using TaskLine.Models;
using TaskLine.Services;

namespace TaskLine.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly TaskService _taskService = new();
    private string _newTaskTitle = string.Empty;
    private TaskItem? _selectedTask;

    public ObservableCollection<TaskItem> Tasks { get; } = [];

    public string NewTaskTitle
    {
        get => _newTaskTitle;
        set => SetField(ref _newTaskTitle, value);
    }

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set => SetField(ref _selectedTask, value);
    }

    public ICommand AddTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    public ICommand ToggleCompleteCommand { get; }

    public MainViewModel()
    {
        AddTaskCommand = new RelayCommand(AddTask, () => !string.IsNullOrWhiteSpace(NewTaskTitle));
        DeleteTaskCommand = new RelayCommand<TaskItem>(DeleteTask);
        ToggleCompleteCommand = new RelayCommand<TaskItem>(ToggleComplete);

        foreach (var task in _taskService.Load())
            Tasks.Add(task);
    }

    private void AddTask()
    {
        var task = new TaskItem { Title = NewTaskTitle.Trim() };
        Tasks.Add(task);
        NewTaskTitle = string.Empty;
        _taskService.Save(Tasks);
    }

    private void DeleteTask(TaskItem? task)
    {
        if (task is null) return;
        Tasks.Remove(task);
        _taskService.Save(Tasks);
    }

    private void ToggleComplete(TaskItem? task)
    {
        if (task is null) return;
        task.IsCompleted = !task.IsCompleted;
        _taskService.Save(Tasks);
    }
}
