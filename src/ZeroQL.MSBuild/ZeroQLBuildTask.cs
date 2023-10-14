using System.Diagnostics;
using Microsoft.Build.Framework;
using ZeroQL.Core.Config;
using Task = Microsoft.Build.Utilities.Task;

namespace ZeroQL.Tasks;

public class ZeroQLBuildTask : Task
{
    [Required]
    public ITaskItem? ConfigFile { get; set; }

    [Output]
    public string CommandToExecute { get; set; }

    [Output]
    public string FileToIncludeInProject { get; set; }

    [Output]
    public string OutputFile { get; set; }

    public override bool Execute()
    {
        var configFile = ConfigFile?.ItemSpec ?? throw new ArgumentNullException(nameof(ConfigFile));
        string? cliOutput = null;
        try
        {
            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = "dotnet";
            p.StartInfo.Arguments = $"zeroql config output -c ./{configFile}";
            p.Start();
            p.WaitForExit();

            var error = p.StandardError.ReadToEnd();
            cliOutput = p.StandardOutput.ReadToEnd();
            
            if (!string.IsNullOrEmpty(error))
            {
                Log.LogError(error);
                return false;
            }
        }
        catch (Exception e)
        {
            Log.LogError("Failed to read config file: {0}", configFile);
            Log.LogErrorFromException(e);
            return false;
        }

        var output = string.IsNullOrEmpty(cliOutput)
            ? configFile
            : cliOutput;

        var commandOutput = output;
        if (!commandOutput.Contains("./obj/"))
        {
            commandOutput = string.Empty;
        }

        FileToIncludeInProject = output.Contains("./obj/")
            ? output
            : string.Empty;

        CommandToExecute = string.IsNullOrEmpty(commandOutput)
            ? $"generate --config ./{configFile}"
            : $"generate --config ./{configFile} --output {commandOutput}";

        OutputFile = output;

        return true;
    }
}