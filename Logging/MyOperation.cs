using NuciLog.Core;

namespace GptActionsOrchestrator.Logging
{
    public sealed class MyOperation : Operation
    {
        MyOperation(string name) : base(name) { }

        public static Operation GetPersonalLogs => new MyOperation(nameof(GetPersonalLogs));

        public static Operation GitHubRepositoryRetrieval => new MyOperation(nameof(GitHubRepositoryRetrieval));
        public static Operation GitHubRepositoryFileContentRetrieval => new MyOperation(nameof(GitHubRepositoryFileContentRetrieval));
        public static Operation GitHubRepositoryReleasesRetrieval => new MyOperation(nameof(GitHubRepositoryReleasesRetrieval));
        public static Operation GitHubUserRepositoriesRetrieval => new MyOperation(nameof(GitHubUserRepositoriesRetrieval));

        public static Operation SteamStoreAppDataRetrieval => new MyOperation(nameof(SteamStoreAppDataRetrieval));
    }
}
