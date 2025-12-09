namespace DataProcessing.Shared.Utilities;

/// <summary>
/// Utility for converting between Unix cron format (6-field) and Quartz.NET cron format.
/// This utility is framework-agnostic and doesn't depend on Quartz.NET.
/// </summary>
public static class CronExpressionConverter
{
    /// <summary>
    /// Converts Unix cron (6-field) to Quartz.NET cron format.
    ///
    /// Unix cron allows both day-of-month and day-of-week to be *.
    /// Quartz.NET requires one to be ? (no specific value).
    ///
    /// Format: seconds minutes hours day-of-month month day-of-week
    /// Example: "0 */5 * * * *" → "0 */5 * * * ?"
    /// </summary>
    /// <param name="unixCron">Unix cron expression (6 fields)</param>
    /// <returns>Quartz.NET compatible cron expression</returns>
    /// <exception cref="ArgumentException">Thrown when cron expression is invalid</exception>
    public static string ConvertUnixToQuartz(string unixCron)
    {
        if (string.IsNullOrWhiteSpace(unixCron))
            throw new ArgumentException("Cron expression cannot be empty", nameof(unixCron));

        var parts = unixCron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 6)
            throw new ArgumentException(
                $"Unix cron must have 6 fields (seconds minutes hours day-of-month month day-of-week). Got: {parts.Length} fields in '{unixCron}'",
                nameof(unixCron));

        // Field indices:
        // 0: seconds
        // 1: minutes
        // 2: hours
        // 3: day-of-month
        // 4: month
        // 5: day-of-week

        var dayOfMonth = parts[3];
        var dayOfWeek = parts[5];

        // Quartz.NET rule: Either day-of-month OR day-of-week must be ? (not both *)

        // Case 1: Both are * → Change day-of-week to ?
        if (dayOfMonth == "*" && dayOfWeek == "*")
        {
            parts[5] = "?";
        }
        // Case 2: day-of-month is specific, day-of-week is * → Change day-of-week to ?
        else if (dayOfMonth != "*" && dayOfMonth != "?" && dayOfWeek == "*")
        {
            parts[5] = "?";
        }
        // Case 3: day-of-week is specific, day-of-month is * → Change day-of-month to ?
        else if (dayOfWeek != "*" && dayOfWeek != "?" && dayOfMonth == "*")
        {
            parts[3] = "?";
        }
        // Case 4: Both are ? → Valid (rare but allowed)
        // Case 5: One is ?, one is * or specific → Already valid
        // No changes needed for Case 4 and 5

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Converts Quartz.NET cron back to Unix format (for display purposes)
    /// Replaces ? with * for user-friendly display
    /// </summary>
    /// <param name="quartzCron">Quartz.NET cron expression</param>
    /// <returns>Unix-style cron expression</returns>
    public static string ConvertQuartzToUnix(string quartzCron)
    {
        if (string.IsNullOrWhiteSpace(quartzCron))
            throw new ArgumentException("Cron expression cannot be empty", nameof(quartzCron));

        var parts = quartzCron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 6)
            throw new ArgumentException(
                $"Quartz cron must have at least 6 fields. Got: {parts.Length} fields in '{quartzCron}'",
                nameof(quartzCron));

        // Replace all ? with * for Unix format
        for (int i = 0; i < parts.Length && i < 6; i++)
        {
            if (parts[i] == "?")
                parts[i] = "*";
        }

        // Return only first 6 fields (ignore optional year field if present)
        return string.Join(" ", parts.Take(6));
    }
}
