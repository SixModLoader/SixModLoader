using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;

namespace SixModLoader.Mods
{
    public abstract class ModContainer
    {
        public ModAttribute Info { get; internal set; }
        public Assembly Assembly { get; internal set; }
        public Type Type { get; internal set; }
        public int Priority { get; internal set; } = (int) global::SixModLoader.Priority.Normal;

        public string File { get; internal set; }
        public string Directory => Path.Combine(SixModLoader.Instance.ModsPath, Info.Name.Replace(" ", "-"));

        public abstract object AbstractInstance { get; internal set; }
    }

    public class ModContainer<T> : ModContainer
    {
        public T Instance { get; internal set; }

        public override object AbstractInstance
        {
            get => Instance;
            internal set => Instance = (T) value;
        }

        public ModContainer()
        {
            var priorityAttribute = typeof(T).GetCustomAttribute<PriorityAttribute>() ?? typeof(T).Assembly.GetCustomAttribute<PriorityAttribute>();
            if (priorityAttribute != null)
            {
                Priority = priorityAttribute.Priority;
            }
        }

        public ModContainer(T instance) : this()
        {
            Assembly = typeof(T).Assembly;
            Type = typeof(T);
            File = typeof(T).Assembly.Location;
            Info = typeof(T).GetCustomAttribute<ModAttribute>();
            Instance = instance;

            Info.Update(typeof(T));
        }
    }

    public class ModManager
    {
        public SixModLoader Loader { get; }
        public List<ModContainer> Mods = new List<ModContainer>();

        public ModManager(SixModLoader loader)
        {
            Loader = loader;
        }

        public ModContainer<T> GetMod<T>()
        {
            return (ModContainer<T>) Mods.SingleOrDefault(x => x is ModContainer<T>);
        }

        public ModContainer GetMod(Assembly assembly)
        {
            return Mods.SingleOrDefault(x => x.Assembly == assembly);
        }

        public void Load()
        {
            try
            {
                foreach (var file in Directory.GetFiles(Loader.BinPath))
                {
                    if (!file.EndsWith(".dll")) continue;

                    var assembly = Assembly.LoadFile(file);
                    Logger.Debug($"Loaded {assembly}");
                }

                var toLoad = new List<ModContainer>();

                foreach (var file in Directory.GetFiles(Loader.ModsPath))
                {
                    if (!file.EndsWith(".dll")) continue;

                    Logger.Info($"Loading {file}");

                    var assembly = Assembly.LoadFile(file);
                    foreach (var type in assembly.GetTypes())
                    {
                        var modsAttribute = type.GetCustomAttribute<ModAttribute>();
                        if (modsAttribute == null) continue;

                        modsAttribute.Update(type);

                        if (type.GetConstructors().Length < 1)
                        {
                            Logger.Warn($"Mod {modsAttribute} don't have any constructors!");
                            continue;
                        }

                        var modContainer = (ModContainer) Activator.CreateInstance(typeof(ModContainer<>).MakeGenericType(type));
                        modContainer.Assembly = assembly;
                        modContainer.Type = type;
                        modContainer.File = file;
                        modContainer.Info = modsAttribute;

                        if (toLoad.Any(x => x.Info.Name == modContainer.Info.Name))
                        {
                            Logger.Error($"Duplicate mod with name {modContainer.Info.Name}");
                            break;
                        }

                        toLoad.Add(modContainer);
                        break;
                    }
                }

                foreach (var modContainer in toLoad.OrderByDescending(x => x.Priority))
                {
                    var type = modContainer.Type;
                    var modsAttribute = modContainer.Info;

                    Loader.ServiceCollection.AddSingleton(modContainer);

                    var constructor = type.GetConstructors()[0];
                    var parameters = new List<object>();
                    foreach (var parameter in constructor.GetParameters())
                    {
                        var service = Loader.Services.GetService(parameter.ParameterType);
                        if (service != null)
                        {
                            parameters.Add(service);
                        }
                        else
                        {
                            parameters.Add(null);
                            Logger.Warn($"{modsAttribute} unrecognized parameter: {parameter} (outdated loader/mod?)");
                        }
                    }

                    var modInstance = type.GetConstructors()[0].Invoke(parameters.ToArray());

                    foreach (var property in type.GetProperties(AccessTools.all).Where(x => x.GetCustomAttribute<InjectAttribute>() != null))
                    {
                        var service = Loader.Services.GetService(property.PropertyType);
                        if (service != null)
                        {
                            property.SetValue(modInstance, service);
                        }
                        else
                        {
                            Logger.Warn($"{modsAttribute} unrecognized property type: {property} (outdated loader/mod?)");
                        }
                    }

                    modContainer.AbstractInstance = modInstance;

                    Loader.EventManager.Register(modInstance);
                    Logger.Info($"Loaded {modsAttribute}");
                    Mods.Add(modContainer);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                if (e is ReflectionTypeLoadException typeLoadException)
                {
                    Logger.Error($"Exceptions: {typeLoadException.LoaderExceptions.Select(x => x.ToString()).Join()}");
                }

                throw;
            }

            Logger.Info($"Loaded {Mods.Count} {"mod".Pluralize(Mods.Count)}!");
        }
    }
}