using System;
using System.IO;
using Umbral.Core;
using UnityEngine;

namespace Umbral.Net
{
    /// <summary>Caché del catálogo en disco. Una caché ilegible (corrupta o a medio escribir) cuenta como "no hay caché".</summary>
    public static class CatalogCache
    {
        public static Catalog TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var parsed = JsonUtility.FromJson<Catalog>(File.ReadAllText(path));
                return parsed != null && parsed.units != null ? parsed : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Catalog] Caché ilegible, se ignora: {e.Message}");
                return null;
            }
        }

        /// <summary>Escribe a un temporal y lo mueve al destino: un corte a medias no deja un archivo roto. No lanza.</summary>
        public static void Write(string path, string json)
        {
            var tmp = path + ".tmp";
            try
            {
                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Catalog] No se pudo guardar la caché: {e.Message}");
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* sin caché la app sigue */ }
            }
        }
    }
}
