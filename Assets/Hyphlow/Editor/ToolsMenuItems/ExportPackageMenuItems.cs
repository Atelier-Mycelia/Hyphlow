using UnityEngine;
using UnityEditor;

namespace AtMycelia.Hyphlow.EditorExt
{
    public class ExportPackageMenuItems : MonoBehaviour
    {
        [MenuItem("Tools/Atelier Mycelia/Hyphlow/Utilities/Export Hyphlow Package")]
        static void ExportAmanitaPackageFull()
        {
            ExportHyphlowPackage( new string[] {"Assets/Hyphlow", "Assets/HyphlowExamples" });
        }

        [MenuItem("Tools/Atelier Mycelia/Hyphlow/Utilities/Export Fungus Package - Lite")]
        static void ExportAmanitaPackageLite()
        {
            ExportHyphlowPackage(new string[] { "Assets/Hyphlow" });
        }

        static void ExportHyphlowPackage(string[] folders)
        {
            string path = EditorUtility.SaveFilePanel("Export Amanita Package", "", 
            "Amanita", "unitypackage");
            if (path.Length == 0)
            {
                return;
            }

            AssetDatabase.ExportPackage(folders, path, ExportPackageOptions.Recurse);
        }
    }
}
