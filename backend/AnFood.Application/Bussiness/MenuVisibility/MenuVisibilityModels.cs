using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AnFood.Application.Bussiness.MenuVisibility;

public class MenuVisibilityModel
{
    public Guid Id { get; set; }
    public string BranchCode { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public List<Guid> MenuItemIds { get; set; } = new();
    public List<Guid> MenuGroupIds { get; set; } = new();
    public List<VisibilitySchedule> Schedules { get; set; } = new();
}

public class MenuVisibilityItemModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public List<Guid> BranchIds { get; set; } = new();
    public List<MenuVisibilityGroupModel> Groups { get; set; } = new();
}

public class MenuVisibilityGroupModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class VisibilitySchedule
{
    public string Kind { get; set; } = "Weekdays";
    public List<int> Days { get; set; } = new(); // Sunday = 0
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public List<VisibilityTimeRange> TimeRanges { get; set; } = new();
}

public class VisibilityTimeRange
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
}

public static class VisibilityScheduleEvaluator
{
    public static bool TryTime(string value, out TimeOnly time) =>
        TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

    public static bool TryDate(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    // Business calendar is Asia/Ho_Chi_Minh (UTC+7), independent of server timezone.
    public static bool IsHidden(IEnumerable<VisibilitySchedule> schedules, DateTimeOffset instant)
    {
        var local = instant.ToOffset(TimeSpan.FromHours(7)).DateTime;
        var today = DateOnly.FromDateTime(local);
        var time = TimeOnly.FromDateTime(local);
        foreach (var schedule in schedules)
        {
            bool Matches(DateOnly date) => schedule.Kind == "Weekdays"
                ? schedule.Days.Contains((int)date.DayOfWeek)
                : TryDate(schedule.StartDate, out var start) && TryDate(schedule.EndDate, out var end) && date >= start && date <= end;
            if (schedule.TimeRanges.Count == 0 && Matches(today)) return true;
            foreach (var range in schedule.TimeRanges)
            {
                if (!TryTime(range.From, out var from) || !TryTime(range.To, out var to) || from == to) continue;
                if (from < to && Matches(today) && time >= from && time < to) return true;
                // An overnight slot belongs to the day on which it starts.
                if (from > to && ((Matches(today) && time >= from) || (Matches(today.AddDays(-1)) && time < to))) return true;
            }
        }
        return false;
    }

    public static string? Validate(List<VisibilitySchedule>? schedules)
    {
        if (schedules == null || schedules.Count == 0 || schedules.Count > 100) return "Cần từ 1 đến 100 lịch ẩn.";
        foreach (var schedule in schedules)
        {
            if (schedule == null || schedule.TimeRanges == null || schedule.TimeRanges.Count > 100) return "Lịch ẩn không hợp lệ.";
            if (schedule.Kind == "Weekdays")
            {
                if (schedule.Days == null || schedule.Days.Count == 0 || schedule.Days.Any(d => d < 0 || d > 6)) return "Vui lòng chọn ngày trong tuần.";
            }
            else if (schedule.Kind != "DateRange" || !TryDate(schedule.StartDate, out var start) || !TryDate(schedule.EndDate, out var end) || start > end)
                return "Khoảng ngày không hợp lệ.";
            foreach (var range in schedule.TimeRanges)
                if (range == null || !TryTime(range.From, out var from) || !TryTime(range.To, out var to) || from == to)
                    return "Khung giờ phải đúng định dạng HH:mm và giờ bắt đầu khác giờ kết thúc.";
        }
        return null;
    }
}
