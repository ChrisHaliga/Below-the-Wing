using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class SceneObjectIdentityTests
    {
        const string ScenePath = "Assets/Scenes/Apron.unity";

        static readonly Regex Identity = new Regex(
            @"^\s*GlobalObjectIdHash:\s*(\d+)\s*$", RegexOptions.Multiline);

        static IReadOnlyList<string> IdentitiesOnDisk()
        {
            Assert.That(File.Exists(ScenePath), Is.True, $"there is no scene at {ScenePath}");

            var found = new List<string>();
            foreach (Match match in Identity.Matches(File.ReadAllText(ScenePath)))
            {
                found.Add(match.Groups[1].Value);
            }

            return found;
        }

        [Test]
        public void EveryNetworkedObjectInTheSceneHasAnIdentityOnDisk()
        {
            var identities = IdentitiesOnDisk();

            Assert.That(identities, Is.Not.Empty,
                "the scene holds the session, and the session is a networked object. None at all " +
                "means either the scene lost it or this is looking for the wrong thing");

            Assert.That(identities, Has.No.Member("0"),
                "a networked object saved with an identity of zero is one netcode refuses to spawn: " +
                "it reports an object that is neither a registered prefab nor an in-scene object and " +
                "creates nothing. Opening the scene in the editor works out the identity and hides " +
                "this, so it is only ever seen in a built player -- where the session never spawns, " +
                "no apron is built, and a player is left looking at bare ground. This is read from " +
                "the file for that reason: a test that loads the scene is repaired before it looks");
        }
    }
}
