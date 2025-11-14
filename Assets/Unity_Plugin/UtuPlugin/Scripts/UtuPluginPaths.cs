// Copyright Alex Quevillon. All Rights Reserved.

using System.IO;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class UtuPluginConfig {
	public string unrealFile_Full_Engine;
	public string unrealFile_Full_UProject;
}

class UtuPluginPaths {
	// Utilities
	public static bool isConstructed = false;
	// Windows
	public static readonly char slash = '/';
	public static readonly char backslash = '\\';
	// Unity
	public static string unityFolder_Full_Project;
	// plugin
	public static string pluginFolder_Rel_UtuPlugin;
	public static string pluginIcon_Rel_Icon_LookingGlass;
	public static string pluginIcon_Rel_Icon_CopyNormal;
	public static string pluginIcon_Rel_Icon_CopyClicked;
	public static string pluginIcon_Rel_Icon_RefreshNormal;
	public static string pluginIcon_Rel_Icon_RefreshClicked;
	public static string pluginIcon_Rel_Tab_Selected;
	public static string pluginIcon_Rel_Tab_Normal;
	public static string pluginIcon_Rel_Color_DarkGrey;
	public static string pluginIcon_Rel_Color_LightGrey;
	public static string pluginIcon_Rel_Color_Grey;
	public static readonly string default_pluginFile_Full_Config = "{%AppData%}/AlexQuevillon/UtuPlugin/Unity_Config.json";
	public static string pluginFile_Full_Config;
	public static readonly string default_pluginFolder_Full_Exports = "{%AppData%}/AlexQuevillon/UtuPlugin/Exports";
	public static string pluginFolder_Full_Exports;
	// Unreal
	public static string pluginFolder_Rel_UnrealPlugins;
	public static readonly string default_unrealFile_Full_Engine = "C:/Program Files/Epic Games/{VersionNumber}/Engine/Binaries/Win64/UE4Editor.exe";
	public static string unrealFile_Full_Engine;
	public static bool unrealFile_Full_Engine_IsValid = false;
	public static readonly string default_unrealFile_Full_UProject = "C:/Users/{Username}/Documents/UnrealProjects/{ProjectName}/{ProjectName}.uproject";
	public static string unrealFile_Full_UProject;
	public static bool unrealFile_Full_UProject_IsValid = false;

	// Constructor
	public static void ConstructUtuPluginPaths() {
		// Utilities
		isConstructed = true;
		// Unity
		unityFolder_Full_Project = Application.dataPath.Remove(Application.dataPath.Length - 6);
		// Plugin
		pluginFolder_Rel_UtuPlugin = GetPluginRelativeFolder();
		pluginIcon_Rel_Icon_LookingGlass = pluginFolder_Rel_UtuPlugin + slash + "Images" + slash + "UTU_Icon_LookingGlass.png";
		pluginIcon_Rel_Icon_CopyNormal = pluginFolder_Rel_UtuPlugin + slash + "Images" + slash + "UTU_Icon_CopyNormal.png";
		pluginIcon_Rel_Icon_CopyClicked = pluginFolder_Rel_UtuPlugin + slash + "Images" + slash + "UTU_Icon_CopyClicked.png";
		pluginIcon_Rel_Icon_RefreshNormal = pluginFolder_Rel_UtuPlugin + slash + "Images" + slash + "UTU_Icon_RefreshNormal.png";
		pluginIcon_Rel_Icon_RefreshClicked = pluginFolder_Rel_UtuPlugin + slash + "Images" + slash + "UTU_Icon_RefreshClicked.png";
		pluginIcon_Rel_Tab_Normal = pluginFolder_Rel_UtuPlugin + slash + "Images" + slash + "UTU_Tab_Normal.png";
		pluginIcon_Rel_Tab_Selected = pluginFolder_Rel_UtuPlugin + slash + "Images" + slash + "UTU_Tab_Selected.png";
		pluginIcon_Rel_Color_DarkGrey = pluginFolder_Rel_UtuPlugin + slash + "Images" + slash + "UTU_Color_DarkGrey.png";
		pluginIcon_Rel_Color_LightGrey = pluginFolder_Rel_UtuPlugin + slash + "Images" + slash + "UTU_Color_LightGrey.png";
		pluginIcon_Rel_Color_Grey = pluginFolder_Rel_UtuPlugin + slash + "Images" + slash + "UTU_Color_Grey.png";
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		pluginFile_Full_Config = default_pluginFile_Full_Config.Replace("{%AppData%}", System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "/Documents");
		pluginFile_Full_Config = default_pluginFile_Full_Config.Replace("{%AppData%}", System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "/Documents");
		pluginFolder_Full_Exports = default_pluginFolder_Full_Exports.Replace("{%AppData%}", System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "/Documents");
#else
		pluginFile_Full_Config = default_pluginFile_Full_Config.Replace("{%AppData%}", System.Environment.GetEnvironmentVariable("AppData"));
		pluginFile_Full_Config = default_pluginFile_Full_Config.Replace("{%AppData%}", System.Environment.GetEnvironmentVariable("AppData"));
		pluginFolder_Full_Exports = default_pluginFolder_Full_Exports.Replace("{%AppData%}", System.Environment.GetEnvironmentVariable("AppData"));
#endif
		// Unreal
		pluginFolder_Rel_UnrealPlugins = pluginFolder_Rel_UtuPlugin + slash + "UnrealPlugin~";
		unrealFile_Full_Engine = default_unrealFile_Full_Engine;
		unrealFile_Full_UProject = default_unrealFile_Full_UProject;
		// Config
		LoadFromConfig();
	}


