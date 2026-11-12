using NUnit.Framework;

namespace Umbral.Core.Tests
{
    public class TelemetryTests
    {
        [Test]
        public void Backoff_CreceYTieneTope()
        {
            Assert.AreEqual(0f, Backoff.DelaySeconds(0));
            Assert.AreEqual(2f, Backoff.DelaySeconds(1));
            Assert.AreEqual(4f, Backoff.DelaySeconds(2));
            Assert.AreEqual(8f, Backoff.DelaySeconds(3));
            Assert.AreEqual(60f, Backoff.DelaySeconds(50));
            for (var i = 1; i < 20; i++) Assert.GreaterOrEqual(Backoff.DelaySeconds(i + 1), Backoff.DelaySeconds(i));
        }

        private static Catalog Catalog() => new Catalog
        {
            units = new[] { new UnitInfo { id = "A-301", basePrice = 520000, areaM2 = 58 } },
            surfaces = new[]
            {
                new SurfaceInfo { id = "floor", name = "Piso", options = new[] { new OptionInfo { id = "c", name = "Cerámica" }, new OptionInfo { id = "w", name = "Madera", delta = 9800 } } },
                new SurfaceInfo { id = "wall", name = "Pared", options = new[] { new OptionInfo { id = "b", name = "Blanco" } } },
            },
        };

        [Test]
        public void SelectionsOf_TrazaLoElegidoEnTodasLasSuperficies()
        {
            var c = new Configurator(Catalog(), "A-301");
            c.Select("floor", "w");
            var s = Ficha.SelectionsOf(c);
            Assert.AreEqual(2, s.Length);
            Assert.AreEqual("w", s[0].option);
            Assert.AreEqual("b", s[1].option);
        }

        [Test]
        public void LaFicha_MuestraUnidadAcabadosTotalYCodigoSinDescuentoNiPlanDePago()
        {
            var catalog = Catalog();
            var text = Ficha.Text(catalog, new ReservationDto
            {
                id = "0123456789abcdef", unitId = "A-301", total = 529800,
                selections = new[] { new SelectionPair { surface = "floor", option = "w" }, new SelectionPair { surface = "wall", option = "b" } },
            });
            StringAssert.Contains("A-301", text);
            StringAssert.Contains("Piso: Madera", text);
            StringAssert.Contains("Pared: Blanco", text);
            StringAssert.Contains("Bs 529,800", text);
            StringAssert.Contains("01234567", text);
            StringAssert.DoesNotContain("descuento", text.ToLowerInvariant());
            StringAssert.DoesNotContain("plan de pago", text.ToLowerInvariant());
        }
    }
}
