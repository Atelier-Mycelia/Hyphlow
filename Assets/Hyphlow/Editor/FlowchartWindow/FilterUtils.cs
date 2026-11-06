using System;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.EditorExt
{
    public static class FilterUtils
    {
        /// <summary>
        /// Returns all Blocks whose name or command content contains the query.
        /// Also sets each Block’s FilterState to Full, Partial, or None.
        /// </summary>
        public static IList<T> FilterBlocks<T>(IReadOnlyCollection<T> allBlocks, string query) 
            where T: IBlock
        {
            var results = new List<T>();

            bool noQuery = string.IsNullOrEmpty(query);
            if (noQuery)
            {
                #region Makes all of them visible
                foreach (var elem in allBlocks)
                {
                    elem.FilteredState = FilteredState.Full;
                    results.Add(elem);
                }
                #endregion
                return results;
            }

            StringComparison caseInsensitive = StringComparison.OrdinalIgnoreCase;
            foreach (var elem in allBlocks)
            {
                bool nameMatch = elem.BlockName.IndexOf(query, caseInsensitive) >= 0;
                if (nameMatch)
                {
                    elem.FilteredState = FilteredState.Full;
                    results.Add(elem);
                    continue;
                }

                bool contentMatch = false;
                for (int i = 0; i < elem.CommandList.Count; i++)
                {
                    var commandEl = elem.CommandList[i];
                    var searchableContent = commandEl.GetSearchableContent();
                    if (searchableContent.IndexOf(query, caseInsensitive) >= 0)
                    {
                        contentMatch = true;
                        break;
                    }
                }

                if (contentMatch)
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
