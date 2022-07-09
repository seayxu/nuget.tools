using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

using System;
using System.Collections.Generic;
using System.Linq;

public static class PackageRepositoryFactory
{
    public static string BaseUrl => "https://www.nuget.org";
    public static string BaseV2Url => "https://www.nuget.org/api/v2";

    public static SourceRepository CreateRepository(PackageSource packageSource, IEnumerable<Lazy<INuGetResourceProvider>>? additionalProviders)
    {
        var providers = Repository.Provider.GetCoreV3();

        if (additionalProviders != null)
        {
            providers = providers.Concat(additionalProviders);
        }

        return Repository.CreateSource(providers, packageSource);
    }

    public static SourceRepository CreateRepository(PackageSource packageSource) => CreateRepository(packageSource, null);

    public static SourceRepository CreateRepository(string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        Uri uri;
        try
        {
            uri = new Uri(source);
        }
        catch (UriFormatException)
        {
            throw new ArgumentException("Invalid URL", nameof(source));
        }

        return CreateRepository(new PackageSource(source));
    }
}