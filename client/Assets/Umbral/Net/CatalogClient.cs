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
            var seedAsset = Resources.Load<TextAsset>(SeedResource);
            var seed = seedAsset != null ? JsonUtility.FromJson<Catalog>(seedAsset.text) : null;

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
                            // Un servidor desactualizado (versión menor que el seed) no se usa ni se cachea.
                            if (CatalogQueries.AcceptServer(fromServer, seed))
                            {
                                CatalogCache.Write(cachePath, json);
                                return fromServer;
                            }
                            Debug.LogWarning($"[Catalog] El servidor trae la versión {fromServer.version}, menor que la del build ({seed.version}): se ignora.");
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
            var cached = CatalogCache.TryRead(cachePath);
            var best = CatalogQueries.PickFresher(cached, seed);
            if (best == null) throw new FileNotFoundException("Falta Assets/Resources/catalog_seed.json");
            return best;
        }
    }
}
