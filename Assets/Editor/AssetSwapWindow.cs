using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    internal sealed class AssetSwapWindow : EditorWindow
    {
        const float DropZoneHeight = 90f;

        string m_Incoming = "";
        string m_Target = "";
        SwapReport m_Report;
        string m_Done = "";
        Vector2 m_Scrolled;

        [MenuItem("Below the Wing/Swap in a new version")]
        static void Open()
        {
            var window = GetWindow<AssetSwapWindow>();
            window.titleContent = new GUIContent("Swap in a new version");
            window.minSize = new Vector2(420f, 320f);
        }

        void OnGUI()
        {
            DrawTheDropZone();

            if (string.IsNullOrEmpty(m_Incoming))
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dropped", Path.GetFileName(m_Incoming));
            EditorGUILayout.LabelField("Replaces", string.IsNullOrEmpty(m_Target) ? "nothing yet" : m_Target);

            if (GUILayout.Button(string.IsNullOrEmpty(m_Target) ? "Find the asset to replace" : "Pick a different asset"))
            {
                PickTheTarget();
            }

            m_Scrolled = EditorGUILayout.BeginScrollView(m_Scrolled);

            DrawTheReport();

            EditorGUILayout.EndScrollView();

            DrawTheButtons();
        }

        void DrawTheDropZone()
        {
            var zone = GUILayoutUtility.GetRect(0f, DropZoneHeight, GUILayout.ExpandWidth(true));

            GUI.Box(zone, "Drop one .fbx or .png here", EditorStyles.helpBox);

            var what = Event.current.type;

            if (what != EventType.DragUpdated && what != EventType.DragPerform)
            {
                return;
            }

            if (!zone.Contains(Event.current.mousePosition))
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (what != EventType.DragPerform)
            {
                return;
            }

            DragAndDrop.AcceptDrag();
            TakeTheDrop(DragAndDrop.paths);

            Event.current.Use();
        }

        void TakeTheDrop(IReadOnlyList<string> dropped)
        {
            m_Report = null;
            m_Done = "";
            m_Target = "";
            m_Incoming = "";

            if (dropped.Count != 1)
            {
                m_Done = $"{dropped.Count} files were dropped. This takes one at a time.";
                return;
            }

            if (AssetSwap.KindOf(dropped[0]) == SwapKind.Unsupported)
            {
                m_Done = $"{Path.GetFileName(dropped[0])} is not a kind this takes. It takes .fbx and .png.";
                return;
            }

            m_Incoming = dropped[0];

            var could = AssetSwap.WhatItCouldReplace(m_Incoming);

            if (could.Count == 1)
            {
                Aim(could[0]);
                return;
            }

            m_Done = could.Count == 0
                ? $"Nothing in the project is called {Path.GetFileName(m_Incoming)}. Find the asset it replaces."
                : $"{could.Count} assets are called {Path.GetFileName(m_Incoming)}. Pick the one it replaces.";
        }

        void PickTheTarget()
        {
            var picked = EditorUtility.OpenFilePanel(
                "The asset this replaces", Application.dataPath, Path.GetExtension(m_Incoming).TrimStart('.'));

            if (string.IsNullOrEmpty(picked))
            {
                return;
            }

            var full = Path.GetFullPath(picked).Replace('\\', '/');
            var root = Path.GetFullPath(Application.dataPath).Replace('\\', '/');

            if (!full.StartsWith(root, System.StringComparison.OrdinalIgnoreCase))
            {
                m_Done = "That file is outside this project, so there is nothing of ours to replace.";
                return;
            }

            Aim("Assets" + full.Substring(root.Length));
        }

        void Aim(string target)
        {
            m_Target = target;
            m_Done = "";
            m_Report = AssetSwap.WhatThisWouldDo(m_Incoming, m_Target);
        }

        void DrawTheReport()
        {
            if (!string.IsNullOrEmpty(m_Done))
            {
                EditorGUILayout.HelpBox(m_Done, MessageType.Info);
            }

            if (m_Report == null)
            {
                return;
            }

            EditorGUILayout.LabelField(m_Report.Note, EditorStyles.wordWrappedLabel);

            if (m_Report.AnythingWentMissing)
            {
                EditorGUILayout.HelpBox(
                    $"{m_Report.Missing.Count} objects are in the current model and not in the dropped one:\n"
                    + string.Join("\n", m_Report.Missing)
                    + "\n\nAnything that reads one of these by name stops finding it. Swapping is "
                    + "still allowed.",
                    MessageType.Warning);
            }

            if (m_Report.LostTheirMesh.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{m_Report.LostTheirMesh.Count} objects are still there and no longer carry a mesh:\n"
                    + string.Join("\n", m_Report.LostTheirMesh)
                    + "\n\nAnything measured from one of these refuses to build.",
                    MessageType.Warning);
            }

            if (m_Report.Added.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{m_Report.Added.Count} objects are new:\n" + string.Join("\n", m_Report.Added),
                    MessageType.Info);
            }
        }

        void DrawTheButtons()
        {
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(m_Target)))
            {
                if (GUILayout.Button("Swap it in, keeping the guid and the import settings"))
                {
                    Swap();
                }
            }

            if (m_Report != null && m_Report.Kind == SwapKind.Model && GUILayout.Button("Rebuild apron scene and prefabs"))
            {
                SceneBootstrap.Rebuild();
            }
        }

        void Swap()
        {
            var wrong = AssetSwap.SwapItIn(m_Incoming, m_Target);
            var stale = m_Report != null && m_Report.Kind == SwapKind.Model
                ? " The prefabs still hold the old model's measurements until a rebuild runs."
                : "";

            m_Done = string.IsNullOrEmpty(wrong)
                ? $"{Path.GetFileName(m_Target)} now holds the dropped file. Its guid and import settings survived.{stale}"
                : wrong;

            m_Report = m_Report != null
                ? new SwapReport(m_Report.Target, m_Report.Kind, m_Report.Missing, m_Report.Added, m_Report.Note)
                : null;
        }
    }
}
