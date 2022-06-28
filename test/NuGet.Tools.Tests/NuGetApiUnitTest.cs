namespace NuGet.Tools.Tests
{
    public class NuGetApiUnitTest
    {
        private const string ApiKeyHeader = "X-NuGet-ApiKey";
        private const string ApiKey = "";
        private const string PackageId = "";
        private const string PackageVersion = "";

        [Fact]
        public async Task DeleteTest()
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation(ApiKeyHeader, ApiKey);
            var ret = await http.DeleteAsync($"https://www.nuget.org/api/v2/package/{PackageId.ToLowerInvariant()}/{PackageVersion}");
            Console.WriteLine(ret);
        }
    }
}