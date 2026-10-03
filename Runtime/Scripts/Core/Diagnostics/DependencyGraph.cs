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
        internal readonly Dictionary<string, DependencyBinding> BindingsByContract = new Dictionary<string, DependencyBinding>();

        public string Name { get; }
        /// <summary>The context this one resolves through; null for the AppContext.</summary>
        public string Parent { get; internal set; }
        /// <summary>How often a context of this name was initialized.</summary>
        public int InitCount { get; internal set; }
        public IEnumerable<DependencyBinding> Bindings => BindingsByContract.Values;

        internal DependencyContext(string name) => Name = name;

        internal DependencyBinding GetOrAddBinding(string contract)
        {
            if (!BindingsByContract.TryGetValue(contract, out DependencyBinding binding))
            {
                binding = new DependencyBinding(Name, contract);
                BindingsByContract.Add(contract, binding);
            }
            return binding;
        }
    }

    public sealed class DependencyBinding
    {
        public string Context { get; }
        /// <summary>Contract type (with its ID, if any); "(non-resolvable)" for <c>AsNonResolvable</c> bindings.</summary>
        public string Contract { get; }
        public string Concrete { get; internal set; }
        public InstanceCreationMode CreationMode { get; internal set; }
        public InstanceAmountMode AmountMode { get; internal set; }
        public bool Tracked { get; internal set; }
        /// <summary>How often it was resolved (or created eagerly). 0: bound, but never used while recording.</summary>
        public int ResolveCount { get; internal set; }

        internal DependencyBinding(string context, string contract)
        {
            Context = context;
            Contract = contract;
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
        public string Contract { get; }
        /// <summary>The context whose binding served the request; empty for arguments and missing optionals.</summary>
        public string ProviderContext { get; }
        public DependencyKind Kind { get; }
        public int Count { get; internal set; }

        internal DependencyEdge(string requester, string contract, string providerContext, DependencyKind kind)
        {
            Requester = requester;
            Contract = contract;
            ProviderContext = providerContext;
            Kind = kind;
        }
    }
}
