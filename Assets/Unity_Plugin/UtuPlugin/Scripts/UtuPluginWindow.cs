// Copyright Alex Quevillon. All Rights Reserved.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.IO;

public class UtuPluginUi : EditorWindow {
	enum UtuPluginStatus { None, Failed, Completed, Cancelled, Exporting };
	UtuPluginStatus status = UtuPluginStatus.None;

	[MenuItem("Plugins/Utu Plugin")]
	private static void OpenWindow() {
		UtuPluginPaths.isConstructed = false;
		UtuPluginUi UtuPluginUi = (UtuPluginUi)GetWindow(typeof(UtuPluginUi));
		UtuPluginUi.titleContent = new GUIContent("Utu Plugin");
		UtuPluginUi.Show();
	}



	// Constructor --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	public void ConstructIfNeeded() {
		if (!UtuPluginPaths.isConstructed) {
			UtuPluginAssets.ClearCachedScenesRelativeFilenames();
			UtuPluginPaths.ConstructUtuPluginPaths();
			LoadIcons();
			BuildReleaseNote();
			BuildProcessSteps();
		}
	}

	// OnGUI --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private UtuPluginCurrentExport currentExport = null;
	private float timeSinceLastRefresh = 0.0f;
	private void Update() {
		if (currentExport != null) {
			timeSinceLastRefresh += Time.deltaTime;
			if (timeSinceLastRefresh >= 1.0f) {
				timeSinceLastRefresh = 0.0f;
				Repaint();
			}
			currentExport = UtuPlugin.ContinueCurrentExport();
			if (currentExport == null) {
				timeSinceLastRefresh = 0.0f;
				Repaint();
				status = status == UtuPluginStatus.Exporting ? UtuPluginStatus.Completed : status;
			}
		}
		else {
			currentExport = UtuPlugin.ContinueCurrentExport();
		}
	}

	private GUIStyle GetMainStyle() {
		GUIStyle style = new GUIStyle();
		style.normal.background = Color_DarkGrey;
		style.padding = new RectOffset(10, 10, 10, 10);
		return style;
	}

	private GUIStyle GetTabStyle() {
		GUIStyle style = new GUIStyle();
		style.normal.background = Color_LightGrey;
		style.padding = new RectOffset(10, 10, 10, 10);
		return style;
	}

	private void OnGUI() {
		ConstructIfNeeded();
		EditorGUILayout.BeginVertical(GetMainStyle()); {
			EditorGUI.BeginDisabledGroup(currentExport != null); {
				BuildTabs();
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.BeginVertical(GetTabStyle()); {
				if (selectedTabIndex == 0) {
					EditorGUI.BeginDisabledGroup(currentExport != null); {
						BuildScenesScrollBox();
						BuildExportButton();
					}
					EditorGUI.EndDisabledGroup();
					BuildProgressBars();
					BuildStatusText();
				}
				else if (selectedTabIndex == 1) {
					BuildLog();
				}
				else if (selectedTabIndex == 2) {
					BuildHelpAndReleaseNote();
				}
			}
			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndVertical();
	}


	// Icons --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private Texture2D Icon_LookingGlass;
	private Texture2D Icon_CopyNormal;
	private Texture2D Icon_CopyClicked;
	private Texture2D Icon_RefreshNormal;
	private Texture2D Icon_RefreshClicked;
	private Texture2D Tab_Normal;
	private Texture2D Tab_Selected;
	private Texture2D Color_DarkGrey;
	private Texture2D Color_LightGrey;
	private Texture2D Color_Grey;
	private void LoadIcons() {
		Icon_LookingGlass = (Texture2D)AssetDatabase.LoadAssetAtPath(UtuPluginPaths.pluginIcon_Rel_Icon_LookingGlass, typeof(Texture));
		Icon_CopyNormal = (Texture2D)AssetDatabase.LoadAssetAtPath(UtuPluginPaths.pluginIcon_Rel_Icon_CopyNormal, typeof(Texture));
		Icon_CopyClicked = (Texture2D)AssetDatabase.LoadAssetAtPath(UtuPluginPaths.pluginIcon_Rel_Icon_CopyClicked, typeof(Texture));
		Icon_RefreshNormal = (Texture2D)AssetDatabase.LoadAssetAtPath(UtuPluginPaths.pluginIcon_Rel_Icon_RefreshNormal, typeof(Texture));
		Icon_RefreshClicked = (Texture2D)AssetDatabase.LoadAssetAtPath(UtuPluginPaths.pluginIcon_Rel_Icon_RefreshClicked, typeof(Texture));
		Tab_Normal = (Texture2D)AssetDatabase.LoadAssetAtPath(UtuPluginPaths.pluginIcon_Rel_Tab_Normal, typeof(Texture));
		Tab_Selected = (Texture2D)AssetDatabase.LoadAssetAtPath(UtuPluginPaths.pluginIcon_Rel_Tab_Selected, typeof(Texture));
		Color_DarkGrey = (Texture2D)AssetDatabase.LoadAssetAtPath(UtuPluginPaths.pluginIcon_Rel_Color_DarkGrey, typeof(Texture));
		Color_LightGrey = (Texture2D)AssetDatabase.LoadAssetAtPath(UtuPluginPaths.pluginIcon_Rel_Color_LightGrey, typeof(Texture));
		Color_Grey = (Texture2D)AssetDatabase.LoadAssetAtPath(UtuPluginPaths.pluginIcon_Rel_Color_Grey, typeof(Texture));
	}

	private int selectedTabIndex = 0;
	private void BuildTabs() {
		EditorGUILayout.BeginHorizontal(); {
			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.richText = true;
			buttonStyle.active.background = Tab_Selected;
			buttonStyle.hover.background = Tab_Selected;
			buttonStyle.normal.background = selectedTabIndex == 0 ? Tab_Selected : Tab_Normal;
			if (GUILayout.Button("<b><size=12><color=white> Export </color></size></b>", buttonStyle, GUILayout.Height(30.0f), GUILayout.Width(170.0f))) {
				selectedTabIndex = 0;
			}
			buttonStyle.normal.background = selectedTabIndex == 1 ? Tab_Selected : Tab_Normal;
			if (GUILayout.Button("<b><size=12><color=white> Log </color></size></b>", buttonStyle, GUILayout.Height(30.0f), GUILayout.Width(170.0f))) {
				selectedTabIndex = 1;
				BuildFormattedLog();
			}
			buttonStyle.normal.background = selectedTabIndex == 2 ? Tab_Selected : Tab_Normal;
			if (GUILayout.Button("<b><size=12><color=white> Information </color></size></b>", buttonStyle, GUILayout.Height(30.0f), GUILayout.Width(170.0f))) {
				selectedTabIndex = 2;
			}
			GUILayout.FlexibleSpace();
			GUIStyle titleLabelStyle = new GUIStyle(GUI.skin.label);
			titleLabelStyle.richText = true;
			titleLabelStyle.padding = new RectOffset(5, 5, 10, 10);
			GUILayout.Label("<size=12><color=grey>1.4</color></size>", titleLabelStyle);
		}
		EditorGUILayout.EndHorizontal();
	}


	// Scenes Scroll Box --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private string ALL = "All";
	private string PREVIOUS_ALL = "PreviousAll";
	private Dictionary<string, bool> scenesDict = new Dictionary<string, bool>() { { "All", true }, { "PreviousAll", true } };
	private Vector2 scenesScrollPosition = Vector2.zero;
	private void BuildScenesScrollBox() {
		// Title
		GUIStyle titleLabelStyle = new GUIStyle(GUI.skin.label);
		titleLabelStyle.richText = true;
		titleLabelStyle.padding = new RectOffset(5, 5, 10, 10);
		GUILayout.Label("<b><size=14><color=white> Select scenes to export: </color></size></b>\n<size=12><color=grey> All the referenced assets will be included. </color></size>", titleLabelStyle);
		// Scrollbox
		GUIStyle scrollBoxStyle = new GUIStyle(GUI.skin.scrollView);
		scrollBoxStyle.padding = new RectOffset(20, 0, 0, 0);
		scrollBoxStyle.normal.background = Color_Grey;
		scenesScrollPosition = GUILayout.BeginScrollView(scenesScrollPosition, scrollBoxStyle);
		// Checkbox "All"
		GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
		toggleStyle.richText = true;
		toggleStyle.padding = new RectOffset(0, 0, 4, 4);
		scenesDict[ALL] = GUILayout.Toggle(scenesDict[ALL], "<size=11><color=" + (scenesDict[ALL] ? "white" : "grey") + ">     " + ALL + "</color></size>", toggleStyle);
		if (scenesDict[ALL] != scenesDict[PREVIOUS_ALL]) {
			scenesDict[PREVIOUS_ALL] = scenesDict[ALL];
			List<string> keys = scenesDict.Keys.ToList<string>();
			foreach (string key in keys) {
				scenesDict[key] = scenesDict[ALL];
			}
		}
		// Checkbox Scenes
		List<string> scenes = UtuPluginAssets.GetScenesRelativeFilenames();
		foreach (string scene in scenes) {
			if (!scenesDict.ContainsKey(scene)) {
				scenesDict.Add(scene, true);
			}
			scenesDict[scene] = GUILayout.Toggle(scenesDict[scene], "<size=11><color=" + (scenesDict[scene] ? "white" : "grey") + ">      " + scene + "</color></size>", toggleStyle);
		}
		GUILayout.EndScrollView();
	}


	private List<string> GetSelectedScenesRelativeFilenames() {
		List<string> ret = new List<string>();
		List<string> keys = scenesDict.Keys.ToList<string>();
		foreach (string key in keys) {
			if (key != ALL && key != PREVIOUS_ALL) {
				if (scenesDict[key] == true) {
					ret.Add(key);
				}
			}
		}
		return ret;
	}

	// Export Button --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	List<string> randomNames = new List<string>(new string[] { "vivacious", "closed", "voracious", "mammoth", "fantastic", "square", "tender", "flippant", "ragged", "abnormal", "psychedelic", "bright", "judicious", "shrill", "addicted", "unused", "wasteful", "upbeat", "bitter", "encouraging", "soggy", "flat", "open", "somber", "loving", "tiny", "cold", "aware", "gray", "fair", "difficult", "magnificent", "amusing", "better", "tangible", "tidy", "drab", "wretched", "plain", "vigorous", "daily", "permissible", "brainy", "maddening", "kind", "lean", "willing", "ablaze", "beautiful", "clammy", "obtainable", "chunky", "windy", "insidious", "military", "madly", "extra-small", "abrupt", "standing", "inconclusive", "sassy", "rustic", "neighborly", "tall", "shiny", "redundant", "outgoing", "tiresome", "disturbed", "spooky", "flowery", "tasteless", "beneficial", "present", "evasive", "sore", "eager", "flawless", "demonic", "possessive", "lacking", "godly", "extra-large", "mysterious", "delightful", "freezing", "abstracted", "witty", "scandalous", "slow", "jagged", "taboo", "omniscient", "hospitable", "jumpy", "silky", "last", "dry", "special", "habitual", "wiggly", "bouncy", "furry", "fearful", "thick", "bright", "grotesque" });
	private string exportName = "Name (Optional)";
	private void BuildExportButton() {
		if (currentExport == null) {
			GUIStyle style = new GUIStyle();
			style.padding = new RectOffset(0, 0, 20, 10);
			GUILayout.BeginHorizontal(style); {
				GUILayout.FlexibleSpace();
				EditorGUI.BeginDisabledGroup(GetSelectedScenesRelativeFilenames().Count() == 0); {
					GUILayout.BeginVertical(); {
						GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
						buttonStyle.richText = true;
						buttonStyle.alignment = TextAnchor.MiddleCenter;
						if (GUILayout.Button("<b><size=12>Export Scenes</size></b>", buttonStyle, GUILayout.Height(40.0f), GUILayout.Width(200.0f))) {
							if (exportName == "Name (Optional)") {
								int x = (int)Random.Range(0, randomNames.Count - 1);
								exportName = randomNames[x] + "_export";
							}
							UtuPlugin.Export(GetSelectedScenesRelativeFilenames(), exportName, false/*All On One Frame*/);
							status = false/*All On One Frame*/ ? UtuPluginStatus.Completed : UtuPluginStatus.Exporting;
							BuildFormattedLog();
						}
						GUILayout.BeginHorizontal(); {
							// Text Field
							GUIStyle textStyle = new GUIStyle(GUI.skin.textField);
							textStyle.alignment = TextAnchor.MiddleCenter;
							exportName = GUILayout.TextField(exportName, textStyle, GUILayout.Height(22.0f));
							// Random Button
							if (GUILayout.Button("R", GUILayout.Height(22.0f), GUILayout.Width(22.0f))) {
								int x = (int)Random.Range(0, randomNames.Count - 1);
								exportName = randomNames[x] + "_export";
							}
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndVertical();
				}
				EditorGUI.EndDisabledGroup();
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
		}
	}

	// Open Json Button --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private void BuildOpenJsonButton() {
		//List<string> jsons = UtuPluginJsonUtilities.GetAvailableExportJsons();
		//EditorGUI.BeginDisabledGroup(jsons.Count <= 0); {
		//	if (GUILayout.Button("Json", GUILayout.Height(40.0f), GUILayout.Width(200.0f))) {
		//		System.Diagnostics.Process.Start(jsons[0]);
		//	}
		//}
		//EditorGUI.EndDisabledGroup();
	}

	// Progress Bars --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private void BuildProgressBars() {
		if (currentExport != null) {
			GUIStyle style = new GUIStyle();
			style.padding = new RectOffset(0, 0, 20, 10);
			GUILayout.BeginHorizontal(style); {
				EditorGUILayout.Separator();
				if (GUILayout.Button("Cancel Export", GUILayout.Height(40.0f), GUILayout.Width(200.0f))) {
					UtuPlugin.CancelExport();
					status = UtuPluginStatus.Cancelled;
				}
				EditorGUILayout.Separator();
			}
			GUILayout.EndHorizontal();
			EditorGUILayout.Separator();
			Rect sceneProgressRect = EditorGUILayout.BeginVertical();
			EditorGUI.ProgressBar(sceneProgressRect, currentExport.percentSceneToProcess, currentExport.nameSceneToProcess);
			GUILayout.Space(24);
			EditorGUILayout.EndVertical();
			Rect actorProgressRect = EditorGUILayout.BeginVertical();
			if (currentExport.currentSceneProcessor != null) {
				EditorGUI.ProgressBar(actorProgressRect, currentExport.currentSceneProcessor.percentActorsToProcess, currentExport.currentSceneProcessor.nameActorToProcess);
			}
			else {
				EditorGUI.ProgressBar(actorProgressRect, 1.0f, "");
			}
			GUILayout.Space(16);
			EditorGUILayout.EndVertical();
			EditorGUILayout.Separator();
		}
	}

	private void BuildStatusText() {
		if (status == UtuPluginStatus.Cancelled) {
			GUILayout.BeginHorizontal(); {
				EditorGUILayout.Separator();
				GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
				labelStyle.normal.textColor = Color.red;
				GUILayout.Label("Export Cancelled", labelStyle);
			}
			GUILayout.EndHorizontal();
		}
		else if (status == UtuPluginStatus.Completed) {
			GUILayout.BeginHorizontal(); {
				EditorGUILayout.Separator();
				GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                UtuLog.GetLogState(out EUtuLog logState, out int wCount, out int eCount);
                if (logState == EUtuLog.Log) {
                    labelStyle.normal.textColor = Color.white;
                }
				else if (logState == EUtuLog.Warning) {
                    labelStyle.normal.textColor = Color.yellow;
                }
                else if (logState == EUtuLog.Error) {
                    labelStyle.normal.textColor = Color.red;
                }
                GUILayout.Label("       Export Completed!\n(With " + Mathf.Max(0, wCount - 1).ToString() + " Warnings & " + Mathf.Max(0, eCount - 1) + " Errors)", labelStyle);
            }
            GUILayout.EndHorizontal();
		}
		else if (status == UtuPluginStatus.Exporting) {
			GUILayout.BeginHorizontal(); {
				EditorGUILayout.Separator();
				GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
				labelStyle.normal.textColor = Color.white;
				GUILayout.Label("Export In Progress...", labelStyle);
			}
			GUILayout.EndHorizontal();
		}
		EditorGUILayout.Separator();
	}


	// Log --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private string formattedLog = "";
	private bool showLog = true;
	private bool showLogPrev = true;
	private bool showWarning = true;
	private bool showWarningPrev = true;
	private bool showError = true;
	private bool showErrorPrev = true;
	private void BuildFormattedLog() {
		formattedLog = "";
		foreach (FUtuLog log in UtuLog.GetLog()) {
			if (log.logCategory == EUtuLog.Log && showLog == true) {
				formattedLog += "<size=11><color=white>" + log.message + "\n</color></size>";
			}
			else if (log.logCategory == EUtuLog.Warning && showWarning == true) {
				formattedLog += "<size=11><color=yellow>" + log.message + "\n</color></size>";
			}
			else if (log.logCategory == EUtuLog.Error && showError == true) {
				formattedLog += "<size=11><color=red>" + log.message + "\n</color></size>";
			}
		}
	}

	private void CopyLogToClipboard() {
		string fullLog = "";
		foreach (FUtuLog log in UtuLog.GetLog()) {
			if (log.logCategory == EUtuLog.Log) {
				fullLog += "L    " + log.message + "\n";
			}
			else if (log.logCategory == EUtuLog.Warning) {
				fullLog += "W    " + log.message + "\n";
			}
			else if (log.logCategory == EUtuLog.Error) {
				fullLog += "E    " + log.message + "\n";
			}
		}
		EditorGUIUtility.systemCopyBuffer = fullLog;
	}

	private Vector2 logcrollPosition = Vector2.zero;
	private void BuildLog() {
		if (showLog != showLogPrev || showWarning != showWarningPrev || showError != showErrorPrev) {
			showLogPrev = showLog;
			showWarningPrev = showWarning;
			showErrorPrev = showError;
			BuildFormattedLog();
		}
		GUILayout.BeginVertical(); {
			GUIStyle hBoxStyle = new GUIStyle();
			hBoxStyle.padding = new RectOffset(0, 0, 10, 10);
			hBoxStyle.normal.background = Color_DarkGrey;
			GUILayout.BeginHorizontal(hBoxStyle); {
				GUILayout.BeginVertical(); {
					GUILayout.BeginHorizontal(hBoxStyle); {
						GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
						toggleStyle.richText = true;
						showLog = GUILayout.Toggle(showLog, "<size=11><color=white> Log  </color></size>", toggleStyle);
						showWarning = GUILayout.Toggle(showWarning, "<size=11><color=yellow> Warning  </color></size>", toggleStyle);
						showError = GUILayout.Toggle(showError, "<size=11><color=red> Error  </color></size>", toggleStyle);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				GUIStyle copyButtonStyle = new GUIStyle(GUI.skin.button);
				copyButtonStyle.active.background = Icon_CopyClicked;
				copyButtonStyle.hover.background = Icon_CopyNormal;
				copyButtonStyle.normal.background = Icon_CopyNormal;
				if (GUILayout.Button("", copyButtonStyle, GUILayout.Height(35.0f), GUILayout.Width(35.0f))) {
					CopyLogToClipboard();
				}
				GUIStyle refreshButtonStyle = new GUIStyle(GUI.skin.button);
				refreshButtonStyle.active.background = Icon_RefreshClicked;
				refreshButtonStyle.hover.background = Icon_RefreshNormal;
				refreshButtonStyle.normal.background = Icon_RefreshNormal;
				if (GUILayout.Button("", refreshButtonStyle, GUILayout.Height(35.0f), GUILayout.Width(35.0f))) {
					BuildFormattedLog();
					Repaint();
				}
			}
			GUILayout.EndHorizontal();
			// Scrollbox
			GUIStyle scrollBoxStyle = new GUIStyle(GUI.skin.scrollView);
			//scrollBoxStyle.padding = new RectOffset(10, 10, 10, 10);
			logcrollPosition = GUILayout.BeginScrollView(logcrollPosition, scrollBoxStyle); {
				GUIStyle vBoxStyle = new GUIStyle();
				vBoxStyle.normal.background = Color_Grey;
				GUILayout.BeginVertical(vBoxStyle); {
					// Release Note
					GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
					labelStyle.richText = true;
					labelStyle.padding = new RectOffset(5, 5, 10, 10);
					GUILayout.Label(formattedLog, labelStyle);
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndScrollView();
		}
		GUILayout.EndVertical();
	}

	// Help and release note --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private string releaseNote = "";
	private void BuildReleaseNote() {
		List<string> asList = new List<string>();
		asList.Add("IMPORTANT:");
		asList.Add("    - Please backup your project before using this tool. ");
		asList.Add("       I've never lost any data using this tool, but I don't want to be responsible for any data loss during the process.");
		asList.Add("       (It is also a good habit to backup your project from time to time or to simply use a source control.)");
		asList.Add("");
		asList.Add("");
		asList.Add("Compatible Unity Versions (Win64 & Mac): 2018 to 2023");
		asList.Add("Compatible Unreal Versions (Win64): 4.25, 4.26, 4.27, 5.1, 5.2, 5.3");
		asList.Add("Compatible Unreal Versions (Mac): 4.25, 4.26, 4.27, 5.1 (5.2, 5.3 should also work, but you'll have to compile them yourself)");
		asList.Add("");
		asList.Add("");
		asList.Add("Demo & Tutorial: https://youtu.be/34qx5Ac8cZo?si=ne29q3cGtNJ6WWsu");
		asList.Add("");
		asList.Add("");
		asList.Add("Supported Assets:");
		asList.Add("    - Scene:");
		asList.Add("        - Creates a World asset");
		asList.Add("        - Adds a SkyLight to simulate Unity's default lighting");
		asList.Add("        - Adds all the actors (Including display name, transform, visibility and tag)");
		asList.Add("        - Recreates the scene's hierarchy");
		asList.Add("        - Supports instance edited materials on static mesh actors");
		asList.Add("        - Limitations:");
		asList.Add("            - If your scenes contain non-uniform scaling on any parent of a rotated mesh, ");
		asList.Add("              the result will not be as expected in Unreal because of the way the meshes are transformed.");
		asList.Add("                - Bad hierarchy examples:");
		asList.Add("                    - Scene root  ->  Non-uniformly scaled object  ->  Rotated mesh");
		asList.Add("                    - Scene root  ->  Non-uniformly scaled object  ->  Uniformly scaled object  ->  Rotated mesh");
		asList.Add("                    - Scene root  ->  Non-uniformly scaled object  ->  Rotated object                   ->  Not rotated mesh");
		asList.Add("                - Good hierarchy examples:");
		asList.Add("                    - Scene root  ->  Uniformly scaled object           ->  Rotated meah");
		asList.Add("                    - Scene root  ->  Non-uniformly scaled object  ->  Not rotated meah");
		asList.Add("                    - Scene root  ->  Uniformly scaled object           ->  Not rotated meah");
		asList.Add("");
		asList.Add("    - Prefab:");
		asList.Add("        - Creates a Blueprint Actor asset");
		asList.Add("        - Adds all the components (Including display name, transform, visibility and tag)");
		asList.Add("        - Recreates the prefab's hierarchy");
		asList.Add("        - Supports instance edited materials on static mesh components");
		asList.Add("        - Supports nested prefabs (child actor components)");
		asList.Add("        - Limitations:");
		asList.Add("            - If your prefabs contain non-uniform scaling on any parent of a rotated mesh, ");
		asList.Add("              the result will not be as expected in Unreal because of the way the meshes are transformed.");
		asList.Add("                - Bad hierarchy examples:");
		asList.Add("                    - Prefab root  ->  Non-uniformly scaled object  ->  Rotated mesh");
		asList.Add("                    - Prefab root  ->  Non-uniformly scaled object  ->  Uniformly scaled object  ->  Rotated mesh");
		asList.Add("                    - Prefab root  ->  Non-uniformly scaled object  ->  Rotated object                   ->  Not rotated mesh");
		asList.Add("                - Good hierarchy examples:");
		asList.Add("                    - Prefab root  ->  Uniformly scaled object           ->  Rotated meah");
		asList.Add("                    - Prefab root  ->  Non-uniformly scaled object  ->  Not rotated meah");
		asList.Add("                    - Prefab root  ->  Uniformly scaled object           ->  Not rotated meah");
		asList.Add("");
		asList.Add("    - Static Mesh:");
		asList.Add("        - Creates a Static Mesh asset using the .fbx file");
		asList.Add("        - Assigns the materials to the mesh");
		asList.Add("        - New feature: ");
		asList.Add("            - Now supports .fbx files with more than one mesh");
		asList.Add("            ");
		asList.Add("    - Skeletal Mesh:");
		asList.Add("        - Creates a Skeletal Mesh asset using the .fbx file");
		asList.Add("        - Assigns the materials to the mesh");
		asList.Add("        - Limitations: ");
		asList.Add("            - Rig may not be supported by Unreal");
		asList.Add("            - Animations are not imported");
		asList.Add("");
		asList.Add("    - Texture:");
		asList.Add("        - Creates a Texture2D asset using the texture file");
		asList.Add("        - Limitations: ");
		asList.Add("            - .tif files are not supported");
		asList.Add("");
		asList.Add("    - Material: ");
		asList.Add("        - Creates a Material asset");
		asList.Add("        - Adds all the referenced textures into the material");
		asList.Add("        - Connects textures to the equivalent parameter");
		asList.Add("        - Sets the equivalent parameters for Metallic, Specular, Emission, Tiling and Offset");
		asList.Add("        - Supported shader types:");
		asList.Add("            - Standard");
		asList.Add("            - Standard (Specular setup)");
		asList.Add("            - Unlit/Color");
		asList.Add("            - Unlit/Texture");
		asList.Add("            - Unlit/Transparent");
		asList.Add("            - Unlit/Transparent Cutout");
		asList.Add("            - Mobile/Diffuse");
		asList.Add("            - Mobile/Unlit (Supports Lightmap)");
		asList.Add("            - Legacy Shaders/Diffuse");
		asList.Add("");
		asList.Add("    - Spotlight:");
		asList.Add("        - Supported parameters:");
		asList.Add("            - Intensity");
		asList.Add("            - Color");
		asList.Add("            - Range");
		asList.Add("            - Cast Shadows");
		asList.Add("            - Spot Angle");
		asList.Add("        - Limitations: ");
		asList.Add("            - Equivalence is not 100% perfect");
		asList.Add("            ");
		asList.Add("    - Pointlight:");
		asList.Add("        - Supported parameters:");
		asList.Add("            - Intensity");
		asList.Add("            - Color");
		asList.Add("            - Range");
		asList.Add("            - Cast Shadows");
		asList.Add("        - Limitations: ");
		asList.Add("            - Equivalence is not 100% perfect");
		asList.Add("            ");
		asList.Add("    - Directionnal Light:");
		asList.Add("        - Supported parameters:");
		asList.Add("            - Intensity");
		asList.Add("            - Color");
		asList.Add("            - Cast Shadows");
		asList.Add("        - Limitations: ");
		asList.Add("            - Equivalence is not 100% perfect");
		asList.Add("");
		asList.Add("    - Camera:");
		asList.Add("        - Supported parameters:");
		asList.Add("            - Clipping planes");
		asList.Add("            - Aspect ratio");
		asList.Add("            - Projection");
		asList.Add("            - Field of view");
		asList.Add("            - Focal length");
		asList.Add("            - Sensor size");
		asList.Add("");
		asList.Add("");
		asList.Add("Unsupported Assets:");
		asList.Add("    - Animations");
		asList.Add("    - Terrains");
		asList.Add("    - Foliage");
		asList.Add("    - Particle Systems");
		asList.Add("    - 2D Objects");
		asList.Add("    - User Interfaces");
		asList.Add("    - Audio");
		asList.Add("    - Scripts");
		asList.Add("    - Physic Objects");
		asList.Add("    - Everyting else that is not in the Supported Assets list");
		asList.Add("");
		asList.Add("");
		releaseNote = "";
		foreach (string x in asList) {
			releaseNote += "\n<color=white><size=12>" + x + "</size></color>";
		}
	}

	private string processSteps = "";
	private void BuildProcessSteps() {
		List<string> asList = new List<string>();
		asList.Add("* In Unity");
		asList.Add("    1 - Install the tool in your Unity project.");
		asList.Add("    2 - Open your Unity project.");
		asList.Add("    3 - Open the tool using the 'Plugins/Utu Plugin' drop down menu at the top.");
		asList.Add("    4 - Select the scenes you want to Export. (The tool will export evertything that is referenced par the selected scenes.)");
		asList.Add("    5 - Name your export (optional)");
		asList.Add("    6 - Press Export");
		asList.Add("");
		asList.Add("* In Unreal");
		asList.Add("    7 - Install the tool in your Unreal project in which you want to Import your scenes.");
		asList.Add("    8 - Open the destination Unreal project.");
		asList.Add("    9 - Open the tool using the 'Utu Plugin' button at the top.");
		asList.Add("   10 - Select the Export that you want to Import.");
		asList.Add("   11 - Press Import.");
		asList.Add("   12 - Go get some coffee   :) ");
		asList.Add("   13 - Once completed, you can look at the 'Log' tab to get more information about the process. (Including the warnings and errors.)");
		asList.Add("");
		asList.Add("");
		processSteps = "";
		foreach (string x in asList) {
			processSteps += "\n<color=white><size=12>" + x + "</size></color>";
		}
	}

	private Vector2 releaseNoteScrollPosition = Vector2.zero;
	private void BuildHelpAndReleaseNote() {
		// Scrollbox
		GUIStyle scrollBoxStyle = new GUIStyle(GUI.skin.scrollView);
		scrollBoxStyle.padding = new RectOffset(10, 10, 10, 10);
		releaseNoteScrollPosition = GUILayout.BeginScrollView(releaseNoteScrollPosition, scrollBoxStyle);
		GUILayout.BeginVertical(); {
			GUIStyle vBoxStyle = new GUIStyle();
			vBoxStyle.normal.background = Color_Grey;
			GUILayout.BeginVertical(vBoxStyle); {
				// Title
				GUIStyle titleLabelStyle = new GUIStyle(GUI.skin.label);
				titleLabelStyle.richText = true;
				titleLabelStyle.padding = new RectOffset(5, 5, 10, 10);
				titleLabelStyle.alignment = TextAnchor.MiddleCenter;
				GUILayout.Label("<color=white><b><size=14> Information </size></b></color>", titleLabelStyle);
				// Release Note
				GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
				labelStyle.richText = true;
				labelStyle.padding = new RectOffset(5, 5, 10, 10);
				GUILayout.Label(releaseNote, labelStyle);
			}
			GUILayout.EndVertical();
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();
			GUILayout.BeginVertical(vBoxStyle); {
				// Title
				GUIStyle titleLabelStyle = new GUIStyle(GUI.skin.label);
				titleLabelStyle.richText = true;
				titleLabelStyle.padding = new RectOffset(5, 5, 10, 10);
				titleLabelStyle.alignment = TextAnchor.MiddleCenter;
				GUILayout.Label("<color=white><b><size=14> Process Steps </size></b></color>", titleLabelStyle);
				// Contact Informations
				GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
				labelStyle.richText = true;
				labelStyle.padding = new RectOffset(5, 5, 10, 10);
				GUILayout.Label(processSteps, labelStyle);
			}
			GUILayout.EndVertical();
		}
		GUILayout.EndVertical();
		GUILayout.EndScrollView();
	}
}
