using System.IO;
using UnityEditor;
using UnityEngine;

namespace Umbral.EditorTools
{
    /// <summary>
    /// Vista exterior PROVISORIA: panorama equirrectangular 2048x1024 generado por código (cielo, horizonte y siluetas de edificios).
    /// Cuando haya fotos 360° reales con dron, una por piso, se reemplaza el PNG (GDD: "a la altura real del piso elegido").
    /// </summary>
    public static class WindowView
    {
        public const string Dir = "Assets/Umbral/View";
        public const string PanoramaPath = Dir + "/panorama_provisorio.png";
        public const string MaterialPath = Dir + "/Vista360.mat";

        public static void Apply()
        {
            Directory.CreateDirectory(Dir);
            if (!File.Exists(PanoramaPath)) File.WriteAllBytes(PanoramaPath, MakePanorama().EncodeToPNG());
            AssetDatabase.ImportAsset(PanoramaPath);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PanoramaPath);

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Mapping", 1f); // Latitude Longitude Layout
            mat.SetFloat("_ImageType", 0f); // 360 grados
            mat.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f));
            mat.SetFloat("_Exposure", 1f);
            EditorUtility.SetDirty(mat);
            RenderSettings.skybox = mat;
        }

        private static Texture2D MakePanorama()
        {
            const int w = 2048, h = 1024;
            var px = new Color[w * h];
            var rng = new System.Random(301);
            var horizon = h / 2;

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                if (y >= horizon) // abajo = cielo (en el mapeo equirrectangular la fila 0 del texture es el polo sur)
                {
                    var t = (y - horizon) / (float)(h - horizon);
                    px[y * w + x] = Color.Lerp(new Color(0.80f, 0.88f, 0.96f), new Color(0.30f, 0.52f, 0.85f), t);
                }
                else // suelo: calle gris
                {
                    var t = y / (float)horizon;
                    px[y * w + x] = Color.Lerp(new Color(0.25f, 0.25f, 0.27f), new Color(0.45f, 0.45f, 0.45f), t);
                }
            }

            // Edificios alrededor, con ventanas.
            var xPos = 0;
            while (xPos < w)
            {
                var bw = rng.Next(24, 70);
                var bh = rng.Next(20, 150);
                var shade = 0.35f + (float)rng.NextDouble() * 0.3f;
                var col = new Color(shade, shade * 1.02f, shade * 1.1f);
                for (var x = xPos; x < Mathf.Min(w, xPos + bw); x++)
                for (var y = horizon; y < horizon + bh; y++)
                {
                    var window = (x % 8 < 4) && (y % 10 < 5) && y > horizon + 6;
                    px[y * w + x] = window ? col * 1.45f : col;
                }
                xPos += bw + rng.Next(0, 18);
            }

            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
