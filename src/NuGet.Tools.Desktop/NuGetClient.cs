using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace NuGet.Tools;

public class NuGetClient
{
    private const string ApiKeyHeader = "X-NuGet-ApiKey";
    private const string ProtocolVersionHeader = "X-NuGet-Protocol-Version";
    private const string ProtocolVersion = "4.1.0";

    /// <summary>
    /// Push a package
    /// <br>
    /// PUT https://www.nuget.org/api/v2/package
    /// </summary>
    /// <param name="key">For example, X-NuGet-ApiKey: {USER_API_KEY}</param>
    /// <returns>
    /// 201, 202 The package was successfully pushed
    /// <br>
    /// 400	The provided package is invalid
    /// <br>
    /// 409	A package with the provided ID and version already exists
    /// </returns>
    public static async Task<bool> PushAsync(FileStream file, string key)
    {
        /*
         * The request header Content-Type is multipart/form-data and the first item in the request body is the raw bytes of the .nupkg being pushed. Subsequent items in the multipart body are ignored. The file name or any other headers of the multipart items are ignored.
         */

        //byte[] data = new byte[file.Length];
        //file.Read(data, 0, data.Length);

        using var http = new HttpClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation(ApiKeyHeader, key);
        http.DefaultRequestHeaders.TryAddWithoutValidation(ProtocolVersionHeader, ProtocolVersion);

        //MultipartContent multipart = new MultipartContent("form-data");
        //multipart.Add(new ByteArrayContent(data));

        var content = new MultipartFormDataContent();

        var packageContent = new StreamContent(file);
        packageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        //"package" and "package.nupkg" are random names for content deserializing
        //not tied to actual package name.
        content.Add(packageContent, "package", "package.nupkg");

        //// Send the data in chunks so that it can be canceled if auth fails.
        //// Otherwise the whole package needs to be sent to the server before the PUT fails.
        //request.Headers.TransferEncodingChunked = true;

        var ret = await http.PutAsync($"https://www.nuget.org/api/v2/package", content).ConfigureAwait(false);
        return ret.IsSuccessStatusCode;
    }

    /// <summary>
    /// Enumerate package versions
    /// <br/>
    /// GET https://api.nuget.org/v3-flatcontainer/{@id}/{LOWER_ID}/index.json
    /// GET https://www.myget.org/F/{org}/api/v3/flatcontainer/{id}/index.json
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static async Task<string[]?> VersionsAsync(string id)
    {
        using var http = new HttpClient();

        try
        {
            var json = await http.GetFromJsonAsync<VersionResponse>($"https://api.nuget.org/v3-flatcontainer/{id.ToLowerInvariant()}/index.json");

            return json?.Versions;
        }
        catch (System.Exception)
        {
        }

        return null;
    }

    /// <summary>
    /// Delete a package
    /// <br/>
    /// nuget.org interprets the package delete request as an "unlist"
    /// <br>
    /// DELETE https://www.nuget.org/api/v2/package/{ID}/{VERSION}
    /// </summary>
    /// <param name="id">The ID of the package to delete</param>
    /// <param name="version">The version of the package to delete</param>
    /// <param name="key">For example, X-NuGet-ApiKey: {USER_API_KEY}</param>
    /// <returns>
    /// 204	The package was deleted
    /// <br/>
    /// 404	No package with the provided ID and VERSION exists
    /// </returns>
    public static async Task<bool> DeleteAsync(string id, string version, string key)
    {
        //using var http = _httpClientFactory.CreateClient();
        using var http = new HttpClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation(ApiKeyHeader, key);

        var ret = await http.DeleteAsync($"https://www.nuget.org/api/v2/package/{id.ToLowerInvariant()}/{version}").ConfigureAwait(false);
        return ret.IsSuccessStatusCode;
    }

    /// <summary>
    /// Relist a package
    /// <br>
    /// POST https://www.nuget.org/api/v2/package/{ID}/{VERSION}
    /// </summary>
    /// <param name="id">The ID of the package to relist</param>
    /// <param name="version">The version of the package to relist</param>
    /// <param name="key">For example, X-NuGet-ApiKey: {USER_API_KEY}</param>
    /// <returns>
    /// 200	The package is now listed
    /// <br/>
    /// 404	No package with the provided ID and VERSION exists
    /// </returns>
    public static async Task<bool> RelistAsync(string id, string version, string key)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add(ApiKeyHeader, key);

        var ret = await http.PostAsync($"https://www.nuget.org/api/v2/package/{id.ToLowerInvariant()}/{version}", null).ConfigureAwait(false);
        return ret.IsSuccessStatusCode;
    }

    private record VersionResponse
    {
        public string[]? Versions { get; set; }
    }
}