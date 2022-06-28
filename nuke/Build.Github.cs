using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;

using Octokit;

using Serilog;

using System;
using System.IO;

using static Nuke.Common.IO.PathConstruction;

[GitHubActions(
    "continuous.build",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    GitHubActionsImage.MacOsLatest,
    AutoGenerate = true,
    PublishArtifacts = true,
    OnPushIncludePaths = new string[] { "**" },
    OnPushExcludePaths = new string[] { ".release" },
    InvokedTargets = new[] { nameof(Artifacts) },
    CacheKeyFiles = new string[0]
)]
[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    PublishArtifacts = true,
    EnableGitHubToken = true,
    OnPushIncludePaths = new string[] { "'.release'" },
    OnPushBranches = new[] { "main" },
    InvokedTargets = new[] { nameof(Deploy) },
    CacheKeyFiles = new string[0]
)]
internal partial class Build
{
    private Target AuthenticatedGitHubClient => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !string.IsNullOrWhiteSpace(GitHubActions.Token))
        .Executes(() =>
        {
            GitHubClient = new GitHubClient(new ProductHeaderValue("nuke-build"))
            {
                Credentials = new Credentials(GitHubActions.Token, AuthenticationType.Bearer)
            };
        });

    private Target GitHubRelease => _ => _
        .Unlisted()
        .Description("Creates a GitHub release (or amends existing) and uploads the artifact")
        .OnlyWhenDynamic(() => !string.IsNullOrWhiteSpace(GitHubActions.Token))
        .DependsOn(AuthenticatedGitHubClient, Archives)
        .Executes(async () =>
        {
            Release release;
            var identifier = GitRepository.Identifier.Split("/");
            var (gitHubOwner, repoName) = (identifier[0], identifier[1]);
            var releaseName = DateTime.Now.ToString("yyyy.MM.dd.HH");

            try
            {
                release = await GitHubClient.Repository.Release.Get(gitHubOwner, repoName, releaseName);
            }
            catch (NotFoundException)
            {
                var newRelease = new NewRelease(releaseName)
                {
                    Body = GitHubActions.Sha,
                    Name = releaseName,
                    Draft = false,
                    Prerelease = GitRepository.IsOnReleaseBranch()
                };
                release = await GitHubClient.Repository.Release.Create(gitHubOwner, repoName, newRelease);
            }

            foreach (var existingAsset in release.Assets)
            {
                await GitHubClient.Repository.Release.DeleteAsset(gitHubOwner, repoName, existingAsset.Id);
            }

            Log.Information("GitHub Release {ReleaseName}", releaseName);
            var packages = GlobFiles(ArtifactsDirectory, "**/*.zip").NotNull();

            foreach (var artifact in packages)
            {
                var releaseAssetUpload = new ReleaseAssetUpload(Path.GetFileName(artifact), "application/zip", File.OpenRead(artifact), null);
                var releaseAsset = await GitHubClient.Repository.Release.UploadAsset(release, releaseAssetUpload);
                Log.Information("upload {BrowserDownloadUrl}", releaseAsset.BrowserDownloadUrl);
            }
        });
}