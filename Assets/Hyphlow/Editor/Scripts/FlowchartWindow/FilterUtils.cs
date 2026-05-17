using System;
using System.Collections.Generic;
using System.Linq;

namespace AtMycelia.Hyphlow.EditorUtils
{
    public static class FilterUtils
    {
        /// <summary>
        /// Returns all Blocks whose name or command content contains the query.
        /// Also sets each Block’s FilterState to Full, Partial, or None.
        /// </summary>
        public static IList<T> FilterBlocks<T>(IReadOnlyCollection<T> allBlocks, string query) where T: IBlock
        {
            var results = new List<T>();

            // No query ? show everything (reset states to Full)
            if (string.IsNullOrEmpty(query))
            {
                foreach (var elem in allBlocks)
                {
                    elem.FilteredState = FilteredState.Full;
                    results.Add(elem);
                }
                return results;
            }

            query = query.ToLowerInvariant();

            foreach (var elem in allBlocks)
            {
                bool nameMatch = elem.BlockName
                    .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

                bool contentMatch = elem.CommandList
                    .Any(commandEl => commandEl.GetSearchableContent()
                        .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);

                if (nameMatch)
                {
                    elem.FilteredState = FilteredState.Full;
                    results.Add(elem);
                }
                else if (contentMatch)
                {
                    elem.FilteredState = FilteredState.Partial;
                    results.Add(elem);
                }
                else
                {
                    elem.FilteredState = FilteredState.None;
                }
            }

            return results;
        }
    }
}
