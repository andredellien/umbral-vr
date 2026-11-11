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

            if (File.Exists(cachePath))
            {
                var cached = JsonUtility.FromJson<Catalog>(File.ReadAllText(cachePath));
                if (cached != null && cached.units != null) return cached;
            }

            var seed = Resources.Load<TextAsset>(SeedResource);
            if (seed == null) throw new FileNotFoundException("Falta Assets/Resources/catalog_seed.json");
            return JsonUtility.FromJson<Catalog>(seed.text);
        }
    }
}
