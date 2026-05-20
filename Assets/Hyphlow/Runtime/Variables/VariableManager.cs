using AtMycelia.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityObj = UnityEngine.Object;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    public interface IVariableManager : IReorderableVariableSource, IDisposable
    {
        /// <summary>
        /// Sets the values of all the vars this is managing back 
        /// to their initial values, as if they were just created.
        /// </summary>
        void ResetAllVars();
    }

    /// <summary>
    /// This class is responsible for the maintenance and upkeep of a collection of variables.
    /// It provides functionality to add, remove, retrieve, and reorder variables, as well as to ensure
    /// that each variable has a unique and valid ID. The manager also handles initialization and
    /// cleanup of variables, and it can notify listeners when variables are added or removed.
    /// </summary>
    [Serializable]
    [MovedFrom(true, "AtMycelia.Hyphlow", "AtMycelia.Amanita.Core")]
    public sealed class VariableManager : IVariableManager, IMuscariableSource,
        IReorderableMuscariableSource
    {
        // Note: Unity does not serialize readonly fields, even if they're plain 
        // old Lists of types it otherwise serializes just fine. So, we have
        // to make these non-readonly and just be careful not to reassign them.
        [FormerlySerializedAs("muscariables")]
        [SerializeReference] private List<Muscariable> _muscariables = new();
        [FormerlySerializedAs("legacyVariables")]
        [SerializeField] private List<Variable> _legacyVariables = new();
        [FormerlySerializedAs("nextValidVarID")]
        [SerializeField] private byte _nextValidVarID = 1;
        [FormerlySerializedAs("isInitted")]
        [SerializeField] private bool _isInitted = false;

        public void Initialize()
        {
            if (IsInitted)
            {
                Debug.LogWarning($"VariableManager for {Name} is already initialized. " +
                    $"Reinitializing will clear all variables and reset the manager. " +
                    $"Proceeding with reinitialization.");
            }
            Clear();
            IsInitted = true;
        }

        public bool IsInitted
        {
            get => _isInitted;
            private set => _isInitted = value;
        }

        /// <summary>
        /// Removes all variables from this manager. Note that this will fire the 
        /// appropriate events for each variable removed, so if you have any listeners
        /// for those events, they will be notified of each individual removal. 
        /// <br></br> <br></br>
        /// If you just want to clear the variables without 
        /// firing events, you can clear the _legacyVariables and _muscariables lists 
        /// directly and then call Refresh() to update the lookup and ensure valid IDs, 
        /// but be aware that doing so will not send any signals about the variables 
        /// being removed, which could lead to issues if you have other parts of your 
        /// code that rely on those signals.
        /// </summary>
        public void Clear(bool triggerSignals = true)
        {
            // Remove them one by one so the right events fire
            while (_legacyVariables.Count > 0)
            {
                RemoveLegacyVarAtIndex(0, triggerSignals);
            }

            while (_muscariables.Count > 0)
            {
                RemoveMuscariAtIndex(0, triggerSignals);
            }

            _nextValidVarID = 1;
        }

        public IVariable RemoveLegacyVarAtIndex(int index, bool triggerSignals = true)
        {
            if (index < 0 || index >= _legacyVariables.Count)
            {
                string errorMessage = $"Index {index} is out of range for legacy variables. Valid range is " +
                    $"0 to {_legacyVariables.Count - 1}. No variable removed.";

                throw new IndexOutOfRangeException(errorMessage);
            }

            Variable toRemove = _legacyVariables[index];
            if (triggerSignals)
            {
                RemoveFromCaches(toRemove, triggerSignals);
            }
            return toRemove;
        }

        private void RemoveFromCaches(IVariable toRemove, bool triggerSignals = true)
        {
            if (triggerSignals)
            {
                PreVariableRemoved(toRemove);
            }
            _legacyVariables.RemoveByReference(toRemove as Variable);
            _muscariables.RemoveByReference(toRemove as Muscariable);

            _lookup.Remove(toRemove.ItemId);
            MarkOwnerAsDirty();
            if (triggerSignals)
            {
                VariableRemoved(toRemove);
                VariableSignals.VariableRemoved(toRemove);
            }
        }

        private Dictionary<byte, IVariable> _lookup = new();
        // ^For faster retrieval by ID. Must be kept in sync with the lists.
        public event Action<IVariable> PreVariableRemoved = delegate { };

        public event Action<IVariable> VariableRemoved = delegate { };

        public IVariable RemoveMuscariAtIndex(int index, bool triggerSignals = true)
        {
            if (index < 0 || index >= _muscariables.Count)
            {
                string errorMessage = $"Index {index} is out of range for muscariables. Valid range is " +
                    $"0 to {_muscariables.Count - 1}. No variable removed.";
                throw new IndexOutOfRangeException(errorMessage);
            }
            Muscariable toRemove = _muscariables[index];
            RemoveFromCaches(toRemove);
            return toRemove;
        }

        public void Initialize(IList<Muscariable> initMuscaris, IList<Variable> initLegacies)
        {
            Initialize();
            AddMultiVars(initMuscaris);

            // For the sake of backwards compatibility with Fungus projects, we won't convert 
            // any legacies we're being initialized with. At least, not here.
            for (int i = 0; i < initLegacies.Count; i++)
            {
                Variable legacy = initLegacies[i];
                _legacyVariables.Add(legacy);
                RegisterIntoVarLookup(new[] { legacy });
            }
        }

        public void AddMultiVars(IEnumerable<IVariable> toAdd)
        {
            EnsureInitialized();
            foreach (var elem in toAdd)
            {
                AddVariable(elem);
            }
        }

#if UNITY_EDITOR
        public void MigrateLegacyVariables(IList<Variable> oldLegacyVariables)
        {
            EnsureInitialized();//

            bool addedAny = false;

            if (oldLegacyVariables != null)
            {
                for (int i = 0; i < oldLegacyVariables.Count; i++)
                {
                    var legacyVar = oldLegacyVariables[i];
                    if (legacyVar == null || IsRegistered(legacyVar))
                    {
                        continue;
                    }

                    EnsureValidIdFor(legacyVar);

                    _legacyVariables.Add(legacyVar);
                    _lookup[legacyVar.ItemId] = legacyVar;
                    addedAny = true;
                }
            }

            if (addedAny)
            {
                Refresh();
            }
        }

        private bool IsRegistered(IVariable variable)
        {
            if (variable == null)
            {
                return false;
            }

            foreach (var existing in _lookup.Values)
            {
                if (ReferenceEquals(existing, variable))
                {
                    return true;
                }
            }

            return false;
        }
#endif

        /// <summary>
        /// Adds a variable to the manager before returning it. If the variable is already registered, it 
        /// will not be added again. If the variable is a legacy var, a Muscariable version of it will 
        /// be created, added, and returned instead.
        /// </summary>
        public IVariable AddVariable(IVariable toAdd)
        {
            EnsureInitialized();
            Muscariable result = AddAsMuscari(toAdd);
            return result;
        }

        /// <summary>
        /// Adds a variable to the manager, converting it to a Muscari beforehand as appropriate. 
        /// Returns the Muscariable that was added, or null if the variable was already registered 
        /// and thus not added.
        /// </summary>
        public Muscariable AddAsMuscari(IVariable toAdd)
        {
            EnsureInitialized();
            if (IsRegistered(toAdd))
            {
                return null;
            }

            Muscariable muscari = toAdd.ToMuscariable();
            Integrate(muscari);
            return muscari;
        }


        /// <summary>
        /// Adds the given Muscariable to the caches, ensuring it has a valid ID and key, 
        /// and setting its owner and parent flowchart references. Also sends the signal
        /// for var-adding.
        /// </summary>
        private void Integrate(Muscariable toAdd)
        {
            toAdd.Key = UniqueKeyGenerator.GetUniqueKeyFor(toAdd.Key, Variables, null);
            toAdd.ParentFlowchart = VarOwner as Flowchart;
            toAdd.Owner = _varOwner;

            AddToCaches(toAdd);
        }

        private void AddToCaches(IVariable toAdd, bool triggerSignals = true)
        {
            if (triggerSignals)
            {
                PreVariableAdded(toAdd);
            }

            if (toAdd is Muscariable)
            {
                _muscariables.Add(toAdd as Muscariable);
            }
            else if (toAdd is Variable)
            {
                _legacyVariables.Add(toAdd as Variable);
            }

            EnsureValidIdFor(toAdd);
            _lookup[toAdd.ItemId] = toAdd;
            MarkOwnerAsDirty();

            if (triggerSignals)
            {
                VariableAdded(toAdd);
                VariableSignals.VariableAdded(toAdd);
            }
        }

        public event Action<IVariable> PreVariableAdded = delegate { };
        public event Action<IVariable> VariableAdded = delegate { };

        /// <summary>
        /// Meant to be called through Unity's OnEnable message. This function ensures that 
        /// all variables have valid IDs, and initializes them with their start values if 
        /// the application is playing. It also registers the manager with the 
        /// SceneObjectReferenceRestorer so that it can restore references for this manager 
        /// when scenes are loaded. This is important because if the manager is disabled, 
        /// it may be in a state where it can't properly restore references 
        /// (for example, if it's been destroyed but not yet removed from the scene), 
        /// and trying to do so could cause errors.
        /// </summary>
        public void OnEnable()
        {
            if (VarOwner is UnityObj ownerUnityObj)
            {
                EnsureValidAndUniqueIdsForAllOurVars();
                foreach (var elem in _lookup.Values)
                {
                    elem.Init(elem.BoxedValue);
                }
            }
            Refresh();
        }

        /// <summary>
        /// Meant to be called through Unity's OnDisable message. This function unregisters the 
        /// manager from the SceneObjectReferenceRestorer so that it won't try to restore 
        /// references for this manager while it's disabled. This is important because if 
        /// the manager is disabled, it may be in a state where it can't properly restore 
        /// references (for example, if it's been destroyed but not yet removed from the 
        /// scene), and trying to do so could cause errors.
        /// </summary>
        public void OnDisable()
        {
            // No-op for now, but we might want to add some cleanup logic here in the
            // future, and if we do, this is where it should go.
        }

        public void Refresh()
        {
            RemoveAllNulls();
            _lookup ??= new Dictionary<byte, IVariable>();
            _lookup.Clear();
            RegisterIntoVarLookup(_muscariables);
            RegisterIntoVarLookup(_legacyVariables);
            EnsureValidAndUniqueIdsForAllOurVars();
            Refreshed();
        }

        private void RemoveAllNulls()
        {
            _muscariables.RemoveAll(var => var == null);
            _legacyVariables.RemoveAll(var => var == null);
        }

        public event Action Refreshed = delegate { };

        private void RegisterIntoVarLookup(IEnumerable<IVariable> varsToRegister)
        {
            foreach (var elem in varsToRegister)
            {
                EnsureValidIdFor(elem);
                _lookup[elem.ItemId] = elem;
            }
        }

        private void EnsureValidIdFor(IVariable iVar)
        {
            // It is possible that the var we're given is already registered under a valid ID.
            // In that case, we want to ignore it.
            _lookup.TryGetValue(iVar.ItemId, out IVariable varWithThatId);
            bool alreadyRegistered = varWithThatId != null && ReferenceEquals(varWithThatId, iVar);
            if (alreadyRegistered)
            {
                return;
            }

            while (iVar.ItemId == Muscariable.InvalidId || _lookup.ContainsKey(iVar.ItemId))
            {
                iVar.ItemId = NextValidVarID();
            }
        }

        /// <summary>
        /// Validates the IDs of all variables in this manager, ensuring that each 
        /// one has a unique and valid ID.
        /// </summary>
        public void EnsureValidAndUniqueIdsForAllOurVars()
        {
            var varsToCheck = Variables;
            for (int i = 0; i < varsToCheck.Count; i++)
            {
                var elem = varsToCheck[i];
                EnsureValidIdFor(elem);
                _lookup[elem.ItemId] = elem;
            }
        }

        /// <summary>
        /// Returns the next valid variable ID for a new variable,
        /// incrementing the internal counter for the next valid ID in the process.
        /// </summary>
        private byte NextValidVarID()
        {
            if (_nextValidVarID == byte.MaxValue)
            {
                _nextValidVarID = 1;
            }

            byte toReturn = _nextValidVarID;
            _nextValidVarID++;
            return toReturn;
        }

        /// <summary>
        /// Returns a defensive copy of the list of variables in this manager. Modifying
        /// the returned list will not modify this manager's internal list. However, you
        /// can still modify the variables themselves, since the ones in the returned
        /// list are the same instances as the ones in this manager.
        /// </summary>
        public IReadOnlyList<IVariable> Variables
        {
            get
            {
                var result = new List<IVariable>(_muscariables.Count + _legacyVariables.Count);
                result.AddRange(_muscariables);
                result.AddRange(_legacyVariables);
                return result;
            }
        }

        /// <summary>
        /// The owner of the variables this manager handles. This is used for determining things like
        /// which Flowchart a variable belongs to, and for sending signals about variable changes.
        /// By default, this is set to the manager itself, but it can be set to something else if
        /// needed (for example, if this here is being used as a sub-manager for another object
        /// that should be considered the real owner).
        /// <br></br><br></br>
        /// This property's setter makes sure to update the variables' Owner fields to that of the
        /// value you're setting. Bookkeeping and whatnot.
        /// </summary>
        public IVariableSource VarOwner
        {
            get
            {
                _varOwner ??= this;
                return _varOwner;
            }
            set
            {
                if (!ReferenceEquals(_varOwner, value))
                {
                    _varOwner = value;
                    _varOwner ??= this;

                    foreach (var elem in _lookup.Values)
                    {
                        if (elem is not Variable)
                        {
                            elem.Owner = _varOwner;
                        }
                    }
                }
            }
        }

        private IVariableSource _varOwner;

        public void RemoveVariable(IVariable toRemove)
        {
            bool alreadyRegistered = _lookup.Values.Contains(toRemove);
            if (!alreadyRegistered)
            {
                return;
            }

            RemoveFromCaches(toRemove);
        }

        public void RemoveVariable(string name, StringComparison strCompare = StringComparison.Ordinal)
        {
            var toRemove = GetVariable(name, strCompare);
            if (toRemove != null)
            {
                RemoveVariable(toRemove);
            }
        }

        public IVariable GetVariable(byte id)
        {
            if (_lookup == null || _lookup.Count == 0)
            {
                Refresh();
            }

            _lookup.TryGetValue(id, out IVariable result);
            return result;
        }

        public bool Contains(IVariable var)
        {
            return _lookup.TryGetValue(var.ItemId, out IVariable found) && found == var;
        }

        public void ResetAll()
        {
            foreach (var variable in _lookup.Values)
            {
                variable.OnReset();
            }
        }


        /// <summary>
        /// Gets a variable by name, returning it as the specified generic type if it is of that type. Null otherwise.
        /// </summary>
        public IVariable<TContent> GetVariable<TContent>(string name, StringComparison strCompare = StringComparison.Ordinal)
        {
            var result = _lookup.Values.FirstOrDefault(var => var.Key.Equals(name, strCompare));
            return result as IVariable<TContent>;
        }

        public Muscariable AddNewVariableOfContentType<T>(string key, T defaultValue,
            AccessScope scope = AccessScope.Private)
        {
            return AddNewVariableOfContentType(typeof(T), key, defaultValue, scope);
        }

        public Muscariable AddNewVariableOfContentType(Type contentType, string key,
            object defaultValue, AccessScope scope = AccessScope.Private)
        {
            EnsureInitialized();
            Muscariable muscaVar = VariableFactory.CreateByContentType(contentType, null);
            muscaVar.BoxedValue = defaultValue;
            muscaVar.Scope = scope;
            Integrate(muscaVar);
            return muscaVar;
        }

        public Muscariable AddVariable(Muscariable toAdd)
        {
            EnsureInitialized();
            return AddAsMuscari(toAdd);
        }

        public void RemoveVariable(Muscariable toRemove)
        {
            RemoveVariable(toRemove as IVariable);
        }

        /// <summary>
        /// Gets a variable by its ID, returning it as the specified generic type 
        /// if it is of that type. Null otherwise.
        /// </summary>
        public T GetVariable<T>(byte itemId) where T : class, IVariable
        {
            _lookup.TryGetValue(itemId, out IVariable found);
            T result = found as T;
            return result;
        }

        /// <summary>
        /// Returns a list of the variables this manager has that are of the 
        /// specified variable type. If you just want to get variables of a
        /// certain <i>content</i> type, use GetMultiVariablesOfContentType instead.
        /// </summary>
        public IList<T> GetMultiVariablesOfType<T>(bool strict = false) where T : IVariable
        {
            var result = GetMultiVariablesOfType(typeof(T), strict)
                .OfType<T>()
                .ToList();
            return result;
        }

        /// <summary>
        /// Returns a list of the variables this manager has that are of the 
        /// specified variable type. If strict is true, only variables whose 
        /// type is <i>exactly</i> varType will be returned. If strict is false, 
        /// variables whose type is varType or any subclass thereof will 
        /// be returned.
        /// <br></br> <br></br>
        /// If you just want to get variables of a certain <i>content</i> type,
        /// use GetMultiVariablesOfContentType instead.
        /// </summary>
        public IList<IVariable> GetMultiVariablesOfType(Type varType, bool strict = false)
        {
            var result = _lookup.Values.Where(IsMatch).ToList();
            bool IsMatch(IVariable var)
            {
                if (strict)
                {
                    return var.GetType() == varType;
                }
                else
                {
                    return varType.IsAssignableFrom(var.GetType());
                }
            }
            return result;
        }

        public IList<T> GetMultiVariablesOfContentType<T>()
        {
            var result = GetMultiVariablesOfContentType(typeof(T)).OfType<T>().ToList();
            return result;
        }

        public IList<IVariable> GetMultiVariablesOfContentType(Type contentType, bool strict = false)
        {
            return _lookup.Values.Where(IsMatch).ToList();

            bool IsMatch(IVariable var)
            {
                if (strict)
                {
                    return var.ContentType == contentType;
                }
                else
                {
                    return contentType.IsAssignableFrom(var.ContentType);
                }
            }
        }

        public TVarType AddNewMuscari<TValueType, TVarType>(string key = "", TValueType initValue = default,
            AccessScope scope = AccessScope.Private) where TVarType : Muscariable<TValueType>, new()
        {
            EnsureInitialized();
            TVarType result = new TVarType();
            result.Value = initValue;
            result.Scope = scope;
            result.Key = key;
            Integrate(result);
            return result;
        }

        public byte NextId { get; }

        public string UniqueId
        {
            get
            {
                if (VarOwner != this)
                {
                    return VarOwner.UniqueId;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Returns the total number of variables in this manager. You'd best use this instead of Variables.Count,
        /// since that property returns a defensive list instead of the actual one. Using that to get the 
        /// var-count can get expensive if this have a lot of variables and are calling it frequently.
        /// This property gets you the count without the extra overhead.
        /// </summary>
        public int VariableCount => _lookup.Count;

        IReadOnlyList<Muscariable> IVariableSource<Muscariable>.Variables => Variables.Cast<Muscariable>().ToList();

        public string Name
        {
            get => _varOwner.Name;
            set
            {
                string warningMessage = $"Attempted to set the name of VariableManager for {_varOwner?.Name}. " +
                    "This is not allowed, since the manager's name is determined by its owner. " +
                    "The name will remain unchanged.";
                Debug.LogWarning(warningMessage);
            }
        }

        public IVariable<TValHeld> AddNewVariable<TValHeld>(string key,
            TValHeld value = default,
            AccessScope scope = AccessScope.Private)
        {
            EnsureInitialized();
            Type valueType = typeof(TValHeld);

            IVariable<TValHeld> newVar = VariableFactory.CreateByContentType(valueType) as IVariable<TValHeld>;

            newVar.Key = UniqueKeyGenerator.GetUniqueKeyFor(key, Variables);
            newVar.Value = value;
            newVar.Scope = scope;
            newVar.ItemId = NextValidVarID();

            IVariable toRegister = newVar;
            AddVariable(toRegister);

            if (Application.IsPlaying(VarOwner as UnityObj))
            {
                newVar.Init(value);
            }

            VariableAdded(toRegister);

            return newVar;
        }

        /// <summary>
        /// This function exists to help make sure we don't lose our vars during any setup process (especially
        /// those in unit tests). This should be called at the beginning of any public function that modifies
        /// the variables in any way, to ensure that if we haven't been initialized yet for some reason, 
        /// we will be before we try to do anything with the vars. 
        /// 
        /// This is especially important for functions that might be called from outside the manager, since 
        /// we can't guarantee that the caller will have called Initialize() first. It's less crucial for 
        /// private functions that are only called from other functions in this class, since we can just 
        /// make sure to call EnsureInitialized() at the beginning of those public functions, but it 
        /// doesn't hurt to be extra safe.
        /// </summary>
        private void EnsureInitialized()
        {
            if (IsInitted)
            {
                return;
            }

            Refresh();
            UpdateNextValidId();
            IsInitted = true;
        }

        private void UpdateNextValidId()
        {
            byte maxIdInUse = 0;
            foreach (var elem in _lookup.Values)
            {
                if (elem.ItemId > maxIdInUse)
                {
                    maxIdInUse = elem.ItemId;
                }
            }
            _nextValidVarID = (byte)(maxIdInUse + 1);
        }


        public T GetVariableOfType<T>() where T : class, IVariable
        {
            var result = _lookup.Values.OfType<T>().FirstOrDefault();
            return result;
        }

        IVariable IVariableSource.GetVariable(string name, StringComparison strCompare)
        {
            return GetVariable(name, strCompare);
        }

        public IVariable GetVariable(string name, StringComparison strCompare = StringComparison.Ordinal)
        {
            var result = _lookup.Values.FirstOrDefault(var => var.Key.Equals(name, strCompare));
            return result;
        }

        public T GetVariableOfType<T>(string name, StringComparison strCompare = StringComparison.Ordinal) where T : class, IVariable
        {
            return GetVariableOfType(typeof(T), name, strCompare) as T;
        }

        public IVariable GetVariableOfType(Type type, string name, StringComparison strCompare = StringComparison.Ordinal)
        {
            IVariable result = null;
            var found = GetVariable(name, strCompare);
            if (found != null && type.IsAssignableFrom(found.GetType()))
            {
                result = found;
            }
            return result;
        }

        public void ReorderVariables(IList<IVariable> newlyOrderedVars)
        {
            if (newlyOrderedVars == null || newlyOrderedVars.Count == 0)
            {
                return;
            }

            var orderedSnapshot = newlyOrderedVars.ToList();
            var whatWeGot = _lookup.Values.ToList();
            if (!orderedSnapshot.SameContentsAs(whatWeGot))
            {
                Debug.LogWarning("Attempted to reorder variables with a list that doesn't have the same " +
                    "contents as the current variables. Reorder aborted.");
                return;
            }

            Clear();
            AddMultiVars(orderedSnapshot);
            Reordered();
        }

        public event Action Reordered = delegate { };

        public void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                foreach (var variable in _lookup.Values)
                {
                    variable.Init(variable.BoxedValue);
                    // ^To accomodate any changes that might have been made to the variables while in edit mode, since those changes won't be serialized and thus would be lost when entering play mode if we didn't do this.
                }
            }
        }

        public void RemoveAll(Predicate<IVariable> match)
        {
            var toRemove = _lookup.Values.Where(var => match(var)).ToList();

            foreach (var elem in toRemove)
            {
                RemoveVariable(elem);
            }
        }

        public Muscariable AddNewVariableOfContentType(Type contentType, string key)
        {
            var result = VariableFactory.CreateByContentType(contentType, null);
            result.Key = key;
            Integrate(result);
            return result;
        }

        private void MarkOwnerAsDirty()
        {
#if UNITY_EDITOR
            if (VarOwner is UnityObj unityObj)
            {
                EditorUtility.SetDirty(unityObj);
            }
#endif
        }

        public void Dispose()
        {
            Clear();
            GetRidOfEvents();
        }

        private void GetRidOfEvents()
        {
            VariableAdded = null;
            VariableRemoved = null;
            PreVariableAdded = null;
            PreVariableRemoved = null;
            Refreshed = null;
            Reordered = null;
        }

        public void ResetAllVars()
        {
            for (int i = 0; i < _lookup.Values.Count; i++)
            {
                var variable = _lookup.Values.ElementAt(i);
                variable.OnReset();
            }
        }
    }
}