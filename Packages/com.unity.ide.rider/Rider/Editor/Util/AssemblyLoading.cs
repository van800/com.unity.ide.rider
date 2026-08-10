using System.Reflection;
using System.Collections.Generic;

namespace Packages.Rider.Editor.Util
{
  public static class AssemblyLoading
  {
    public static IReadOnlyList<Assembly> GetLoadedAssemblies()
    {
#if UNITY_6000_5_OR_NEWER
      return UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies();
#else
      return System.AppDomain.CurrentDomain.GetAssemblies();
#endif
    }

    public static Assembly LoadFromPath(string assemblyPath)
    {
#if UNITY_6000_5_OR_NEWER
      return UnityEngine.Assemblies.CurrentAssemblies.LoadFromPath(assemblyPath);
#else
      return Assembly.LoadFrom(assemblyPath);
#endif
    }

    public static Assembly LoadFromBytes(byte[] rawAssembly)
    {
#if UNITY_6000_5_OR_NEWER
      return UnityEngine.Assemblies.CurrentAssemblies.LoadFromBytes(rawAssembly);
#else
      return System.AppDomain.CurrentDomain.Load(rawAssembly);
#endif
    }

    public static string GetLoadedAssemblyLocation(this Assembly assembly)
    {
#if UNITY_6000_5_OR_NEWER
      return UnityEngine.AssemblyExtension.GetLoadedAssemblyPath(assembly);
#else
      return assembly.Location;
#endif
    }
  }
}