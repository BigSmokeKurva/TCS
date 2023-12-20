namespace TCS
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo MoscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");

        public static DateTime GetMoscowTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MoscowTimeZone);
        }
    }
}
