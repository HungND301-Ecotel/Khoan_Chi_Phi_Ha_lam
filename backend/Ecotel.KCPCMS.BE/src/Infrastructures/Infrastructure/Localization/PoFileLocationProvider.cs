using Infrastructure.Common.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OrchardCore.Localization;

namespace Infrastructure.Localization;

public class PoFileLocationProvider(IHostEnvironment hostingEnvironment,
        IOptions<LocalizationOptions> localizationOptions)
    : ILocalizationFileLocationProvider
{
    private readonly IFileProvider _fileProvider = hostingEnvironment.ContentRootFileProvider;
    private readonly string _resourcesContainer = localizationOptions.Value.ResourcesPath;

    public IEnumerable<IFileInfo> GetLocations(string cultureName)
    {
        // Loads all *.po files from the culture folder under the Resource Path.
        // for example, src\Host\Localization\en-US\exceptions.po
        foreach (var file in _fileProvider.GetDirectoryContents(PathExtensions.Combine(_resourcesContainer, cultureName)))
        {
            yield return file;
        }
    }
}