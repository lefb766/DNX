using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using NuGet;
using Microsoft.Framework.Runtime.DependencyManagement;
using System.Linq;
using System.IO;
using System.Runtime.Versioning;

namespace Microsoft.Framework.PackageManager.Utils
{
    internal static class LockFileUtils
    {
        public static LockFileLibrary CreateLockFileLibraryForProject(Runtime.Project project, IPackage package, SHA512 sha512)
        {
            var lockFileLib = new LockFileLibrary();
            lockFileLib.Name = package.Id;
            lockFileLib.Version = package.Version;

            using (var nupkgStream = package.GetStream())
            {
                lockFileLib.Sha = Convert.ToBase64String(sha512.ComputeHash(nupkgStream));
            }
            lockFileLib.Files = package.GetFiles().Select(p => p.Path).ToList();

            foreach (var frameworkInfo in project.GetTargetFrameworks())
            {
                var group = new LockFileFrameworkGroup();
                group.TargetFramework = frameworkInfo.FrameworkName;

                IEnumerable<PackageDependencySet> dependencySet;
                if (VersionUtility.TryGetCompatibleItems(frameworkInfo.FrameworkName, package.DependencySets, out dependencySet))
                {
                    var set = dependencySet.FirstOrDefault()?.Dependencies?.ToList();

                    if (set != null)
                    {
                        group.Dependencies = set;
                    }
                }

                // TODO: Remove this when we do #596
                // ASP.NET Core isn't compatible with generic PCL profiles
                if (!string.Equals(frameworkInfo.FrameworkName.Identifier, VersionUtility.AspNetCoreFrameworkIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    IEnumerable<FrameworkAssemblyReference> frameworkAssemblies;
                    if (VersionUtility.TryGetCompatibleItems(frameworkInfo.FrameworkName, package.FrameworkAssemblies, out frameworkAssemblies))
                    {
                        foreach (var assemblyReference in frameworkAssemblies)
                        {
                            if (!assemblyReference.SupportedFrameworks.Any() &&
                                !VersionUtility.IsDesktop(frameworkInfo.FrameworkName))
                            {
                                // REVIEW: This isn't 100% correct since none *can* mean 
                                // any in theory, but in practice it means .NET full reference assembly
                                // If there's no supported target frameworks and we're not targeting
                                // the desktop framework then skip it.

                                // To do this properly we'll need all reference assemblies supported
                                // by each supported target framework which isn't always available.
                                continue;
                            }

                            group.FrameworkAssemblies.Add(assemblyReference);
                        }
                    }
                }

                group.RuntimeAssemblies = GetPackageAssemblies(package, frameworkInfo.FrameworkName);

                string contractPath = Path.Combine("lib", "contract", package.Id + ".dll");
                var hasContract = lockFileLib.Files.Any(path => path == contractPath);
                var hasLib = group.RuntimeAssemblies.Any();

                if (hasContract && hasLib && !VersionUtility.IsDesktop(frameworkInfo.FrameworkName))
                {
                    group.CompileTimeAssemblies.Add(contractPath);
                }
                else if (hasLib)
                {
                    group.CompileTimeAssemblies.AddRange(group.RuntimeAssemblies);
                }

                lockFileLib.FrameworkGroups.Add(group);
            }

            return lockFileLib;
        }

        private static List<string> GetPackageAssemblies(IPackage package, FrameworkName targetFramework)
        {
            var results = new List<string>();

            IEnumerable<IPackageAssemblyReference> compatibleReferences;
            if (VersionUtility.TryGetCompatibleItems(targetFramework, package.AssemblyReferences, out compatibleReferences))
            {
                // Get the list of references for this target framework
                var references = compatibleReferences.ToList();

                // See if there's a list of specific references defined for this target framework
                IEnumerable<PackageReferenceSet> referenceSets;
                if (VersionUtility.TryGetCompatibleItems(targetFramework, package.PackageAssemblyReferences, out referenceSets))
                {
                    // Get the first compatible reference set
                    var referenceSet = referenceSets.FirstOrDefault();

                    if (referenceSet != null)
                    {
                        // Remove all assemblies of which names do not appear in the References list
                        references.RemoveAll(r => !referenceSet.References.Contains(r.Name, StringComparer.OrdinalIgnoreCase));
                    }
                }

                foreach (var reference in references)
                {
                    // Skip anything that isn't a dll. Unfortunately some packages put random stuff
                    // in the lib folder and they surface as assembly references
                    if (!Path.GetExtension(reference.Path).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    results.Add(reference.Path);
                }
            }

            return results;
        }

    }
}