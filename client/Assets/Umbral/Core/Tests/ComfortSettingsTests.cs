using NUnit.Framework;

namespace Umbral.Core.Tests
{
    public class ComfortSettingsTests
    {
        [Test] public void ElGiroEsPorPasosDe30Grados() => Assert.AreEqual(30f, ComfortSettings.SnapTurnDegrees);
        [Test] public void AlturaDentroDelRangoNoCambia() => Assert.AreEqual(1.7f, ComfortSettings.ClampEyeHeight(1.7f));
        [Test] public void AlturaMuyBajaSeLimita() => Assert.AreEqual(ComfortSettings.MinEyeHeight, ComfortSettings.ClampEyeHeight(0.2f));
        [Test] public void AlturaMuyAltaSeLimita() => Assert.AreEqual(ComfortSettings.MaxEyeHeight, ComfortSettings.ClampEyeHeight(5f));
        [Test] public void ValorInvalidoUsaLaAlturaDePie() => Assert.AreEqual(ComfortSettings.StandingEyeHeight, ComfortSettings.ClampEyeHeight(float.NaN));
        [Test] public void ElFundidoEsBreve() => Assert.Less(ComfortSettings.FadeOutSeconds + ComfortSettings.FadeInSeconds, 0.5f);
    }
}
