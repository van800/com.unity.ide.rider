using System;

namespace Packages.Rider.Editor.Debugger
{
  [Flags]
  public enum Il2CppDebugSupport
  {
    None = 0,
    PreserveUnityEngineDlls = 1 << 0,
    PreservePlayerDlls = 1 << 1,
  }
}