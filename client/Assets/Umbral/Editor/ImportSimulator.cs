using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace Umbral.EditorTools
{
    public static class ImportSimulator
    {
        [MenuItem("Umbral/Import XRI Samples")]
        public static void Import()
        {
            foreach (var name in new[] { "XR Device Simulator", "Starter Assets" })
            {
                var sample = Sample.FindByPackage("com.unity.xr.interaction.toolkit", null)
                    .FirstOrDefault(s => s.displayName == name);
                if (sample.displayName == null) throw new System.Exception($"Sample '{name}' no encontrado");
                var ok = sample.Import(Sample.ImportOptions.OverridePreviousImports);
                Debug.Log($"SIM_IMPORT {name} ok={ok}");
            }
            AssetDatabase.Refresh();
                    }
    }
}
