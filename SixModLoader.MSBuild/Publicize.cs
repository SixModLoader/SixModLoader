using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AssemblyPublicizer.Library;
using Microsoft.Build.Framework;

namespace SixModLoader.MSBuild
{
    public class Publicize : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string CurrentDirectory { get; set; }

        [Required]
        public ITaskItem[] Input { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Normal, "Set directory to " + CurrentDirectory);
            Directory.SetCurrentDirectory(CurrentDirectory);

            var sha1 = SHA1.Create();

            foreach (var input in Input)
            {
                var file = input.ItemSpec;
                if (!File.Exists(file))
                {
                    Log.LogError("Can't find " + input);
                    continue;
                }

                var hashFile = Path.Combine("publicized_assemblies", file) + ".sha1";
                var hash = sha1.ComputeHash(File.ReadAllBytes(file));

                if (File.Exists(hashFile) && File.ReadAllBytes(hashFile).SequenceEqual(hash))
                {
                    Log.LogMessage(MessageImportance.Normal, "Skipping publicizing" + input);
                    continue;
                }

                Log.LogMessage(MessageImportance.Normal, "Publicizing " + input);
                Publicizer.Publicize(file);
                File.WriteAllBytes(hashFile, hash);
            }

            return true;
        }
    }
}