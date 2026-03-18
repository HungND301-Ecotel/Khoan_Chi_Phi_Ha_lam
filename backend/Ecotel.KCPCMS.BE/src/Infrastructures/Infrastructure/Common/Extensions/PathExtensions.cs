using System.Text;
using Microsoft.Extensions.Primitives;

namespace Infrastructure.Common.Extensions;

public class PathExtensions
{
    private const string CurrentDirectoryToken = ".";
    private const string ParentDirectoryToken = "..";
    private static readonly char[] PathSeparators = ['/', '\\'];
    public static string Combine(string path, string other = null!)
    {
        if (string.IsNullOrWhiteSpace(other))
        {
            return path;
        }

        if (other.StartsWith('/') || other.StartsWith('\\'))
        {
            return other;
        }

        int index = path.LastIndexOfAny(PathSeparators);

        if (index != path.Length - 1)
        {
            return path + "/" + other;
        }

        return string.Concat(path.AsSpan(0, index + 1), other);
    }
    public static string Combine(string path, params string[] others)
    {
        string result = path;

        foreach (string t in others)
        {
            result = Combine(result, t);
        }

        return result;
    }
    public static string ResolvePath(string path)
    {
        var pathSegment = new StringSegment(path);
        if (path[0] == PathSeparators[0] || path[0] == PathSeparators[1])
        {
            pathSegment = pathSegment.Subsegment(1);
        }

        var tokenizer = new StringTokenizer(pathSegment, PathSeparators);
        bool requiresResolution = false;
        foreach (var segment in tokenizer)
        {
            if (segment.Length == 0 ||
                segment.Equals(ParentDirectoryToken) ||
                segment.Equals(CurrentDirectoryToken))
            {
                requiresResolution = true;
                break;
            }
        }

        if (!requiresResolution)
        {
            return path;
        }

        var pathSegments = new List<StringSegment>();
        foreach (var segment in tokenizer)
        {
            if (segment.Length == 0)
            {
                continue;
            }

            if (segment.Equals(ParentDirectoryToken))
            {
                if (pathSegments.Count == 0)
                {
                    return path;
                }

                pathSegments.RemoveAt(pathSegments.Count - 1);
            }
            else if (!segment.Equals(CurrentDirectoryToken))
            {
                pathSegments.Add(segment);
            }
        }

        var builder = new StringBuilder();
        foreach (var segment in pathSegments)
        {
            builder.Append('/');
            builder.Append(segment.Buffer, segment.Offset, segment.Length);
        }

        return builder.ToString();
    }
}