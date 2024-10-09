namespace mPartnerAdmin_CommunicationScheduler.Utilities
{
    public static class CronUtility
    {
        public static string ConvertDaysOfWeek(string days)
        {
            return days.Replace("Mon", "MON")
                       .Replace("Tue", "TUE")
                       .Replace("Wed", "WED")
                       .Replace("Thu", "THU")
                       .Replace("Fri", "FRI")
                       .Replace("Sat", "SAT")
                       .Replace("Sun", "SUN");
        }

        public static string ConvertMonths(string months)
        {
            return months.Replace("Jan", "1")
                         .Replace("Feb", "2")
                         .Replace("Mar", "3")
                         .Replace("Apr", "4")
                         .Replace("May", "5")
                         .Replace("Jun", "6")
                         .Replace("Jul", "7")
                         .Replace("Aug", "8")
                         .Replace("Sep", "9")
                         .Replace("Oct", "10")
                         .Replace("Nov", "11")
                         .Replace("Dec", "12");
        }

        public static string GetCronExpression(string frequencyType)
        {
            switch (frequencyType)
            {
                case "Daily":
                    return "0 0 0 * * ?"; // Runs at midnight daily
                case "Weekly":
                    return "0 0 0 ? * MON"; // Runs at midnight every Monday
                case "Monthly":
                    return "0 0 0 1 * ?"; // Runs on the first day of every month
                default:
                    return "0 0 0 * * ?"; // Default to daily
            }
        }
    }
}
