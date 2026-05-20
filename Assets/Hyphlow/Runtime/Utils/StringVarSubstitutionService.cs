using AtMycelia.Hyphlow.Sys;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Service for substituting Hyphlow IVariable values in strings.
    /// </summary>
    public class StringVarSubstitutionService : IStringVarSubstitutor
    {
        /// <summary>
        /// Shared default instance for callers that don't need custom configuration.
        /// </summary>
        public static IStringVarSubstitutor Shared { get; } = new StringVarSubstitutionService();

        public const string SubstituteVariableRegexString = "{\\$.*?}";

        public string SubstituteVariables(string input, IVariableSource variableSource)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            if (variableSource == null)
            {
                string errorMessage = "StringVarSubstitutionService.SubstituteVariables was called with " +
                    "a null IVariableSource. Returning the input string without substitution.";
                Debug.LogError(errorMessage);
                return input;
            }

            Regex regex = new Regex(SubstituteVariableRegexString);
            MatchCollection matches = regex.Matches(input);
            if (matches.Count == 0)
            {
                return input;
            }

            StringBuilder sb = new StringBuilder(input);
            HashSet<string> missingKeys = null;
            bool changed = false;

            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                string key = match.Value.Substring(2, match.Value.Length - 3);
                if (TryGetVariableValue(key, variableSource, out string value))
                {
                    sb.Replace(match.Value, value);
                    changed = true;
                    continue;
                }

                missingKeys ??= new HashSet<string>(StringComparer.Ordinal);
                if (missingKeys.Add(key))
                {
                    Debug.LogError($"StringVarSubstitutionService: Variable key '{key}' was not found in any Flowchart or global variable sets.");
                }
            }

            return changed ? sb.ToString() : input;
        }

        private bool TryGetVariableValue(string key, IVariableSource variableSource, out string value)
        {
            IVariable variable;
            if (TryGetVarFromSource(variableSource, key, out variable) ||
                TryGetVarFromScene(variableSource, key, out variable) ||
                TryGetVarFromGlobalSources(key, out variable))
            {
                value = variable.BoxedValue.ToString();
                return true;
            }

            value = null;
            return false;
        }

        private bool TryGetVarFromSource(IVariableSource source, string key, out IVariable variable)
        {
            variable = source.GetVariable(key, StringComparison.Ordinal);
            return variable != null;
        }

        private bool TryGetVarFromScene(IVariableSource sourceToIgnore, string key, 
            out IVariable variable)
        {
            variable = null;
            IReadOnlyList<Flowchart> flowcharts = FlowchartRegistry.GetSceneFlowcharts();
            for (int i = 0; i < flowcharts.Count; i++)
            {
                Flowchart flowchart = flowcharts[i];
                if (flowchart == null || ReferenceEquals(flowchart, sourceToIgnore))
                {
                    continue;
                }

                IVariable candidate = flowchart.GetVariable(key, StringComparison.Ordinal);
                bool isVisibleToUs = candidate != null && 
                    (candidate.Scope & AccessScopeDefaults.VisibleToOutsiders) != 0;
                if (!isVisibleToUs)
                {
                    continue;
                }

                variable = candidate;
            }

            bool result = variable != null;
            return result;
        }

        private bool TryGetVarFromGlobalSources(string key, out IVariable variable)
        {
            variable = null;
            var sourcesToConsider = GetGlobalVariableSources();
            
            if (sourcesToConsider.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < sourcesToConsider.Count; i++)
            {
                VariableSourceAsset source = sourcesToConsider[i];

                IVariable candidate = source.GetVariableByName(key, StringComparison.Ordinal);
                variable = candidate;
                break;
                
            }

            bool result = variable != null;
            return result;
        }

        private IList<VariableSourceAsset> GetGlobalVariableSources()
        {
            IList<VariableSourceAsset> result = new List<VariableSourceAsset>();
            var registryConfigs = HyphlowRuntimeSysAssets.S.VariableRegistryConfigs;
            if (registryConfigs == null)
            {
                string errorMessage = "StringVarSubstitutionService.TryGetVarFromGlobalSources was called, but " +
                    "HyphlowRuntimeSysAssets.S.VariableRegistryConfigs is null.";
                Debug.LogError(errorMessage);
                return result;
            }
            for (int i = 0; i < registryConfigs.Count; i++)
            {
                VariableRegistryConfig config = registryConfigs[i];
                bool validConfig = config != null && config.GlobalSources != null &&
                    config.GlobalSources.Count > 0;
                if (!validConfig)
                {
                    continue;
                }

                for (int j = 0; j < config.GlobalSources.Count; j++)
                {
                    VariableSourceAsset source = config.GlobalSources[j];
                    if (source == null)
                    {
                        // Something likely went pretty wrong is the registry has any null entries,
                        // so log an error but keep going to try to get as many valid sources as possible.
                        string errorMessage = $"StringVarSubstitutionService.TryGetVarFromGlobalSources found " +
                            $"a null VariableSourceAsset in the GlobalSources list of VariableRegistryConfig " +
                            $"at index {i}.";
                        Debug.LogError(errorMessage);
                        continue;
                    }

                    result.Add(source);
                }

                break;
            }

            return result;
        }
    }
}