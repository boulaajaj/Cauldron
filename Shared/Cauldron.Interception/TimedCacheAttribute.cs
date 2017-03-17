﻿#if DESKTOP
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.ComponentModel;
using System.Runtime.Caching;
using System.Text;
using System.Threading;

namespace Cauldron.Interception
{
    /// <summary>
    /// Provides a timed caching for the intercepted method.
    /// <para/>
    /// The cache is dependent to the passed arguments. The arguments requires a proper implementation of <see cref="object.GetHashCode"/> and a unique <see cref="object.ToString"/> value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class TimedCacheAttribute : Attribute
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private TimeSpan decayPeriod;

        /// <summary>
        /// Initializes a new instance of <see cref="TimedCacheAttribute"/>
        /// </summary>
        /// <param name="decayPeriod">The maximum period of cache lifetime</param>
        public TimedCacheAttribute(TimeSpan decayPeriod)
        {
            this.decayPeriod = decayPeriod;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TimedCacheAttribute"/>
        /// <para/>
        /// Default cache decay period is 30 minutes.
        /// </summary>
        public TimedCacheAttribute() : this(TimeSpan.FromMinutes(30))
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string CreateKey(string methodName, object[] arguments)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < arguments.Length; i++)
                if (arguments[i] != null)
                    sb.Append(arguments[i].GetHashCode() + arguments[i].ToString());

            sb.Append(methodName);
            return sb.ToString();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool HasCache(string key)
        {
            var cache = MemoryCache.Default;
            return cache[key] != null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetCache(string key, object content)
        {
            semaphore.Wait();

            try
            {
                var cache = MemoryCache.Default;

                var policy = new CacheItemPolicy();
                policy.AbsoluteExpiration = DateTimeOffset.Now.Add(this.decayPeriod);
                policy.ChangeMonitors.Add(new TimedCacheChangeMonitor());

                cache.Set(key, content, policy);
            }
            finally
            {
                semaphore.Release();
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public object GetCache(string key)
        {
            semaphore.Wait();

            try
            {
                var cache = MemoryCache.Default;
                return cache[key];
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    internal sealed class TimedCacheChangeMonitor : ChangeMonitor
    {
        private string uniqueId;

        public TimedCacheChangeMonitor()
        {
            this.uniqueId = Guid.NewGuid().ToString();
            ClearCache += TimedCacheChangeMonitor_ClearCache;
            base.InitializationComplete();
        }

        public static event EventHandler ClearCache;

        public override string UniqueId { get { return this.uniqueId; } }

        public static void Clear() => ClearCache?.Invoke(null, EventArgs.Empty);

        protected override void Dispose(bool disposing)
        {
            ClearCache -= TimedCacheChangeMonitor_ClearCache;
        }

        private void TimedCacheChangeMonitor_ClearCache(object sender, EventArgs e) => base.OnChanged(null);
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#endif