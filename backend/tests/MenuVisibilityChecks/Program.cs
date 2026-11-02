using AnFood.Application.Bussiness.MenuVisibility;

var checks = 0;
void Check(bool result, string name) { if (!result) throw new Exception(name); checks++; }
bool Hidden(VisibilitySchedule schedule, string instant) => VisibilityScheduleEvaluator.IsHidden(new[] { schedule }, DateTimeOffset.Parse(instant));
var weekly = new VisibilitySchedule { Days = new() { 1 }, TimeRanges = new() { new() { From = "11:00", To = "13:30" }, new() { From = "17:00", To = "19:00" } } };
Check(Hidden(weekly, "2026-10-26T04:00:00Z"), "Start inclusive in UTC+7");
Check(!Hidden(weekly, "2026-10-26T06:30:00Z"), "End exclusive");
Check(!Hidden(weekly, "2026-10-26T07:00:00Z"), "Gap between slots");
Check(Hidden(weekly, "2026-10-26T11:00:00Z"), "Second slot");
Check(!Hidden(weekly, "2026-10-27T04:00:00Z"), "Other weekday visible");
var overnight = new VisibilitySchedule { Days = new() { 1 }, TimeRanges = new() { new() { From = "22:00", To = "02:00" } } };
Check(Hidden(overnight, "2026-10-27T01:59:00+07:00"), "Overnight follows previous day");
Check(!Hidden(overnight, "2026-10-27T02:00:00+07:00"), "Overnight ends exactly");
Check(!Hidden(overnight, "2026-10-26T01:00:00+07:00"), "Previous day not selected");
var dateRange = new VisibilitySchedule { Kind = "DateRange", StartDate = "2026-10-26", EndDate = "2026-10-27" };
Check(Hidden(dateRange, "2026-10-25T17:00:00Z"), "Date boundary uses Vietnam time");
Check(Hidden(dateRange, "2026-10-27T23:59:00+07:00"), "Inclusive last date all day");
Check(!Hidden(dateRange, "2026-10-28T00:00:00+07:00"), "After last date");
Check(VisibilityScheduleEvaluator.Validate(new() { weekly }) == null, "Valid weekly schedule");
Check(VisibilityScheduleEvaluator.Validate(new() { new() }) != null, "Reject no weekday");
Check(VisibilityScheduleEvaluator.Validate(new() { new() { Kind = "DateRange", StartDate = "2026-10-28", EndDate = "2026-10-26" } }) != null, "Reject reversed dates");
Check(VisibilityScheduleEvaluator.Validate(new() { new() { Days = new() { 1 }, TimeRanges = new() { new() { From = "11:00", To = "11:00" } } } }) != null, "Reject ambiguous equal times");
Check(VisibilityScheduleEvaluator.Validate(new() { new() { Days = new() { 1 }, TimeRanges = new() { new() { From = "25:00", To = "11:00" } } } }) != null, "Reject invalid time");
Console.WriteLine($"Passed {checks} menu visibility checks.");
