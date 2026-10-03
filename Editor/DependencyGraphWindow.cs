using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Calluna.DI.Editor
{
    /// <summary>
    /// Shows what the <see cref="DependencyRecorder"/> recorded: contexts with their bindings and who used
    /// them, what each requester resolved, and bindings never used. Exports Mermaid or DOT (filtered by the
    /// search text). Turn on "Record on Play" before entering play mode - contexts initialize right away.
    /// </summary>
    public class DependencyGraphWindow : EditorWindow
    {
        private enum Tab { Contexts, Requesters, Unused }

        private static readonly string[] s_tabNames = { "Contexts", "Requesters", "Unused bindings" };
        private const string HideInternalsPrefsKey = "Calluna.DI.DependencyGraph.HideInternals";

        private Tab _tab;
        private string _search = string.Empty;
        private Vector2 _scroll;
        private readonly HashSet<string> _expanded = new HashSet<string>();
        private DependencyBinding _selected;
        private GUIStyle _richLabel;
        private GUIStyle _countLabel;

        private static bool HideInternals => EditorPrefs.GetBool(HideInternalsPrefsKey, true);

        [MenuItem("Window/Calluna/DI Dependency Graph")]
        public static void Open() => GetWindow<DependencyGraphWindow>("DI Dependency Graph");

        private void OnEnable() => DependencyRecorder.OnChanged += Repaint;

        private void OnDisable() => DependencyRecorder.OnChanged -= Repaint;

        // Edges are recorded without events - refresh while playing.
        private void OnInspectorUpdate()
        {
            if (DependencyRecorder.IsRecording)
                Repaint();
        }

        private void OnGUI()
        {
            _richLabel ??= new GUIStyle(EditorStyles.label) { richText = true };
            _countLabel ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            DrawToolbar();
            DependencyGraph graph = DependencyRecorder.Graph;
            if (!graph.Contexts.Any())
            {
                EditorGUILayout.HelpBox(
                    "Nothing recorded yet. Turn on \"Record on Play\" and enter play mode - the contexts are recorded " +
                    "when they initialize, the resolves while the game runs.", MessageType.Info);
                return;
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case Tab.Contexts: DrawContexts(graph); break;
                case Tab.Requesters: DrawRequesters(graph); break;
                case Tab.Unused: DrawUnused(graph); break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                bool recordOnPlay = EditorPrefs.GetBool(DependencyRecorder.RecordOnPlayPrefsKey, false);
                bool newRecordOnPlay = GUILayout.Toggle(recordOnPlay, "Record on Play", EditorStyles.toolbarButton);
                if (newRecordOnPlay != recordOnPlay)
                    EditorPrefs.SetBool(DependencyRecorder.RecordOnPlayPrefsKey, newRecordOnPlay);

                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                {
                    string label = DependencyRecorder.IsRecording ? "● Recording" : "Record";
                    if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(90)))
                    {
                        if (DependencyRecorder.IsRecording)
                            DependencyRecorder.Stop();
                        else
                            DependencyRecorder.Start();
                    }
                }
                bool hideInternals = HideInternals;
                bool newHideInternals = GUILayout.Toggle(hideInternals,
                    new GUIContent("Hide DI internals", "Leaves out the DI's own plumbing - in the views and the export."),
                    EditorStyles.toolbarButton);
                if (newHideInternals != hideInternals)
                    EditorPrefs.SetBool(HideInternalsPrefsKey, newHideInternals);
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
                {
                    DependencyRecorder.Clear();
                    _selected = null;
                }

                GUILayout.Space(8);
                _tab = (Tab)GUILayout.Toolbar((int)_tab, s_tabNames, EditorStyles.toolbarButton, GUILayout.Width(330));
                GUILayout.FlexibleSpace();
                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(80), GUILayout.MaxWidth(220));

                if (EditorGUILayout.DropdownButton(new GUIContent("Export"), FocusType.Passive, EditorStyles.toolbarDropDown))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Mermaid file..."), false, () => Export("mmd", DependencyRecorder.ToMermaid(_search, HideInternals)));
                    menu.AddItem(new GUIContent("DOT file..."), false, () => Export("dot", DependencyRecorder.ToDot(_search, HideInternals)));
                    menu.AddItem(new GUIContent("Copy Mermaid"), false, () => EditorGUIUtility.systemCopyBuffer = DependencyRecorder.ToMermaid(_search, HideInternals));
                    menu.ShowAsContext();
                }
            }
        }

        private static void Export(string extension, string content)
        {
            string path = EditorUtility.SaveFilePanel("Export dependency graph", string.Empty, "di-dependencies", extension);
            if (!string.IsNullOrEmpty(path))
                System.IO.File.WriteAllText(path, content);
        }

        // --- Contexts ---

        private void DrawContexts(DependencyGraph graph)
        {
            foreach (DependencyContext context in graph.Contexts.OrderBy(c => c.Name))
            {
                List<DependencyBinding> bindings = context.Bindings
                    .Where(b => IsShown(b) && (Matches(context.Name) || Matches(b.Contract) || Matches(b.Concrete)))
                    .OrderBy(b => b.Contract)
                    .ToList();
                if (bindings.Count == 0)
                    continue;
                if (!Foldout("context:" + context.Name, $"{DependencyGraphExporter.ContextTitle(context)}  ({bindings.Count})"))
                    continue;
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (DependencyBinding binding in bindings)
                        DrawBinding(graph, binding);
                }
            }
        }

        private void DrawBinding(DependencyGraph graph, DependencyBinding binding)
        {
            string text = $"<b>{Escape(binding.Contract)}</b>   {Escape(DependencyGraphExporter.BindingInfo(binding))}";
            if (binding.ResolveCount == 0)
                text = $"<color=#888888>{text}</color>";
            Rect rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            if (GUI.Button(rect, text, _richLabel))
                _selected = _selected == binding ? null : binding;
            if (_selected != binding)
                return;
            using (new EditorGUI.IndentLevelScope())
            {
                List<DependencyEdge> users = graph.Edges
                    .Where(e => e.Kind == DependencyKind.Binding && e.ProviderContext == binding.Context &&
                                binding.Contracts.Contains(e.Contract) && IsShown(e))
                    .OrderByDescending(e => e.Count)
                    .ToList();
                if (users.Count == 0)
                    EditorGUILayout.LabelField("Not used while recording.", EditorStyles.miniLabel);
                foreach (DependencyEdge edge in users)
                {
                    string contract = binding.Contracts.Count > 1 ? $"   as {edge.Contract}" : string.Empty;
                    Row($"← {edge.Requester}{contract}", $"{edge.Count}×");
                }
            }
        }

        // --- Requesters ---

        private void DrawRequesters(DependencyGraph graph)
        {
            foreach (IGrouping<string, DependencyEdge> group in graph.Edges
                         .Where(e => IsShown(e) && (Matches(e.Requester) || Matches(e.Contract) || Matches(e.ProviderContext)))
                         .GroupBy(e => e.Requester)
                         .OrderBy(g => g.Key))
            {
                if (!Foldout("requester:" + group.Key, $"{group.Key}  ({group.Count()})"))
                    continue;
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (DependencyEdge edge in group.OrderBy(e => e.Kind).ThenBy(e => e.Contract))
                        Row(Describe(edge), $"{edge.Count}×");
                }
            }
        }

        private static string Describe(DependencyEdge edge) => edge.Kind switch
        {
            DependencyKind.Binding => $"→ {edge.Contract}   from {edge.ProviderContext}",
            DependencyKind.Argument => $"→ {edge.Contract}   (argument)",
            _ => $"→ {edge.Contract}   (optional, not bound)"
        };

        // --- Unused ---

        private void DrawUnused(DependencyGraph graph)
        {
            EditorGUILayout.HelpBox(
                "Bindings that were never resolved or created while recording. Only meaningful after the parts of " +
                "the game that use them were played; bindings resolved only in rare flows show up here too.",
                MessageType.None);
            foreach (DependencyContext context in graph.Contexts.OrderBy(c => c.Name))
            {
                List<DependencyBinding> unused = context.Bindings
                    .Where(b => b.ResolveCount == 0 && IsShown(b) &&
                                (Matches(context.Name) || Matches(b.Contract) || Matches(b.Concrete)))
                    .OrderBy(b => b.Contract)
                    .ToList();
                if (unused.Count == 0)
                    continue;
                EditorGUILayout.LabelField($"{context.Name}  ({unused.Count})", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (DependencyBinding binding in unused)
                        Row($"{binding.Contract}   {DependencyGraphExporter.BindingInfo(binding)}", string.Empty);
                }
            }
        }

        // --- Helpers ---

        // A full-width line with an optional right-aligned count; the full text is also the tooltip.
        private void Row(string text, string count)
        {
            const float countWidth = 56;
            Rect rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            float textWidth = string.IsNullOrEmpty(count) ? rect.width : rect.width - countWidth;
            GUI.Label(new Rect(rect.x, rect.y, textWidth, rect.height), new GUIContent(text, text), EditorStyles.label);
            if (!string.IsNullOrEmpty(count))
                GUI.Label(new Rect(rect.xMax - countWidth, rect.y, countWidth, rect.height), count, _countLabel);
        }

        private bool Foldout(string key, string label)
        {
            bool searching = !string.IsNullOrEmpty(_search);
            bool expanded = searching || _expanded.Contains(key);
            bool newExpanded = EditorGUILayout.Foldout(expanded, label, true);
            if (!searching && newExpanded != expanded)
            {
                if (newExpanded)
                    _expanded.Add(key);
                else
                    _expanded.Remove(key);
            }
            return newExpanded;
        }

        private static bool IsShown(DependencyBinding binding) => !(HideInternals && binding.IsInternal);

        private static bool IsShown(DependencyEdge edge) => !(HideInternals && edge.IsInternal);

        private bool Matches(string text) =>
            string.IsNullOrEmpty(_search) ||
            (text != null && text.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0);

        // Rich text would read generic brackets as tags.
        private static string Escape(string text) => text.Replace("<", "<​");
    }
}
