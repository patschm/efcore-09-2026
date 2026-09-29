using System.IO;
namespace WebShop.Tools.OpsConsole.Services;

// This app runs from Tools/OpsConsole/bin/<config>/net10.0-windows/, several levels below the
// repo root it needs to reference (infra/*.bicep, k8s/, Tools/CosmosMigration) - walks up from
// the executable's own location looking for WebShop.slnx as the repo-root marker, rather than
// hardcoding a relative "../../../.." that breaks the moment the build output layout changes.
public static class RepoLocator
{
    private static readonly Lazy<string> RootLazy = new(FindRoot);

    public static string Root => RootLazy.Value;

    public static string InfraDir => Path.Combine(Root, "infra");
    public static string K8sDir => Path.Combine(Root, "k8s");
    public static string CosmosMigrationProject => Path.Combine(Root, "Tools", "CosmosMigration", "WebShop.Tools.CosmosMigration.csproj");

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "WebShop.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the WebShop repo root (looked for WebShop.slnx in every parent directory of " +
            AppContext.BaseDirectory + "). This app must be run from inside a checkout of the repo.");
    }
}
