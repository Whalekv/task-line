using TaskLine.ViewModels;
using Xunit;

namespace TaskLine.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public void AddTask_AddsToCollection()
    {
        var vm = new MainViewModel();
        var initialCount = vm.Tasks.Count;

        vm.NewTaskTitle = "Buy milk";
        vm.AddTaskCommand.Execute(null);

        Assert.Equal(initialCount + 1, vm.Tasks.Count);
        Assert.Equal("Buy milk", vm.Tasks[^1].Title);
    }

    [Fact]
    public void AddTask_ClearsNewTaskTitle()
    {
        var vm = new MainViewModel();
        vm.NewTaskTitle = "Some task";
        vm.AddTaskCommand.Execute(null);

        Assert.Equal(string.Empty, vm.NewTaskTitle);
    }

    [Fact]
    public void AddTaskCommand_CannotExecute_WhenTitleEmpty()
    {
        var vm = new MainViewModel();
        vm.NewTaskTitle = "  ";

        Assert.False(vm.AddTaskCommand.CanExecute(null));
    }

    [Fact]
    public void DeleteTask_RemovesFromCollection()
    {
        var vm = new MainViewModel();
        vm.NewTaskTitle = "Task to delete";
        vm.AddTaskCommand.Execute(null);

        var task = vm.Tasks[^1];
        vm.DeleteTaskCommand.Execute(task);

        Assert.DoesNotContain(task, vm.Tasks);
    }

    [Fact]
    public void ToggleComplete_FlipsIsCompleted()
    {
        var vm = new MainViewModel();
        vm.NewTaskTitle = "Toggle me";
        vm.AddTaskCommand.Execute(null);

        var task = vm.Tasks[^1];
        Assert.False(task.IsCompleted);

        vm.ToggleCompleteCommand.Execute(task);
        Assert.True(task.IsCompleted);

        vm.ToggleCompleteCommand.Execute(task);
        Assert.False(task.IsCompleted);
    }
}
