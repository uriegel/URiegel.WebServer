using System;
using System.Collections.Generic;

static class IEnumerableExtensions
{
    /// <summary>
    /// Perform an action on all elements of an Enumerable. The action can succeed or fail. If the actions fails, Perform will return true
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="sourceList"></param>
    /// <param name="perform"></param>
    /// <param name="trueOnAll"></param>
    /// <returns></returns>
    public static bool Perform<TSource>(this IEnumerable<TSource> sourceList, Func<TSource, bool> perform, bool trueOnAll = false)
    {
        if (sourceList == null)
            return true;
        var result = trueOnAll;
        foreach (var source in sourceList)
        {
            var ok = perform(source);
            if (trueOnAll && !ok)
                result = false;
            else if (!trueOnAll && ok)
                result = true;
        }
        return result;
    }
}