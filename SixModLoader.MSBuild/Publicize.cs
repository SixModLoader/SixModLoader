using System.IO;
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

            foreach (var input in Input)
            {
                Log.LogMessage(MessageImportance.Normal, "Publicizing " + input);
                Publicizer.Publicize(input.ItemSpec);
            }

            return true;
        }
    }
}