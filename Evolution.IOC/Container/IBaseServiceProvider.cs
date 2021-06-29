using Evolution.IOC.Exception;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Evolution.IOC
{
    public abstract class IBaseServiceProvider : IIocServiceProvider
    {
        private IDictionary<string, IList<Type>> cache = new ConcurrentDictionary<string, IList<Type>>();
        private readonly Lazy<List<Assembly>> CacheOfWorkingDirectorySubAllAssemblies = new Lazy<List<Assembly>>(GetAllAssemblies, false);

        /// <summary>
        /// .net core/.net 5 doesn't support to assemble the local dll path of current process main module.
        /// </summary>
        /// <param name="typeFullName"></param>
        /// <returns></returns>
        protected Type GetRealType(string typeFullName)
        {
            try
            {
                var type =  Type.GetType(typeFullName);
                if (type == null) throw new NullResultException();
                return type;
            }
            catch (System.Exception e)
            {
                foreach (var assembly in CacheOfWorkingDirectorySubAllAssemblies.Value)
                {
                    var typeInfo = assembly.DefinedTypes.Where(it => string.Equals(it.FullName, typeFullName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (typeInfo != null)
                    {
                        return typeInfo;
                    }
                }
            }
            return null;
        }

        public abstract IRegisterInfo GetService(string interfaceFullName);

        public abstract IEnumerable<IRegisterInfo> GetServices();

        private static List<Assembly> GetAllAssemblies()
        {
            var dlls = GetAllDllOrExe();
            dlls = FilterDllOrExe(dlls);
            return ConvertToAssemblies(dlls);
        }

        private static IEnumerable<FileInfo> FilterDllOrExe(IEnumerable<FileInfo> dlls)
        {
            var currentMainModuleName = Process.GetCurrentProcess().MainModule.FileName;
            try
            {
                var assembly = Assembly.LoadFile(currentMainModuleName);
                if (assembly == null) throw new System.Exception("Need to filter main module assembly.");
                return dlls;
            }
            catch(System.Exception e)
            {
                return dlls.Where(it => !string.Equals(currentMainModuleName, it.FullName, StringComparison.OrdinalIgnoreCase));
            }
        }

        private static List<Assembly> ConvertToAssemblies(IEnumerable<FileInfo> dlls)
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (var dll in dlls)
            {
                Assembly assembly = Assembly.LoadFile(dll.FullName);
                assemblies.Add(assembly);
            }
            return assemblies;
        }

        private static IEnumerable<FileInfo> GetAllDllOrExe()
        {
            Process process = Process.GetCurrentProcess();
            var workingDirectory = System.IO.Path.GetDirectoryName(process.MainModule.FileVersionInfo.FileName);
            var files = new DirectoryInfo(workingDirectory).EnumerateFiles();
            return GetDlls(files);
        }

        private void InitCache(Type interfaceType)
        {
            Process process = Process.GetCurrentProcess();
            var workingDirectory = System.IO.Path.GetDirectoryName(process.MainModule.FileVersionInfo.FileName);
            var files = new DirectoryInfo(workingDirectory).EnumerateFiles();
            var dlls = GetDlls(files).ToList();
            foreach (FileInfo usingDll in dlls)
            {
                SetImpleTypeToCache(interfaceType, usingDll);
            }
        }

        private void SetImpleTypeToCache(Type interfaceType, FileInfo usingDll)
        {
            string interfaceFullName = interfaceType.FullName;
            Assembly assembly = Assembly.LoadFile(usingDll.FullName);
            var imples = assembly.DefinedTypes.Where(it => interfaceType.IsAssignableFrom(it) && it.IsClass).ToList();
            if (imples != null && imples.Any())
            {
                imples.ForEach(it =>
                {
                    IList<Type> impleTypes = null;
                    if (cache.TryGetValue(interfaceFullName, out impleTypes))
                    {
                        if (impleTypes == null)
                        {
                            cache[interfaceFullName] = new List<Type>();
                        }
                        cache[interfaceFullName].Add(it);
                    }
                    else
                    {
                        cache.Add(interfaceFullName, new List<Type>() { it });
                    }
                });
            }
        }

        private static IEnumerable<FileInfo> GetDlls(IEnumerable<FileInfo> files)
        {
            if (files == null) return new List<FileInfo>();
            return files.Where(it => System.IO.Path.GetFileName(it.FullName.ToLower()).EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || System.IO.Path.GetFileName(it.FullName.ToLower()).EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        }
    }
}
