using System.Collections.Generic;
using System.Linq;

namespace GitSubdirTools.Libs
{
    public static class ExLinq
    {
        public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource?> source)
            where TSource : class
            => source.Where(source1 => source1 != null)!;

        public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource?> source)
            where TSource : struct
            => source.Where(source1 => source1 != null).Select(arg => arg!.Value);
    }
}
