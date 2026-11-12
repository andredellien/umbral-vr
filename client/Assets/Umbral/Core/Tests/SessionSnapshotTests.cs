using NUnit.Framework;

namespace Umbral.Core.Tests
{
    public class SessionSnapshotTests
    {
        private Catalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = new Catalog
            {
                validUntil = "2026-12-31",
                units = new[] { new UnitInfo { id = "A-301", basePrice = 520000, areaM2 = 58 } },
                surfaces = new[]
                {
                    new SurfaceInfo { id = "floor", name = "Piso", options = new[] { new OptionInfo { id = "f0", name = "Cerámica", delta = 0 }, new OptionInfo { id = "f1", name = "Madera", delta = 9800 } } },
                    new SurfaceInfo { id = "wall", name = "Pared", options = new[] { new OptionInfo { id = "w0", name = "Blanco", delta = 0 } } },
                },
            };
        }

        [Test]
        public void SinConfigurador_NoHayUnidadNiTotal()
        {
            var s = SessionSnapshot.Build("1234", new SessionFlow(), null, -1, false);
            Assert.AreEqual("1234", s.code);
            Assert.AreEqual((int)SessionStage.Lobby, s.stage);
            Assert.IsNull(s.unitId);
            Assert.AreEqual(0, s.total);
            Assert.IsEmpty(s.choices);
        }

        [Test]
        public void ConConfigurador_ReflejaAcabadosYTotal()
        {
            var c = new Configurator(catalog, "A-301");
            c.Select("floor", "f1");
            var flow = new SessionFlow { Seated = true };
            var s = SessionSnapshot.Build("1234", flow, c, 2, true);

            Assert.AreEqual("A-301", s.unitId);
            Assert.AreEqual(c.Total, s.total);
            Assert.AreEqual(2, s.choices.Length);
            Assert.AreEqual("floor", s.choices[0].surface);
            Assert.AreEqual("f1", s.choices[0].option);
            Assert.AreEqual(2, s.solarStop);
            Assert.IsTrue(s.seated);
            Assert.IsTrue(s.paused);
        }

        [Test]
        public void Describe_UsaNombresDelCatalogoYCaeAIdsSinCatalogo()
        {
            var c = new Configurator(catalog, "A-301");
            c.Select("floor", "f1");
            var s = SessionSnapshot.Build("1234", new SessionFlow(), c, 1, true);

            var text = s.Describe(catalog);
            StringAssert.Contains("Piso: Madera", text);
            StringAssert.Contains(PriceFormat.Bolivianos(c.Total), text);
            StringAssert.Contains("EN PAUSA", text);
            StringAssert.Contains("Sol: 12:00", text);

            StringAssert.Contains("floor: f1", s.Describe(null));
        }
    }

    public class SessionControlTests
    {
        [Test]
        public void PausaYReanudar_CambianElEstadoYAvisanSoloSiCambia()
        {
            var control = new SessionControl();
            var changes = 0;
            control.Changed += () => changes++;

            Assert.IsTrue(control.Apply(SessionCommand.Pause(), null));
            Assert.IsTrue(control.Paused);
            Assert.IsFalse(control.Apply(SessionCommand.Pause(), null));
            Assert.IsTrue(control.Apply(SessionCommand.Resume(), null));
            Assert.IsFalse(control.Paused);
            Assert.IsFalse(control.Apply(SessionCommand.Resume(), null));
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void Salto_LlamaAlCallbackConLaEtapa()
        {
            var control = new SessionControl();
            SessionStage? got = null;
            Assert.IsTrue(control.Apply(SessionCommand.Jump(SessionStage.Llegada), s => got = s));
            Assert.AreEqual(SessionStage.Llegada, got);
        }

        [Test]
        public void SaltoInvalidoOEnPausa_SeIgnora()
        {
            var control = new SessionControl();
            var calls = 0;
            Assert.IsFalse(control.Apply(new SessionCommand { kind = SessionCommand.JumpKind, stage = 99 }, _ => calls++));
            Assert.IsFalse(control.Apply(new SessionCommand { kind = SessionCommand.JumpKind, stage = -1 }, _ => calls++));

            control.SetPaused(true);
            Assert.IsFalse(control.Apply(SessionCommand.Jump(SessionStage.Maqueta), _ => calls++));
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void ComandoNuloODesconocido_SeIgnora()
        {
            var control = new SessionControl();
            Assert.IsFalse(control.Apply(null, null));
            Assert.IsFalse(control.Apply(new SessionCommand { kind = "descuento" }, null));
            Assert.IsFalse(control.Paused);
        }
    }
}
