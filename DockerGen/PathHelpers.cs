using System.Text;

namespace DockerGen;

/// <summary>
/// Helpers to work with paths.
/// </summary>
public static class PathHelpers
{
    private static readonly char[] Separators = { '\\', '/' };

    /// <summary>
    /// Indicates whether paths are case sensitive on current platform.
    /// </summary>
    public static bool PathsCaseSensitive => OperatingSystem.IsWindows();

    /// <summary>
    /// Gets a hash code of specified directory ignoring trailing separators by default
    /// </summary>
    public static int GetPathHashCode(string path, bool ignoreTrailingSeparators = true)
    {
        return (ignoreTrailingSeparators ? path.TrimEnd(Separators) : path).ToUpperInvariant().GetHashCode();
    }

    /// <summary>
    /// Performs a recursive file search based on the supplied pattern.
    /// </summary>
    public static IEnumerable<string> FindFilesRecursive(
        string directory,
        string filter = "*",
        bool makeRelative = false)
    {
        var queue = new Queue<string>();
        queue.Enqueue(directory);

        while (queue.Count > 0)
        {
            string dir = queue.Dequeue();
            foreach (var file in Directory.GetFiles(dir, filter))
                yield return makeRelative ? MakeRelative(file, directory) : file;

            foreach (var child in Directory.GetDirectories(dir))
                queue.Enqueue(child);
        }
    }

    /// <summary>
    /// Compares paths based on their elements.
    /// </summary>
    public static bool PathEquals(string path1, string path2)
    {
        var parts1 = ExplodePath(path1);
        var parts2 = ExplodePath(path2);

        if (parts1.Length != parts2.Length)
            return false;

        // Compare paths ignoring the case on Windows.
        var comparison = PathsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        for (int i = 0; i < parts1.Length; i++)
        {
            if (!string.Equals(parts1[i], parts2[i], comparison))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks whether file directory contains specified directory directory.
    /// </summary>
    public static bool IsFileUnderPath(string filePath, string directory)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(directory))
            return false;

        var fileParts = ExplodePath(filePath);
        var dirParts = ExplodePath(directory);

        if (dirParts.Length > fileParts.Length)
            return false;

        for (int i = 0; i < dirParts.Length; i++)
        {
            if (!string.Equals(dirParts[i], fileParts[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Recursively deletes specified directory with all its files and subdirs, optionally performing an action
    /// before deleting each file.
    /// </summary>
    public static void DeleteDirectory(string path, Action<string>? childPreAction = null)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException("Directory doesn't exist!");

        foreach (var file in Directory.GetFiles(path))
        {
            if (childPreAction != null)
                childPreAction(file);

            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (var subDir in Directory.GetDirectories(path))
            DeleteDirectory(subDir, childPreAction);

        Directory.Delete(path);
    }

    /// <summary>
    /// Checks whether specified filename has a specified extension.
    /// </summary>
    public static bool HasExtension(string fileName, string extension)
    {
        return !string.IsNullOrEmpty(fileName)
               && !string.IsNullOrEmpty(extension)
               && fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures that specified directory has a trailing <see cref="Path.DirectorySeparatorChar"/>.
    /// </summary>
    public static string EnsureTrailingSeparator(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (Path.EndsInDirectorySeparator(path))
            return path;

        return path + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// Rebases an absolute <paramref name="path"/> on top of <paramref name="target"/> relative to <paramref name="source"/> directory.
    /// </summary>
    public static string RebasePath(string path, string source, string target)
    {
        return MakeAbsolute(MakeRelative(path, source), target);
    }

    /// <summary>
    /// Ensures a directory has Unix-style slashes.
    /// </summary>
    public static string EnsureUnixSlashes(string path)
    {
        if (path.IndexOf('\\') == -1)
            return path;

        return path.Replace('\\', '/');
    }

    /// <summary>
    /// Ensures a directory has correct platform-specific slashes.
    /// </summary>
    public static string EnsurePlatformSlashes(string path)
    {
        char correctSlash = OperatingSystem.IsWindows() ? '\\' : '/';
        char incorrectSlash = OperatingSystem.IsWindows() ? '/' : '\\';

        if (path.IndexOf(incorrectSlash) == -1)
            return path;

        return path.Replace(incorrectSlash, correctSlash);
    }

    /// <summary>
    /// Makes an absolute directory relative to a specified directory.
    /// </summary>
    public static string MakeRelative(string path, string relativeTo)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(relativeTo))
            return path;

        var pathParts = ExplodePath(path);
        var relativeParts = ExplodePath(relativeTo);
        int cnt;

        for (cnt = 0; cnt < Math.Min(pathParts.Length, relativeParts.Length); cnt++)
            if (!string.Equals(pathParts[cnt], relativeParts[cnt], StringComparison.OrdinalIgnoreCase))
                break;

        if (cnt == 0)
            return path;

        var sb = new StringBuilder();
        for (int i = 0; i < (relativeParts.Length - cnt); i++)
            sb.Append(@"..").Append(Path.DirectorySeparatorChar);

        for (int i = cnt; i < pathParts.Length; i++)
        {
            sb.Append(pathParts[i]);
            if (i < pathParts.Length - 1)
                sb.Append(Path.DirectorySeparatorChar);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Makes a specified directory absolute relative to <paramref name="relativeTo"/>.
    /// </summary>
    public static string MakeAbsolute(string path, string relativeTo)
    {
        if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(relativeTo))
            return relativeTo;

        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(relativeTo))
            return path;

        if (Path.IsPathRooted(path))
            return path;

        var pathParts = ExplodePath(path);
        var relativeParts = ExplodePath(relativeTo);
        int cnt;

        for (cnt = 0; cnt < pathParts.Length; cnt++)
            if (pathParts[cnt] != "..")
                break;

        var sb = new StringBuilder();

        for (int i = 0; i < relativeParts.Length - cnt; i++)
            sb.Append(relativeParts[i]).Append(Path.DirectorySeparatorChar);

        for (int i = cnt; i < pathParts.Length; i++)
        {
            sb.Append(pathParts[i]);
            if (i < pathParts.Length - 1)
                sb.Append(Path.DirectorySeparatorChar);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Explodes a directory into its components.
    /// </summary>
    public static string[] ExplodePath(string path)
    {
        return path.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string GetDirectory(string fileName)
        => Path.GetDirectoryName(fileName) ?? throw new SystemException("Failed to get directory");

    /// <summary>
    /// Adds a suffix to the filename, before file extension.
    /// </summary>
    /// <param name="fileName">File name to add suffix to.</param>
    /// <param name="suffix">A suffix to add.</param>
    public static string AddSuffix(string fileName, string suffix)
    {
        string ext = Path.GetExtension(fileName);
        if (ext == string.Empty)
            return fileName + suffix;

        return fileName.Insert(fileName.Length - ext.Length, suffix);
    }

    /// <summary>
    /// Checks whether specified directory exists and creates it if it doesn't.
    /// </summary>
    /// <param name="directory">Directory to check.</param>
    public static void EnsureDirectoryExists(string directory)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }
}