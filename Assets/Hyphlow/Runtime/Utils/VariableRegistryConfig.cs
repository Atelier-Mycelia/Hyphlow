using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace AtMycelia.Hyphlow
{
    [CreateAssetMenu(fileName = "NewVariableRegistryConfig", 
        menuName = "Atelier Mycelia/Hyphlow/Variable Registry Config")]
    public sealed class VariableRegistryConfig : ScriptableObject
    {
        [SerializeField]
        [FormerlySerializedAs("globalSources")]
        private List<VariableSourceAsset> _globalSources = new List<VariableSourceAsset>();

        public IReadOnlyList<VariableSourceAsset> GlobalSources
        {
            get
            {
                Refresh();
                return _globalSources;
            }
        }

        public VariableSourceAsset GetGlobalSourceByUid(string uid)
        {
            Refresh();
            bool found = _globalSourcesByUid.TryGetValue(uid, out VariableSourceAsset source);
            if (!found)
            {
                string errorMessage = $"Attempted to get global variable source with UID {uid} from {name} " +
                    $"({GetInstanceID()}), but no source with that UID was found.";
                Debug.LogError(errorMessage, this);
                return null;
            }
            return source;
        }

        void Refresh()
        {
            _globalSources ??= new List<VariableSourceAsset>();
            _globalSources.RemoveAll(source => source == null);
            RefreshSourceDictAndEnforceUniqueIds();
        }

        private void EnsureGlobalSourcesList()
        {
            _globalSources ??= new List<VariableSourceAsset>();
        }

        private IDictionary<string, VariableSourceAsset> _globalSourcesByUid = new Dictionary<string, VariableSourceAsset>();

        private void RefreshSourceDictAndEnforceUniqueIds()
        {
            // Two different VSAs can end up with the same Uid if the user duplicates
            // one in the editor, so we need to check for that and enforce unique IDs.
            _globalSourcesByUid.Clear();
            for (int i = 0; i < _globalSources.Count; i++)
            {
                VariableSourceAsset source = _globalSources[i];
                if (source == null)
                {
                    continue;
                }

                string uid = source.UniqueId;

                bool invalidId = string.IsNullOrEmpty(uid);
                if (invalidId)
                {
                    string errorMessage = $"Variable source {source.name} " +
                        $"({source.GetInstanceID()}) has an invalid UID. " +
                        $"This source will be ignored.";
                    Debug.LogError(errorMessage, source);
                    continue;
                }

                bool idIsUnique = !_globalSourcesByUid.ContainsKey(uid); //
                if (!idIsUnique)
                {
                    string logMessage = $"Variable Source Asset {source.name} ({source.GetInstanceID()}) " +
                        $"has a Uid that is not unique. Perhaps it was duplicated from another Vsa? " +
                        $"Either way, I'll give it a new Uid.";
                    Debug.LogWarning(logMessage, source);
                    source.ForceResetUid();
                    continue;
                }

                _globalSourcesByUid.Add(uid, source);
            }
        }
        
        public void SetGlobalSources(IList<VariableSourceAsset> sources)
        {
            EnsureGlobalSourcesList();
            _globalSources.Clear();

            if (sources == null)
            {
                string errorMessage = $"Attempted to set global sources list to null on {name} " +
                    $"({GetInstanceID()}). This is not allowed. The list will be cleared instead.";
                Debug.LogError(errorMessage, this);

                return;
            }

            _globalSources.AddRange(sources);
            _globalSources.RemoveAll(source => source == null);

            Changed();
        }

        public event Action Changed = delegate { };

        private void OnEnable()
        {
            ToggleSubs(true);
        }
        
        private void ToggleSubs(bool on)
        {
            if (on)
            {
                VsaSignals.VsaDestroyed += OnVsaDestroyed;
            }
        }

        private void OnVsaDestroyed(string vsaName, string vsaUid)
        {
            Refresh();
            // ^This should keep us from holding on to any null refs
            // that would otherwise be caused by VSAs being destroyed.
        }

        private void OnDisable()
        {
            ToggleSubs(false);
        }
        
        private void OnValidate()
        {
            Refresh();
            Changed();
        }

    }
}