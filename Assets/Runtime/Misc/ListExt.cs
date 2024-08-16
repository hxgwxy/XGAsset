using System.Collections.Generic;
using System.Linq;

namespace XGAsset.Runtime.Misc
{
    public static class ListExt
    {
        public static List<T> Sort2List<T>(this List<T> list)
        {
            list.Sort();
            return list;
        }

        public static List<T> Sort2List<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.ToList().Sort2List();
        }

        public static List<T> ToRefList<T>(this IEnumerable<T> enumerable, List<T> list = null)
        {
            list ??= new List<T>();
            list.Clear();
            foreach (var item in enumerable)
            {
                list.Add(item);
            }

            return list;
        }
    }
}