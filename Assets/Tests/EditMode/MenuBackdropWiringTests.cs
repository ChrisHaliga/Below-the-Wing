using System.Collections.Generic;
using BelowTheWing.Menu;
using BelowTheWing.Wiring;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class MenuBackdropWiringTests
    {
        readonly List<GameObject> m_Built = new List<GameObject>();

        [TearDown]
        public void ClearUp()
        {
            foreach (var thing in m_Built)
            {
                Object.DestroyImmediate(thing);
            }

            m_Built.Clear();
        }

        [Test]
        public void ABackdropWhoseDoorsWereNeverWiredFindsTheOnesStandingInTheScene()
        {
            var doors = Build<MenuCartDoors>("Cart");
            var backdrop = Build<MenuBackdrop>("Backdrop");

            Assert.That(backdrop.CartDoors, Is.SameAs(doors),
                "a serialized reference is lost whenever a script is reimported under a new guid, " +
                "and the menu has already been through that once");
        }

        [Test]
        public void ABackdropWithNoDoorsAnywhereNamesWhatIsMissing()
        {
            var backdrop = Build<MenuBackdrop>("Backdrop");

            var refused = Assert.Throws<MisbuiltException>(() => _ = backdrop.CartDoors);

            Assert.That(refused.Message, Does.Contain("Backdrop"));
            Assert.That(refused.Message, Does.Contain("doors"));
        }

        [Test]
        public void ABackdropWithNowhereToStandNamesTheStation()
        {
            var backdrop = Build<MenuBackdrop>("Backdrop");

            var refused = Assert.Throws<MisbuiltException>(() => backdrop.StandingAt(MenuStation.Inside));

            Assert.That(refused.Message, Does.Contain("Inside"));
        }

        [Test]
        public void ABackdropWithNoCrewNamesTheFigureItCannotFind()
        {
            var backdrop = Build<MenuBackdrop>("Backdrop");

            Assert.Throws<MisbuiltException>(() => backdrop.PlateOver(0));
        }

        T Build<T>(string called) where T : Component
        {
            var thing = new GameObject(called);

            m_Built.Add(thing);

            return thing.AddComponent<T>();
        }
    }
}
