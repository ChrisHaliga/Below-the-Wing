using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    public enum SwapKind
    {
        Unsupported,

        Model,

        Picture
    }

    public sealed class SwapReport
    {
        public string Target { get; }

        public SwapKind Kind { get; }

        public IReadOnlyList<string> Missing { get; }

        public IReadOnlyList<string> Added { get; }

        public string Note { get; }

        public int PartsNow { get; }

        public int PartsDropped { get; }

        public IReadOnlyList<string> LostTheirMesh { get; }

        public SwapReport(
            string target, SwapKind kind, IReadOnlyList<string> missing, IReadOnlyList<string> added,
            string note, int partsNow = 0, int partsDropped = 0, IReadOnlyList<string> lostTheirMesh = null)
        {
            Target = target;
            Kind = kind;
            Missing = missing;
            Added = added;
            Note = note;
            PartsNow = partsNow;
            PartsDropped = partsDropped;
            LostTheirMesh = lostTheirMesh ?? Array.Empty<string>();
        }

        public bool AnythingWentMissing => Missing.Count > 0;
    }

    public static class AssetSwap
    {
        public const string StagingFolder = "Assets/Content/_Swapping";

        static readonly string[] None = Array.Empty<string>();

        public static SwapKind KindOf(string path)
        {
            var extension = Path.GetExtension(path ?? "").ToLowerInvariant();

            return extension switch
            {
                ".fbx" => SwapKind.Model,
                ".png" => SwapKind.Picture,
                _ => SwapKind.Unsupported
            };
        }

        public static IReadOnlyList<string> WhatItCouldReplace(string incoming)
        {
            var wanted = Path.GetFileName(incoming ?? "");
            var found = new List<string>();

            if (string.IsNullOrEmpty(wanted))
            {
                return found;
            }

            foreach (var guid in AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(wanted)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (string.Equals(Path.GetFileName(path), wanted, StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith(StagingFolder, StringComparison.Ordinal))
                {
                    found.Add(path);
                }
            }

            found.Sort(StringComparer.Ordinal);

            return found;
        }

        public static IReadOnlyList<string> WhatItIsMadeOf(string modelPath)
            => PartsOf(modelPath, mustHaveAMesh: false);

        public static IReadOnlyList<string> WhatItIsSolidIn(string modelPath)
            => PartsOf(modelPath, mustHaveAMesh: true);

        static IReadOnlyList<string> PartsOf(string modelPath, bool mustHaveAMesh)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var named = new List<string>();

            if (asset == null)
            {
                return named;
            }

            foreach (var part in asset.GetComponentsInChildren<Transform>(true))
            {
                if (part == asset.transform || (mustHaveAMesh && !HasAMesh(part)))
                {
                    continue;
                }

                named.Add(part.name);
            }

            named.Sort(StringComparer.Ordinal);

            return named;
        }

        static bool HasAMesh(Transform part)
        {
            var filter = part.GetComponent<MeshFilter>();

            if (filter != null && filter.sharedMesh != null)
            {
                return true;
            }

            var skinned = part.GetComponent<SkinnedMeshRenderer>();

            return skinned != null && skinned.sharedMesh != null;
        }

        public static IReadOnlyList<string> WhatIsInTheFirstOnly(
            IReadOnlyList<string> first, IReadOnlyList<string> second)
        {
            var other = new HashSet<string>(second, StringComparer.Ordinal);
            var only = new List<string>();

            foreach (var name in first)
            {
                if (!other.Contains(name))
                {
                    only.Add(name);
                }
            }

            return only;
        }

        public static string GuidOf(string assetPath) => AssetDatabase.AssetPathToGUID(assetPath);

        public static SwapReport WhatThisWouldDo(string incoming, string target)
        {
            var kind = KindOf(target);

            if (kind == SwapKind.Unsupported)
            {
                return new SwapReport(target, kind, None, None, $"{Path.GetExtension(target)} is not a kind this takes.");
            }

            if (kind == SwapKind.Picture)
            {
                return new SwapReport(target, kind, None, None, PictureNote(incoming, target));
            }

            var staged = ImportedUnderTheTargetsOwnSettings(incoming, target);

            try
            {
                var was = WhatItIsMadeOf(target);
                var now = WhatItIsMadeOf(staged);

                if (now.Count == 0)
                {
                    return new SwapReport(
                        target, kind, None, None,
                        "The dropped file imported with nothing inside it, so it is either not a " +
                        "model or Unity could not read it.");
                }

                return new SwapReport(
                    target,
                    kind,
                    WhatIsInTheFirstOnly(was, now),
                    WhatIsInTheFirstOnly(now, was),
                    $"{was.Count} objects now, {now.Count} in the dropped file.",
                    was.Count,
                    now.Count,
                    WhatIsInTheFirstOnly(WhatItIsSolidIn(target), WhatItIsSolidIn(staged)));
            }
            finally
            {
                Discard(staged);
            }
        }

        public static string SwapItIn(string incoming, string target)
        {
            var was = GuidOf(target);

            File.Copy(incoming, Path.GetFullPath(target), overwrite: true);
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);

            var now = GuidOf(target);

            return string.Equals(was, now, StringComparison.Ordinal)
                ? ""
                : $"The guid changed from {was} to {now}, so every reference to this asset is now " +
                  "broken. Put the old guid back into the .meta by hand.";
        }

        static string PictureNote(string incoming, string target)
        {
            var now = AssetDatabase.LoadAssetAtPath<Texture2D>(target);
            var dropped = new Texture2D(2, 2);

            try
            {
                if (now == null || !dropped.LoadImage(File.ReadAllBytes(incoming)))
                {
                    return "The dropped file could not be read as a picture.";
                }

                return now.width == dropped.width && now.height == dropped.height
                    ? $"{now.width} x {now.height}, unchanged."
                    : $"{now.width} x {now.height} becomes {dropped.width} x {dropped.height}.";
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dropped);
            }
        }

        static string ImportedUnderTheTargetsOwnSettings(string incoming, string target)
        {
            Directory.CreateDirectory(StagingFolder);

            var staged = $"{StagingFolder}/{Path.GetFileName(target)}";

            File.Copy(incoming, Path.GetFullPath(staged), overwrite: true);
            CopyTheImportSettings(target, staged);

            AssetDatabase.ImportAsset(staged, ImportAssetOptions.ForceSynchronousImport);

            return staged;
        }

        static void CopyTheImportSettings(string target, string staged)
        {
            var from = $"{target}.meta";

            if (!File.Exists(from))
            {
                return;
            }

            var lines = File.ReadAllLines(from);

            for (var line = 0; line < lines.Length; line++)
            {
                if (lines[line].StartsWith("guid: ", StringComparison.Ordinal))
                {
                    lines[line] = $"guid: {GUID.Generate().ToString().Replace("-", "")}";
                    break;
                }
            }

            File.WriteAllLines($"{staged}.meta", lines);
        }

        static void Discard(string staged)
        {
            AssetDatabase.DeleteAsset(staged);

            if (Directory.Exists(StagingFolder) && Directory.GetFiles(StagingFolder).Length == 0)
            {
                AssetDatabase.DeleteAsset(StagingFolder);
            }
        }
    }
}
