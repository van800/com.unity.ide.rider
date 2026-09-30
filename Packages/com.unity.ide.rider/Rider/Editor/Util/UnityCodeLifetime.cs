#if UNITY_6000_5_OR_NEWER
using System.Threading;

namespace Packages.Rider.Editor
{
  public static partial class UnityCodeLifetime
  {
    internal static CancellationToken Token => cts.Token;

    private static CancellationTokenSource cts;
    
    [Unity.Scripting.LifecycleManagement.OnCodeLoaded]
    private static void Initialize()
    {
      cts = new CancellationTokenSource();
    }

    [Unity.Scripting.LifecycleManagement.OnCodeUnloading]
    private static void Terminate()
    {
      cts.Cancel();
    }
  }
}
#endif