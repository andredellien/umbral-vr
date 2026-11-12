using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Umbral.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.UI;

namespace Umbral.App.Tests
{
    public class FinishTests
    {
        [Test]
        public void TodaOpcionDelCatalogoTieneColor()
        {
            var catalog = JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog_seed").text);
            foreach (var surface in catalog.surfaces)
                foreach (var option in surface.options)
                    Assert.IsTrue(FinishPalette.TryGet(option.id, out _), $"Falta color para '{option.id}' en FinishPalette");
        }

        [Test]
        public void WrapPartePalabrasLargas()
        {
            Assert.AreEqual("Porcelanato\nde gran\nformato", FinishSwatch.Wrap("Porcelanato de gran formato", 12));
            Assert.AreEqual("Cuarzo", FinishSwatch.Wrap("Cuarzo", 12));
        }

        private static void AssertColor(Color expected, Color actual) =>
            Assert.That(actual, Is.EqualTo(expected).Using(new ColorEqualityComparer(0.01f)));

        private static Color BaseColor(Renderer r)
        {
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            return block.GetColor("_BaseColor");
        }

        [UnityTest]
        public IEnumerator ElegirUnaMuestraCambiaElPrecioDeLaMunecaYElColorDeLaSuperficie()
        {
            var load = SceneManager.LoadSceneAsync("Apartment_Greybox");
            while (!load.isDone) yield return null;

            // Los controles del rig están apagados sin dispositivos: activamos la rama del panel de la muñeca.
            var wrist = Object.FindFirstObjectByType<WristPrice>(FindObjectsInactive.Include);
            for (var tr = wrist.transform; tr != null; tr = tr.parent) tr.gameObject.SetActive(true);
            var label = wrist.GetComponentInChildren<Text>();
            var timeout = Time.realtimeSinceStartup + 15f;
            while ((label.text == "" || label.text == "Cargando...") && Time.realtimeSinceStartup < timeout) yield return null;
            yield return null;
            var host = Object.FindFirstObjectByType<ConfiguratorHost>();
            var catalog = host.Ready.Result.Catalog; // el catálogo real de la corrida (servidor, caché o seed): los precios esperados salen de él
            string Price(params (string surface, string option)[] picks)
            {
                var sel = CatalogQueries.DefaultSelection(catalog);
                foreach (var (surface, option) in picks) sel[surface] = option;
                return PriceFormat.Bolivianos(PriceCalculator.Total(catalog, "A-301", sel));
            }
            StringAssert.Contains(Price(), label.text);

            var swatches = Object.FindObjectsByType<FinishSwatch>();
            Assert.AreEqual(9, swatches.Length, "3 superficies x 3 opciones");
            FinishSwatch Find(string id) { foreach (var s in swatches) if (s.OptionId == id) return s; return null; }

            var floor = GameObject.Find("Floor").GetComponent<Renderer>();
            AssertColor(FinishPalette.ColorFor("floor-ceramic"), BaseColor(floor));

            Find("floor-wood").Choose();
            AssertColor(FinishPalette.ColorFor("floor-wood"), BaseColor(floor));
            StringAssert.Contains(Price(("floor", "floor-wood")), label.text);

            Find("counter-quartz").Choose();
            Find("wall-accent").Choose();
            StringAssert.Contains(Price(("floor", "floor-wood"), ("counter", "counter-quartz"), ("wall", "wall-accent")), label.text);

            var wall = GameObject.Find("Acabado_Pared_Oeste").GetComponentInChildren<Renderer>();
            AssertColor(FinishPalette.ColorFor("wall-accent"), BaseColor(wall));

            Find("floor-ceramic").Choose();
            Find("counter-laminate").Choose();
            Find("wall-white").Choose();
            StringAssert.Contains(Price(), label.text);
        }
    }
}
