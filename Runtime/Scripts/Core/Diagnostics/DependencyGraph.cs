using System.Collections.Generic;

namespace Calluna.DI
{
    /// <summary>
    /// What <see cref="DependencyRecorder"/> recorded, aggregated by type: contexts with their bindings,
    /// and who resolved which contract from which context how often. Contexts of the same name (e.g. the
    /// GameObjectContexts of a pooled prefab) are merged.
    /// </summary>
    public sealed class DependencyGraph
    {
        internal readonly Dictionary<string, DependencyContext> ContextsByName = new Dictionary<string, DependencyContext>();
        internal readonly Dictionary<(string, string, string, DependencyKind), DependencyEdge> EdgesByKey =
            new Dictionary<(string, string, string, DependencyKind), DependencyEdge>();

        public IEnumerable<DependencyContext> Contexts => ContextsByName.Values;
        public IEnumerable<DependencyEdge> Edges => EdgesByKey.Values;

        /// <summary>The binding of <paramref name="contract"/> in <paramref name="context"/>, if recorded.</summary>
        public DependencyBinding FindBinding(string context, string contract) =>
            ContextsByName.TryGetValue(context, out DependencyContext found) &&
            found.BindingsByContract.TryGetValue(contract, out DependencyBinding binding)
                ? binding
                : null;

        internal DependencyContext GetOrAddContext(string name)
        {
            if (!ContextsByName.TryGetValue(name, out DependencyContext context))
            {
                context = new DependencyContext(name);
                ContextsByName.Add(name, context);
            }
            return context;
        }

        internal void Clear()
        {
            ContextsByName.Clear();
            EdgesByKey.Clear();
        }
    }

    public sealed class DependencyContext
    {
        // Every contract of a binding (Bind<A>().And<B>()) points to the same node.
        internal readonly Dictionary<string, DependencyBinding> BindingsByContract = new Dictionary<string, DependencyBinding>();
        private readonly List<DependencyBinding> _bindings = new List<DependencyBinding>();

        public string Name { get; }
        /// <summary>The context this one resolves through; null for the AppContext.</summary>
        public string Parent { get; internal set; }
        /// <summary>How often a context of this name was initialized.</summary>
        public int InitCount { get; internal set; }
        public IEnumerable<DependencyBinding> Bindings => _bindings;

        internal DependencyContext(string name) => Name = name;

        /// <summary>The node of a binding with these contracts; merges nodes recorded for single contracts before.</summary>
        internal DependencyBinding GetOrAddBinding(IReadOnlyList<string> contracts)
        {
            DependencyBinding node = null;
            foreach (string contract in contracts)
            {
                if (!BindingsByContract.TryGetValue(contract, out DependencyBinding existing) || existing == node)
                    continue;
                if (node == null)
                    node = existing;
                else
                    Merge(existing, node);
            }
            if (node == null)
            {
                node = new DependencyBinding(Name);
                _bindings.Add(node);
            }
            foreach (string contract in contracts)
            {
                node.AddContract(contract);
                BindingsByContract[contract] = node;
            }
            return node;
        }

        private void Merge(DependencyBinding source, DependencyBinding target)
        {
            foreach (string contract in source.Contracts)
            {
                target.AddContract(contract);
                BindingsByContract[contract] = target;
            }
            target.ResolveCount += source.ResolveCount;
            _bindings.Remove(source);
        }
    }

    public sealed class DependencyBinding
    {
        private readonly List<string> _contracts = new List<string>();

        public string Context { get; }
        /// <summary>
        /// The contract types (with their IDs, if any) joined by " | ";
        /// "(non-resolvable) …" for <c>AsNonResolvable</c> bindings.
        /// </summary>
        public string Contract { get; private set; } = string.Empty;
        public IReadOnlyList<string> Contracts => _contracts;
        public string Concrete { get; internal set; }
        public InstanceCreationMode CreationMode { get; internal set; }
        public InstanceAmountMode AmountMode { get; internal set; }
        public bool Tracked { get; internal set; }
        /// <summary>A binding of the DI's own plumbing, or a context's binding of itself.</summary>
        public bool IsInternal { get; internal set; }
        /// <summary>How often it was resolved (or created eagerly). 0: bound, but never used while recording.</summary>
        public int ResolveCount { get; internal set; }

        internal DependencyBinding(string context) => Context = context;

        internal void AddContract(string contract)
        {
            if (_contracts.Contains(contract))
                return;
            _contracts.Add(contract);
            _contracts.Sort(System.StringComparer.Ordinal);
            Contract = string.Join(" | ", _contracts);
        }
    }

    public enum DependencyKind
    {
        /// <summary>Resolved through a binding of <see cref="DependencyEdge.ProviderContext"/>.</summary>
        Binding,
        /// <summary>An argument passed to the requester (<c>WithArgument</c>, factories, pools).</summary>
        Argument,
        /// <summary><c>ResolveOptional</c> found no binding.</summary>
        MissingOptional
    }

    public sealed class DependencyEdge
    {
        /// <summary>
        /// The type whose injection or creation resolved the contract;
        /// <see cref="DependencyRecorder.OutsideInjection"/> for resolves through a kept resolver later on.
        /// </summary>
        public string Requester { get; }
        /// <summary>The single contract that was requested.</summary>
        public string Contract { get; }
        /// <summary>The context whose binding served the request; empty for arguments and missing optionals.</summary>
        public string ProviderContext { get; }
        public DependencyKind Kind { get; }
        /// <summary>Requested by the DI itself, or of the DI's own plumbing.</summary>
        public bool IsInternal { get; }
        public int Count { get; internal set; }

        internal DependencyEdge(string requester, string contract, string providerContext, DependencyKind kind, bool isInternal)
        {
            Requester = requester;
            Contract = contract;
            ProviderContext = providerContext;
            Kind = kind;
            IsInternal = isInternal;
        }
    }
}
