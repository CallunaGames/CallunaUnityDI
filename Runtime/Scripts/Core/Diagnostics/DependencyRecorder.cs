using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Calluna.DI
{
    /// <summary>
    /// Records the dependency graph at runtime: which type resolved which contract from which context, and
    /// the bindings of every context (including those never resolved). Aggregated by type, so pooled objects
    /// don't multiply the graph. View it in the editor (Window > Calluna > DI Dependency Graph) or export it
    /// as Mermaid or DOT.
    /// <para>
    /// Available in the editor - started on entering play mode when "Record on Play" is on - and in builds
    /// with the scripting define <c>CALLUNA_DI_RECORDER</c>, where it starts automatically; export it there
    /// with <see cref="WriteMermaid"/> / <see cref="WriteDot"/>. Without the define, <see cref="IsRecording"/>
    /// is constantly false and the hooks cost nothing.
    /// </para>
    /// </summary>
    public static class DependencyRecorder
    {
        /// <summary>Requester of resolves outside any injection - e.g. through a resolver kept for later.</summary>
        public const string OutsideInjection = "(outside injection)";
        /// <summary>Requester of the instances a context creates eagerly (<c>NonLazy</c>, <c>AsNonResolvable</c>).</summary>
        public const string NonLazy = "(non-lazy)";
        public const string RecordOnPlayPrefsKey = "Calluna.DI.DependencyRecorder.RecordOnPlay";

        private static readonly DependencyGraph s_graph = new DependencyGraph();
        private static readonly List<string> s_requesters = new List<string>();
        private static bool s_recording;

        public static DependencyGraph Graph => s_graph;

#if UNITY_EDITOR || CALLUNA_DI_RECORDER
        public static bool IsRecording => s_recording;
#else
        public static bool IsRecording => false;
#endif

        public static bool IsAvailable =>
#if UNITY_EDITOR || CALLUNA_DI_RECORDER
            true;
#else
            false;
#endif

        public static event Action OnChanged;

        public static void Start()
        {
            if (!IsAvailable)
            {
                Debug.LogWarning($"[{nameof(DependencyRecorder)}] Not available in this build - add the scripting define CALLUNA_DI_RECORDER.");
                return;
            }
            s_recording = true;
            OnChanged?.Invoke();
        }

        public static void Stop()
        {
            s_recording = false;
            s_requesters.Clear();
            OnChanged?.Invoke();
        }

        public static void Clear()
        {
            s_graph.Clear();
            s_requesters.Clear();
            OnChanged?.Invoke();
        }

        public static string ToMermaid(string filter = null) => DependencyGraphExporter.ToMermaid(s_graph, filter);

        public static string ToDot(string filter = null) => DependencyGraphExporter.ToDot(s_graph, filter);

        public static void WriteMermaid(string path, string filter = null) =>
            File.WriteAllText(path, ToMermaid(filter), Encoding.UTF8);

        public static void WriteDot(string path, string filter = null) =>
            File.WriteAllText(path, ToDot(filter), Encoding.UTF8);

        // Each play session (and each build start) records a graph of its own - also without a domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void StartOnLoad()
        {
            Clear();
            s_recording = false;
#if UNITY_EDITOR
            if (UnityEditor.EditorPrefs.GetBool(RecordOnPlayPrefsKey, false))
                Start();
#elif CALLUNA_DI_RECORDER
            Start();
#endif
        }

        // --- Hooks (callers check IsRecording first) ---

        internal static void PushRequester(Type requester) => s_requesters.Add(TypeNames.Get(requester));

        internal static void PopRequester()
        {
            if (s_requesters.Count > 0)
                s_requesters.RemoveAt(s_requesters.Count - 1);
        }

        internal static void RecordContext(string name, string parent, IEnumerable<KeyValuePair<BindingKey, Binding>> bindings,
            IEnumerable<Binding> nonResolvableBindings)
        {
            DependencyContext context = s_graph.GetOrAddContext(name);
            context.Parent = parent;
            context.InitCount++;
            foreach (KeyValuePair<BindingKey, Binding> entry in bindings)
                Describe(context.GetOrAddBinding(TypeNames.Get(entry.Key)), entry.Value);
            foreach (Binding binding in nonResolvableBindings)
                Describe(context.GetOrAddBinding(NonResolvableContract(binding)), binding);
            OnChanged?.Invoke();
        }

        internal static void RecordResolve(BindingKey key, string providerContext, Binding binding)
        {
            string contract = TypeNames.Get(key);
            DependencyBinding node = s_graph.GetOrAddContext(providerContext).GetOrAddBinding(contract);
            if (node.Concrete == null)
                Describe(node, binding);
            node.ResolveCount++;
            AddEdge(contract, providerContext, DependencyKind.Binding);
        }

        internal static void RecordNonLazy(string providerContext, Binding binding, List<BindingKey> keys)
        {
            s_requesters.Add(NonLazy);
            try
            {
                if (keys.Count == 0)
                {
                    string contract = NonResolvableContract(binding);
                    DependencyBinding node = s_graph.GetOrAddContext(providerContext).GetOrAddBinding(contract);
                    if (node.Concrete == null)
                        Describe(node, binding);
                    node.ResolveCount++;
                    AddEdge(contract, providerContext, DependencyKind.Binding);
                }
                foreach (BindingKey key in keys)
                    RecordResolve(key, providerContext, binding);
            }
            finally
            {
                PopRequester();
            }
        }

        private static string NonResolvableContract(Binding binding) =>
            $"(non-resolvable) {TypeNames.Get(binding.ConcreteType)}";

        internal static void RecordArgument(BindingKey key) =>
            AddEdge(TypeNames.Get(key), string.Empty, DependencyKind.Argument);

        internal static void RecordMissingOptional(BindingKey key) =>
            AddEdge(TypeNames.Get(key), string.Empty, DependencyKind.MissingOptional);

        private static void AddEdge(string contract, string providerContext, DependencyKind kind)
        {
            string requester = s_requesters.Count > 0 ? s_requesters[s_requesters.Count - 1] : OutsideInjection;
            var key = (requester, contract, providerContext, kind);
            if (!s_graph.EdgesByKey.TryGetValue(key, out DependencyEdge edge))
            {
                edge = new DependencyEdge(requester, contract, providerContext, kind);
                s_graph.EdgesByKey.Add(key, edge);
            }
            edge.Count++;
        }

        private static void Describe(DependencyBinding node, Binding binding)
        {
            node.Concrete = TypeNames.Get(binding.ConcreteType);
            node.CreationMode = binding.CreationMode;
            node.AmountMode = binding.AmountMode;
            node.Tracked = binding.AmountMode == InstanceAmountMode.Single || binding.TrackInstances;
        }
    }
}
