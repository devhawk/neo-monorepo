using Microsoft.Extensions.Configuration;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.IO.Path;

namespace Neo.Plugins
{
    public abstract class Plugin : IDisposable
    {
        public static readonly List<Plugin> Plugins = new List<Plugin>();
        internal static readonly List<ILogPlugin> Loggers = new List<ILogPlugin>();
        internal static readonly Dictionary<string, IStorageProvider> Storages = new Dictionary<string, IStorageProvider>();
        internal static readonly List<IPersistencePlugin> PersistencePlugins = new List<IPersistencePlugin>();
        internal static readonly List<IP2PPlugin> P2PPlugins = new List<IP2PPlugin>();
        internal static readonly List<IMemoryPoolTxObserverPlugin> TxObserverPlugins = new List<IMemoryPoolTxObserverPlugin>();

        public static readonly string PluginsDirectory = Combine(GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
        private static readonly FileSystemWatcher configWatcher;

        public virtual string ConfigFile => Combine(PluginsDirectory, GetType().Assembly.GetName().Name, "config.json");
        public virtual string Name => GetType().Name;
        public virtual string Description => "";
        public virtual string Path => Combine(PluginsDirectory, GetType().Assembly.ManifestModule.ScopeName);
        public virtual Version Version => GetType().Assembly.GetName().Version;

        static Plugin()
        {
            if (Directory.Exists(PluginsDirectory))
            {
                configWatcher = new FileSystemWatcher(PluginsDirectory)
                {
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size,
                };
                configWatcher.Changed += ConfigWatcher_Changed;
                configWatcher.Created += ConfigWatcher_Changed;
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        protected Plugin()
        {
            Plugins.Add(this);

            if (this is ILogPlugin logger) Loggers.Add(logger);
            if (this is IStorageProvider storage) Storages.Add(Name, storage);
            if (this is IP2PPlugin p2p) P2PPlugins.Add(p2p);
            if (this is IPersistencePlugin persistence) PersistencePlugins.Add(persistence);
            if (this is IMemoryPoolTxObserverPlugin txObserver) TxObserverPlugins.Add(txObserver);
            if (this is IApplicationEngineProvider provider) ApplicationEngine.SetApplicationEngineProvider(provider);

            Configure();
        }

        protected virtual void Configure()
        {
        }

        private static void ConfigWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            switch (GetExtension(e.Name))
            {
                case ".json":
                    try
                    {
                        Plugins.FirstOrDefault(p => p.ConfigFile == e.FullPath)?.Configure();
                    }
                    catch (FormatException) { }
                    break;
                case ".dll":
                    if (e.ChangeType != WatcherChangeTypes.Created) return;
                    if (GetDirectoryName(e.FullPath) != PluginsDirectory) return;
                    try
                    {
                        LoadPlugin(Assembly.Load(File.ReadAllBytes(e.FullPath)));
                    }
                    catch { }
                    break;
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains(".resources"))
                return null;

            AssemblyName an = new AssemblyName(args.Name);

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly is null)
                assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == an.Name);
            if (assembly != null) return assembly;

            string filename = an.Name + ".dll";
            string path = filename;
            if (!File.Exists(path)) path = Combine(GetDirectoryName(Assembly.GetEntryAssembly().Location), filename);
            if (!File.Exists(path)) path = Combine(PluginsDirectory, filename);
            if (!File.Exists(path)) path = Combine(PluginsDirectory, args.RequestingAssembly.GetName().Name, filename);
            if (!File.Exists(path)) return null;

            try
            {
                return Assembly.Load(File.ReadAllBytes(path));
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(Plugin), LogLevel.Error, ex);
                return null;
            }
        }

        public virtual void Dispose()
        {
        }

        protected IConfigurationSection GetConfiguration()
        {
            return new ConfigurationBuilder().AddJsonFile(ConfigFile, optional: true).Build().GetSection("PluginConfiguration");
        }

        private static void LoadPlugin(Assembly assembly)
        {
            foreach (Type type in assembly.ExportedTypes)
            {
                if (!type.IsSubclassOf(typeof(Plugin))) continue;
                if (type.IsAbstract) continue;

                ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                try
                {
                    constructor?.Invoke(null);
                }
                catch (Exception ex)
                {
                    Utility.Log(nameof(Plugin), LogLevel.Error, ex);
                }
            }
        }

        internal static void LoadPlugins()
        {
            if (!Directory.Exists(PluginsDirectory)) return;
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string filename in Directory.EnumerateFiles(PluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    assemblies.Add(Assembly.Load(File.ReadAllBytes(filename)));
                }
                catch { }
            }
            foreach (Assembly assembly in assemblies)
            {
                LoadPlugin(assembly);
            }
        }

        protected void Log(object message, LogLevel level = LogLevel.Info)
        {
            Utility.Log($"{nameof(Plugin)}:{Name}", level, message);
        }

        protected virtual bool OnMessage(object message)
        {
            return false;
        }

        internal protected virtual void OnSystemLoaded(NeoSystem system)
        {
        }

        public static bool SendMessage(object message)
        {
            foreach (Plugin plugin in Plugins)
                if (plugin.OnMessage(message))
                    return true;
            return false;
        }
    }
}
