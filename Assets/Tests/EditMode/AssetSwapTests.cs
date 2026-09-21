using System.Collections.Generic;
using System.IO;
using BelowTheWing.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class AssetSwapTests
    {
        const string Folder = "Assets/Content/_SwapTests";

        readonly List<string> m_Dropped = new List<string>();

        [SetUp]
        public void SetUp() => Directory.CreateDirectory(Folder);

        [TearDown]
        public void TearDown()
        {
            foreach (var path in m_Dropped)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            m_Dropped.Clear();

            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.Refresh();
        }

        string APictureInTheProject(string called, int across, int down)
        {
            var at = $"{Folder}/{called}";

            File.WriteAllBytes(Path.GetFullPath(at), APicture(across, down));
            AssetDatabase.ImportAsset(at, ImportAssetOptions.ForceSynchronousImport);

            return at;
        }

        string APictureOnDisk(int across, int down)
        {
            var at = Path.Combine(Path.GetTempPath(), $"swap-{across}x{down}.png");

            File.WriteAllBytes(at, APicture(across, down));
            m_Dropped.Add(at);

            return at;
        }

        static byte[] APicture(int across, int down)
        {
            var drawn = new Texture2D(across, down);

            drawn.SetPixels(new Color[across * down]);
            drawn.Apply();

            var bytes = drawn.EncodeToPNG();
            Object.DestroyImmediate(drawn);

            return bytes;
        }

        [Test]
        public void OnlyModelsAndPicturesAreTaken()
        {
            Assert.That(AssetSwap.KindOf("a/b/thing.fbx"), Is.EqualTo(SwapKind.Model));
            Assert.That(AssetSwap.KindOf("a/b/THING.FBX"), Is.EqualTo(SwapKind.Model),
                "an extension a modelling tool wrote in capitals is the same extension");
            Assert.That(AssetSwap.KindOf("a/b/thing.png"), Is.EqualTo(SwapKind.Picture));

            Assert.That(AssetSwap.KindOf("a/b/thing.blend"), Is.EqualTo(SwapKind.Unsupported),
                "a .blend is the file a model is authored in, not the one Unity reads");
            Assert.That(AssetSwap.KindOf("a/b/thing.txt"), Is.EqualTo(SwapKind.Unsupported));
            Assert.That(AssetSwap.KindOf(""), Is.EqualTo(SwapKind.Unsupported));
        }

        [Test]
        public void ADroppedFileFindsTheAssetOfTheSameName()
        {
            var mine = APictureInTheProject("swap-me.png", 4, 4);

            var could = AssetSwap.WhatItCouldReplace("C:/somewhere/else/swap-me.png");

            Assert.That(could, Does.Contain(mine),
                "a file dropped from anywhere is matched to the project asset of the same name");
            Assert.That(could.Count, Is.EqualTo(1),
                $"one asset is called swap-me.png and {could.Count} were offered");
        }

        [Test]
        public void ANameNothingInTheProjectUsesMatchesNothing()
        {
            Assert.That(AssetSwap.WhatItCouldReplace("C:/somewhere/forklift.fbx"), Is.Empty,
                "nothing in the project is called forklift.fbx, so the window has to ask where it goes");
        }

        [Test]
        public void TheComparisonNamesWhatWentMissingAndWhatIsNew()
        {
            var was = new[] { "Body", "Door1", "Door2", "SEAT" };
            var now = new[] { "Body", "Door1", "Tailgate" };

            Assert.That(AssetSwap.WhatIsInTheFirstOnly(was, now), Is.EquivalentTo(new[] { "Door2", "SEAT" }),
                "anything the current model has and the dropped one does not is what stops being found");

            Assert.That(AssetSwap.WhatIsInTheFirstOnly(now, was), Is.EquivalentTo(new[] { "Tailgate" }));
        }

        [Test]
        public void ASwapKeepsTheGuidAndTheMetaThatCarriesIt()
        {
            var mine = APictureInTheProject("keep-my-guid.png", 4, 4);
            var meta = $"{mine}.meta";

            var guidWas = AssetSwap.GuidOf(mine);
            var metaWas = File.ReadAllBytes(meta);
            var bytesWere = File.ReadAllBytes(mine);

            Assert.That(guidWas, Is.Not.Empty, "the fixture asset never imported, so nothing is under test");

            var wrong = AssetSwap.SwapItIn(APictureOnDisk(8, 8), mine);

            Assert.That(wrong, Is.Empty, wrong);

            Assert.That(AssetSwap.GuidOf(mine), Is.EqualTo(guidWas),
                "the guid changed, which is what breaks every scene and prefab that points at this " +
                "asset while it still previews correctly in the inspector");

            Assert.That(File.ReadAllBytes(meta), Is.EqualTo(metaWas),
                "the .meta was rewritten, so the import settings it carried are gone too");

            Assert.That(File.ReadAllBytes(mine), Is.Not.EqualTo(bytesWere),
                "the asset's bytes are unchanged, so nothing was actually swapped in");
        }

        [Test]
        public void APictureReportsTheSizeItBecomes()
        {
            var mine = APictureInTheProject("resize-me.png", 4, 4);

            var report = AssetSwap.WhatThisWouldDo(APictureOnDisk(8, 8), mine);

            Assert.That(report.Kind, Is.EqualTo(SwapKind.Picture));
            Assert.That(report.Note, Does.Contain("4 x 4").And.Contain("8 x 8"),
                $"the report reads '{report.Note}', which does not say what the size becomes");

            Assert.That(report.AnythingWentMissing, Is.False, "a picture has no objects to lose");
        }

        [Test]
        public void AModelComparedWithItselfHasLostNothing()
        {
            const string jet = "Assets/Content/Vehicles/crj_200.fbx";

            var report = AssetSwap.WhatThisWouldDo(jet, jet);

            Assert.That(report.Kind, Is.EqualTo(SwapKind.Model));

            Assert.That(report.PartsDropped, Is.GreaterThan(0),
                "the dropped model read as having no objects at all, so an empty Missing list " +
                "below would say nothing about whether the comparison works");

            Assert.That(report.PartsDropped, Is.EqualTo(report.PartsNow),
                $"the same file read as {report.PartsNow} objects in the project and " +
                $"{report.PartsDropped} when staged, so the two are being imported differently");

            Assert.That(report.Missing, Is.Empty,
                $"a model compared with itself reports {string.Join(", ", report.Missing)} missing. " +
                "The dropped file is staged under a copy of the target's own import settings for " +
                "exactly this reason: under Unity's defaults the jet's Blender camera and light " +
                "come in and every swap reports differences that are not there");

            Assert.That(report.Added, Is.Empty,
                $"a model compared with itself reports {string.Join(", ", report.Added)} as new");

            Assert.That(report.LostTheirMesh, Is.Empty,
                $"a model compared with itself reports {string.Join(", ", report.LostTheirMesh)} as " +
                "having lost its mesh");
        }

        [Test]
        public void AnObjectThatKeptItsNameAndLostItsMeshIsReported()
        {
            var report = AssetSwap.WhatThisWouldDo(
                "Assets/Content/Vehicles/baggage_cart.fbx",
                "Assets/Content/Vehicles/belt_loader.fbx");

            Assert.That(AssetSwap.WhatItIsSolidIn("Assets/Content/Vehicles/crj_200.fbx"),
                Does.Not.Contain("Camera"),
                "importCameras 0 strips the camera component and leaves the object, so comparing " +
                "names alone cannot see a part that kept its name and lost its mesh");

            Assert.That(report.LostTheirMesh, Does.Contain("Belt"),
                $"the loader's Belt is neither present nor solid in a cart, and the report lists " +
                $"[{string.Join(", ", report.LostTheirMesh)}]");
        }

        [Test]
        public void AModelMissingWhatTheOldOneHadSaysWhichParts()
        {
            var report = AssetSwap.WhatThisWouldDo(
                "Assets/Content/Vehicles/baggage_cart.fbx",
                "Assets/Content/Vehicles/belt_loader.fbx");

            Assert.That(report.AnythingWentMissing, Is.True,
                "a cart dropped onto the belt loader shares almost nothing with it, and the tool " +
                "reported nothing missing");

            Assert.That(report.Missing, Does.Contain("Belt"),
                $"the loader's Belt is gone and the report lists [{string.Join(", ", report.Missing)}]");

            Assert.That(report.Added, Is.Not.Empty,
                "the cart's own parts are new here, and the report claims none are");
        }

        [Test]
        public void ASwapLeavesNoStagingFolderBehind()
        {
            var mine = APictureInTheProject("tidy-up.png", 4, 4);

            AssetSwap.WhatThisWouldDo(APictureOnDisk(8, 8), mine);

            Assert.That(Directory.Exists(AssetSwap.StagingFolder), Is.False,
                $"{AssetSwap.StagingFolder} is still in the project, so a copy of somebody's art " +
                "is sitting in Assets waiting to be committed by accident");
        }

        [Test]
        public void TheStagingCopyIsNeverOfferedAsSomethingToReplace()
        {
            Directory.CreateDirectory(AssetSwap.StagingFolder);

            var staged = $"{AssetSwap.StagingFolder}/decoy.png";
            File.WriteAllBytes(Path.GetFullPath(staged), APicture(4, 4));
            AssetDatabase.ImportAsset(staged, ImportAssetOptions.ForceSynchronousImport);

            try
            {
                Assert.That(AssetSwap.WhatItCouldReplace("C:/somewhere/decoy.png"), Is.Empty,
                    "the tool's own staging copy was offered as the asset to overwrite, which " +
                    "would swap a file into the folder it is about to delete");
            }
            finally
            {
                AssetDatabase.DeleteAsset(AssetSwap.StagingFolder);
            }
        }
    }
}
