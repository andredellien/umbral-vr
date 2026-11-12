using System;
using NUnit.Framework;

namespace Umbral.Core.Tests
{
    public class SessionFlowTests
    {
        private static Catalog Catalog() => new Catalog
        {
            units = new[]
            {
                new UnitInfo { id = "A", status = "available" },
                new UnitInfo { id = "B" }, // sin estado = disponible
                new UnitInfo { id = "C", status = "sold" },
                new UnitInfo { id = "D", status = "reserved" },
            }
        };

        [Test]
        public void EmpiezaEnElLobbyYAvanzaEnOrden()
        {
            var flow = new SessionFlow();
            Assert.AreEqual(SessionStage.Lobby, flow.Stage);
            Assert.IsTrue(flow.Next());
            Assert.AreEqual(SessionStage.Maqueta, flow.Stage);
            Assert.IsTrue(flow.Next());
            Assert.AreEqual(SessionStage.Llegada, flow.Stage);
            Assert.IsFalse(flow.Next(), "No hay etapa después de la llegada");
            Assert.AreEqual(SessionStage.Llegada, flow.Stage);
        }

        [Test]
        public void SePuedeSaltarPorIndiceYPorEtapa()
        {
            var flow = new SessionFlow();
            flow.JumpTo(2);
            Assert.AreEqual(SessionStage.Llegada, flow.Stage);
            flow.JumpTo(SessionStage.Lobby);
            Assert.AreEqual(SessionStage.Lobby, flow.Stage);
            Assert.Throws<ArgumentOutOfRangeException>(() => flow.JumpTo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => flow.JumpTo(SessionFlow.StageCount));
        }

        [Test]
        public void StageChanged_SoloSeDisparaSiCambia()
        {
            var flow = new SessionFlow();
            var n = 0;
            flow.StageChanged += _ => n++;
            flow.JumpTo(SessionStage.Lobby); // ya estaba
            Assert.AreEqual(0, n);
            flow.JumpTo(SessionStage.Maqueta);
            Assert.AreEqual(1, n);
        }

        [Test]
        public void SoloSePuedeElegirUnaUnidadDisponible()
        {
            var flow = new SessionFlow();
            var catalog = Catalog();
            Assert.IsTrue(flow.SelectUnit(catalog, "A"));
            Assert.IsTrue(flow.SelectUnit(catalog, "B"));
            Assert.AreEqual("B", flow.SelectedUnitId);

            Assert.IsFalse(flow.SelectUnit(catalog, "C"));
            Assert.IsFalse(flow.SelectUnit(catalog, "D"));
            Assert.IsFalse(flow.SelectUnit(catalog, "Z"));
            Assert.AreEqual("B", flow.SelectedUnitId, "Un intento inválido no cambia la elección");
        }

        [Test]
        public void StatusOf_VacioONuloEsDisponible()
        {
            Assert.AreEqual(UnitStatus.Available, CatalogQueries.StatusOf(new UnitInfo()));
            Assert.AreEqual(UnitStatus.Available, CatalogQueries.StatusOf(new UnitInfo { status = "" }));
            Assert.AreEqual(UnitStatus.Sold, CatalogQueries.StatusOf(new UnitInfo { status = "sold" }));
            Assert.AreEqual(UnitStatus.Reserved, CatalogQueries.StatusOf(new UnitInfo { status = "reserved" }));
        }

        [Test]
        public void PickFresher_ElSeedNuevoGanaALaCacheVieja()
        {
            var cache = new Catalog { version = 1 };
            var seed = new Catalog { version = 2 };
            Assert.AreSame(seed, CatalogQueries.PickFresher(cache, seed));
            var sameVersionCache = new Catalog { version = 2 };
            Assert.AreSame(sameVersionCache, CatalogQueries.PickFresher(sameVersionCache, seed), "A igual versión manda la caché (puede venir del servidor)");
            Assert.AreSame(seed, CatalogQueries.PickFresher(null, seed));
            Assert.AreSame(cache, CatalogQueries.PickFresher(cache, null));
        }
    }

    public class TutorialStepsTests
    {
        [Test]
        public void LosPasosVanEnOrden()
        {
            var t = new TutorialSteps();
            Assert.AreEqual(TutorialStep.Aim, t.Current);
            Assert.IsFalse(t.Complete(TutorialStep.Grab), "Agarrar antes de tiempo no cuenta");
            Assert.IsFalse(t.Complete(TutorialStep.Teleport));
            Assert.IsTrue(t.Complete(TutorialStep.Aim));
            Assert.AreEqual(TutorialStep.Teleport, t.Current);
            Assert.IsTrue(t.Complete(TutorialStep.Teleport));
            Assert.IsFalse(t.Completed);
            Assert.IsTrue(t.Complete(TutorialStep.Grab));
            Assert.IsTrue(t.Completed);
            Assert.AreEqual(TutorialSteps.Count, t.DoneCount);
        }

        [Test]
        public void AllDone_SeDisparaUnaSolaVez()
        {
            var t = new TutorialSteps();
            var all = 0;
            t.AllDone += () => all++;
            t.Complete(TutorialStep.Aim);
            t.Complete(TutorialStep.Teleport);
            t.Complete(TutorialStep.Grab);
            t.Complete(TutorialStep.Grab);
            t.Complete(TutorialStep.Aim);
            Assert.AreEqual(1, all);
        }
    }
}
