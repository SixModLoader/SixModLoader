using System;
using System.Linq;
using System.Reflection;
using NuGet.Versioning;

namespace SixModLoader.Mods
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class VersionLabelsAttribute : Attribute
    {
        public string Labels { get; }

        public VersionLabelsAttribute(string labels)
        {
            Labels = labels;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ModAttribute : Attribute
    {
        internal bool Auto { get; set; }
        public string Name { get; private set; }
        public SemanticVersion Version { get; private set; }
        public string[] Authors { get; private set; }

        public override string ToString()
        {
            return $"{Name} ({Version})";
        }

        public ModAttribute(string name, string version, string[] authors)
        {
            Name = name;
            Version = SemanticVersion.Parse(version);
            Authors = authors;
        }

        /// <summary>
        /// Automatically get mod info from assembly
        /// <br/><see cref="Name"/> from <see cref="AssemblyTitleAttribute.Title"/> or <see cref="AssemblyName.Name"/>
        /// <br/><see cref="Version"/> from <see cref="AssemblyName.Version"/> and <see cref="VersionLabelsAttribute.Labels"/>
        /// <br/><see cref="Authors"/> from <see cref="AssemblyCompanyAttribute.Company"/> (split by ",")
        /// </summary>
        public ModAttribute()
        {
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
                Version = new SemanticVersion(assemblyName.Version.Major, assemblyName.Version.Minor, assemblyName.Version.Build, assembly.GetCustomAttribute<VersionLabelsAttribute>()?.Labels);
                Authors = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company?.Split(',').Select(x => x.Trim()).ToArray() ?? new string[0];
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
    }
}