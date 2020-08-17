using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Editor.ManualActions
{
  public static class TestLogMessage
  {
    [MenuItem("Tests/EmitLogMessage")]
    public static void EmitLogMessage()
    {
      Debug.Log("main thread message");
      var t = new Thread(() => { Debug.Log("background log message"); });
      t.Start();
    }
  }
}