	public static void Set_unrealFile_Full_Engine(string path) {
		unrealFile_Full_Engine = path == "" ? default_unrealFile_Full_Engine : path;
		unrealFile_Full_Engine_IsValid = unrealFile_Full_Engine.EndsWith("UE4Editor.exe") && File.Exists(unrealFile_Full_Engine);
		SaveToConfig();
	}
	public static void Set_unrealFile_Full_UProject(string path)
	{
		unrealFile_Full_UProject = path == "" ? default_unrealFile_Full_UProject : path;
		unrealFile_Full_UProject_IsValid = unrealFile_Full_UProject.EndsWith(".uproject") && File.Exists(unrealFile_Full_UProject);
		SaveToConfig();
	}


	private static void LoadFromConfig() {
		if (File.Exists(pluginFile_Full_Config)) {
			try {
				UtuPluginConfig UtuPluginConfig = JsonUtility.FromJson<UtuPluginConfig>(File.ReadAllText(pluginFile_Full_Config));
				Set_unrealFile_Full_Engine(UtuPluginConfig.unrealFile_Full_Engine);
				Set_unrealFile_Full_UProject(UtuPluginConfig.unrealFile_Full_UProject);
			}
			catch {
				SaveToConfig();
			}
		}
	}

	private static void SaveToConfig() {
		UtuPluginConfig UtuPluginConfig = new UtuPluginConfig();
		UtuPluginConfig.unrealFile_Full_Engine = unrealFile_Full_Engine;
		UtuPluginConfig.unrealFile_Full_UProject = unrealFile_Full_UProject;
		File.WriteAllText(pluginFile_Full_Config, JsonUtility.ToJson(UtuPluginConfig, true));
	}



	private static string GetPluginRelativeFolder() {
		foreach (string x in AssetDatabase.GetAllAssetPaths()) {
			if (x.EndsWith("UtuPluginPaths.cs")) {
				return x.Replace("/Scripts/UtuPluginPaths.cs", "");
			}
		}
		return "";
	}


	public static string FormatForWindows(string path, bool addQuotes = false) {
		if (addQuotes) {
			return "\"" + path.Replace(slash, backslash) + "\"";
		}
		else {
			return path.Replace(slash, backslash);
		}
	}

	public static string MakeAbsolute(string path) {
		return unityFolder_Full_Project + path;
	}
}
