using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TaskLine.Models;

namespace TaskLine.Services;

public class TaskService
{
    private static readonly string DataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskLine");

    private static readonly string DataFile = Path.Combine(DataFolder, "tasks.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public List<TaskItem> Load()
    {
        if (!File.Exists(DataFile))
            return [];

        try
        {
            var json = File.ReadAllText(DataFile);
            return JsonSerializer.Deserialize<List<TaskItem>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(IEnumerable<TaskItem> tasks)
    {
        Directory.CreateDirectory(DataFolder);
        var json = JsonSerializer.Serialize(tasks, JsonOptions);
        File.WriteAllText(DataFile, json);
    }
}
