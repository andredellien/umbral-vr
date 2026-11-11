using System;
using NUnit.Framework;

namespace Umbral.Core.Tests
{
    public class CatalogQueriesTests
    {
        private Catalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = new Catalog
            {
                units = new[] { new UnitInfo { id = "A-301", floor = 3, areaM2 = 58 } },
                surfaces = new[]
                {
                    new SurfaceInfo
                    {
                        id = "floor",
                        options = new[]
                        {
                            new OptionInfo { id = "ceramic", name = "Cerámica" },
                            new OptionInfo { id = "wood", name = "Madera" },
                        }
                    },
                    new SurfaceInfo
                    {
                        id = "wall",
                        options = new[] { new OptionInfo { id = "white", name = "Blanco" } }
                    },
                }
            };
        }

        [Test]
        public void FindUnit_DevuelveLaUnidad()
        {
            Assert.AreEqual(3, CatalogQueries.FindUnit(catalog, "A-301").floor);
        }

        [Test]
        public void FindUnit_Desconocida_Lanza()
        {
            Assert.Throws<ArgumentException>(() => CatalogQueries.FindUnit(catalog, "NO-EXISTE"));
        }

        [Test]
        public void FindOption_DevuelveLaOpcion()
        {
            Assert.AreEqual("Madera", CatalogQueries.FindOption(catalog, "floor", "wood").name);
        }

        [Test]
        public void FindOption_Desconocida_Lanza()
        {
            Assert.Throws<ArgumentException>(() => CatalogQueries.FindOption(catalog, "floor", "marmol"));
            Assert.Throws<ArgumentException>(() => CatalogQueries.FindOption(catalog, "techo", "x"));
        }

        [Test]
        public void DefaultSelection_ElijeLaPrimeraOpcionDeCadaSuperficie()
        {
            var selection = CatalogQueries.DefaultSelection(catalog);
            Assert.AreEqual(2, selection.Count);
            Assert.AreEqual("ceramic", selection["floor"]);
            Assert.AreEqual("white", selection["wall"]);
        }
    }
}
