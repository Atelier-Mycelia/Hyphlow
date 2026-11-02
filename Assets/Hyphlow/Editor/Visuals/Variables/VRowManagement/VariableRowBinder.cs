using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Ties row handlers to the elements a ListView recycles. Each shell element owns the
    /// handler currently shown in it: binding reuses that handler when the variable's handler
    /// type matches and swaps it through the pool otherwise. Kept free of ListView so the
    /// lifecycle can be exercised without a panel.
    ///
    /// Also the <see cref="IRowEditHost"/> every handler talks to; it forwards to whatever
    /// <see cref="Host"/> is set at the time, so handlers never hold a stale host.
    /// </summary>
    public class VariableRowBinder : IRowEditHost, IDisposable
    {
        public VariableRowBinder(RowVisualHandlerPool pool)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        public IRowEditHost Host { get; set; }
        public RowVisualHandlerPool Pool => _pool;
        public int BoundCount => _bound.Count;

        public VisualElement CreateShell()
        {
            // The shells made here will be the parents of the handlers' root elements
            var shell = new VisualElement { name = "VariableRowContainer" };
            var style = shell.style;
            style.flexDirection = FlexDirection.Column;
            style.position = Position.Relative;
            style.flexGrow = 0;
            style.flexShrink = 0;
            return shell;
        }

        public void Bind(VisualElement shell, IVariable variable)
        {
            if (shell == null || variable == null)
            {
                return;
            }

            IRowVisualHandler handler = HandlerOf(shell);
            Type neededType = _pool.ResolveHandlerType(variable.ContentType);
            if (handler != null && handler.GetType() != neededType)
            {
                Destroy(shell);
                handler = null;
            }

            handler ??= _pool.Take(neededType);
            shell.userData = handler;

            try
            {
                handler.Bind(variable, this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VariableRowBinder] Failed to bind a row for '{variable.Key}' " +
                    $"({variable.ContentType?.Name}): {ex.Message}");
                Debug.LogException(ex);
            }

            _bound.Add(handler);
            if (handler.RowRoot != null && handler.RowRoot.parent != shell)
            {
                shell.Add(handler.RowRoot);
            }
        }

        /// <summary>
        /// Stops the shell's handler from reacting to its variable. The visuals stay in
        /// place for the next Bind.
        /// </summary>
        public void Unbind(VisualElement shell)
        {
            IRowVisualHandler handler = HandlerOf(shell);
            if (handler == null)
            {
                return;
            }

            handler.Unbind();
            _bound.Remove(handler);
        }

        /// <summary>
        /// Hands the shell's handler back to the pool and empties the shell.
        /// </summary>
        public void Destroy(VisualElement shell)
        {
            if (shell == null)
            {
                return;
            }

            IRowVisualHandler handler = HandlerOf(shell);
            if (handler != null)
            {
                _bound.Remove(handler);
                _pool.Return(handler);
            }

            shell.userData = null;
            shell.Clear();
        }

        public static IRowVisualHandler HandlerOf(VisualElement shell)
        {
            return shell?.userData as IRowVisualHandler;
        }

        public bool TryGetHandler(IVariable variable, out IRowVisualHandler handler)
        {
            // Reference equality on purpose: variables may override Equals
            handler = _bound.FirstOrDefault(elem => ReferenceEquals(elem.Variable, variable));
            return handler != null;
        }

        private readonly RowVisualHandlerPool _pool;
        private readonly HashSet<IRowVisualHandler> _bound = new HashSet<IRowVisualHandler>();

        public void KeyEdited(IVariable variable, string newKey) => Host?.KeyEdited(variable, newKey);
        public void ScopeEdited(IVariable variable, AccessScope newScope) => Host?.ScopeEdited(variable, newScope);
        public void ValueEdited(IVariable variable, object newValue) => Host?.ValueEdited(variable, newValue);
        public void RemoveRequested(IVariable variable) => Host?.RemoveRequested(variable);

        public void Dispose()
        {
            Host = null;
            foreach (IRowVisualHandler handler in _bound.ToList())
            {
                handler.Dispose();
            }

            _bound.Clear();
            _pool.Clear();
        }
    }
}
