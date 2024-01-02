namespace TCS
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo MoscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
        public static DateTime ToMoscow(DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, MoscowTimeZone);
        }
        public static DateTime GetUnspecifiedUtc()
        {
            return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        }
    }
}
