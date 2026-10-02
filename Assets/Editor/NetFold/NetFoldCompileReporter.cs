using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;

[InitializeOnLoad]
public static class NetFoldCompileReporter
{
    const string ReportPath = "Logs/netfold-script-errors.txt";

    static NetFoldCompileReporter()
    {
        CompilationPipeline.assemblyCompilationFinished += OnAssembly;
        CompilationPipeline.compilationFinished += OnFinished;
    }

    static void OnAssembly(string assembly, CompilerMessage[] messages)
    {
        var sb = new StringBuilder();
        int errors = 0;
        for (int i = 0; i < messages.Length; i++)
        {
            if (messages[i].type == CompilerMessageType.Error)
            {
                errors++;
                sb.AppendLine(messages[i].file + "(" + messages[i].line + "): " + messages[i].message);
            }
        }

        if (errors > 0)
        {
            File.AppendAllText(Abs(), "[" + assembly + "]\n" + sb);
        }
    }

    static void OnFinished(object _)
    {
        File.AppendAllText(Abs(), "COMPILE_FINISHED " + System.DateTime.Now.ToString("o") + "\n");
    }

    static string Abs()
    {
        string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ReportPath));
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Logs");
        return path;
    }
}
