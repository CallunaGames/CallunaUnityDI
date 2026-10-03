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

        private static readonly DependencyGraph _graph = new DependencyGraph();
        private static readonly List<(string Name, bool IsInternal)> _requesters = new List<(string, bool)>();
        private static bool _recording;

        public static DependencyGraph Graph => _graph;

#if UNITY_EDITOR || CALLUNA_DI_RECORDER
        public static bool IsRecording => _recording;
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
            _recording = true;
            OnChanged?.Invoke();
        }

        public static void Stop()
        {
            _recording = false;
            _requesters.Clear();
            OnChanged?.Invoke();
        }

        public static void Clear()
        {
            _graph.Clear();
            _requesters.Clear();
            OnChanged?.Invoke();
        }

        public static string ToMermaid(string filter = null, bool hideInternals = true) =>
            DependencyGraphExporter.ToMermaid(_graph, filter, hideInternals);

        public static string ToDot(string filter = null, bool hideInternals = true) =>
            DependencyGraphExporter.ToDot(_graph, filter, hideInternals);

        public static void WriteMermaid(string path, string filter = null, bool hideInternals = true) =>
            File.WriteAllText(path, ToMermaid(filter, hideInternals), Encoding.UTF8);

        public static void WriteDot(string path, string filter = null, bool hideInternals = true) =>
            File.WriteAllText(path, ToDot(filter, hideInternals), Encoding.UTF8);

        // Each play session (and each build start) records a graph of its own - also without a domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void StartOnLoad()
        {
            Clear();
            _recording = false;
#if UNITY_EDITOR
            if (UnityEditor.EditorPrefs.GetBool(RecordOnPlayPrefsKey, false))
                Start();
#elif CALLUNA_DI_RECORDER
            Start();
#endif
        }

        // --- Hooks (callers check IsRecording first) ---

        internal static void PushRequester(Type requester) =>
            _requesters.Add((TypeNames.Get(requester), TypeNames.IsInternal(requester)));

        /// <summary>Pushes <paramref name="requester"/> while recording; dispose to pop it.</summary>
        internal static RequesterScope Requester(Type requester)
        {
            if (!IsRecording)
                return default;
            PushRequester(requester);
            return new RequesterScope(true);
        }

        internal readonly struct RequesterScope : IDisposable
        {
            private readonly bool _pushed;

            public RequesterScope(bool pushed) => _pushed = pushed;

            public void Dispose()
            {
                if (_pushed)
                    PopRequester();
            }
        }

        internal static void PopRequester()
        {
            if (_requesters.Count > 0)
                _requesters.RemoveAt(_requesters.Count - 1);
        }

        internal static void RecordContext(string name, string parent, IEnumerable<KeyValuePair<BindingKey, Binding>> bindings,
            IEnumerable<Binding> nonResolvableBindings)
        {
            DependencyContext context = _graph.GetOrAddContext(name);
            context.Parent = parent;
            context.InitCount++;
            // One node per binding with all its contracts (Bind<A>().And<B>()).
            Dictionary<Binding, List<BindingKey>> keysByBinding = new Dictionary<Binding, List<BindingKey>>();
            foreach (KeyValuePair<BindingKey, Binding> entry in bindings)
            {
                if (!keysByBinding.TryGetValue(entry.Value, out List<BindingKey> keys))
                    keysByBinding.Add(entry.Value, keys = new List<BindingKey>());
                keys.Add(entry.Key);
            }
            foreach (KeyValuePair<Binding, List<BindingKey>> entry in keysByBinding)
                Describe(context.GetOrAddBinding(Contracts(entry.Value)), entry.Value, entry.Key);
            foreach (Binding binding in nonResolvableBindings)
                Describe(context.GetOrAddBinding(new[] { NonResolvableContract(binding) }), null, binding);
            OnChanged?.Invoke();
        }

        internal static void RecordResolve(BindingKey key, string providerContext, Binding binding)
        {
            string contract = TypeNames.Get(key);
            DependencyBinding node = _graph.GetOrAddContext(providerContext).GetOrAddBinding(new[] { contract });
            if (node.Concrete == null)
                Describe(node, new[] { key }, binding);
            node.ResolveCount++;
            AddEdge(contract, providerContext, DependencyKind.Binding,
                IsInternal(key) || TypeNames.IsInternal(binding.ConcreteType));
        }

        internal static void RecordNonLazy(string providerContext, Binding binding, List<BindingKey> keys)
        {
            _requesters.Add((NonLazy, false));
            try
            {
                if (keys.Count == 0)
                {
                    string contract = NonResolvableContract(binding);
                    DependencyBinding node = _graph.GetOrAddContext(providerContext).GetOrAddBinding(new[] { contract });
                    if (node.Concrete == null)
                        Describe(node, null, binding);
                    node.ResolveCount++;
                    AddEdge(contract, providerContext, DependencyKind.Binding, TypeNames.IsInternal(binding.ConcreteType));
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
            AddEdge(TypeNames.Get(key), string.Empty, DependencyKind.Argument, IsInternal(key));

        internal static void RecordMissingOptional(BindingKey key) =>
            AddEdge(TypeNames.Get(key), string.Empty, DependencyKind.MissingOptional, IsInternal(key));

        private static bool IsInternal(BindingKey key) => TypeNames.IsInternal(key.Type);

        private static string[] Contracts(List<BindingKey> keys)
        {
            string[] contracts = new string[keys.Count];
            for (int i = 0; i < keys.Count; i++)
                contracts[i] = TypeNames.Get(keys[i]);
            return contracts;
        }

        private static void AddEdge(string contract, string providerContext, DependencyKind kind, bool contractIsInternal)
        {
            (string name, bool isInternal) requester = _requesters.Count > 0
                ? _requesters[_requesters.Count - 1]
                : (OutsideInjection, false);
            var key = (requester.name, contract, providerContext, kind);
            if (!_graph.EdgesByKey.TryGetValue(key, out DependencyEdge edge))
            {
                edge = new DependencyEdge(requester.name, contract, providerContext, kind,
                    requester.isInternal || contractIsInternal);
                _graph.EdgesByKey.Add(key, edge);
            }
            edge.Count++;
        }

        private static void Describe(DependencyBinding node, IEnumerable<BindingKey> keys, Binding binding)
        {
            node.Concrete = TypeNames.Get(binding.ConcreteType);
            node.CreationMode = binding.CreationMode;
            node.AmountMode = binding.AmountMode;
            node.Tracked = binding.AmountMode == InstanceAmountMode.Single || binding.TrackInstances;
            bool isInternal = TypeNames.IsInternal(binding.ConcreteType);
            if (keys != null)
                foreach (BindingKey key in keys)
                    isInternal |= IsInternal(key);
            node.IsInternal = isInternal;
        }
    }
}
