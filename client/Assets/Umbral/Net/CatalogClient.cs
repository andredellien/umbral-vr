using System;
using System.IO;
using Umbral.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Umbral.Net
{
    /// <summary>
    /// Carga el catálogo con tres niveles: servidor -> caché en disco -> seed incluido en el build.
    /// Así la app funciona sin internet (ADR-001 #8).
    /// </summary>
    public static class CatalogClient
    {
        private const string CacheFile = "catalog.json";
        private const string SeedResource = "catalog_seed"; // Assets/Resources/catalog_seed.json

        public static async Awaitable<Catalog> LoadAsync(string baseUrl, int timeoutSeconds = 5)
        {
            var cachePath = Path.Combine(Application.persistentDataPath, CacheFile);

            if (!string.IsNullOrEmpty(baseUrl))
            {
                try
                {
                    using var request = UnityWebRequest.Get(baseUrl.TrimEnd('/') + "/catalog");
                    request.timeout = timeoutSeconds;
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var json = request.downloadHandler.text;
                        var fromServer = JsonUtility.FromJson<Catalog>(json);
                        if (fromServer != null && fromServer.units != null)
                        {
                            File.WriteAllText(cachePath, json);
                            return fromServer;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Catalog] Servidor no disponible: {request.error}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Catalog] Error de red: {e.Message}");
                }
            }

            // Sin servidor: la caché en disco, salvo que el seed del build sea de una versión mayor
            // (una caché vieja sin los campos nuevos taparía el catálogo actualizado).
            Catalog cached = null;
            if (File.Exists(cachePath))
            {
                var parsed = JsonUtility.FromJson<Catalog>(File.ReadAllText(cachePath));
                if (parsed != null && parsed.units != null) cached = parsed;
            }

            var seedAsset = Resources.Load<TextAsset>(SeedResource);
            var seed = seedAsset != null ? JsonUtility.FromJson<Catalog>(seedAsset.text) : null;
            var best = CatalogQueries.PickFresher(cached, seed);
            if (best == null) throw new FileNotFoundException("Falta Assets/Resources/catalog_seed.json");
            return best;
        }
    }
}
