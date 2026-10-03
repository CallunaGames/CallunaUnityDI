using System;
using System.Collections.Generic;
using System.Linq;

namespace Calluna.DI
{
    /// <summary>
    /// Readable, cached type names for the dependency graph: <c>ValueTweener&lt;Vector2&gt;</c>, <c>Outer.Inner</c>.
    /// A type whose short name is already taken by another type gets its namespace
    /// (e.g. <c>Newtonsoft.Json.JsonSerializer</c> next to <c>JsonSerializer</c>).
    /// </summary>
    internal static class TypeNames
    {
        private static readonly Dictionary<Type, string> s_names = new Dictionary<Type, string>();
        private static readonly Dictionary<string, Type> s_shortNameOwners = new Dictionary<string, Type>();
        private static readonly System.Reflection.Assembly s_diAssembly = typeof(TypeNames).Assembly;

        public static string Get(BindingKey key) =>
            key.ID == null ? Get(key.Type) : $"{Get(key.Type)} [{key.ID}]";

        public static string Get(Type type)
        {
            if (type == null)
                return "?";
            if (!s_names.TryGetValue(type, out string name))
            {
                name = Create(type);
                s_names.Add(type, name);
            }
            return name;
        }

        /// <summary>
        /// A type of the DI's own plumbing (not public), the contexts' self-binding, or a generic type over
        /// one of them (e.g. <c>Factory&lt;ChildDIContext, Resolver&gt;</c>).
        /// </summary>
        public static bool IsInternal(Type type)
        {
            if (type == null)
                return false;
            if (type.Assembly == s_diAssembly && (!(type.IsPublic || type.IsNestedPublic) || type == typeof(DIContext)))
                return true;
            return type.IsGenericType && type.GetGenericArguments().Any(IsInternal);
        }

        private static string Create(Type type)
        {
            string name = ShortName(type);
            if (!s_shortNameOwners.TryGetValue(name, out Type owner))
                s_shortNameOwners.Add(name, type);
            else if (owner != type && !string.IsNullOrEmpty(type.Namespace))
                name = $"{type.Namespace}.{name}";
            return name;
        }

        private static string ShortName(Type type)
        {
            string name = type.Name;
            if (type.IsGenericType)
            {
                int tick = name.IndexOf('`');
                if (tick >= 0)
                    name = name.Substring(0, tick);
                name += "<" + string.Join(", ", type.GetGenericArguments().Select(Get)) + ">";
            }
            return type.IsNested && !type.IsGenericParameter ? $"{Get(type.DeclaringType)}.{name}" : name;
        }
    }
}
