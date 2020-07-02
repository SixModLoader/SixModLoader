namespace SixModLoader
{
    public static class Utils
    {
        public static string Pluralize(this string text, int count)
        {
            return text + (count == 1 ? "" : "s");
        }
    }
}
