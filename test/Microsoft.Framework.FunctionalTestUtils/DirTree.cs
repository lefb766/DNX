// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Framework.FunctionalTestUtils
{
    public class DirTree
    {
        private Dictionary<string, string> _pathToContents;

        private DirTree(params string[] fileRelativePaths)
        {
            _pathToContents = fileRelativePaths.ToDictionary(f => f, _ => string.Empty);
        }

        public static DirTree CreateFromList(params string[] fileRelativePaths)
        {
            return new DirTree(fileRelativePaths);
        }

        public static DirTree CreateFromDirectory(string dirPath)
        {
            var dirTree = new DirTree();

            dirPath = EnsureTrailingForwardSlash(dirPath);

            var dirFileList = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories)
                .Select(x => x.Substring(dirPath.Length))
                .Select(x => GetPathWithForwardSlashes(x));

            // If we only generate a list of files, empty dirs will be left out
            // So we mark an empty dir with trailing forward slash (e.g. "path/to/dir/") and put it into the list
            var dirEmptySubDirList = Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories)
                .Where(x => !Directory.GetFileSystemEntries(x).Any())
                .Select(x => x.Substring(dirPath.Length))
                .Select(x => GetPathWithForwardSlashes(x))
                .Select(x => EnsureTrailingForwardSlash(x));

            foreach (var file in dirFileList)
            {
                var fullPath = Path.Combine(dirPath, file);
                var onDiskFileContents = File.ReadAllText(fullPath);
                dirTree._pathToContents[file] = onDiskFileContents;
            }

            foreach (var emptyDir in dirEmptySubDirList)
            {
                // Empty dirs don't have contents
                dirTree._pathToContents[emptyDir] = null;
            }

            return dirTree;
        }

        public DirTree WithFileContents(string relativePath, string contents)
        {
            _pathToContents[relativePath] = contents;
            return this;
        }

        public DirTree WithFileContents(string relativePath, string format, params object[] args)
        {
            _pathToContents[relativePath] = string.Format(format, args);
            return this;
        }

        public DirTree WithSubDir(string relativePath, DirTree subDir)
        {
            // Append a DirTree as a subdir of current DirTree
            foreach (var pair in subDir._pathToContents)
            {
                var newPath = Path.Combine(relativePath, pair.Key);
                _pathToContents[newPath] = pair.Value;
            }
            return this;
        }

        public DirTree RemoveFile(string relativePath)
        {
            _pathToContents.Remove(relativePath);
            return this;
        }

        public DirTree RemoveSubDir(string relativePath)
        {
            relativePath = EnsureTrailingForwardSlash(relativePath);
            var removedKeys = new List<string>();
            foreach (var pair in _pathToContents)
            {
                if (pair.Key.StartsWith(relativePath))
                {
                    removedKeys.Add(pair.Key);
                }
            }
            foreach (var removedKey in removedKeys)
            {
                _pathToContents.Remove(removedKey);
            }
            return this;
        }

        public DirTree WriteTo(string rootDirPath)
        {
            foreach (var pair in _pathToContents)
            {
                var path = Path.Combine(rootDirPath, pair.Key);

                if (path.EndsWith("/") && !Directory.Exists(path))
                {
                    // Create an empty dir, which is represented as "path/to/dir/"
                    Directory.CreateDirectory(path);
                    continue;
                }

                var parentDir = Path.GetDirectoryName(path);

                if (!Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                File.WriteAllText(path, pair.Value);
            }

            return this;
        }

        public bool MatchDirectoryOnDisk(string dirPath, bool compareFileContents = true)
        {
            dirPath = EnsureTrailingForwardSlash(dirPath);

            var dirFileList = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories)
                .Select(x => x.Substring(dirPath.Length))
                .Select(x => GetPathWithForwardSlashes(x));

            var dirEmptySubDirList = Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories)
                .Where(x => !Directory.GetFileSystemEntries(x).Any())
                .Select(x => x.Substring(dirPath.Length))
                .Select(x => GetPathWithForwardSlashes(x))
                .Select(x => EnsureTrailingForwardSlash(x));

            var expectedFileList = _pathToContents.Keys.Where(x => !x.EndsWith("/"));
            var expectedEmptySubDirList = _pathToContents.Keys.Where(x => x.EndsWith("/"));

            var expectedFileCount = expectedFileList.Count();
            var actualFileCount = dirFileList.Count();
            if (expectedFileCount != actualFileCount)
            {
                Console.Error.WriteLine("Number of files in '{0}' is {1}, while expected number is {2}.",
                    dirPath, actualFileCount, expectedFileCount);
                Console.Error.WriteLine("Missing files: " +
                    string.Join(",", _pathToContents.Keys.Except(dirFileList)));
                Console.Error.WriteLine("Extra files: " +
                    string.Join(",", dirFileList.Except(_pathToContents.Keys)));
                return false;
            }

            foreach (var file in dirFileList)
            {
                if (!_pathToContents.ContainsKey(file))
                {
                    Console.Error.WriteLine("Expecting '{0}', which doesn't exist in '{1}'", file, dirPath);
                    return false;
                }

                var fullPath = Path.Combine(dirPath, file);
                var onDiskFileContents = File.ReadAllText(fullPath);
                if (!string.Equals(onDiskFileContents, _pathToContents[file]))
                {
                    Console.Error.WriteLine("The contents of '{0}' don't match expected contents.", fullPath);
                    Console.Error.WriteLine("Expected:");
                    Console.Error.WriteLine(_pathToContents[file]);
                    Console.Error.WriteLine("Actual:");
                    Console.Error.WriteLine(onDiskFileContents);
                    return false;
                }
            }

            return true;
        }

        private static string EnsureTrailingForwardSlash(string dirPath)
        {
            if (!string.IsNullOrEmpty(dirPath))
            {
                dirPath = dirPath[dirPath.Length - 1] == '/' ?
                        dirPath : dirPath + '/';
            }
            return dirPath;
        }

        private static string GetPathWithForwardSlashes(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}