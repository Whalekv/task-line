# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**TaskLine** — a WPF desktop application for personal task management, built with C# and .NET.

## Commands

```bash
# Build
dotnet build TaskLine.sln

# Run
dotnet run --project TaskLine/TaskLine.csproj

# Run tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

You can also open `TaskLine.sln` in Visual Studio and use F5 to run or Ctrl+Shift+B to build.

## Architecture

The project follows the **MVVM** (Model-View-ViewModel) pattern:

```
TaskLine/
  Models/          # Plain data classes (Task, Category, etc.)
  ViewModels/      # Bindable logic; implement INotifyPropertyChanged via a base class
  Views/           # XAML windows and user controls; code-behind is minimal
  Services/        # Data persistence, filtering, sorting logic
  Commands/        # ICommand implementations (RelayCommand)
```

### Key conventions

- ViewModels expose `ObservableCollection<T>` for list bindings and use `RelayCommand` for button actions.
- Views bind exclusively through DataContext — no business logic in code-behind.
- Services are injected into ViewModels via constructor; ViewModels do not directly access storage.
- Data is persisted locally (JSON or SQLite in `%AppData%\TaskLine\`).
