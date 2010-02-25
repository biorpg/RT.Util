using System;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>Represents a path-related exception.</summary>
    public class PathException : RTException
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        public PathException() : base() { }
        public PathException(string message) : base(message) { }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
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
        private static string _cachedAppPath = "";

        /// <summary>
        /// Returns the application path with a directory separator char at the end.
        /// The expression 'Ut.AppPath + "FileName"' yields a valid fully qualified
        /// file name. Supports network paths.
        /// </summary>
        public static string AppPath
        {
            get
            {
                if (_cachedAppPath == "")
                {
                    _cachedAppPath = Application.ExecutablePath;
                    _cachedAppPath = _cachedAppPath.Remove(
                        _cachedAppPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
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
            return AppPath + path;
        }

        /// <summary>
        /// Combines the full path containing the running executable with one or more strings.
        /// Ensures that only a single <see cref="Path.DirectorySeparatorChar"/> separates
        /// the executable path and every string.
        /// </summary>
        public static string AppPathCombine(string path1, params string[] morePaths)
        {
            return Combine(AppPath, path1, morePaths);
        }

        /// <summary>
        /// This function returns a fully qualified name for the subpath, relative
        /// to the executable directory. This is for the purist programmers who can't
        /// handle AppPath returning something "invalid" :)
        /// </summary>
        public static string MakeAppSubpath(string subpath)
        {
            return AppPath + subpath;
        }

        /// <summary>
        /// Returns a normalized copy of the specified path.
        /// A "normalized path" is a path to a directory (not a file!) which
        /// ALWAYS ends with a slash. Cf. <see>NormName</see>.
        ///
        /// <para>Returns null for null inputs.</para>
        /// </summary>
        /// <param name="path">Path to be normalised</param>
        /// <returns>Normalised version of Path</returns>
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
        /// A "normalized name" is file or directory name which NEVER
        /// ends with a slash. Cf. <see>NormPath</see>. This includes
        /// the root directory on Windows, which normalised name is "C:"
        /// and unix, where it is "".
        ///
        /// <para>Returns null for null inputs.</para>
        /// </summary>
        public static string NormName(string filedir)
        {
            if (filedir == null || filedir.Length == 0)
                return null;
            else if (filedir[filedir.Length - 1] == Path.DirectorySeparatorChar)
                return filedir;
            else
                return filedir + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// Ensures that no path ends with a back slash except for the root path.
        /// Surely this is a funny path, unlike the N(o)rm(al)Path above...
        /// </summary>
        public static string FunnyPath(string path)
        {
            if (path == null)
                return "";
            else if (path.Length == 0)
                return "";
            else if (path.Length == 2 && path[1] == ':')
                return path + Path.DirectorySeparatorChar;
            else if (path.Length == 3)
                return path;
            else if (path[path.Length - 1] == Path.DirectorySeparatorChar)
                return path.Substring(0, path.Length - 1);
            else
                return path;
        }

        /// <summary>
        /// Checks whether <paramref name="path"/> refers to a subdirectory inside <paramref name="refPath"/>.
        /// </summary>
        public static bool IsSubpathOf(string subpath, string parentPath)
        {
            string parentPathNormalized = PathUtil.NormPath(parentPath);
            string subpathNormalized = PathUtil.NormPath(subpath);

            if (subpathNormalized.Length <= parentPathNormalized.Length)
                return false;

            return subpathNormalized.Substring(0, parentPathNormalized.Length).Equals(parentPathNormalized, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Checks whether <paramref name="path"/> refers to a subdirectory inside <paramref name="refPath"/> or the same directory.
        /// </summary>
        public static bool IsSubpathOfOrSame(string subpath, string parentPath)
        {
            string parentPathNormalized = PathUtil.NormPath(parentPath);
            string subpathNormalized = PathUtil.NormPath(subpath);

            if (subpathNormalized.Length < parentPathNormalized.Length)
                return false;

            return subpathNormalized.Substring(0, parentPathNormalized.Length - 1).Equals(parentPathNormalized.Substring(0, parentPathNormalized.Length - 1), StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns the number of sublevels 'path' is away from ref_path.
        /// Positive numbers indicate that path is deeper than ref_path;
        /// negative that it's above ref_path. If neither is a subpath of
        /// the other returns int.MaxValue.
        /// </summary>
        /// <param name="ref_path">Reference path</param>
        /// <param name="path">Path to be compared</param>
        public static int PathLevelDistance(string ref_path, string path)
        {
            string p1 = PathUtil.NormPath(ref_path.ToUpper());
            string p2 = PathUtil.NormPath(path.ToUpper());

            if (p1 == p2)
                return 0;

            if (p1.Length < p2.Length)
            {
                if (p2.Substring(0, p1.Length) != p1)
                    return int.MaxValue;
                p1 = p2.Substring(p1.Length);
                return p1.Count(c => c == Path.DirectorySeparatorChar);
            }
            else
            {
                if (p1.Substring(0, p2.Length) != p2)
                    return int.MaxValue;
                p2 = p1.Substring(p2.Length);
                return -p2.Count(c => c == Path.DirectorySeparatorChar);
            }
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
        /// Returns a path to "fullpath", relative to "root".
        /// Throws an exception if "fullpath" is not a subpath of "root".
        /// </summary>
        public static string ExtractRelativePath(string root, string fullpath)
        {
            root = PathUtil.NormPath(root);
            if (root.ToLower() == PathUtil.NormPath(fullpath).ToLower())
                return "";
            if (!fullpath.ToLower().StartsWith(root.ToLower()))
                throw new PathException("Path \"{0}\" is not a subpath of \"{1}\"".Fmt(fullpath, root));
            return fullpath.Substring(root.Length);
        }

        /// <summary>
        /// Joins the two paths using the OS separator character. If the second path is absolute,
        /// only the second path is returned. Identical to <see cref="Path.Combine"/>.
        /// </summary>
        public static string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        /// <summary>
        /// Joins multiple paths using the OS separator character. If any of the paths is absolute,
        /// all preceding paths are discarded.
        /// </summary>
        public static string Combine(string path1, string path2, params string[] morepaths)
        {
            string result = Combine(path1, path2);
            foreach (string p in morepaths)
                result = Combine(result, p);
            return result;
        }

        /// <summary>
        /// Creates all directories in the path to the specified file if they don't exist.
        /// Accepts filenames relative to the current directory.
        /// </summary>
        public static void CreatePathToFile(string filename)
        {
            string dir = Path.GetDirectoryName(Path.Combine(".", filename));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
