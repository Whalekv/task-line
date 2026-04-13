using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TaskLine.Models;

namespace TaskLine.Services;

/// <summary>
/// 负责任务数据的本地持久化，以 JSON 格式读写 <see cref="TaskItem"/> 列表。
/// 数据文件存储于 <c>%AppData%\TaskLine\tasks.json</c>。
/// </summary>
public class TaskService
{
    #region 私有字段

    /// <summary>应用数据目录的完整路径（%AppData%\TaskLine）。</summary>
    private static readonly string DataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskLine");

    /// <summary>任务数据文件的完整路径。</summary>
    private static readonly string DataFile = Path.Combine(DataFolder, "tasks.json");

    /// <summary>JSON 序列化选项：启用缩进格式，并忽略属性名大小写。</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    #endregion

    #region 公共方法

    /// <summary>
    /// 从本地 JSON 文件加载所有任务，并自动将过期未完成任务顺延至今天。
    /// 若文件不存在或解析失败，则返回空列表。
    /// </summary>
    /// <returns>处理后的 <see cref="TaskItem"/> 列表；失败时返回空列表。</returns>
    public List<TaskItem> Load()
    {
        List<TaskItem> tasks;

        if (!File.Exists(DataFile))
            return [];

        try
        {
            var json = File.ReadAllText(DataFile);
            tasks = JsonSerializer.Deserialize<List<TaskItem>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }

        // 顺延逻辑：将过期未完成任务的日期更新为今天
        var today = DateTime.Today;
        var dirty = false;

        foreach (var task in tasks)
        {
            if (!task.IsCompleted && (task.DueDate == null || task.DueDate.Value.Date < today))
            {
                task.DueDate = today;
                dirty = true;
            }
        }

        if (dirty)
            Save(tasks);

        return tasks;
    }

    /// <summary>
    /// 将任务集合序列化并保存到本地 JSON 文件。
    /// 目标目录不存在时会自动创建。
    /// </summary>
    /// <param name="tasks">要持久化的任务集合。</param>
    public void Save(IEnumerable<TaskItem> tasks)
    {
        Directory.CreateDirectory(DataFolder);
        var json = JsonSerializer.Serialize(tasks, JsonOptions);
        File.WriteAllText(DataFile, json);
    }

    #endregion
}
