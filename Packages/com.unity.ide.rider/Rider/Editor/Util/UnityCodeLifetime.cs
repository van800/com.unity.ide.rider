using System.Threading;

namespace Packages.Rider.Editor
{
  // ReSharper disable once PartialTypeWithSinglePart
  public static partial class UnityCodeLifetime
  {
    internal static CancellationToken Token => cts.Token;

    private static CancellationTokenSource cts;
    
#if UNITY_6000_5_OR_NEWER
    [Unity.Scripting.LifecycleManagement.OnCodeLoaded]
#endif
    private static void Initialize()
    {
      cts = new CancellationTokenSource();
    }

#if UNITY_6000_5_OR_NEWER
    [Unity.Scripting.LifecycleManagement.OnCodeUnloading]
#endif
    private static void Terminate()
    {
      cts.Cancel();
      cts = null;
    }

#if !UNITY_6000_5_OR_NEWER
    static UnityCodeLifetime()
    {
      Initialize();
      UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Terminate;
    }
#endif
  }
}
