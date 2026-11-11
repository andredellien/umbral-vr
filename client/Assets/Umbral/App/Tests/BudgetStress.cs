using UnityEngine;
using UnityEngine.Rendering;

namespace Umbral.App.Tests
{
    /// <summary>
    /// Spike de presupuesto: genera N objetos con malla propia (sin batching posible) hasta llegar a
    /// ~triangles en total, para medir draw calls y triángulos en el peor caso. Solo para pruebas.
    /// </summary>
    public class BudgetStress : MonoBehaviour
    {
        public int objects = 100;
        public int totalTriangles = 500_000;
        public int materialCount = 10;

        public int SpawnedTriangles { get; private set; }

        private void Start()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var materials = new Material[materialCount];
            for (var i = 0; i < materialCount; i++)
            {
                materials[i] = new Material(shader) { color = Color.HSVToRGB(i / (float)materialCount, 0.5f, 0.9f) };
                materials[i].enableInstancing = false;
            }

            // UV sphere con lon = 2*lat -> triángulos ~ 4*lat^2
            var perObject = totalTriangles / objects;
            var lat = Mathf.Max(3, Mathf.RoundToInt(Mathf.Sqrt(perObject / 4f)));
            var side = Mathf.CeilToInt(Mathf.Sqrt(objects));

            for (var i = 0; i < objects; i++)
            {
                var go = new GameObject($"Stress_{i}", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3((i % side) * 0.8f, 1f + (i / side) * 0.8f, 3f);
                go.transform.localScale = Vector3.one * 0.4f;

                var mesh = BuildSphere(lat, lat * 2);
                SpawnedTriangles += mesh.triangles.Length / 3;
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = materials[i % materialCount];
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        private static Mesh BuildSphere(int lat, int lon)
        {
            var vertices = new Vector3[(lat + 1) * (lon + 1)];
            var triangles = new int[lat * lon * 6];
            for (var y = 0; y <= lat; y++)
            {
                var phi = Mathf.PI * y / lat;
                for (var x = 0; x <= lon; x++)
                {
                    var theta = 2f * Mathf.PI * x / lon;
                    vertices[y * (lon + 1) + x] = new Vector3(
                        Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta)) * 0.5f;
                }
            }
            var t = 0;
            for (var y = 0; y < lat; y++)
            for (var x = 0; x < lon; x++)
            {
                var a = y * (lon + 1) + x;
                var b = a + lon + 1;
                triangles[t++] = a; triangles[t++] = b; triangles[t++] = a + 1;
                triangles[t++] = a + 1; triangles[t++] = b; triangles[t++] = b + 1;
            }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
