using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
#endif

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Maintains a registry of all available variables from various sources accessible in the scene.
    /// </summary>
    public static class VariableRegistry
    {
        [InitializeOnLoadMethod]
        private static void InitializeRegistry()
        {
            ToggleSubs(false);
            ToggleSubs(true);

            InitializeDelayed();
        }

        private static void ToggleSubs(bool on)
        {
            ToggleEditorSubs(on);
            ToggleRuntimeSubs(on);
        }

        private static void ToggleEditorSubs(bool on)
        {
#if UNITY_EDITOR
            if (on)
            {
                Selection.selectionChanged += OnSelectionChanged;
                AssemblyReloadEvents.afterAssemblyReload += InitializeDelayed;
                EditorSceneManager.sceneOpened += OnSceneOpened;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }
            else
            {
                Selection.selectionChanged -= OnSelectionChanged;
                AssemblyReloadEvents.afterAssemblyReload -= InitializeDelayed;
            }
#endif
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                Rebuild();
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Rebuild();
        }

        private static void OnSelectionChanged()
        {
#if UNITY_EDITOR
            var selected = Selection.activeGameObject;

            if (selected != null && selected.TryGetComponent<Flowchart>(out var fc))
            {
                Rebuild(fc);
            }
#endif
        }

        private static void InitializeDelayed()
        {
            EditorApplication.delayCall += () =>
            {
                Rebuild();
            };
        }

        private static void ToggleRuntimeSubs(bool on)
        {
            if (on)
            {
                FlowchartSignals.VariableAdded += OnVarAddedOrRemoved;
                FlowchartSignals.VariableRemoved += OnVarAddedOrRemoved;
                FlowchartRegistry.FullRefreshed += OnFcRegFullRefreshed;
                FlowchartSignals.FlowchartDestroyed += OnFlowchartDestroyed;
                VariableSignals.PostValueChange += OnVariableValueChanged;

                VsaSignals.VsaEnabled -= OnVsaChanged;
                VsaSignals.VsaDisabled -= OnVsaChanged;
                VsaSignals.VariableAdded -= OnAnyVariableChanged;
                VsaSignals.VariableRemoved -= OnAnyVariableChanged;
            }
            else
            {
                FlowchartSignals.VariableAdded -= OnVarAddedOrRemoved;
                FlowchartSignals.VariableRemoved -= OnVarAddedOrRemoved;
                FlowchartRegistry.FullRefreshed -= OnFcRegFullRefreshed;
                FlowchartSignals.FlowchartDestroyed -= OnFlowchartDestroyed;
                VariableSignals.PostValueChange -= OnVariableValueChanged;

                VsaSignals.VsaEnabled -= OnVsaChanged;
                VsaSignals.VsaDisabled -= OnVsaChanged;
                VsaSignals.VariableAdded -= OnAnyVariableChanged;
                VsaSignals.VariableRemoved -= OnAnyVariableChanged;
            }
        }

        private static void OnVsaChanged(VariableSourceAsset asset)
        {
            Rebuild();
        }

        private static void OnAnyVariableChanged(VariableSourceAsset _, IVariable _2)
        {
            Rebuild();
        }

        private static void OnVariableValueChanged(IVariable variable, object arg2)
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                EditorApplication.delayCall += () =>
                {
                    if (variable == null)
                    {
                        return;
                    }
                    if (Application.isPlaying)
                    {
                        return; // We only want to respond to var value changes in the editor,
                                // since that's the only time we care about keeping the registry's
                                // values up to date with the actual variable values in the scene.
                    }
                    OnSelectionChanged();
                };
#endif
            }
            else
            {
                Rebuild();
            }
        }


        private static void OnFlowchartDestroyed(Flowchart flowchart)
        {
            Rebuild();
        }

        private static void OnFcRegFullRefreshed()
        {
            Rebuild();
        }

        private static void OnVarAddedOrRemoved(Flowchart flowchart, IVariable _)
        {
            Rebuild(flowchart);
        }

        public static void Rebuild(IVariableSource localSource = null)
        {
            RefreshSources();
            _registeredVars.Clear();
            _varsByType.Clear();

            RegisterLocalVars();
            void RegisterLocalVars()
            {
                if (localSource != null)
                {
                    IReadOnlyList<IVariable> localVars = localSource.Variables;
                    for (int i = 0; i < localVars.Count; i++)
                    {
                        IVariable toRegister = localVars[i];
                        string key = string.Format(_localSourceKeyFormat,
                            localSource.Name, toRegister.Key);
                        Register(key, toRegister);
                    }
                }
            }

            RegisterOtherNonGlobalSourceVars();
            void RegisterOtherNonGlobalSourceVars()
            {
                // At least for now, these will only be flowcharts. Why? Those are the
                // only other variable sources that we expect to exist in a scene.
                IReadOnlyList<Flowchart> otherFcs = FindFlowchartsToGoThrough();
                IReadOnlyList<Flowchart> FindFlowchartsToGoThrough()
                {
                    IReadOnlyList<Flowchart> cachedFcs = FlowchartRegistry.GetFlowcharts();
                    List<Flowchart> toGoThrough = new List<Flowchart>(cachedFcs.Count);

                    for (int i = 0; i < cachedFcs.Count; i++)
                    {
                        var fc = cachedFcs[i];
                        bool shouldConsider = fc != null && !ReferenceEquals(fc, localSource);
                        if (shouldConsider)
                        {
                            toGoThrough.Add(fc);
                        }
                    }

                    return toGoThrough;
                }

                for (int i = 0; i < otherFcs.Count; i++)
                {
                    Flowchart otherElem = otherFcs[i];
                    IReadOnlyList<IVariable> otherVars = otherElem.Variables;
                    for (int j = 0; j < otherVars.Count; j++)
                    {
                        IVariable toRegister = otherVars[j];
                        bool isVisible = (toRegister.Scope & AccessScopeDefaults.VisibleToOutsiders)
                            != 0;
                        if (!isVisible)
                        {
                            continue;
                        }

                        // To make it clear to users these variables are _not_ local to the
                        // source they're editing from, we prefix said vars with their owners'
                        // names based on a specific format.
                        string key = string.Format(_nonLocalFlowchartKeyFormat,
                            otherElem.gameObject.name, toRegister.Key);
                        Register(key, toRegister);
                    }
                }
            }

            RegisterGlobals();
            void RegisterGlobals()
            {
                for (int i = 0; i < _registeredSources.Count; i++)
                {
                    VariableSourceAsset source = _registeredSources[i];
                    IReadOnlyList<IVariable> sourceVariables = source.Variables;
                    for (int j = 0; j < sourceVariables.Count; j++)
                    {
                        IVariable toRegister = sourceVariables[j];
                        string key = string.Format(_globalSourceKeyFormat, source.name, toRegister.Key);
                        Register(key, toRegister);
                        bool isLegacyVariable = toRegister is Variable;
                        if (!isLegacyVariable)
                        {
                            toRegister.Owner = source;
                        }
                    }
                }
            }

            RegistryChanged?.Invoke();
        }


        private static void RefreshSources()
        {
            _registeredSources.Clear();
            IList<string> assetGuids = AssetDatabase.FindAssets("t:VariableSourceAsset");
            IList<string> assetPaths = new List<string>(assetGuids.Count);

            for (int i = 0; i < assetGuids.Count; i++)
            {
                string guid = assetGuids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                assetPaths.Add(path);

                var asset = AssetDatabase.LoadAssetAtPath<VariableSourceAsset>(path);
                if (asset.IncludeInRegistry)
                {
                    _registeredSources.Add(asset);
                }
            }
        }

        // This should exclude the VSAs that have their IncludeInRegistry property set to false,
        // since those are meant to be ignored by this class.
        private static readonly List<VariableSourceAsset> _registeredSources 
            = new List<VariableSourceAsset>();

        public static IReadOnlyList<VariableSourceAsset> RegisteredSources => _registeredSources;

        // Master dictionary of all variables
        private static readonly Dictionary<string, IVariable> _registeredVars = 
            new Dictionary<string, IVariable>();

        // Secondary index: contentType -> dict of vars
        private static readonly Dictionary<Type, Dictionary<string, IVariable>> _varsByType =
            new Dictionary<Type, Dictionary<string, IVariable>>();

        public static IReadOnlyDictionary<string, IVariable> Variables => _registeredVars;
        public static event Action RegistryChanged = delegate { };

        /// <summary>
        /// Registers the given variable under the given key, and also adds it to the secondary index for its content type.
        /// </summary>
        private static void Register(string key, IVariable toRegister)
        {
            // The key we want to register the var under won't necessarily be the same as the
            // var's own key, since we might want to prefix or postfix it with something.
            _registeredVars[key] = toRegister;
            
            var type = toRegister.ContentType;
            var dictForContentType = EnsureDictForContentType(type);
            dictForContentType[key] = toRegister;
        }

        private static Dictionary<string, IVariable> EnsureDictForContentType(Type contentType)
        {
            _varsByType.TryGetValue(contentType, out var dictForContentType);
            bool weHaveDictForContentType = dictForContentType != null;
            if (!weHaveDictForContentType)
            {
                dictForContentType = new Dictionary<string, IVariable>();
                _varsByType[contentType] = dictForContentType;
            }
            return dictForContentType;
        }

        private static readonly string _localSourceKeyFormat = "This/{1}";
        // ^This should usually make the local Fc's name show up at the top of the
        // var-selection popup
        private static readonly string _nonLocalFlowchartKeyFormat = "[Others in Scene]/[{0}]/{1}";
        private static readonly string _globalSourceKeyFormat = "~Globals~/~{0}~/{1}";

        /// <summary>
        /// Returns available variables matching the given content type. If getAllAssignableTypes 
        /// is true, it also returns variables whose content types are assignable to the given 
        /// content type (e.g. if contentType is Component, it also returns variables of type 
        /// SpriteRenderer since SpriteRenderer is a Component).
        /// </summary>
        public static IReadOnlyDictionary<string, IVariable> GetVarsOfType(Type contentType = null, 
            bool getAllAssignableTypes = false)
        {
            IReadOnlyDictionary<string, IVariable> result;
            bool giveThemEverything = contentType == null;
            if (giveThemEverything)
            {
                result = _registeredVars;
            }
            else
            {
                if (getAllAssignableTypes)
                {
                    var merged = new Dictionary<string, IVariable>();
                    foreach (var kvp in _varsByType)
                    {
                        var type = kvp.Key;
                        bool compatible = TypeUtils.TypesCompatible(contentType, type);
                        if (compatible)
                        {
                            foreach (var kvp2 in kvp.Value)
                            {
                                merged[kvp2.Key] = kvp2.Value;
                            }
                        }
                    }
                    result = merged;
                }
                else if (_varsByType.TryGetValue(contentType, out var dict))
                {
                    // This way, we don't make a whole new dictionary if we don't have to
                    result = dict;
                }
                else
                {
                    result = _emptyDict;
                }

            }
            return result;
        }

        /// <summary>
        /// Returns available variables matching any of the given content types.
        /// If null/empty, returns all.
        /// </summary>
        public static IReadOnlyDictionary<string, IVariable> GetVarsOfMultiTypes(
            IList<Type> contentTypes = null, 
            bool getAllAssignableTypes = false)
        {
            IReadOnlyDictionary<string, IVariable> result;
            bool giveThemEverything = contentTypes == null || contentTypes.Count == 0;
            if (giveThemEverything)
            {
                result = _registeredVars;
            }
            else if (contentTypes.Count == 1)
            {
                return GetVarsOfType(contentTypes[0], getAllAssignableTypes);
            }
            else
            {
                var merged = new Dictionary<string, IVariable>();
                if (getAllAssignableTypes)
                {
                    foreach (var kvp in _varsByType)
                    {
                        var type = kvp.Key;
                        bool compatible = false;
                        for (int i = 0; i < contentTypes.Count; i++)
                        {
                            if (TypeUtils.TypesCompatible(contentTypes[i], type))
                            {
                                compatible = true;
                                break;
                            }
                        }

                        if (compatible)
                        {
                            foreach (var kvp2 in kvp.Value)
                            {
                                merged[kvp2.Key] = kvp2.Value;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < contentTypes.Count; i++)
                    {
                        var type = contentTypes[i];
                        if (_varsByType.TryGetValue(type, out var dict))
                        {
                            foreach (var kvp in dict)
                            {
                                merged[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }

                result = merged;

            }
            return result;
        }

        private static readonly IReadOnlyList<VariableSourceAsset> _emptySources = new List<VariableSourceAsset>();
        private static readonly ReadOnlyDictionary<string, IVariable> _emptyDict =
            new ReadOnlyDictionary<string, IVariable>(new Dictionary<string, IVariable>());
    }
}