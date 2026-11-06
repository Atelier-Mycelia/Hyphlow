using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AtMycelia.Hyphlow
{
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public static class VariableTypeDiscovery
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        public static void DiscoverAndRegister()
        {
            RefreshVariableTypeRegistry();
            RefreshVariableDataTypeRegistry();

#if UNITY_EDITOR
            AssemblyReloadEvents.afterAssemblyReload -= RefreshVariableTypeRegistry;
            AssemblyReloadEvents.afterAssemblyReload -= RefreshVariableDataTypeRegistry;

            AssemblyReloadEvents.afterAssemblyReload += RefreshVariableTypeRegistry;
            AssemblyReloadEvents.afterAssemblyReload += RefreshVariableDataTypeRegistry;
#endif
        }

        /// <summary>
        /// TypeCache is editor-only (and much faster), so builds fall back to reflection.
        /// </summary>
        private static IEnumerable<Type> TypesDerivedFrom<T>()
        {
#if UNITY_EDITOR
            return TypeCache.GetTypesDerivedFrom<T>();
#else
            Type baseType = typeof(T);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(TypesIn)
                .Where(type => type != baseType && baseType.IsAssignableFrom(type));
#endif
        }

#if !UNITY_EDITOR
        private static IEnumerable<Type> TypesIn(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // Some types failing to load shouldn't cost us the rest of the assembly's
                return e.Types.Where(type => type != null);
            }
        }
#endif

        private static void RefreshVariableTypeRegistry()
        {
            IEnumerable<Type> varSubtypes = TypesDerivedFrom<IVariable>()
                         .Where((elem) => elem.IsConcrete() &&
                         !ShouldExcludeDueToBeingForTests(elem));

            SetVarTypeRegistry();
            void SetVarTypeRegistry()
            {
                VariableTypeRegistry.Clear();
                VariableTypeActions typeActions = new VariableTypeActions()
                {
                    CompareFunc = VarCompareFunc,
                    DescFunc = VarGetDescription,
                    SetFunc = VarSetFunc
                };
                VariableTypeRegistry.RegisterMultiVariableTypes(varSubtypes, typeActions);
            }
        }

        private static bool VarCompareFunc(IVariable varInvolved, IVariableData varData, CompareOperator compareOp)
        {
            bool result = varInvolved.Evaluate(compareOp, varData.BoxedValue);
            return result;
        }

        private static bool ShouldExcludeDueToBeingForTests(Type typeToCheck)
        {
            VariableInfoAttribute attr = typeToCheck.GetCustomAttribute<VariableInfoAttribute>();
            // We should only include test types if the currently-running scene is a test scene,
            // to avoid cluttering the variable type registry with test types during normal use.
            // This is because test types are often used for testing and debugging purposes, and
            // may not be relevant or useful in a non-test context.
            if (attr != null)
            {
                var activeScene = SceneManager.GetActiveScene();
                string sceneName = activeScene.name;
                bool weAreInTestScene = sceneName.Contains("Test");
                if (attr.IsTest && (!weAreInTestScene || Application.isEditor))
                {
                    Debug.Log($"Excluding test type {typeToCheck.Name} from variable type registry " +
                        $"because the active scene is not a test scene.");
                    return true;
                }
            }

            return false;
        }

        private static string VarGetDescription(IVariableData varData)
        {
            return varData.GetDescription();
        }

        private static void VarSetFunc(IVariable iVar, IVariableData varData, SetOperator setOp)
        {
            iVar.Apply(setOp, varData.BoxedValue);
        }

        private static void RefreshVariableDataTypeRegistry()
        {
            IEnumerable<Type> varDataSubtypes = TypesDerivedFrom<IVariableData>()
                         .Where((elem) => elem.IsConcrete());

            VariableDataTypeRegistry.Clear();

            foreach (var elem in varDataSubtypes)
            {
                VariableDataAttribute attr = elem.GetCustomAttribute<VariableDataAttribute>();
                if (attr != null)
                {
                    VariableDataTypeRegistry.Register(elem);
                }
            }
        }

    }
}