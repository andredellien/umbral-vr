using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Umbral.Core.Tests
{
    public class PriceCalculatorTests
    {
        private Catalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = new Catalog
            {
                units = new[] { new UnitInfo { id = "A-301", basePrice = 500000 } },
                surfaces = new[]
                {
                    new SurfaceInfo
                    {
                        id = "floor",
                        options = new[]
                        {
                            new OptionInfo { id = "ceramic", delta = 0 },
                            new OptionInfo { id = "porcelain", delta = 4200 },
                        }
                    },
                    new SurfaceInfo
                    {
                        id = "counter",
                        options = new[]
                        {
                            new OptionInfo { id = "laminate", delta = 0 },
                            new OptionInfo { id = "quartz", delta = 6200 },
                        }
                    },
                }
            };
        }

        [Test]
        public void SinElecciones_DevuelveElPrecioBase()
        {
            Assert.AreEqual(500000, PriceCalculator.Total(catalog, "A-301", null));
        }

        [Test]
        public void ConLaSeleccionPorDefecto_DevuelveElPrecioBase()
        {
            var selection = CatalogQueries.DefaultSelection(catalog);
            Assert.AreEqual(500000, PriceCalculator.Total(catalog, "A-301", selection));
        }

        [Test]
        public void SumaLasDiferenciasDeLosAcabadosElegidos()
        {
            var selection = new Dictionary<string, string> { { "floor", "porcelain" }, { "counter", "quartz" } };
            Assert.AreEqual(510400, PriceCalculator.Total(catalog, "A-301", selection));
        }

        [Test]
        public void UnidadDesconocida_Lanza()
        {
            Assert.Throws<ArgumentException>(() => PriceCalculator.Total(catalog, "NO-EXISTE", null));
        }

        [Test]
        public void OpcionDesconocida_Lanza()
        {
            var selection = new Dictionary<string, string> { { "floor", "marmol" } };
            Assert.Throws<ArgumentException>(() => PriceCalculator.Total(catalog, "A-301", selection));
        }
    }
}
