using System;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Lightweight list pool to avoid allocations when diffing block collections.
    /// </summary>
    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> _pool = new();

        public static List<T> Get()
        {
            return _pool.Count > 0 ? 
                _pool.Pop() : 
                new List<T>();
        }

        public static void Release(List<T> list)
        {
            list.Clear();
            _pool.Push(list);
        }

        public struct DisposableList : IDisposable
        {
            private List<T> _list;

            public DisposableList(List<T> list)
            {
                this._list = list;
            }

            public static implicit operator List<T>(DisposableList disposable) => disposable._list;

            public void Dispose()
            {
                if (_list != null)
                {
                    Release(_list);
                    _list = null;
                }
            }
        }

        public static DisposableList Get(out List<T> list)
        {
            list = Get();
            return new DisposableList(list);
        }
    }

}