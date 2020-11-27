using System;

namespace Neo.IO.Caching
{
    // MONOREPO PATCH: public visibility
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ReflectionCacheAttribute : Attribute
    {
        /// <summary>
        /// Type
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type</param>
        public ReflectionCacheAttribute(Type type)
        {
            Type = type;
        }
    }
}
