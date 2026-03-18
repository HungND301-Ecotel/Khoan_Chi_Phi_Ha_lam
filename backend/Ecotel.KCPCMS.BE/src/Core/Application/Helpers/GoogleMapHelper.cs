using System.Text.RegularExpressions;

namespace Application.Helpers;
public static class GoogleMapHelper
{
    public static double? GetLatitude(string embedUrl)
    {
        if (string.IsNullOrWhiteSpace(embedUrl))
        {
            return null;
        }

        var match = Regex.Match(embedUrl, @"!3d([0-9\.\-]+)");
        if (match.Success && double.TryParse(match.Groups[1].Value, out double latitude))
        {
            return latitude;
        }
        return null;
    }

    public static double? GetLongitude(string embedUrl)
    {
        if (string.IsNullOrWhiteSpace(embedUrl))
        {
            return null;
        }

        var match = Regex.Match(embedUrl, @"!2d([0-9\.\-]+)");
        if (match.Success && double.TryParse(match.Groups[1].Value, out double longitude))
        {
            return longitude;
        }
        return null;
    }
}
