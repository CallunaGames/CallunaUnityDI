using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Calluna.DI
{
    /// <summary>
    /// Writes a <see cref="DependencyGraph"/> as Mermaid (flowchart) or DOT (Graphviz): contexts as groups
    /// with their bindings (one node per binding, with all its contracts), requesters pointing at what they
    /// resolved. A requester that is the concrete type of exactly one binding is drawn as that binding, so the
    /// graph connects. Bindings never used are drawn dashed. An optional filter (case-insensitive) keeps the
    /// edges and bindings whose types or contexts contain it; the DI's own plumbing is left out by default.
    /// </summary>
    public static class DependencyGraphExporter
    {
        public static string ToMermaid(DependencyGraph graph, string filter = null, bool hideInternals = true)
        {
            Model model = new Model(graph, filter, hideInternals);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("flowchart LR");
            sb.AppendLine("    classDef unused stroke-dasharray: 4 4,color:#888");
            sb.AppendLine("    classDef requester fill:#eef,stroke:#88a");
            foreach (DependencyContext context in model.Contexts)
            {
                sb.AppendLine($"    subgraph {model.ContextId(context)}[\"{Mermaid(ContextTitle(context))}\"]");
                foreach (DependencyBinding binding in model.BindingsOf(context))
                {
                    string cls = binding.ResolveCount == 0 ? ":::unused" : string.Empty;
                    sb.AppendLine($"        {model.BindingId(binding)}[\"{Mermaid(binding.Contract)}<br/>{Mermaid(BindingInfo(binding))}\"]{cls}");
                }
                sb.AppendLine("    end");
            }
            foreach (string requester in model.FreeRequesters)
                sb.AppendLine($"    {model.RequesterId(requester)}([\"{Mermaid(requester)}\"]):::requester");
            foreach (DependencyEdge edge in model.Edges)
            {
                string from = model.SourceId(edge.Requester);
                string label = $"|\"{edge.Count}\"|";
                switch (edge.Kind)
                {
                    case DependencyKind.Binding:
                        sb.AppendLine($"    {from} -->{label} {model.TargetId(edge)}");
                        break;
                    case DependencyKind.Argument:
                        sb.AppendLine($"    {from} -.->{label} {model.TargetId(edge)}[/\"arg: {Mermaid(edge.Contract)}\"/]");
                        break;
                    case DependencyKind.MissingOptional:
                        sb.AppendLine($"    {from} -.->{label} {model.TargetId(edge)}[\\\"missing: {Mermaid(edge.Contract)}\"\\]");
                        break;
                }
            }
            return sb.ToString();
        }

        public static string ToDot(DependencyGraph graph, string filter = null, bool hideInternals = true)
        {
            Model model = new Model(graph, filter, hideInternals);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("digraph DI {");
            sb.AppendLine("    rankdir=LR;");
            sb.AppendLine("    node [fontname=\"Helvetica\", fontsize=10];");
            sb.AppendLine("    edge [fontname=\"Helvetica\", fontsize=9];");
            foreach (DependencyContext context in model.Contexts)
            {
                sb.AppendLine($"    subgraph cluster_{model.ContextId(context)} {{");
                sb.AppendLine($"        label=\"{Dot(ContextTitle(context))}\";");
                foreach (DependencyBinding binding in model.BindingsOf(context))
                {
                    string style = binding.ResolveCount == 0 ? ", style=dashed, fontcolor=gray50" : string.Empty;
                    sb.AppendLine($"        {model.BindingId(binding)} [shape=box, label=\"{Dot(binding.Contract)}\\n{Dot(BindingInfo(binding))}\"{style}];");
                }
                sb.AppendLine("    }");
            }
            foreach (string requester in model.FreeRequesters)
                sb.AppendLine($"    {model.RequesterId(requester)} [shape=ellipse, style=filled, fillcolor=\"#eeeeff\", label=\"{Dot(requester)}\"];");
            foreach (DependencyEdge edge in model.Edges)
            {
                string from = model.SourceId(edge.Requester);
                string to = model.TargetId(edge);
                if (edge.Kind == DependencyKind.Argument)
                    sb.AppendLine($"    {to} [shape=parallelogram, label=\"arg: {Dot(edge.Contract)}\"];");
                else if (edge.Kind == DependencyKind.MissingOptional)
                    sb.AppendLine($"    {to} [shape=trapezium, fontcolor=gray50, label=\"missing: {Dot(edge.Contract)}\"];");
                string style = edge.Kind == DependencyKind.Binding ? string.Empty : ", style=dashed";
                sb.AppendLine($"    {from} -> {to} [label=\"{edge.Count}\"{style}];");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        public static string ContextTitle(DependencyContext context)
        {
            string title = context.Parent == null ? context.Name : $"{context.Name} (parent: {context.Parent})";
            return context.InitCount > 1 ? $"{title} ×{context.InitCount}" : title;
        }

        public static string BindingInfo(DependencyBinding binding)
        {
            string amount = binding.AmountMode == InstanceAmountMode.Single
                ? "Single"
                : binding.Tracked ? "PerRequest" : "PerRequest, untracked";
            string concrete = binding.Concrete == null || binding.Contracts.Contains(binding.Concrete) ||
                              binding.Contract.EndsWith(binding.Concrete, StringComparison.Ordinal)
                ? string.Empty
                : binding.Concrete + " · ";
            return $"{concrete}{amount} · {binding.CreationMode} · used {binding.ResolveCount}×";
        }

        private static string Mermaid(string text) => text
            .Replace("\"", "#quot;")
            .Replace("<", "#lt;")
            .Replace(">", "#gt;");

        private static string Dot(string text) => text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");

        /// <summary>The filtered graph with stable node ids.</summary>
        private sealed class Model
        {
            public readonly List<DependencyContext> Contexts = new List<DependencyContext>();
            public readonly List<DependencyEdge> Edges = new List<DependencyEdge>();
            public readonly List<string> FreeRequesters = new List<string>();

            private readonly DependencyGraph _graph;
            private readonly Dictionary<DependencyContext, List<DependencyBinding>> _bindings =
                new Dictionary<DependencyContext, List<DependencyBinding>>();
            private readonly Dictionary<object, string> _ids = new Dictionary<object, string>();
            private readonly Dictionary<string, DependencyBinding> _bindingOfConcrete = new Dictionary<string, DependencyBinding>();

            public Model(DependencyGraph graph, string filter, bool hideInternals)
            {
                _graph = graph;
                bool all = string.IsNullOrWhiteSpace(filter);
                bool Matches(string text) => all || (text != null && text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

                // A requester is drawn as a binding when exactly one binding has it as concrete type.
                HashSet<string> ambiguous = new HashSet<string>();
                foreach (DependencyContext context in graph.Contexts)
                    foreach (DependencyBinding binding in context.Bindings)
                        if (binding.Concrete != null && !_bindingOfConcrete.TryAdd(binding.Concrete, binding))
                            ambiguous.Add(binding.Concrete);
                foreach (string concrete in ambiguous)
                    _bindingOfConcrete.Remove(concrete);

                HashSet<DependencyBinding> included = new HashSet<DependencyBinding>();
                foreach (DependencyEdge edge in graph.Edges.OrderBy(e => e.Requester).ThenBy(e => e.Contract))
                {
                    if (hideInternals && edge.IsInternal)
                        continue;
                    DependencyBinding target = edge.Kind == DependencyKind.Binding
                        ? graph.FindBinding(edge.ProviderContext, edge.Contract)
                        : null;
                    if (!Matches(edge.Requester) && !Matches(edge.Contract) && !Matches(edge.ProviderContext) &&
                        !Matches(target?.Concrete))
                        continue;
                    Edges.Add(edge);
                    if (target != null)
                        included.Add(target);
                    if (_bindingOfConcrete.TryGetValue(edge.Requester, out DependencyBinding source))
                        included.Add(source);
                    else if (!FreeRequesters.Contains(edge.Requester))
                        FreeRequesters.Add(edge.Requester);
                }

                foreach (DependencyContext context in graph.Contexts.OrderBy(c => c.Name))
                {
                    List<DependencyBinding> bindings = context.Bindings
                        .Where(b => included.Contains(b) ||
                                    (!(hideInternals && b.IsInternal) &&
                                     (Matches(b.Contract) || Matches(b.Concrete) || Matches(context.Name))))
                        .OrderBy(b => b.Contract)
                        .ToList();
                    if (bindings.Count == 0)
                        continue;
                    Contexts.Add(context);
                    _bindings.Add(context, bindings);
                }
            }

            public IEnumerable<DependencyBinding> BindingsOf(DependencyContext context) => _bindings[context];

            public string ContextId(DependencyContext context) => Id(context, "c");
            public string BindingId(DependencyBinding binding) => Id(binding, "b");
            public string RequesterId(string requester) => Id("requester:" + requester, "r");

            public string SourceId(string requester) =>
                _bindingOfConcrete.TryGetValue(requester, out DependencyBinding binding)
                    ? BindingId(binding)
                    : RequesterId(requester);

            public string TargetId(DependencyEdge edge)
            {
                if (edge.Kind != DependencyKind.Binding)
                    return Id($"{edge.Kind}:{edge.Contract}", edge.Kind == DependencyKind.Argument ? "a" : "m");
                DependencyBinding binding = _graph.FindBinding(edge.ProviderContext, edge.Contract);
                return binding != null ? BindingId(binding) : Id($"binding:{edge.ProviderContext}:{edge.Contract}", "b");
            }

            private string Id(object node, string prefix)
            {
                if (!_ids.TryGetValue(node, out string id))
                {
                    id = prefix + _ids.Count;
                    _ids.Add(node, id);
                }
                return id;
            }
        }
    }
}
