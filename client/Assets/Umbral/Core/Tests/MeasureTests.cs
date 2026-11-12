using NUnit.Framework;

namespace Umbral.Core.Tests
{
    public class MeasureTests
    {
        [Test] public void DistanciaEnUnEje() => Assert.AreEqual(200, Measure.Centimeters(2f, 0f, 0f));
        [Test] public void DistanciaEnTresEjes() => Assert.AreEqual(300, Measure.Centimeters(1f, 2f, 2f)); // 3-4-5 en 3D
        [Test] public void ElSignoNoImporta() => Assert.AreEqual(150, Measure.Centimeters(-1.5f, 0f, 0f));
        [Test] public void RedondeaAlCentimetro() => Assert.AreEqual(124, Measure.Centimeters(1.236f, 0f, 0f));
        [Test] public void MismoPunto_EsCero() => Assert.AreEqual(0, Measure.Centimeters(0f, 0f, 0f));
        [Test] public void ValoresInvalidos_DanCero()
        {
            Assert.AreEqual(0, Measure.Centimeters(float.NaN, 0f, 0f));
            Assert.AreEqual(0, Measure.Centimeters(float.PositiveInfinity, 0f, 0f));
        }
        [Test] public void FormatoFijo() => Assert.AreEqual("1234 cm", Measure.Format(1234));

        [Test]
        public void LasParadasDelRelojEstanOrdenadasDentroDelHorarioDelGdd()
        {
            Assert.AreEqual(4, SolarStops.Hours.Length);
            Assert.AreEqual(4, SolarStops.Names.Length);
            for (var i = 0; i < 4; i++)
            {
                Assert.GreaterOrEqual(SolarStops.Hours[i], 6);
                Assert.LessOrEqual(SolarStops.Hours[i], 19);
                if (i > 0) Assert.Greater(SolarStops.Hours[i], SolarStops.Hours[i - 1]);
            }
            Assert.AreEqual("7:00", SolarStops.Label(0));
        }
    }
}
