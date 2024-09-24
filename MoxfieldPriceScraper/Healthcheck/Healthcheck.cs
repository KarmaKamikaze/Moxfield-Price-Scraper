using Newtonsoft.Json;

namespace MoxfieldPriceScraper.Healthcheck;

public static class Healthcheck
{
    private const string StatusFile = "tasks.status";
    private static readonly object FileLock = new object();

    /// <summary>
    /// Initializes the status file with an empty dictionary if it doesn't exist.
    /// </summary>
    public static void InitializeStatusFile()
    {
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        Directory.CreateDirectory(dataDirectory);
        var statusFilePath = Path.Combine(dataDirectory, StatusFile);

        lock (FileLock)
        {
            if (!File.Exists(statusFilePath))
            {
                var initialStatus = new TaskStatus();
                File.WriteAllText(statusFilePath, JsonConvert.SerializeObject(initialStatus, Formatting.Indented));
            }
        }
    }

    /// <summary>
    /// Updates the status of a task in the status file.
    /// </summary>
    /// <param name="taskName">The task name associated with the status that needs to update.</param>
    /// <param name="status">The new status.</param>
    public static void UpdateTaskStatus(string taskName, string status)
    {
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        var statusFilePath = Path.Combine(dataDirectory, StatusFile);

        lock (FileLock)
        {
            var taskStatus = JsonConvert.DeserializeObject<TaskStatus>(File.ReadAllText(statusFilePath));
            if (taskStatus != null)
            {
                taskStatus.Statuses[taskName] = status;
                File.WriteAllText(statusFilePath, JsonConvert.SerializeObject(taskStatus, Formatting.Indented));
            }
        }
    }

    /// <summary>
    /// Checks if any tasks are running.
    /// </summary>
    /// <returns>True if tasks are still running, false otherwise.</returns>
    public static bool AreTasksRunning()
    {
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        var statusFilePath = Path.Combine(dataDirectory, StatusFile);
        
        lock (FileLock)
        {
            if (!File.Exists(statusFilePath))
            {
                return false;
            }

            var taskStatus = JsonConvert.DeserializeObject<TaskStatus>(File.ReadAllText(statusFilePath));
            return taskStatus != null && taskStatus.Statuses.ContainsValue("running");
        }
    }
}
