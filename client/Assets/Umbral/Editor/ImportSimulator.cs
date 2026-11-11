using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace Umbral.EditorTools
{
    public static class ImportSimulator
    {
        [MenuItem("Umbral/Import XR Device Simulator")]
        public static void Import()
        {
            var sample = Sample.FindByPackage("com.unity.xr.interaction.toolkit", null)
                .FirstOrDefault(s => s.displayName == "XR Device Simulator");
            if (sample.displayName == null) throw new System.Exception("Sample no encontrado");
            var ok = sample.Import(Sample.ImportOptions.OverridePreviousImports);
            AssetDatabase.Refresh();
            Debug.Log("SIM_IMPORT ok=" + ok + " path=" + sample.importPath);
        }
    }
}
