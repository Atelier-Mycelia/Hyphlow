using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using PackageUtils = AtMycelia.EditorExt.PackageUtils;

namespace AtMycelia.Hyphlow.EditorExt
{
    public sealed class HyphlowEditorSysAssets : ScriptableObject
    {
        [Serializable]
        public class EditorTexture
        {
            [SerializeField] [FormerlySerializedAs("free")]
private Texture2D free;
            [SerializeField] [FormerlySerializedAs("pro")]
private Texture2D pro;

            public Texture2D Texture2D
            {
                get { return EditorGUIUtility.isProSkin && pro != null ? pro : free; }
            }

            public EditorTexture(Texture2D free, Texture2D pro)
            {
                this.free = free;
                this.pro = pro;
            }
        }

        [SerializeField]
        [FormerlySerializedAs("add")]
        private EditorTexture _add;
        [SerializeField]
        [FormerlySerializedAs("add_small")]
        private EditorTexture _add_small;
        [SerializeField]
        [FormerlySerializedAs("delete")]
        private EditorTexture _delete;
        [SerializeField]
        [FormerlySerializedAs("down")]
        private EditorTexture _down;
        [SerializeField]
        [FormerlySerializedAs("duplicate")]
        private EditorTexture _duplicate;
        [SerializeField]
        [FormerlySerializedAs("fungus_mushroom")]
        private EditorTexture _mushroomIcon;
        [SerializeField]
        [FormerlySerializedAs("up")]
        private EditorTexture _up;
        [SerializeField]
        [FormerlySerializedAs("command_background")]
        private EditorTexture _command_background;
        [SerializeField]
        [FormerlySerializedAs("play_big")]
        private EditorTexture _play_big;
        [SerializeField]
        [FormerlySerializedAs("play_small")]
        private EditorTexture _play_small;
        [SerializeField]
        private EditorTexture _hyphlow_logo;
        [SerializeField] private FlowchartWindowConfig _fcwConfig;

        private static HyphlowEditorSysAssets _instance;
        private static readonly string _subfolderLocation = "Editor"; // Relative to Resources folder
        private static readonly string _searchFilter = "t:HyphlowEditorSysAssets";
        private static readonly string _assetName = "HyphlowEditorSysAssets";

        public static HyphlowEditorSysAssets S
        {
            get
            {
                if (_instance == null)
                {
                    string[] guids = AssetDatabase.FindAssets(_searchFilter);

                    if (guids.Length == 0)
                    {
                        _instance = SOUtils.EnsureSOExists<HyphlowEditorSysAssets>(_subfolderLocation, _assetName);
                    }
                    else
                    {
                        bool weAreInPackages = PackageUtils.HasPackageWithName("Hyphlow");
                        // ^When this is true, best get the one under Packages
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        for (int i = 1; i < guids.Length; i++)
                        {
                            string pathToCheck = AssetDatabase.GUIDToAssetPath(guids[i]);
                            bool foundIt = !pathToCheck.Contains("Assets") &&
                                pathToCheck.Contains("Packages") &&
                                pathToCheck.Contains("Hyphlow");
                            if (foundIt)
                            {
                                path = pathToCheck;
                                break;
                            }
                        }

                        if (guids.Length > 1)
                        {
                            Debug.LogError("Multiple HyphlowEditorSysAssets assets found!");
                        }

                        _instance = AssetDatabase.LoadAssetAtPath(path, 
                            typeof(HyphlowEditorSysAssets)) as HyphlowEditorSysAssets;
                    }
                }

                return _instance;
            }
        }

        public static Texture2D Add { get { return S._add.Texture2D; } }
        public static Texture2D AddSmall { get { return S._add_small.Texture2D; } }
        public static Texture2D Delete { get { return S._delete.Texture2D; } }
        public static Texture2D Down { get { return S._down.Texture2D; } }
        public static Texture2D Duplicate { get { return S._duplicate.Texture2D; } }
        public static Texture2D FungusMushroom { get { return S._mushroomIcon.Texture2D; } }
        public static Texture2D Up { get { return S._up.Texture2D; } }
        public static Texture2D CommandBackground { get { return S._command_background.Texture2D; } }
        public static Texture2D PlayBig { get { return S._play_big.Texture2D; } }
        public static Texture2D PlaySmall { get { return S._play_small.Texture2D; } }
        public static Texture2D HyphlowLogo { get { return S._hyphlow_logo.Texture2D; } }
        public static FlowchartWindowConfig FcwConfig { get { return S._fcwConfig; } }
    }
}
