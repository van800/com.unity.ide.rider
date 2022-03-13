using System;
using UnityEditor;
using UnityEngine;

namespace Packages.Rider.Editor
{
    [FilePath("ProjectSettings/RiderScriptEditorPersistedState.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class RiderScriptEditorPersistedState: ScriptableSingleton<RiderScriptEditorPersistedState>
    {
        [SerializeField] private long lastWriteTicks;

        public DateTime? LastWrite
        {
            get => DateTime.FromBinary(lastWriteTicks);
            set
            {
                if (!value.HasValue) return;
                lastWriteTicks = value.Value.ToBinary();
                Save(true);
            }
        }
    }
}