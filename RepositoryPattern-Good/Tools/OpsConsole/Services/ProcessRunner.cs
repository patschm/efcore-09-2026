using System.Diagnostics;
using System.Text;

namespace WebShop.Tools.OpsConsole.Services;

public sealed record ProcessResult(int ExitCode)
{
    public bool Succeeded => ExitCode == 0;
}

// Every operation in this app (az, kubectl, docker, psql, dotnet) is shelled out to, per the
// project's own established convention this session - see project memory's "execution engine"
// decision. Routed through `cmd.exe /c` rather than launching the target executable directly:
// `az` (and several other CLI tools' Windows installers) resolve to a .cmd wrapper script, and
// Win32's CreateProcess - what Process.Start ultimately calls - has no idea how to run a .cmd
// file without cmd.exe as the interpreter, even though typing the same name at a terminal prompt
// works fine (the shell does that resolution for you). Using ArgumentList (not a hand-built
// Arguments string) for the real command's own arguments lets .NET quote each one correctly
// (paths with spaces, etc.) without this class having to reimplement Windows' quoting rules.
public static class ProcessRunner
{
    public static Task<ProcessResult> RunAsync(
        string exeName,
        IReadOnlyList<string> args,
        Action<string> onOutputLine,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default) =>
        RunCoreAsync(exeName, args, onOutputLine, onOutputLine, environmentVariables, workingDirectory, cancellationToken);

    // `az` (and kubectl/docker) routinely write to stderr even on success - a plain WARNING, a
    // deprecation notice, a "please update" nag (confirmed the hard way: `az account list`
    // prints "WARNING: A few accounts are skipped..." to stderr on every successful call that
    // has a disabled subscription). An earlier version of this method fed both streams into one
    // shared buffer via two independently-racing async readers, so that warning could land in
    // the MIDDLE of the JSON text on stdout - corrupting it just often enough to make
    // JsonDocument.Parse throw, which callers weren't even catching, so subscriptions/accounts
    // silently came back empty with no visible error anywhere. Keeping the two streams in
    // separate buffers and returning stdout ONLY is what actually fixes that - stderr is still
    // captured (StandardError) for callers that want to log/inspect it, just never mixed into
    // the text a caller is about to hand to a JSON parser.
    public static async Task<(ProcessResult Result, string StandardOutput, string StandardError)> RunAndCaptureAsync(
        string exeName,
        IReadOnlyList<string> args,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var result = await RunCoreAsync(
            exeName, args, line => stdout.AppendLine(line), line => stderr.AppendLine(line),
            environmentVariables, workingDirectory, cancellationToken);
        return (result, stdout.ToString(), stderr.ToString());
    }

    private static async Task<ProcessResult> RunCoreAsync(
        string exeName,
        IReadOnlyList<string> args,
        Action<string> onStdOutLine,
        Action<string> onStdErrLine,
        IReadOnlyDictionary<string, string>? environmentVariables,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory,
        };

        psi.ArgumentList.Add("/c");
        psi.ArgumentList.Add(exeName);
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
                psi.Environment[key] = value;
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) onStdOutLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) onStdErrLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await using (cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch { /* already exited between the check and the kill - fine, nothing to clean up */ }
        }))
        {
            await process.WaitForExitAsync(cancellationToken);
        }

        return new ProcessResult(process.ExitCode);
    }
}
