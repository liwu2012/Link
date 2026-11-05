using System.Globalization;

namespace KingdeeTrace
{
    internal static class SqlUtil
    {
        public static string Str(string value)
        {
            return value == null ? "NULL" : "'" + value.Replace("'", "''") + "'";
        }

        public static string Num(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string Dec(decimal value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
