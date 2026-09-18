using System;
using BelowTheWing.EditorTools;
using BelowTheWing.Menu;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class SerializedFieldsTests
    {
        GameObject m_Object;

        [SetUp]
        public void SetUp() => m_Object = new GameObject("Target");

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(m_Object);

        [Test]
        public void WritingAFieldTheComponentDoesNotHaveIsRefusedByName()
        {
            var target = m_Object.AddComponent<MenuBackdrop>();

            var refused = Assert.Throws<InvalidOperationException>(
                () => SerializedFields.Set(target, "m_Nope", 1.5f),
                "a misspelt field has to stop the build, or the scene ships with an empty reference");

            Assert.That(refused.Message, Does.Contain("MenuBackdrop").And.Contain("m_Nope"));
        }

        [Test]
        public void WritingAnObjectToAFieldTheComponentDoesNotHaveIsRefusedByName()
        {
            var target = m_Object.AddComponent<MenuBackdrop>();

            Assert.Throws<InvalidOperationException>(
                () => SerializedFields.Set(target, "m_Nope", (UnityEngine.Object)m_Object));
        }

        [Test]
        public void WritingAFieldTheComponentHasLandsInIt()
        {
            var target = m_Object.AddComponent<MenuBackdrop>();

            SerializedFields.Set(target, "m_PlateHeightMetres", 2.5f);

            var written = new UnityEditor.SerializedObject(target).FindProperty("m_PlateHeightMetres").floatValue;
            Assert.That(written, Is.EqualTo(2.5f).Within(1e-4f));
        }
    }
}
