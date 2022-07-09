using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;

using Octokit;

using Serilog;

using System;
using System.IO;
using System.Linq;

using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
internal partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Archives);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    private readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] private readonly Solution Solution;
    [GitRepository] private readonly GitRepository GitRepository;
    private GitHubActions GitHubActions => GitHubActions.Instance;
    private GitHubClient GitHubClient;

    private AbsolutePath SourceDirectory => RootDirectory / "src";
    private AbsolutePath OutputDirectory => RootDirectory / "output";
    private AbsolutePath PublishDirectory => RootDirectory / "publish";
    private AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();
    }

    private Target Initial => _ => _
        .Description("Initial")
        .OnlyWhenStatic(() => IsServerBuild)
        .Executes(() =>
        {
        });

    private Target Clean => _ => _
        .Description("Clean Solution")
        .DependsOn(Initial)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
            EnsureCleanDirectory(PublishDirectory);
        });

    private Target Restore => _ => _
        .Description("Restore Solution")
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .SetProjectFile(Solution)
            );
        });

    private Target Compile => _ => _
        .Description("Compile Solution")
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(OutputDirectory)
                .SetNoRestore(true)
            );
        });

    private string[] platforms = new[]
    {
        "win-x86", "win-x64", "win-arm", "win-arm64",
        "linux-x64","linux-arm","osx-x64"
    };

    private Target Publish => _ => _
        .Description("Publish Project")
        .DependsOn(Compile)
        .Executes(() =>
        {
            var projectName = "NuGet.Tools.Desktop";
            var project = Solution.GetProject(projectName);
            foreach (var platform in platforms)
            {
                DotNetPublish(_ => _
                    .SetProject(project.Path)
                    .SetConfiguration(Configuration)
                    .SetRuntime(platform)
                    .SetSelfContained(false)
                    .SetPublishSingleFile(true)
                    .SetPublishTrimmed(false)
                    .SetOutput(PublishDirectory / platform)
                    .SetNoRestore(false)
                );
            }
        });

    private Target Archives => _ => _
        .Description("Archives Package")
        //.OnlyWhenStatic(() => IsServerBuild, () => Configuration.Equals(Configuration.Release))
        .DependsOn(Publish)
        .Executes(() =>
        {
            foreach (var platform in platforms)
            {
                var dir = PublishDirectory / platform;
                if (Directory.Exists(dir))
                {
                    var archive = ArtifactsDirectory / $"{platform}.zip";

                    CompressZip(dir, ArtifactsDirectory / "app" / $"NuGetTools-{platform}-{DateTime.Now:yyyyMMddHH}.zip", fileMode: System.IO.FileMode.CreateNew);
                }
            }
        });

    private Target Artifacts => _ => _
        .DependsOn(Archives)
        .OnlyWhenStatic(() => IsServerBuild)
        .OnlyWhenStatic(() => GlobFiles(ArtifactsDirectory, "**/*.zip").Count() > 0)
        .Produces(ArtifactsDirectory / "**/*.zip")
        .Description("Upload Artifacts")
        .Executes(() =>
        {
            //Log.Information("Upload artifacts to azure...");
            //AzurePipelines
            //    .UploadArtifacts("artifacts", "artifacts", ArtifactsDirectory);
            //Log.Information("Upload artifacts to azure finished.");
        });

    private Target Deploy => _ => _
        .Description("Deploy")
        .DependsOn(Artifacts, GitHubRelease)
        .Executes(() =>
        {
            Log.Information("Deployed");
        });
}