using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;


namespace aggregator.Engine
{
    public static class Extensions
    {
        public static string GetTeamProject(this WorkItem workItem)
        {
            return workItem.Fields.GetCastedValueOrDefault("System.TeamProject", default(string));
        }

        // source https://stackoverflow.com/a/22222439/100864
        public static IEnumerable<IEnumerable<T>> Paginate<T>(this IEnumerable<T> source, int pageSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (pageSize <= 1) throw new ArgumentException("Must be greater than 1", nameof(pageSize));

            // see https://rules.sonarsource.com/csharp/RSPEC-4456
            return PaginateIterator(source, pageSize);
        }

        private static IEnumerable<IEnumerable<T>> PaginateIterator<T>(this IEnumerable<T> source, int pageSize)
        {
            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var currentPage = new List<T>(pageSize)
                    {
                        enumerator.Current
                    };

                while (currentPage.Count < pageSize && enumerator.MoveNext())
                {
                    currentPage.Add(enumerator.Current);
                }
                yield return new ReadOnlyCollection<T>(currentPage);
            }
        }
    }
}
