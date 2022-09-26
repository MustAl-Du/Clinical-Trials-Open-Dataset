using Parquet.Data;

namespace XmlToParquet
{
    public static class Extensions
    {
        public static void AddIfNotNull<T>(this List<T> list, T? item)
            where T : class
        {
            if (item != null)
            {
                list.Add(item);
            }
        }

        public static char ToUpper(this char c)
        {
            return Char.ToUpper(c);
        }

        public static T? TryPeek<T>(this Stack<T> stack)
        {
            if(!stack.Any())
            {
                return default(T);
            }

            return stack.Peek();
        }
    }
}
