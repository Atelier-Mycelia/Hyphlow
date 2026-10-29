using AtMycelia.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Type = System.Type;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AtMycelia.Hyphlow
{
    [CreateAssetMenu(fileName = "NewVariableSet",
        menuName = "Atelier Mycelia/Hyphlow/VariableSet")]
    [MovedFrom(true, sourceNamespace: "AtMycelia.Hyphlow", sourceAssembly: "Assembly-CSharp",
        sourceClassName: "VariableSourceAsset")]
    public class VariableSet : ScriptableObject, IReorderableMuscariableSource,
        IForceResetUidHandler, IRefreshable
    {
        [SerializeField] private bool _includeInSaves = true;
        [Tooltip("Whether or not this source's variables can be considered as options in " +
            "the var-select popups.")]
        [SerializeField] private bool _includeInRegistry = true;
        [FormerlySerializedAs("uniqueId")]
        [SerializeField, HideInInspector] private string _uniqueId = string.Empty;
        [SerializeField] private bool _alwaysKeepGuid = true;
        [SerializeField, HideInInspector] private VariableManager _varManager = new VariableManager();
        // Serialized so detached copies stay detached through domain reloads
        [SerializeField, HideInInspector] private bool _isDetached;

        public virtual bool IncludeInRegistry
        {
            get => _includeInRegistry;
        }

        public virtual void ForceResetUid()
        {
            UniqueId = Guid.NewGuid().ToString();
        }

        public bool IncludeInSaves
        {
            get => _includeInSaves;
            set => _includeInSaves = value;
        }

        public string UniqueId
        {
            get => _uniqueId;
            set
            {
                if (!string.IsNullOrEmpty(_uniqueId))
                {
                    Debug.LogWarning($"Warning: Overwriting existing AssetId on VariableSourceAsset {name}.",
                        this);
                }

                string prevId = _uniqueId;
                _uniqueId = value;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssetIfDirty(this);
                    AssetDatabase.Refresh();
                }
#endif
            }
        }

        public virtual bool AlwaysKeepGuid
        {
            get => _alwaysKeepGuid;
            set => _alwaysKeepGuid = value;
        }

        /// <summary>
        /// Whether this is a detached copy (see <see cref="CreateDetachedCopy"/>). Detached
        /// copies share their original's UniqueId, but raise no VarSetSignals and take
        /// no part in Play Mode backups, so the rest of Hyphlow never mistakes them for
        /// the original.
        /// </summary>
        public bool IsDetached
        {
            get
            {
                if (s_creatingDetached)
                {
                    return true;
                }

#if UNITY_EDITOR
                // Someone saved a copy as an asset of its own, which makes it a real set
                if (_isDetached && AssetDatabase.Contains(this))
                {
                    return false;
                }
#endif
                return _isDetached;
            }
        }

        // Set while CreateDetached runs, since the new instance's OnEnable happens before
        // we can flag it
        private static bool s_creatingDetached;

        /// <summary>
        /// Makes a new, empty, in-memory set that's detached from the start. Meant for
        /// sets that don't exist as assets yet (see CreateDetachedCopy for the rest).
        /// Destroy it when done.
        /// </summary>
        public static VariableSet CreateDetached()
        {
            VariableSet created;
            s_creatingDetached = true;
            try
            {
                created = CreateInstance<VariableSet>();
            }
            finally
            {
                s_creatingDetached = false;
            }

            created._isDetached = true;
            created.hideFlags = HideFlags.DontSave;
            return created;
        }

        /// <summary>
        /// Makes an in-memory copy of this set that's safe to edit or hold as a snapshot
        /// without the rest of Hyphlow reacting to it. Use ApplyStateFrom to carry its
        /// state back over. Destroy it when done; it's never saved with the scene or project.
        /// </summary>
        public VariableSet CreateDetachedCopy()
        {
            // Instantiate runs the copy's OnEnable before we'd get the chance to flag it, so
            // the flag goes on this one for the duration, and gets copied over with the rest.
            bool wasDetached = _isDetached;
            _isDetached = true;
            VariableSet copy;
            try
            {
                copy = Instantiate(this);
            }
            finally
            {
                _isDetached = wasDetached;
            }

            copy.name = name;
            copy.hideFlags = HideFlags.DontSave;
            copy.Refresh();
            return copy;
        }

        public IReadOnlyList<IVariable> Variables
        {
            get
            {
                return _varManager.Variables;
            }
        }

        IReadOnlyList<Muscariable> IVariableSource<Muscariable>.Variables
        {
            get
            {
                return _varManager.Variables.OfType<Muscariable>().ToList();
            }
        }

        public string Name
        {
            get => name;
            set
            {
                name = value;
            }
        }

        /// <summary>
        /// Creates and returns a new Muscariable of the content type,
        /// assigning it the passed key and starting value.
        /// Ignores the scope param, as VariableSourceAssets always
        /// have (functionally) global scopes for their vars.
        /// </summary>
        public virtual Muscariable<TContent> AddNewVariableOfContentType<TContent>(string key,
            TContent startingVal = default, AccessScope scope = AccessScope.Private)
        {
            var result = _varManager.AddNewVariable(key, startingVal, AccessScope.Global);
            return (Muscariable<TContent>)result; 
        }

        public virtual Muscariable AddNewVariableOfContentType(Type contentType, string key, object defaultVal,
            AccessScope scope = AccessScope.Private)
        {
            var result = _varManager.AddNewVariableOfContentType(contentType, key, defaultVal, scope);
            return result;
        }

        /// <summary>
        /// If the var is a legacy one, it will be converted to a Muscariable. Returns
        /// the variable added.
        /// </summary>
        public virtual IVariable AddVariable(IVariable var)
        {
            var result = _varManager.AddVariable(var);
            return result;
        }

        public event Action VariablesReordered
        {
            add
            {
                _varManager.Reordered += value;
            }
            remove
            {
                _varManager.Reordered -= value;
            }
        }

        [SerializeField, HideInInspector] protected byte _nextVarID = 1;
        public event Action<IVariable> VariableAdded
        {
            add
            {
                _varManager.VariableAdded += value;
            }
            remove
            {
                _varManager.VariableAdded -= value;
            }
        }

        public Muscariable GetVariableByName(string name, StringComparison strCompare = StringComparison.Ordinal)
        {
            var result = _varManager.GetVariable(name, strCompare);
            return (Muscariable)result;
        }

        public virtual IVariable GetVariable(byte itemID)
        {
            var result = _varManager.GetVariable(itemID);
            return result;
        }

        /// <summary>
        /// Returns all variables in this source that have the specified content type. If strict is false,
        /// variables whose content types are assignable to the specified content type will also be returned. 
        /// If true, only variables whose content types are <i>exactly</i> the one passed will be returned.
        /// </summary>
        public virtual IList<Muscariable> GetVarsByContentType<TContent>(bool strict = false)
        {
            var result = GetVarsByContentType(typeof(TContent), strict);
            return result;
        }

        public virtual IList<Muscariable> GetVarsByContentType(Type contentType, bool strict = false)
        {
            var result = _varManager.GetMultiVariablesOfContentType(contentType, strict);
            return result.OfType<Muscariable>().ToList();
        }

        /// <summary>
        /// Returns all variables in this source that are of the specified type. If strict is false,
        /// variables whose types are assignable to the specified type will also be returned. If 
        /// strict is true, only variables whose types are exactly the one passed will be returned.
        /// 
        /// If you want a list of vars of a specified <i>content</i> type, 
        /// use GetVarsByContentType instead.
        /// 
        /// </summary>
        public virtual IList<TVar> GetVarsByType<TVar>(bool strict = false) where TVar : Muscariable
        {
            return (IList<TVar>)GetVarsByType(typeof(TVar));
        }

        public virtual IList<Muscariable> GetVarsByType(Type varType)
        {
            IList<Muscariable> result = new List<Muscariable>();
            var multiVarsOfType = _varManager.GetMultiVariablesOfType(varType);
            for (int i = 0; i < multiVarsOfType.Count; i++)
            {
                var currentVar = multiVarsOfType[i];
                if (currentVar is Muscariable muscari)
                {
                    result.Add(muscari);
                }
            }
            return result;
        }

        public void ReorderVariables(IList<IVariable> newOrder)
        {
            if (newOrder == null || newOrder.Count == 0) return;

            IList<IVariable> whatWeGot = Variables.ToList();
            var newOrderToPass = newOrder;

            if (newOrder is not IReadOnlyList<IVariable>) // To avoid a crash from the casting below
            {
                newOrderToPass = newOrder.ToList();
            }
            bool sameContents = whatWeGot.SameContentsAs((IReadOnlyList<IVariable>)newOrderToPass);
            if (!sameContents)
            {
                Debug.LogWarning($"VariableSource: ReorderVariables called with a list that " +
                    "doesn't contain the same elements as this source.", this);
                return;
            }
            else
            {
                _varManager.ReorderVariables(newOrder);
            }
        }

        public virtual void RemoveVariable(string key, StringComparison strCompare = StringComparison.Ordinal)
        {
            _varManager.RemoveVariable(key, strCompare);
        }

        public virtual void RemoveVariable(IVariable variable)
        {
            if (variable is not Muscariable muscari)
            {
                string logMessage = $"Cannot remove {variable} (a non-Muscariable) from a VariableSource asset; " +
                    $"it can't hold that in the first place.";
                Debug.LogWarning(logMessage, this);
                return;
            }

            _varManager.RemoveVariable(variable);
        }

        public event Action<IVariable> VariableRemoved
        {
            add
            {
                _varManager.VariableRemoved += value;
            }
            remove
            {
                _varManager.VariableRemoved -= value;
            }
        }

        public virtual void Refresh()
        {
            EnsureValidUniqueId();

            _varManager.VarOwner = this;
            _varManager.Refresh();
            EnsureContentsAreMarkedAsGlobal();
        }

        private void EnsureContentsAreMarkedAsGlobal()
        {
            var vars = Variables;
            for (int i = 0; i < vars.Count; i++)
            {
                var currentVar = vars[i];
                currentVar.Scope = AccessScope.Global;
            }
        }

        public event Action Refreshed
        {
            add
            {
                _varManager.Refreshed += value;
            }
            remove
            {
                _varManager.Refreshed -= value;
            }
        }

        public Muscariable AddVariable(Muscariable toAdd)
        {
            // The manager will handle the post-signaling and the actual addition,
            // so we don't need to do either here.
            var result = _varManager.AddVariable(toAdd);
            return result;
        }

        public void RemoveVariable(Muscariable toRemove)
        {
            _varManager.RemoveVariable(toRemove);
        }

        protected virtual void OnEnable()
        {
            SetSubs(false);
            SetSubs(true);
            _varManager.VarOwner = this;
            if (IsDetached)
            {
                return;
            }
#if UNITY_EDITOR
            if (!AssetDatabase.Contains(this))
            {
                // We don't want to assign IDs to non-assets. At least, not necessarily right when they're created.
                return;
            }
#endif
            EnsureValidUniqueId();
            VarSetSignals.VsaEnabled(this);
        }

        protected virtual void SetSubs(bool on)
        {
            if (on)
            {
                _varManager.VariableAdded += OnPostVarAdded;
                _varManager.VariableRemoved += OnPostVarRemoved;
                _varManager.PreVariableAdded += OnPreVarAdded;
                _varManager.PreVariableRemoved += OnPreVarRemoved;
            }
            else
            {
                _varManager.VariableAdded -= OnPostVarAdded;
                _varManager.VariableRemoved -= OnPostVarRemoved;
                _varManager.PreVariableAdded -= OnPreVarAdded;
                _varManager.PreVariableRemoved -= OnPreVarRemoved;
            }

            EditorSetSubs(on);
        }

        // The manager's own events stay wired up even for detached copies (their own
        // listeners may care); only relaying them to everyone else is skipped.
        private void OnPostVarAdded(IVariable variable)
        {
            if (!IsDetached)
            {
                VarSetSignals.VariableAdded(this, variable);
            }
        }

        private void OnPostVarRemoved(IVariable variable)
        {
            if (!IsDetached)
            {
                VarSetSignals.VariableRemoved(this, variable);
            }
        }

        private void OnPreVarAdded(IVariable variable)
        {
            if (!IsDetached)
            {
                VarSetSignals.PreVariableAdded(this, variable);
            }
        }

        private void OnPreVarRemoved(IVariable variable)
        {
            if (!IsDetached)
            {
                VarSetSignals.PreVariableRemoved(this, variable);
            }
        }

        /// <summary>
        /// Hook for editor-only subscriptions. Restoring sets after Play Mode used to be
        /// done from here, but is now VariableSetPlayModeSnapshots' job (in the editor
        /// assembly). Kept so overrides in subclasses still compile.
        /// </summary>
        protected virtual void EditorSetSubs(bool on)
        {
        }

        protected virtual void EnsureValidUniqueId()
        {
            // Copies mirror their originals' ids, even when those are empty
            bool thisIsTestOnly = SceneManager.GetActiveScene().name.StartsWith("InitTestScene");
            if (thisIsTestOnly || IsDetached)
            {
                return;
            }

            if (string.IsNullOrEmpty(_uniqueId))
            {
                _uniqueId = Guid.NewGuid().ToString();
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        protected virtual void OnDisable()
        {
            SetSubs(false);
            if (!IsDetached)
            {
                VarSetSignals.VsaDisabled(this);
            }
        }

        protected virtual void OnDestroy()
        {
            // Would otherwise report the original (same name, same id) as destroyed
            if (!IsDetached)
            {
                VarSetSignals.VsaDestroyed(name, _uniqueId);
            }
        }

        // Only the body is editor-only, so subclasses' overrides still compile in builds
        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
            if (!AssetDatabase.Contains(this))
            {
                // We don't want to assign IDs to non-assets. At least, not necessarily right when they're created.
                return;
            }

            EnsureValidUniqueId();
#endif
        }

        public bool Contains(IVariable var)
        {
            return _varManager.Contains(var);
        }

        T IVariableSource.GetVariableOfType<T>()
        {
            var result = _varManager.GetVariableOfType<T>();
            return result;
        }

        IVariable IVariableSource.GetVariable(string name, StringComparison strCompare)
        {
            return GetVariableByName(name, strCompare);
        }

        T IVariableSource.GetVariableOfType<T>(string name, StringComparison strCompare)
        {
            var result = _varManager.GetVariableOfType<T>(name, strCompare);
            return result;
        }

        public IVariable GetVariableOfType(Type type, string name, StringComparison strCompare = StringComparison.Ordinal)
        {
            var result = _varManager.GetVariableOfType(type, name, strCompare);
            return result;
        }

#if UNITY_EDITOR

        Muscariable IMuscariableSource.AddNewVariableOfContentType<TContentType>(string k, TContentType defaultVal, AccessScope scope)
        {
            return ((IMuscariableSource)_varManager).AddNewVariableOfContentType(k, defaultVal, scope);
        }

        public Muscariable AddNewVariableOfContentType(Type contentType, string key)
        {
            return (_varManager).AddNewVariableOfContentType(contentType, key);
        }

        /// <summary>
        /// Makes this set's settings and variables match the source's. Variables both sets
        /// have (same ItemId and type) keep their instances here, so registries, owners
        /// and open UI that point at them stay valid; only their state is overwritten.
        /// Variables only the source has get copied in, and ones it lacks get removed,
        /// with the usual signals. Order ends up the same as the source's.
        /// <br></br><br></br>
        /// This set's identity is left alone: its name, UniqueId and asset file. This
        /// marks the set dirty, but doesn't save it.
        /// </summary>
        public virtual void ApplyStateFrom(VariableSet source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (ReferenceEquals(source, this))
            {
                return;
            }

            _includeInSaves = source._includeInSaves;
            _includeInRegistry = source._includeInRegistry;
            _alwaysKeepGuid = source._alwaysKeepGuid;
            _nextVarID = source._nextVarID;
            _varManager.ApplyStateFrom(source._varManager);

            Refresh();
            EditorUtility.SetDirty(this);
            if (!IsDetached)
            {
                VarSetSignals.StateApplied(this);
            }
        }

#endif
    }

    public interface IForceResetUidHandler
    {
        void ForceResetUid();
    }

    public interface IVarConvertible<TTargetType> where TTargetType : IVariable
    {
        TTargetType ToVar();
    }

    
}