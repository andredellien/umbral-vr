using System;
using NUnit.Framework;

namespace Umbral.Core.Tests
{
    public class ConfiguratorTests
    {
        private Catalog catalog;

        [SetUp]
        public void SetUp()
        {
            // Mismos valores que server/seed/catalog.json.
            catalog = new Catalog
            {
                validUntil = "2026-12-31",
                units = new[]
                {
                    new UnitInfo { id = "A-301", basePrice = 520000, areaM2 = 58 },
                    new UnitInfo { id = "A-1201", basePrice = 565000, areaM2 = 58 },
                },
                surfaces = new[]
                {
                    Surface("floor", ("floor-ceramic", 0), ("floor-porcelain-xl", 4200), ("floor-wood", 9800)),
                    Surface("counter", ("counter-laminate", 0), ("counter-granite", 3500), ("counter-quartz", 6200)),
                    Surface("wall", ("wall-white", 0), ("wall-warm-grey", 0), ("wall-accent", 1800)),
                }
            };
        }

        private static SurfaceInfo Surface(string id, params (string id, long delta)[] options)
        {
            var list = new OptionInfo[options.Length];
            for (var i = 0; i < options.Length; i++) list[i] = new OptionInfo { id = options[i].id, delta = options[i].delta };
            return new SurfaceInfo { id = id, options = list };
        }

        [Test]
        public void PorDefecto_ElTotalEsElPrecioBase()
        {
            Assert.AreEqual(520000, new Configurator(catalog, "A-301").Total);
        }

        [Test]
        public void LaCombinacionMasCara_SumaTodasLasDiferencias()
        {
            var c = new Configurator(catalog, "A-301");
            c.Select("floor", "floor-wood");
            c.Select("counter", "counter-quartz");
            c.Select("wall", "wall-accent");
            Assert.AreEqual(520000 + 9800 + 6200 + 1800, c.Total); // 537,800
        }

        [Test]
        public void ElegirDosVecesLaMismaSuperficie_ReemplazaNoAcumula()
        {
            var c = new Configurator(catalog, "A-301");
            c.Select("floor", "floor-wood");
            c.Select("floor", "floor-porcelain-xl");
            Assert.AreEqual(520000 + 4200, c.Total);
            Assert.AreEqual("floor-porcelain-xl", c.SelectedOptionId("floor"));
        }

        [Test]
        public void VolverALaOpcionIncluida_VuelveAlPrecioBase()
        {
            var c = new Configurator(catalog, "A-301");
            c.Select("floor", "floor-wood");
            c.Select("floor", "floor-ceramic");
            Assert.AreEqual(520000, c.Total);
        }

        [Test]
        public void BlancoYGrisCalido_CuestanLoMismo()
        {
            var c = new Configurator(catalog, "A-301");
            c.Select("wall", "wall-white");
            var a = c.Total;
            c.Select("wall", "wall-warm-grey");
            Assert.AreEqual(a, c.Total);
        }

        [Test]
        public void OpcionDesconocida_LanzaYElEstadoNoCambia()
        {
            var c = new Configurator(catalog, "A-301");
            c.Select("floor", "floor-wood");
            var changes = 0;
            c.Changed += () => changes++;

            Assert.Throws<ArgumentException>(() => c.Select("floor", "no-existe"));
            Assert.Throws<ArgumentException>(() => c.Select("no-existe", "floor-wood"));

            Assert.AreEqual("floor-wood", c.SelectedOptionId("floor"));
            Assert.AreEqual(520000 + 9800, c.Total);
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void UnidadDesconocida_Lanza()
        {
            Assert.Throws<ArgumentException>(() => new Configurator(catalog, "Z-9"));
        }

        [Test]
        public void CadaUnidadUsaSuPropioPrecioBase()
        {
            var c = new Configurator(catalog, "A-1201");
            c.Select("floor", "floor-porcelain-xl");
            Assert.AreEqual(565000 + 4200, c.Total);
        }

        [Test]
        public void Changed_SeDisparaSoloCuandoLaEleccionCambia()
        {
            var c = new Configurator(catalog, "A-301");
            var changes = 0;
            c.Changed += () => changes++;

            c.Select("floor", "floor-ceramic"); // ya era la elegida
            Assert.AreEqual(0, changes);
            c.Select("floor", "floor-wood");
            Assert.AreEqual(1, changes);
            c.Select("floor", "floor-wood");
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void ElFormatoDePrecioNoDependeDelIdioma()
        {
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("es-BO");
                Assert.AreEqual("Bs 520,000", PriceFormat.Bolivianos(520000));
                Assert.AreEqual("Incluido", PriceFormat.Delta(0));
                Assert.AreEqual("+Bs 4,200", PriceFormat.Delta(4200));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
