namespace NtFreX.BuildingBlocks.Standard.Extensions;

public static class PathExtensions
{
    public static string NormalizeRelativePath(string relativePath, string rootPath)
    {
        var folder = rootPath.EndsWith(@"\") ? rootPath.Substring(0, rootPath.Length - 1) : Path.GetDirectoryName(rootPath);
        var rootedPath = relativePath;
        if (!Path.IsPathRooted(relativePath) && !string.IsNullOrEmpty(folder))
            rootedPath = Path.Combine(folder, relativePath);

        //TODO: to uppercase if case insensitive file system (windows)
        return Path.GetFullPath(rootedPath);
    }
}

