using System;
using System.IO;
using System.Linq;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

namespace Packages.Rider.Editor
{
  internal class PluginSettings
  {
    public static string[] defaultExtensions
    {
      get
      {
        var customExtensions = new[] {"json", "asmdef", "log", "xaml", "tt", "t4", "ttinclude"};
        return EditorSettings.projectGenerationBuiltinExtensions.Concat(EditorSettings.projectGenerationUserExtensions)
          .Concat(customExtensions).Distinct().ToArray();
      }
    }

    public static string[] HandledExtensions
    {
      get
      {
        return HandledExtensionsString.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.TrimStart('.', '*'))
          .ToArray();
      } 
    }

    public static string HandledExtensionsString
    {
      get { return EditorPrefs.GetString("Rider_UserExtensions", string.Join(";", defaultExtensions));}
      set { EditorPrefs.SetString("Rider_UserExtensions", value); }
    }

    public static bool SupportsExtension(string path)
    {
      var extension = Path.GetExtension(path);
      if (string.IsNullOrEmpty(extension))
        return false;
      // cs is a default extension, which should always be handled
      return extension == ".cs" || HandledExtensions.Contains(extension.TrimStart('.'));
    }
    
    public static LoggingLevel SelectedLoggingLevel
    {
      get => (LoggingLevel) EditorPrefs.GetInt("Rider_SelectedLoggingLevel", 0);
      set
      {
        EditorPrefs.SetInt("Rider_SelectedLoggingLevel", (int) value);
      }
    }

    public static bool LogEventsCollectorEnabled
    {
      get { return EditorPrefs.GetBool("Rider_LogEventsCollectorEnabled", true); }
      private set { EditorPrefs.SetBool("Rider_LogEventsCollectorEnabled", value); }
    }

    /// <summary>
    /// Preferences menu layout
    /// </summary>
    /// <remarks>
    /// Contains all 3 toggles: Enable/Disable; Debug On/Off; Writing Launch File On/Off
    /// </remarks>
    [SettingsProvider]
    private static SettingsProvider RiderPreferencesItem()
    {
      if (!RiderScriptEditor.IsRiderInstallation(RiderScriptEditor.CurrentEditor))
        return null;
      if (!RiderScriptEditorData.instance.shouldLoadEditorPlugin)
        return null;
      var provider = new SettingsProvider("Preferences/Rider", SettingsScope.User)
      {
        label = "Rider",
        keywords = new[] { "Rider" },
        guiHandler = (searchContext) =>
        {
          EditorGUIUtility.labelWidth = 200f;
          EditorGUILayout.BeginVertical();

          GUILayout.BeginVertical();
          LogEventsCollectorEnabled =
            EditorGUILayout.Toggle(new GUIContent("Pass Console to Rider:"), LogEventsCollectorEnabled);

          GUILayout.EndVertical();
          GUILayout.Label("");

          if (!string.IsNullOrEmpty(EditorPluginInterop.LogPath))
          {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Log file:");
            var previous = GUI.enabled;
            GUI.enabled = previous && SelectedLoggingLevel != LoggingLevel.OFF;
            var button = GUILayout.Button(new GUIContent("Open log"));
            if (button)
            {
              //UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(PluginEntryPoint.LogPath, 0);
              // works much faster than the commented code, when Rider is already started
              CodeEditor.CurrentEditor.OpenProject(EditorPluginInterop.LogPath, 0, 0);
            }

            GUI.enabled = previous;
            GUILayout.EndHorizontal();
          }

          var loggingMsg =
            @"Sets the amount of Rider Debug output. If you are about to report an issue, please select Verbose logging level and attach Unity console output to the issue.";
          SelectedLoggingLevel =
            (LoggingLevel) EditorGUILayout.EnumPopup(new GUIContent("Logging Level:", loggingMsg),
              SelectedLoggingLevel);


          EditorGUILayout.HelpBox(loggingMsg, MessageType.None);

          LinkButton("https://github.com/JetBrains/resharper-unity");

          GUILayout.FlexibleSpace();
          GUILayout.BeginHorizontal();

          GUILayout.FlexibleSpace();
          var assembly = EditorPluginInterop.EditorPluginAssembly;
          if (assembly != null)
          {
            var version = assembly.GetName().Version;
            GUILayout.Label("Plugin version: " + version, new GUIStyle(GUI.skin.label)
            {
              margin = new RectOffset(4, 4, 4, 4),
            });
          }

          GUILayout.EndHorizontal();

          EditorGUILayout.EndVertical();
        }
      };
      return provider;
    }

    private static void LinkButton(string url)
    {
      var style = EditorStyles.linkLabel;

      var bClicked = GUILayout.Button(url, style);

      var rect = GUILayoutUtility.GetLastRect();
      rect.width = style.CalcSize(new GUIContent(url)).x;
      EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

      if (bClicked)
        Application.OpenURL(url);
    }
  }
}