using System;
using System.Linq;
using System.Reflection;
using NuGet.Versioning;

namespace SixModLoader.Mods
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModAttribute : Attribute
    {
        internal bool Auto { get; set; }
        public string Id { get; }
        public string Name { get; private set; }
        public SemanticVersion Version { get; private set; }
        public string[] Authors { get; private set; }

        public IdentifiedLogger Logger { get; private set; }
        
        public override string ToString()
        {
            return $"{Name} ({Version})";
        }

        public ModAttribute(string id, string name, string version, string[] authors)
        {
            Id = id;
            Name = name;
            Version = SemanticVersion.Parse(version);
            Authors = authors;
            Logger = new IdentifiedLogger(Name);
        }

        /// <summary>
        /// Automatically get mod info from assembly
        /// <br/><see cref="Name"/> from <see cref="AssemblyTitleAttribute.Title"/> or <see cref="AssemblyName.Name"/>
        /// <br/><see cref="Version"/> from <see cref="AssemblyInformationalVersionAttribute.InformationalVersion"/> or <see cref="AssemblyName.Version"/>
        /// <br/><see cref="Authors"/> from <see cref="AssemblyCompanyAttribute.Company"/> (split by ",")
        /// </summary>
        public ModAttribute(string id)
        {
            Id = id;
            Auto = true;
        }

        public void Update(Type type)
        {
            if (Auto)
            {
                Auto = false;
                
                var assembly = type.Assembly;
                var assemblyName = assembly.GetName();

                Name = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? assemblyName.Name;
                Authors = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company?.Split(',').Select(x => x.Trim()).ToArray() ?? new string[0];
                Logger = new IdentifiedLogger(Name);

                var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                Version = version != null ? SemanticVersion.Parse(version) : new SemanticVersion(assemblyName.Version.Major, assemblyName.Version.Minor, assemblyName.Version.Build);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
    }
}