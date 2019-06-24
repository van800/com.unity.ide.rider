using System;

namespace Packages.Rider.Editor.UnitTesting
{
  [Serializable]
  public enum EventType { TestStarted, TestFinished, RunFinished }

  [Serializable]
  public class TestEvent
  {
    public EventType myType;
    public string myID;
    public string myAssemblyName;
    public string myOutput;
    public string myResultState;
    public double myDuration;
    public string myParentID;
    
    public TestEvent(EventType type, string id, string assemblyName, string output, double duration, string resultState, string parentID)
    {
      myType = type;
      myID = id;
      myAssemblyName = assemblyName;
      myOutput = output;
      myResultState = resultState;
      myDuration = duration;
      myParentID = parentID;
    }
  }
}