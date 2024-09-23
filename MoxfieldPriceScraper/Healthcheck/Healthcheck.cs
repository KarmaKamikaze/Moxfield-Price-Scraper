using Newtonsoft.Json;

namespace MoxfieldPriceScraper.Healthcheck;

public static class Healthcheck
{
    private const string StatusFilePath = "tasks.status";
    private static readonly object FileLock = new object();

    /// <summary>
    /// Initializes the status file with an empty dictionary if it doesn't exist.
    /// </summary>
    public static void InitializeStatusFile()
    {
        lock (FileLock)
        {
            if (!File.Exists(StatusFilePath))
            {
                var initialStatus = new TaskStatus();
                File.WriteAllText(StatusFilePath, JsonConvert.SerializeObject(initialStatus, Formatting.Indented));
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
        lock (FileLock)
        {
            var taskStatus = JsonConvert.DeserializeObject<TaskStatus>(File.ReadAllText(StatusFilePath));
            if (taskStatus != null)
            {
                taskStatus.Statuses[taskName] = status;
                File.WriteAllText(StatusFilePath, JsonConvert.SerializeObject(taskStatus, Formatting.Indented));
            }
        }
    }

    /// <summary>
    /// Checks if any tasks are running.
    /// </summary>
    /// <returns>True if tasks are still running, false otherwise.</returns>
    public static bool AreTasksRunning()
    {
        lock (FileLock)
        {
            if (!File.Exists(StatusFilePath))
            {
                return false;
            }

            var taskStatus = JsonConvert.DeserializeObject<TaskStatus>(File.ReadAllText(StatusFilePath));
            return taskStatus != null && taskStatus.Statuses.ContainsValue("running");
        }
    }
}
