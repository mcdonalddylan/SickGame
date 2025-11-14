// Copyright Alex Quevillon. All Rights Reserved.

using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class UtuPluginCurrentExport {
	// Global
	public UtuPluginJson json;
	public string timestamp = "";
	// Delayed Specific
	private List<string> scenesRelativeFilenamesToProcess;
	public UtuPluginSceneProcessor currentSceneProcessor = null;
	public int countSceneToProcess = 1;
	public int amountSceneToProcess = 0;
	public float percentSceneToProcess = 0.0f;
	public string nameSceneToProcess = "";

	public void Export(List<string> selectedScenesRelativeFilenames, string exportName, bool executeFullExportOnSameFrame) {
		BeginExport(selectedScenesRelativeFilenames, exportName);
		if (executeFullExportOnSameFrame) {
			while (ContinueExport(executeFullExportOnSameFrame) != true) {
				// ContinueExport 
			}
		}
	}

	public bool ContinueExport(bool executeFullExportOnSameFrame) {
		if (currentSceneProcessor == null && scenesRelativeFilenamesToProcess.Count == 0) {
			UtuLog.Error("UtuPluginCurrentExport::ContinueImport() Was called even though the list is already empty. This should never happen!");
			return true;
		}
		if (currentSceneProcessor == null && scenesRelativeFilenamesToProcess.Count > 0) {
			currentSceneProcessor = new UtuPluginSceneProcessor();
			nameSceneToProcess = scenesRelativeFilenamesToProcess[0];
			currentSceneProcessor.Export(json, scenesRelativeFilenamesToProcess[0], executeFullExportOnSameFrame);
			scenesRelativeFilenamesToProcess.RemoveAt(0);
		}
		if (executeFullExportOnSameFrame) {
			currentSceneProcessor = null;
			countSceneToProcess = amountSceneToProcess;
			percentSceneToProcess = (float)countSceneToProcess / (float)amountSceneToProcess;
		}
		else {
			if (currentSceneProcessor.ContinueExport()) {
				currentSceneProcessor = null;
				countSceneToProcess++;
				percentSceneToProcess = (float)countSceneToProcess / (float)amountSceneToProcess;
			}
		}
		if (currentSceneProcessor == null && scenesRelativeFilenamesToProcess.Count == 0) {
			CompleteExport();
			return true;
		}
		return false;
	}

	public void BeginExport(List<string> selectedScenesRelativeFilenames, string exportName) {
		scenesRelativeFilenamesToProcess = selectedScenesRelativeFilenames;
		countSceneToProcess = 1;
		amountSceneToProcess = scenesRelativeFilenamesToProcess.Count;
		percentSceneToProcess = (float)countSceneToProcess / (float)amountSceneToProcess;
		json = new UtuPluginJson();
		json.json_info = new UtuPluginJsonInfo();
		json.json_info.export_datetime = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
		json.json_info.export_name = exportName;
		timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
		json.json_info.export_timestamp = timestamp;
		CreateExportDirectory();
		UtuLog.ClearLog();
		UtuLog.InitializeNewLog(timestamp);
		UtuLog.Separator();
		UtuLog.Log("Beginning New Export...");
		UtuLog.Log("    Time: " + System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
		UtuLog.Log("    Export Name: " + json.json_info.export_name);
		UtuLog.Log("    Supported Asset Types: ");
		UtuLog.Log("        Texture");
		UtuLog.Log("        Material");
		UtuLog.Log("        Scene");
		UtuLog.Log("        Static Mesh");
		UtuLog.Log("        Prefab");
		UtuLog.Log("    Supported Components Types: ");
		UtuLog.Log("        Empty");
		UtuLog.Log("        Static Mesh");
		UtuLog.Log("        Prefab");
		UtuLog.Log("    Scene Count to Process: " + amountSceneToProcess.ToString());
	}

	private void CompleteExport() {
		UtuLog.Separator();
		UtuLog.Log("Completing Export...");
		UtuLog.Log("    Time: " + System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
		UtuPluginJsonUtilities.DumpExportJsonToFile(json, timestamp);
		UtuLog.Separator();
		UtuLog.Log("Generated Data:");
		UtuLog.Log("    Scenes (" + json.scenes.Count.ToString() + "):");
		foreach (UtuPluginScene x in json.scenes) {
			UtuLog.Log("        " + x.asset_relative_filename);
		}
		UtuLog.Empty();
		UtuLog.Log("    Prefabs (" + json.prefabs_first_pass.Count.ToString() + "):");
		foreach (UtuPluginPrefabFirstPass x in json.prefabs_first_pass) {
			UtuLog.Log("        " + x.asset_relative_filename);
		}
		UtuLog.Empty();
		UtuLog.Log("    Meshes (" + json.meshes.Count.ToString() + "):");
		foreach (UtuPluginMesh x in json.meshes) {
			UtuLog.Log("        " + x.asset_relative_filename);
			if (x.mesh_materials_relative_filenames.Count > 0) {
				UtuLog.Log("            Linked Materials:");
				foreach (string y in x.mesh_materials_relative_filenames) {
					UtuLog.Log("                " + y);
				}
			}
			UtuLog.SemiSeparator("        ");
		}
		UtuLog.Empty();
		UtuLog.Log("    Materials (" + json.materials.Count.ToString() + "):");
		foreach (UtuPluginMaterial x in json.materials) {
			UtuLog.Log("        " + x.asset_relative_filename);
			List<string> validTextures = new List<string>();
			foreach (string y in x.material_textures_relative_filenames_for_unsupported_shaders) {
				if (y != "") {
					validTextures.Add(y);
				}
			}
			if (validTextures.Count > 0) {
				UtuLog.Log("            Linked Textures:");
				foreach (string y in validTextures) {
					UtuLog.Log("                " + y);
				}
			}
			UtuLog.SemiSeparator("        ");
		}
		UtuLog.Empty();
		UtuLog.Log("    Textures (" + json.textures.Count.ToString() + "):");
		foreach (UtuPluginTexture x in json.textures) {
			UtuLog.Log("        " + x.asset_relative_filename);
		}
		UtuLog.Separator();
		UtuLog.Log("Export Completed!");
		UtuLog.Log("    Time: " + System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
		UtuLog.GetLogState(out EUtuLog logState, out int warningCount, out int errorCount);
		if (warningCount == 0) {
			UtuLog.Log("Warning Count: " + warningCount.ToString());
		}
		else {
			UtuLog.Warning("Warning Count: " + warningCount.ToString());
		}
		if (errorCount == 0) {
			UtuLog.Log("Error Count: " + errorCount.ToString());
		}
		else {
			UtuLog.Error("Error Count: " + errorCount.ToString());
		}
		UtuLog.Separator();
	}

	private void CreateExportDirectory() {
		string path = UtuPluginPaths.pluginFolder_Full_Exports + UtuPluginPaths.slash + timestamp;
		if (!Directory.Exists(path)) {
			Directory.CreateDirectory(path);
		}
	}
}


public class UtuPlugin {
	private static UtuPluginCurrentExport currentExportJob = null;

	public static void Export(List<string> selectedScenesRelativeFilenames, string exportName, bool executeFullExportOnSameFrame) {
		currentExportJob = new UtuPluginCurrentExport();
		currentExportJob.Export(selectedScenesRelativeFilenames, exportName, executeFullExportOnSameFrame);
		if (executeFullExportOnSameFrame) {
			currentExportJob = null;
		}
	}

	public static UtuPluginCurrentExport ContinueCurrentExport() {
		if (currentExportJob != null) {
			if (currentExportJob.ContinueExport(false)) {
				currentExportJob = null;
			}
		}
		return currentExportJob;
	}

	public static void CancelExport() {
		currentExportJob = null;
	}
}
