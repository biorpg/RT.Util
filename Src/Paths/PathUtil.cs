using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>Represents a path-related exception.</summary>
    public sealed class PathException : RTException
    {
        /// <summary>Constructor.</summary>
        public PathException() : base() { }
        /// <summary>Constructor.</summary>
        /// <param name="message">Exception message.</param>
        public PathException(string message) : base(message) { }
    }

    /// <summary>
    /// Provides path-related utilities.
    /// </summary>
    public static class PathUtil
    {
        /// <summary>
        /// Stores a copy of the value generated by AppPath. This way AppPath
        /// only needs to generate it once.
        /// </summary>
        private static string _cachedAppPath = null;

        /// <summary>
        /// Returns the full path to the directory containing the application's entry assembly.
        /// Will succeed for the main AppDomain of an application started as an .exe; will
        /// throw for anything that doesn't have an entry assembly, such as a manually created AppDomain.
        /// </summary>
        /// <seealso cref="AppPathCombine(string[])"/>
        public static string AppPath
        {
            get
            {
                if (_cachedAppPath == null)
                {
                    if (Assembly.GetEntryAssembly() == null)
                        throw new InvalidOperationException("PathUtil.AppPath is not supported in an AppDomain that is not the default AppDomain. More precisely, PathUtil.AppPath requires Assembly.GetEntryAssembly() to be non-null.");
                    _cachedAppPath = Assembly.GetEntryAssembly().Location;
                    _cachedAppPath = Path.GetDirectoryName(_cachedAppPath);
                }
                return _cachedAppPath;
            }
        }

        /// <summary>
        /// Combines the full path containing the running executable with the specified string.
        /// Ensures that only a single <see cref="Path.DirectorySeparatorChar"/> separates the two.
        /// </summary>
        public static string AppPathCombine(string path)
        {
            return Path.Combine(AppPath, path);
        }

        /// <summary>
        /// Combines the full path containing the running executable with one or more strings.
        /// Ensures that only a single <see cref="Path.DirectorySeparatorChar"/> separates
        /// the executable path and every string.
        /// </summary>
        public static string AppPathCombine(params string[] morePaths)
        {
            return Path.Combine(morePaths);
        }

        /// <summary>Normalises the specified path. A "normalised path" is a path to a
        /// directory (not a file!) which always ends with a slash.</summary>
        /// <param name="path">Path to be normalised.</param>
        /// <returns>Normalised version of <paramref name="path"/>, or null if the input was null.</returns>
        public static string NormPath(string path)
        {
            if (path == null)
                return null;
            else if (path.Length == 0)
                return "" + Path.DirectorySeparatorChar;
            else if (path[path.Length - 1] == Path.DirectorySeparatorChar)
                return path;
            else
                return path + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// Checks whether <paramref name="subpath"/> refers to a subdirectory inside <paramref name="parentPath"/>.
        /// </summary>
        public static bool IsSubpathOf(string subpath, string parentPath)
        {
            string parentPathNormalized = PathUtil.NormPath(parentPath);
            string subpathNormalized = PathUtil.NormPath(subpath);

            if (subpathNormalized.Length <= parentPathNormalized.Length)
                return false;

            return subpathNormalized.Substring(0, parentPathNormalized.Length).Equals(parentPathNormalized, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks whether <paramref name="subpath"/> refers to a subdirectory inside <paramref name="parentPath"/> or the same directory.
        /// </summary>
        public static bool IsSubpathOfOrSame(string subpath, string parentPath)
        {
            string parentPathNormalized = PathUtil.NormPath(parentPath);
            string subpathNormalized = PathUtil.NormPath(subpath);

            if (subpathNormalized.Length < parentPathNormalized.Length)
                return false;

            return subpathNormalized.Substring(0, parentPathNormalized.Length).Equals(parentPathNormalized, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Expands all occurrences of "$(NAME)" in the specified string with the special folder path
        /// for the current machine/user. See remarks for details.</summary>
        /// <remarks>
        /// <para>Expands all occurrences of "$(NAME)", where NAME is the name of one of the values of the <see cref="Environment.SpecialFolder"/>
        /// enum. There is no support for escaping such a replacement, and invalid names are ignored.</para>
        /// <para>The following additional names are also recognised:</para>
        /// <list type="table">
        /// <item><term>$(Temp)</term><description>expands to the system's temporary folder path (Path.GetTempPath()).</description></item>
        /// <item><term>$(AppPath)</term><description>expands to the directory containing the entry assembly (Assembly.GetEntryAssembly()).
        ///    Throws an <see cref="InvalidOperationException"/> if there is no entry assembly (e.g. in a secondary app domain).</description></item>
        /// </list>
        /// </remarks>
        public static string ExpandPath(string path)
        {
            foreach (var folderEnum in EnumStrong.GetValues<Environment.SpecialFolder>())
                path = path.Replace("$(" + folderEnum + ")", Environment.GetFolderPath(folderEnum));
            path = path.Replace("$(Temp)", Path.GetTempPath());
            if (path.Contains("$(AppPath)"))
            {
                if (Assembly.GetEntryAssembly() == null)
                    throw new InvalidOperationException("ExpandPath() cannot expand $(AppPath) in an AppDomain where Assembly.GetEntryAssembly() is null.");
                path = path.Replace("$(AppPath)", Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            }
            return path;
        }

        /// <summary>
        /// Checks to see whether the specified path starts with any of the standard paths supported by
        /// <see cref="ExpandPath"/>, and if so, replaces the prefix with a "$(NAME)" string and returns
        /// the resulting value.
        /// </summary>
        public static string UnexpandPath(string path)
        {
            foreach (var folderEnum in EnumStrong.GetValues<Environment.SpecialFolder>())
                if (path.StartsWith(Environment.GetFolderPath(folderEnum)))
                    return "$(" + folderEnum + ")" + path.Substring(Environment.GetFolderPath(folderEnum).Length);
            if (path.StartsWith(Path.GetTempPath()))
                return "$(Temp)" + path.Substring(Path.GetTempPath().Length);
            return path;
        }

        /// <summary>
        /// Deletes the specified directory only if it is empty, and then
        /// checks all parents to see if they have become empty too. If so,
        /// deletes them too. Does not throw any exceptions.
        /// </summary>
        public static void DeleteEmptyDirs(string path)
        {
            try
            {
                while (path.Length > 3)
                {
                    if (Directory.GetFileSystemEntries(path).Length > 0)
                        break;

                    File.SetAttributes(path, FileAttributes.Normal);
                    Directory.Delete(path);
                    path = Path.GetDirectoryName(path);
                }
            }
            catch { }
        }

        /// <summary>
        /// Returns the "parent" path of the specified path by removing the last name
        /// from the path, separated by either forward or backslash. If the original
        /// path ends in slash, the returned path will also end with a slash.
        /// </summary>
        public static string ExtractParent(string path)
        {
            if (path == null)
                throw new ArgumentNullException();

            int pos = -1;
            if (path.Length >= 2)
                pos = path.LastIndexOfAny(new[] { '/', '\\' }, path.Length - 2);
            if (pos < 0)
                throw new PathException("Path \"{0}\" does not have a parent path.".Fmt(path));

            // Leave the slash if the original path also ended in slash
            if (path[path.Length - 1] == '/' || path[path.Length - 1] == '\\')
                pos++;

            return path.Substring(0, pos);
        }

        /// <summary>
        /// Returns the "parent" path of the specified path by removing the last group
        /// from the path, separated by the "separator" character. If the original
        /// path ends in slash, the returned path will also end with a slash.
        /// </summary>
        public static string ExtractParent(string path, char separator)
        {
            if (path == null)
                throw new ArgumentNullException();

            int pos = -1;
            if (path.Length >= 2)
                pos = path.LastIndexOf(separator, path.Length - 2);
            if (pos < 0)
                throw new PathException("Path \"{0}\" does not have a parent path.".Fmt(path));

            // Leave the slash if the original path also ended in slash
            if (path[path.Length - 1] == separator)
                pos++;

            return path.Substring(0, pos);
        }

        /// <summary>
        /// Returns the name and extension of the last group in the specified path,
        /// separated by either of the two slashes.
        /// </summary>
        public static string ExtractNameAndExt(string path)
        {
            if (path == null)
                throw new ArgumentNullException();

            int pos = path.LastIndexOfAny(new[] { '/', '\\' });

            if (pos < 0)
                return path;
            else
                return path.Substring(pos + 1);
        }

        /// <summary>
        /// Joins the two paths using the OS separator character. If the second path is absolute,
        /// only the second path is returned.
        /// </summary>
        [Obsolete("Use Path.Combine instead.")]
        public static string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        /// <summary>
        /// Joins multiple paths using the OS separator character. If any of the paths is absolute,
        /// all preceding paths are discarded.
        /// </summary>
        [Obsolete("Use Path.Combine instead.")]
        public static string Combine(string path1, string path2, params string[] morepaths)
        {
            string result = Path.Combine(path1, path2);
            foreach (string p in morepaths)
                result = Path.Combine(result, p);
            return result;
        }

        /// <summary>
        /// Creates all directories in the path to the specified file if they don't exist.
        /// Accepts filenames relative to the current directory.
        /// </summary>
        public static void CreatePathToFile(string filename)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(".", filename)));
        }

        /// <summary>
        /// Strips a single trailing directory separator, whether it's the forward- or backslash. Preserves the single separator at the end of
        /// paths referring to the root of a drive, such as "C:\". Removes at most a single separator, never more.
        /// </summary>
        public static string StripTrailingSeparator(string path)
        {
            if (path.EndsWith('/') || path.EndsWith('\\'))
                return (path.Length == 3 && path[1] == ':') ? path : path.Substring(0, path.Length - 1);
            else
                return path;
        }

        /// <summary>
        /// Changes a relative <paramref name="toggledPath"/> to an absolute and vice versa, with respect to <paramref name="basePath"/>.
        /// Neither path must be an empty string. Any trailing slashes are ignored and the result won't have one except for root "C:\"-style paths.
        /// Forward slashes, multiple repeated slashes, and any redundant "." or ".." elements are correctly interpreted and eliminated.
        /// See Remarks for some special cases.
        /// </summary>
        /// <remarks>
        /// Relative paths that specify a drive letter "C:thing" are not supported and result in undefined behaviour. If the toggled path is relative
        /// then all ".." levels that expand beyond the root directory are silently discarded.
        /// </remarks>
        /// <param name="basePath">An absolute path to the directory which serves as the base for absolute/relative conversion.</param>
        /// <param name="toggledPath">An absolute or a relative path to be converted.</param>
        /// <returns>The converted path.</returns>
        /// <exception cref="ToggleRelativeException">Conversion could not be performed for the reason specified in the exception object.</exception>
        public static string ToggleRelative(string basePath, string toggledPath)
        {
            if (basePath.Length == 0)
                throw new ToggleRelativeException(ToggleRelativeProblem.InvalidBasePath);
            if (toggledPath.Length == 0)
                throw new ToggleRelativeException(ToggleRelativeProblem.InvalidToggledPath);
            if (!Path.IsPathRooted(basePath))
                throw new ToggleRelativeException(ToggleRelativeProblem.BasePathNotAbsolute);

            try { basePath = Path.GetFullPath(basePath + "\\"); }
            catch { throw new ToggleRelativeException(ToggleRelativeProblem.InvalidBasePath); }

            if (!Path.IsPathRooted(toggledPath))
                try { return PathUtil.StripTrailingSeparator(Path.GetFullPath(Path.Combine(basePath, toggledPath))); }
                catch { throw new ToggleRelativeException(ToggleRelativeProblem.InvalidToggledPath); }

            // Both basePath and toggledPath are absolute. Need to relativize toggledPath.
            try { toggledPath = Path.GetFullPath(toggledPath + "\\"); }
            catch { throw new ToggleRelativeException(ToggleRelativeProblem.InvalidToggledPath); }
            int prevPos = -1;
            int pos = toggledPath.IndexOf(Path.DirectorySeparatorChar);
            while (pos != -1 && pos < basePath.Length && basePath.Substring(0, pos + 1).Equals(toggledPath.Substring(0, pos + 1), StringComparison.OrdinalIgnoreCase))
            {
                prevPos = pos;
                pos = toggledPath.IndexOf(Path.DirectorySeparatorChar, pos + 1);
            }
            if (prevPos == -1)
                throw new ToggleRelativeException(ToggleRelativeProblem.PathsOnDifferentDrives);
            var piece = basePath.Substring(prevPos + 1);
            var result = PathUtil.StripTrailingSeparator((".." + Path.DirectorySeparatorChar).Repeat(piece.Count(ch => ch == Path.DirectorySeparatorChar)) + toggledPath.Substring(prevPos + 1));
            return result.Length == 0 ? "." : result;
        }
    }

    /// <summary>Details a problem that occurred while using <see cref="PathUtil.ToggleRelative"/>.</summary>
    public enum ToggleRelativeProblem
    {
        /// <summary>The base path is not an absolute path.</summary>
        BasePathNotAbsolute,
        /// <summary>The two paths are both absolute and on different drives, making it impossible to make one of them relative to the other.</summary>
        PathsOnDifferentDrives,
        /// <summary>The base path is not a valid path and/or contains invalid characters.</summary>
        InvalidBasePath,
        /// <summary>The toggled path is not a valid path and/or contains invalid characters.</summary>
        InvalidToggledPath
    }

    /// <summary>Indicates an error that occurred while using <see cref="PathUtil.ToggleRelative"/>.</summary>
    public class ToggleRelativeException : ArgumentException
    {
        /// <summary>Details the problem that occurred.</summary>
        public ToggleRelativeProblem Problem { get; private set; }
        /// <summary>Constructor.</summary>
        public ToggleRelativeException(ToggleRelativeProblem problem) : base(problem.ToString()) { Problem = problem; }
    }
}
