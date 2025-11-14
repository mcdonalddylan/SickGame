// Copyright Alex Quevillon. All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class UtuPluginAssets {
	private static bool isCachedScenesValid = false;
	private static List<string> cachedScenesRelativeFilenames = new List<string>();
	public static List<string> GetScenesRelativeFilenames() {
		if (!isCachedScenesValid) {
			List<string> ret = new List<string>();
			foreach (string x in AssetDatabase.GetAllAssetPaths()) {
				if (x.StartsWith("Assets") && x.EndsWith(".unity")) {
					ret.Add(x);
				}
			}
			ret.Sort();
			cachedScenesRelativeFilenames = ret;
			isCachedScenesValid = true;
		}
		return cachedScenesRelativeFilenames;
	}

	public static void ClearCachedScenesRelativeFilenames() {
		isCachedScenesValid = false;
	}
}


public class UtuPluginSceneProcessor {
	private UtuPluginJson json;
	private UtuPluginScene scene;
	private List<UtuPluginActor> actors = new List<UtuPluginActor>();
	private List<GameObject> gameObjects = new List<GameObject>();

	private Dictionary<string, Vector3> submeshPivotOffsets = new Dictionary<string, Vector3>();

	public int countActorsToProcess = 1;
	public int amountActorsToProcess = 0;
	public float percentActorsToProcess = 0.0f;
	public string nameActorToProcess = "";


	public void Export(UtuPluginJson utuPluginJson, string sceneRelativeFilename, bool executeFullExportOnSameFrame) {
		BeginExport(utuPluginJson, sceneRelativeFilename);
		if (executeFullExportOnSameFrame) {
			while (ContinueExport() != true) {
				// ContinueExport 
			}
		}
	}

	public void BeginExport(UtuPluginJson utuPluginJson, string sceneRelativeFilename) {
		json = utuPluginJson;
		scene = CreateScene(sceneRelativeFilename);
		UtuLog.Separator();
		UtuLog.Log("    Generating Data for Scene...");
		UtuLog.Log("        Scene Name: " + scene.asset_name);
		UtuLog.Log("        Scene Relative Name: " + scene.asset_relative_filename);
		UtuLog.SemiSeparator("    ");
		UtuLog.Log("    Opening Scene...");
		gameObjects = new List<GameObject>(OpenSceneWorld(sceneRelativeFilename));
		actors = new List<UtuPluginActor>();
		countActorsToProcess = 1;
		amountActorsToProcess = gameObjects.Count;
		percentActorsToProcess = (float)countActorsToProcess / (float)amountActorsToProcess;
		UtuLog.Log("        Quantity GameObjects in Scene: " + gameObjects.Count.ToString());
		UtuLog.SemiSeparator("    ");
		UtuLog.Log("    Generating Data for GameObjects...");
	}

	public bool ContinueExport() {
		if (gameObjects.Count == 0) {
			UtuLog.Error("UtuPluginSceneProcessor::ContinueImport() Was called even though the list is already empty. This should never happen!");
			return true;
		}
		nameActorToProcess = gameObjects[0].name;
		UtuPluginActor actor = ProcessActor(gameObjects[0], true);
		if (actor != null) {
			scene.scene_actors.Add(actor);
		}
		gameObjects.RemoveAt(0);
		countActorsToProcess++;
		percentActorsToProcess = (float)countActorsToProcess / (float)amountActorsToProcess;
		if (gameObjects.Count == 0) {
			CompleteExport();
			return true;
		}
		return false;
	}

	private void CompleteExport() {
		UtuLog.SemiSeparator("    ");
		UtuLog.Log("    Data Generation for Scene Completed!");
		json.scenes.Add(scene);
		json.json_info.scenes.Add(scene.asset_name);
	}

	private UtuPluginScene CreateScene(string sceneRelativeFilename) {
		UtuPluginScene utuPluginScene = new UtuPluginScene();
		utuPluginScene.asset_relative_filename = sceneRelativeFilename;
		utuPluginScene.asset_name = sceneRelativeFilename.Split(UtuPluginPaths.slash).Last<string>();
		ErrorIfBadCharactersInPath(utuPluginScene.asset_relative_filename);
		return utuPluginScene;
	}

	private GameObject[] OpenSceneWorld(string sceneRelativeFilename) {
		EditorSceneManager.OpenScene(sceneRelativeFilename, OpenSceneMode.Single);
#if UNITY_2023_1_OR_NEWER
		return Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
		return Object.FindObjectsOfType<GameObject>();
#endif
	}

	private UtuPluginActor ProcessActor(GameObject go, bool enableLog) {
		UtuPluginActor utuPluginActor = CreateActor(go);
		if (PrefabUtility.IsPartOfRegularPrefab(go)) {
			if (PrefabUtility.GetOutermostPrefabInstanceRoot(go) == go) {
				if (/*!go.name.Contains("(Missing Prefab)") &&*/ PrefabUtility.GetCorrespondingObjectFromSource(go) != null) {
					utuPluginActor.actor_types.Add(UtuActorType.Prefab);
					utuPluginActor.actor_prefab = CreateActorPrefab(go);
				}
				else {
					UtuLog.Warning("        Missing Prefab detected! Exporting GameObject '" + go.name + "' as Empty GameObject.");
					utuPluginActor.actor_types.Add(UtuActorType.Empty);
				}
			}
			else {
				return null; // Do not return prefabs components as they won't be added individually in the scene!
			}
		}
		else {
			if (go.GetComponent<MeshFilter>()) {
				if (go.GetComponent<MeshFilter>().sharedMesh) {
					utuPluginActor.actor_types.Add(UtuActorType.StaticMesh);
					utuPluginActor.actor_mesh = CreateActorMesh(go);
					// Separated
					{
						MeshFilter meshFilter = go.GetComponent<MeshFilter>();
						string subMeshName = meshFilter.sharedMesh.ToString().Replace(" (UnityEngine.Mesh)", "");
						if (submeshPivotOffsets.ContainsKey(utuPluginActor.actor_mesh.actor_mesh_relative_filename + "_" + subMeshName))
						{
							utuPluginActor.actor_relative_location_if_separated = utuPluginActor.actor_relative_location - submeshPivotOffsets[utuPluginActor.actor_mesh.actor_mesh_relative_filename + "_" + subMeshName];
							utuPluginActor.actor_world_location_if_separated = utuPluginActor.actor_world_location - (utuPluginActor.actor_world_rotation * submeshPivotOffsets[utuPluginActor.actor_mesh.actor_mesh_relative_filename + "_" + subMeshName]);
						}
					}
				}
				else {
					UtuLog.Warning("        Missing Mesh in MeshFilter component detected while analysing: '" + go.name + "'. Component will be ignored.");
				}
			}
			if (go.GetComponent<SkinnedMeshRenderer>()) {
				if (go.GetComponent<SkinnedMeshRenderer>().sharedMesh) {
					utuPluginActor.actor_types.Add(UtuActorType.SkeletalMesh);
					utuPluginActor.actor_mesh = CreateActorSkeletalMesh(go);
                    // Cancel the transform. Because ... I don't know.
                    utuPluginActor.actor_relative_location = Vector3.zero;
                    utuPluginActor.actor_relative_rotation = Quaternion.identity;
                    utuPluginActor.actor_relative_scale = Vector3.one;
                    utuPluginActor.actor_world_location = go.transform.parent.position;
                    utuPluginActor.actor_world_rotation = go.transform.parent.rotation;
                    utuPluginActor.actor_world_scale = go.transform.parent.lossyScale;
                }
				else {
					UtuLog.Warning("        Missing Mesh in SkinnedMeshRenderer component detected while analysing: '" + go.name + "'. Component will be ignored.");
				}
			}
			if (go.GetComponent<Light>()) {
				Light light = go.GetComponent<Light>();
				if (light.type == LightType.Spot) {
					utuPluginActor.actor_types.Add(UtuActorType.SpotLight);
					utuPluginActor.actor_light = CreateActorLight(go);
				}
				else if (light.type == LightType.Directional) {
					utuPluginActor.actor_types.Add(UtuActorType.DirectionalLight);
					utuPluginActor.actor_light = CreateActorLight(go);
				}
				else if (light.type == LightType.Point) {
					utuPluginActor.actor_types.Add(UtuActorType.PointLight);
					utuPluginActor.actor_light = CreateActorLight(go);
				}
				else {
					UtuLog.Warning("        Unsupported LightType '" + light.type.ToString() + "' detected while analysing: '" + go.name + "'. Component will be ignored.");
				}
			}
			if (go.GetComponent<Camera>()) {
				utuPluginActor.actor_types.Add(UtuActorType.Camera);
				utuPluginActor.actor_camera = CreateActorCamera(go);
			}
			if (utuPluginActor.actor_types.Count != 1) { // If actor_types == 1, we don't need to have an empty root above the other components.
				UtuLog.Log("        Because GameObject '" + go.name + "' has " + utuPluginActor.actor_types.Count.ToString() + " supported components, adding an Empty GameObject as root of its hierarchy.");
				utuPluginActor.actor_types.Add(UtuActorType.Empty);
			}
		}
		return utuPluginActor;
	}

	// Actor -----------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private UtuPluginActor CreateActor(GameObject go) {
		UtuPluginActor utuPluginActor = new UtuPluginActor();
		utuPluginActor.actor_id = go.GetInstanceID();
		utuPluginActor.actor_parent_id = go.transform.parent ? go.transform.parent.gameObject.GetInstanceID() : UtuConst.INVALID_INT;
		utuPluginActor.actor_display_name = go.name;
		utuPluginActor.actor_tag = go.tag;
		utuPluginActor.actor_is_visible = go.activeSelf;
		utuPluginActor.actor_world_location = go.transform.position;
		utuPluginActor.actor_world_location_if_separated = go.transform.position;
		utuPluginActor.actor_world_rotation = go.transform.rotation;
		utuPluginActor.actor_world_scale = go.transform.lossyScale;
		utuPluginActor.actor_relative_location = go.transform.localPosition;
		utuPluginActor.actor_relative_location_if_separated = go.transform.localPosition;
		utuPluginActor.actor_relative_rotation = go.transform.localRotation;
		utuPluginActor.actor_relative_scale = go.transform.localScale;
		utuPluginActor.actor_is_movable = !go.isStatic;

		// Because Unreal does not give you the possibility to have Static actor child of a Movable actor
		if (go.isStatic)
		{
			Transform transform = go.transform;
			while (transform.parent != null)
			{
				transform = transform.parent;
				if (!transform.gameObject.isStatic)
				{
					// Movable Parent Detected
					UtuLog.Warning("        Detected a Static Gameobject with a Movable parent, which cannot be done in Unreal. This object will be set to movable. \n            Movable Object: " + transform.name + " \n            Static Object: " + go.name);
					utuPluginActor.actor_is_movable = true;
				}
			}
		}

		return utuPluginActor;
	}


	// Camera -----------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private UtuPluginActorCamera CreateActorCamera(GameObject go) {
		Camera camera = go.GetComponent<Camera>();
		UtuPluginActorCamera utuPluginActorCamera = new UtuPluginActorCamera();
		utuPluginActorCamera.camera_aspect_ratio = camera.aspect;
		utuPluginActorCamera.camera_far_clip_plane = camera.farClipPlane;
		utuPluginActorCamera.camera_persp_field_of_view = camera.fieldOfView;
		utuPluginActorCamera.camera_phys_focal_length = camera.focalLength;
		utuPluginActorCamera.camera_is_perspective = !camera.orthographic;
		utuPluginActorCamera.camera_is_physical = camera.usePhysicalProperties;
		utuPluginActorCamera.camera_near_clip_plane = camera.nearClipPlane;
		utuPluginActorCamera.camera_phys_sensor_size = camera.sensorSize;
		utuPluginActorCamera.camera_viewport_rect = new Quaternion(camera.rect.x, camera.rect.y, camera.rect.height, camera.rect.width);
		utuPluginActorCamera.camera_ortho_size = camera.orthographicSize;
		return utuPluginActorCamera;
	}


	// Light -----------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private UtuPluginActorLight CreateActorLight(GameObject go) {
		Light light = go.GetComponent<Light>();
		UtuPluginActorLight utuPluginActorLight = new UtuPluginActorLight();
		utuPluginActorLight.light_color = ColorUtility.ToHtmlStringRGBA(light.color); //new Quaternion(light.color.r, light.color.g, light.color.b, light.color.a);
		utuPluginActorLight.light_intensity = light.intensity;
		utuPluginActorLight.light_is_casting_shadows = light.shadows != LightShadows.None;
		utuPluginActorLight.light_range = light.range;
		utuPluginActorLight.light_spot_angle = light.spotAngle;
		return utuPluginActorLight;
	}


	// Static Mesh -----------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private UtuPluginActorMesh CreateActorMesh(GameObject go) {
		MeshFilter meshFilter = go.GetComponent<MeshFilter>();
		UtuPluginActorMesh utuPluginActorMesh = new UtuPluginActorMesh();
		string assetRelativeFilename = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
		if (assetRelativeFilename == "") {
			// fake it for instances
			if (meshFilter.sharedMesh.name.Contains(" Instance")) {
				foreach (UtuPluginMesh mesh in json.meshes) {
					if (mesh.asset_relative_filename.Contains(meshFilter.sharedMesh.name.Replace(" Instance", "") + ".fbx")) {
						assetRelativeFilename = mesh.asset_relative_filename;
						break;
					}
				}
			}
			else
            {
				// Fake it for meshes with broken MeshFilters
				string meshName = go.name;
				if (go.name.Contains(" ("))
				{
					meshName = go.name.Substring(0,go.name.LastIndexOf('('));
				}
				foreach (UtuPluginMesh mesh in json.meshes)
				{
					if (mesh.asset_relative_filename.Contains(meshName))
					{
						assetRelativeFilename = mesh.asset_relative_filename;
						UtuLog.Warning("        Using mesh '" + assetRelativeFilename + "' for gameobject '" + go.name + "' because it doesn't have a valid MeshFilter component.");
						break;
					}
				}
			}
			if (assetRelativeFilename == "")
			{
				UtuLog.Warning("        Mesh filter is empty on gameobject  '" + go.name + "'");
			}
		}
		utuPluginActorMesh.actor_mesh_relative_filename = assetRelativeFilename.Contains(UtuConst.DEFAULT_RESOURCES) ? meshFilter.sharedMesh.name : assetRelativeFilename;
		string subMeshName = meshFilter.sharedMesh.ToString().Replace(" (UnityEngine.Mesh)", ""); //meshFilter.sharedMesh.ToString().Split(' ').Last<string>()
		utuPluginActorMesh.actor_mesh_relative_filename_if_separated = utuPluginActorMesh.actor_mesh_relative_filename.Replace(".fbx", "_" + subMeshName.Replace(" Instance", "") + ".fbx");
		if (go.GetComponent<Renderer>()) {
			List<Material> materials = new List<Material>();
			go.GetComponent<Renderer>().GetSharedMaterials(materials);
			//Material[] materials = go.GetComponent<Renderer>().sharedMaterials;
			foreach (Material material in materials) {
				string materialRelativeFilename = AssetDatabase.GetAssetPath(material);
				utuPluginActorMesh.actor_mesh_materials_relative_filenames.Add(AssetDatabase.GetAssetPath(material));
				AddMaterialToGlobalMaterialList(materialRelativeFilename, material);
			}
		}
        // Because Unreal does not give you the possibility of stretching the vertices and deform the mesh's default shape.
        Vector3 rot = go.transform.rotation.eulerAngles;
        if (!UtuConst.NearlyEquals(rot.x, 0.0f, 1.0f) || !UtuConst.NearlyEquals(rot.y, 0.0f, 1.0f) || !UtuConst.NearlyEquals(rot.y, 0.0f, 1.0f)) {
            // Rotation Detected
            Transform transform = go.transform;
            while (transform.parent != null) {
                transform = transform.parent;
                if (!UtuConst.NearlyEquals(transform.lossyScale.x, transform.lossyScale.y, 0.1f) || !UtuConst.NearlyEquals(transform.lossyScale.x, transform.lossyScale.z, 0.1f)) {
                    // Non-Uniform Scaling Detected
                    UtuLog.Warning("        Non-Uniform Scaling Detected on a Parent of a Rotated Static Mesh! The Static Mesh may not be imported correctly in Unreal. \n            Scaled Object: " + transform.name + " \n            Rotated Child Static Mesh: " + go.name);
                }
            }
        }
		//bool shouldSeparate = !assetRelativeFilename.Contains(go.name + ".");
		//UtuLog.Error(" -- " + assetRelativeFilename + "  " + go.name + ".");
        AddMeshToGlobalMeshList(utuPluginActorMesh.actor_mesh_relative_filename, meshFilter);
		return utuPluginActorMesh;
	}

	private UtuPluginActorMesh CreateActorSkeletalMesh(GameObject go) {
		SkinnedMeshRenderer meshRenderer = go.GetComponent<SkinnedMeshRenderer>();
		UtuPluginActorMesh utuPluginActorMesh = new UtuPluginActorMesh();
		string assetRelativeFilename = AssetDatabase.GetAssetPath(meshRenderer.sharedMesh);
		utuPluginActorMesh.actor_mesh_relative_filename = assetRelativeFilename.Contains(UtuConst.DEFAULT_RESOURCES) ? meshRenderer.sharedMesh.name : assetRelativeFilename;
		List<Material> materials = new List<Material>();
		meshRenderer.GetSharedMaterials(materials);
		foreach (Material material in materials) {
			string materialRelativeFilename = AssetDatabase.GetAssetPath(material);
			utuPluginActorMesh.actor_mesh_materials_relative_filenames.Add(AssetDatabase.GetAssetPath(material));
			AddMaterialToGlobalMaterialList(materialRelativeFilename, material);
		}
		AddMeshToGlobalSkeletalMeshList(utuPluginActorMesh.actor_mesh_relative_filename, meshRenderer);
		return utuPluginActorMesh;
	}

	private void AddMeshToGlobalMeshList(string meshRelativeFilename, MeshFilter meshFilter) {
        if (meshRelativeFilename != "") {

            if (!json.existing_meshes.Contains(meshRelativeFilename)) {
			    json.existing_meshes.Add(meshRelativeFilename);
			    UtuPluginMesh utuPluginMesh = new UtuPluginMesh();
			    utuPluginMesh.asset_relative_filename = meshRelativeFilename;
				ErrorIfBadCharactersInPath(utuPluginMesh.asset_relative_filename);
				utuPluginMesh.asset_name = meshRelativeFilename.Split(UtuPluginPaths.slash).Last<string>();
			    utuPluginMesh.mesh_file_absolute_filename = meshRelativeFilename.Contains(UtuConst.DEFAULT_RESOURCES) ? meshFilter.sharedMesh.name : UtuPluginPaths.unityFolder_Full_Project + meshRelativeFilename;
                utuPluginMesh.is_skeletal_mesh = false;
			    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(meshRelativeFilename);
				ModelImporter modelImporter = (AssetImporter.GetAtPath(meshRelativeFilename) as ModelImporter);
				utuPluginMesh.mesh_import_scale_factor = modelImporter ? modelImporter.useFileScale == false ? modelImporter.globalScale * 100.0f : modelImporter.globalScale : 1.0f;
				utuPluginMesh.use_file_scale = modelImporter ? modelImporter.useFileScale : false;
				//ModelImporter modelImporter = AssetImporter.GetAtPath(meshRelativeFilename) as ModelImporter;
				//utuPluginMesh.mesh_import_scale_factor = modelImporter ? modelImporter.fileScale : 1.0f;
				//utuPluginMesh.mesh_import_convert_units = modelImporter ? modelImporter.fileScale != modelImporter.globalScale : true;
				if (go != null)
				{
					utuPluginMesh.mesh_import_position_offset = go.transform.position;
				    utuPluginMesh.mesh_import_rotation_offset = go.transform.rotation;
                    utuPluginMesh.mesh_import_scale_offset = go.transform.lossyScale;

					if (!UtuConst.NearlyEquals(go.transform.lossyScale.x, go.transform.lossyScale.y, 0.01f) || !UtuConst.NearlyEquals(go.transform.lossyScale.x, go.transform.lossyScale.z, 0.01f)) {
                            UtuLog.Warning("        Non-Uniform Scaling Detected on this Static Mesh! The Static Mesh may not be imported correctly in Unreal. \n            Static Mesh: " + utuPluginMesh.asset_relative_filename);
                    }
                    if (go.transform.childCount > 0) {
						Transform[] children_transforms = go.transform.GetComponentsInChildren<Transform>();
						foreach (Transform transform in children_transforms) {
							UtuPluginSubmesh subMesh = new UtuPluginSubmesh();
							subMesh.submesh_name = transform.name;
							subMesh.submesh_relative_location = transform.localPosition;
							subMesh.submesh_relative_rotation = transform.localRotation;
							subMesh.submesh_relative_scale = transform.localScale;
							utuPluginMesh.submeshes.Add(subMesh);
							submeshPivotOffsets.Add(utuPluginMesh.asset_relative_filename + "_" + subMesh.submesh_name, subMesh.submesh_relative_location);
						}
                        UtuLog.Log("        More than one mesh detected in fbx file. This is a new feature. The Static Mesh may not be imported correctly in Unreal. \n            Static Mesh: " + utuPluginMesh.asset_relative_filename);
                    }
                    if (go.GetComponent<Renderer>() != null) {
					    Material[] materials = go.GetComponent<Renderer>().sharedMaterials;
					    foreach (Material material in materials) {
						    string materialRelativeFilename = AssetDatabase.GetAssetPath(material);
						    utuPluginMesh.mesh_materials_relative_filenames.Add(AssetDatabase.GetAssetPath(material));
						    AddMaterialToGlobalMaterialList(materialRelativeFilename, material);
					    }
				    }
			    }
			    json.meshes.Add(utuPluginMesh);
			    json.json_info.meshes.Add(utuPluginMesh.asset_name);
		    }
        }
    }

	private void AddMeshToGlobalSkeletalMeshList(string meshRelativeFilename, SkinnedMeshRenderer meshRenderer) {
		if (!json.existing_meshes.Contains(meshRelativeFilename) && meshRelativeFilename != "") {
			json.existing_meshes.Add(meshRelativeFilename);
			UtuPluginMesh utuPluginMesh = new UtuPluginMesh();
			utuPluginMesh.asset_relative_filename = meshRelativeFilename;
			ErrorIfBadCharactersInPath(utuPluginMesh.asset_relative_filename);
			utuPluginMesh.asset_name = meshRelativeFilename.Split(UtuPluginPaths.slash).Last<string>();
			utuPluginMesh.mesh_file_absolute_filename = meshRelativeFilename.Contains(UtuConst.DEFAULT_RESOURCES) ? meshRenderer.sharedMesh.name : UtuPluginPaths.unityFolder_Full_Project + meshRelativeFilename;
            utuPluginMesh.is_skeletal_mesh = true;
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(meshRelativeFilename);
			utuPluginMesh.mesh_import_scale_factor = (AssetImporter.GetAtPath(meshRelativeFilename) as ModelImporter) ? (AssetImporter.GetAtPath(meshRelativeFilename) as ModelImporter).globalScale : 1.0f;
			if (go != null) {
				utuPluginMesh.mesh_import_position_offset = go.transform.position;
				utuPluginMesh.mesh_import_rotation_offset = go.transform.rotation;
                utuPluginMesh.mesh_import_scale_offset = go.transform.lossyScale;
                if (go.GetComponent<Renderer>() != null) {
                    Material[] materials = go.GetComponent<Renderer>().sharedMaterials;
					foreach (Material material in materials) {
						string materialRelativeFilename = AssetDatabase.GetAssetPath(material);
						utuPluginMesh.mesh_materials_relative_filenames.Add(AssetDatabase.GetAssetPath(material));
						AddMaterialToGlobalMaterialList(materialRelativeFilename, material);
					}
				}
			}
            // Animations
            Animator animator = meshRenderer.gameObject.GetComponent<Animator>();
            if (animator == null) {
                animator = meshRenderer.gameObject.GetComponentInParent<Animator>();
                if (animator == null) {
                    animator = meshRenderer.gameObject.GetComponentInChildren<Animator>();
                }
            }
            if (animator != null) { 
                foreach (AnimationClip animationClip in AnimationUtility.GetAnimationClips(animator.gameObject)) {
                    string path = AssetDatabase.GetAssetPath(animationClip);
                    if (path != "" && !utuPluginMesh.skeletal_mesh_animations_relative_filenames.Contains(path)) {
                        utuPluginMesh.skeletal_mesh_animations_relative_filenames.Add(path);
                        UtuLog.Warning("        Animation Detected. They are not supported at the momment. You will need to import them manually in Unreal.  \n            Animation: '" + path + "'");
                    }
                }
            }
			json.meshes.Add(utuPluginMesh);
			json.json_info.meshes.Add(utuPluginMesh.asset_name);
		}
	}

	private string GetTexturePathAndAddItToGlobal(Material material, string textureName) {
		Texture texture = material.GetTexture(textureName);
		string textureRelativeFilename = AssetDatabase.GetAssetPath(texture);
		AddTextureToGlobalMaterialList(textureRelativeFilename, texture);
		return textureRelativeFilename;
	}

    private Quaternion ColorToQuat(Color color) {
        return new Quaternion(color.r, color.g, color.b, color.a);
    }

	private void AddMaterialToGlobalMaterialList(string materialRelativeFilename, Material material) {
		if (!json.existing_materials.Contains(materialRelativeFilename) && materialRelativeFilename != "") {
			json.existing_materials.Add(materialRelativeFilename);
			UtuPluginMaterial utuPluginMaterial = new UtuPluginMaterial();
			utuPluginMaterial.asset_relative_filename = materialRelativeFilename;
			ErrorIfBadCharactersInPath(utuPluginMaterial.asset_relative_filename);
			utuPluginMaterial.asset_name = materialRelativeFilename.Split(UtuPluginPaths.slash).Last<string>();
			if (material.shader.name == "Standard" || material.shader.name == "Standard (Specular setup)") {
				utuPluginMaterial.albedo_relative_filename = GetTexturePathAndAddItToGlobal(material, "_MainTex");
                utuPluginMaterial.albedo_tiling_and_offset = ColorToQuat(material.GetColor("_MainTex_ST"));
				utuPluginMaterial.albedo_multiply_color = ColorUtility.ToHtmlStringRGBA(material.GetColor("_Color"));
				utuPluginMaterial.smoothness = material.GetFloat("_Glossiness");
				utuPluginMaterial.normal_relative_filename = GetTexturePathAndAddItToGlobal(material, "_BumpMap");
                utuPluginMaterial.normal_tiling_and_offset = ColorToQuat(material.GetColor("_BumpMap_ST"));
				utuPluginMaterial.normal_intensity = material.GetFloat("_BumpScale");
                utuPluginMaterial.height_relative_filename = GetTexturePathAndAddItToGlobal(material, "_ParallaxMap");
                utuPluginMaterial.height_tiling_and_offset = ColorToQuat(material.GetColor("_ParallaxMap_ST"));
				utuPluginMaterial.occlusion_relative_filename = GetTexturePathAndAddItToGlobal(material, "_OcclusionMap");
                utuPluginMaterial.occlusion_tiling_and_offset = ColorToQuat(material.GetColor("_OcclusionMap_ST"));
                utuPluginMaterial.occlusion_intensity = material.GetFloat("_OcclusionStrength");
                utuPluginMaterial.is_emissive = material.IsKeywordEnabled("_EMISSION");
				utuPluginMaterial.emission_relative_filename = GetTexturePathAndAddItToGlobal(material, "_EmissionMap");
                utuPluginMaterial.emission_tiling_and_offset = ColorToQuat(material.GetColor("_EmissionMap_ST"));
				utuPluginMaterial.emission_color = ColorUtility.ToHtmlStringRGBA(material.GetColor("_EmissionColor"));
                utuPluginMaterial.detail_mask_relative_filename = GetTexturePathAndAddItToGlobal(material, "_DetailMask");
                utuPluginMaterial.detail_mask_tiling_and_offset = ColorToQuat(material.GetColor("_DetailMask_ST"));
				utuPluginMaterial.detail_albedo_relative_filename = GetTexturePathAndAddItToGlobal(material, "_DetailAlbedoMap");
                utuPluginMaterial.detail_albedo_tiling_and_offset = ColorToQuat(material.GetColor("_DetailAlbedoMap_ST"));
                utuPluginMaterial.detail_normal_relative_filename = GetTexturePathAndAddItToGlobal(material, "_DetailNormalMap");
                utuPluginMaterial.detail_normal_tiling_and_offset = ColorToQuat(material.GetColor("_DetailNormalMap_ST"));
                utuPluginMaterial.detail_normal_intensity = material.GetFloat("_DetailNormalMapScale");
				int render_mode = Mathf.RoundToInt(material.GetFloat("_Mode"));
				switch (render_mode) {
					case 0:
						utuPluginMaterial.shader_opacity = UtuShaderOpacity.Opaque;
						break;
					case 1:
						utuPluginMaterial.shader_opacity = UtuShaderOpacity.Masked;
						break;
					case 3:
						utuPluginMaterial.shader_opacity = UtuShaderOpacity.Translucent;
						break;
					default:
						UtuLog.Warning("Unsupported material shader mode detected: '" + render_mode.ToString() + "' for material: '" + materialRelativeFilename + "'");
						UtuLog.Warning("    Will mark this material as Opaque.");
						utuPluginMaterial.shader_opacity = UtuShaderOpacity.Opaque;
						break;
				}
				if (material.shader.name == "Standard") {
					utuPluginMaterial.shader_type = UtuShaderType.Standard;
					utuPluginMaterial.metallic_relative_filename = GetTexturePathAndAddItToGlobal(material, "_MetallicGlossMap");
                    utuPluginMaterial.metallic_tiling_and_offset = ColorToQuat(material.GetColor("_MetallicGlossMap_ST"));
					utuPluginMaterial.metallic_intensity = material.GetFloat("_Metallic");
                }
				else if (material.shader.name == "Standard (Specular setup)") {
					utuPluginMaterial.shader_type = UtuShaderType.StandardSpecular;
					utuPluginMaterial.specular_relative_filename = GetTexturePathAndAddItToGlobal(material, "_SpecGlossMap");
                    utuPluginMaterial.specular_tiling_and_offset = ColorToQuat(material.GetColor("_SpecGlossMap_ST"));
					utuPluginMaterial.specular_intensity = material.GetFloat("_SpecularHighlights");
                    utuPluginMaterial.specular_color = ColorUtility.ToHtmlStringRGBA(material.GetColor("_SpecColor"));
				}
			}
			else if (material.shader.name == "Unlit/Color") {
				utuPluginMaterial.shader_type = UtuShaderType.UnlitColor;
				utuPluginMaterial.albedo_multiply_color = ColorUtility.ToHtmlStringRGBA(material.GetColor("_Color"));
			}
			else if (material.shader.name == "Unlit/Texture" || material.shader.name == "Mobile/Unlit (Supports Lightmap)") {
				utuPluginMaterial.shader_type = UtuShaderType.UnlitTexture;
				utuPluginMaterial.albedo_relative_filename = GetTexturePathAndAddItToGlobal(material, "_MainTex");
                utuPluginMaterial.albedo_tiling_and_offset = ColorToQuat(material.GetColor("_MainTex_ST"));
            }
            else if (material.shader.name == "Unlit/Transparent") {
				utuPluginMaterial.shader_type = UtuShaderType.UnlitTransparent;
				utuPluginMaterial.albedo_relative_filename = GetTexturePathAndAddItToGlobal(material, "_MainTex");
                utuPluginMaterial.albedo_tiling_and_offset = ColorToQuat(material.GetColor("_MainTex_ST"));
            }
            else if (material.shader.name == "Unlit/Transparent Cutout") {
				utuPluginMaterial.shader_type = UtuShaderType.UnlitCutout;
				utuPluginMaterial.albedo_relative_filename = GetTexturePathAndAddItToGlobal(material, "_MainTex");
                utuPluginMaterial.albedo_tiling_and_offset = ColorToQuat(material.GetColor("_MainTex_ST"));
            }
            else if (material.shader.name == "Mobile/Diffuse") {
                utuPluginMaterial.shader_type = UtuShaderType.MobileDiffuse;
                utuPluginMaterial.albedo_relative_filename = GetTexturePathAndAddItToGlobal(material, "_MainTex");
                utuPluginMaterial.albedo_tiling_and_offset = ColorToQuat(material.GetColor("_MainTex_ST"));
            }
            else if (material.shader.name == "Legacy Shaders/Diffuse") {
                utuPluginMaterial.shader_type = UtuShaderType.LegacyDiffuse;
                utuPluginMaterial.albedo_relative_filename = GetTexturePathAndAddItToGlobal(material, "_MainTex");
                utuPluginMaterial.albedo_tiling_and_offset = ColorToQuat(material.GetColor("_MainTex_ST"));
                utuPluginMaterial.albedo_multiply_color = ColorUtility.ToHtmlStringRGBA(material.GetColor("_Color"));
            }
            else {
				utuPluginMaterial.shader_type = UtuShaderType.Unsupported;
				UtuLog.Warning("        Unsupported material shader detected: '" + material.shader.name + "' for material: '" + materialRelativeFilename + "'" +
                    "\n            Supported shaders are: Standard, Standard (Specular setup), Unlit/Color, Unlit/Texture, Unlit/Transparent, Unlit/Transparent Cutout, Mobile/Diffuse, Mobile/Unlit (Supports Lightmap) & Legacy Shaders/Diffuse" + 
                    "\n            Will still export the textures and add them into the material in Unreal, but you will need to connect them manually.");
				string[] textureNames = material.GetTexturePropertyNames();
				foreach (string textureName in textureNames) {
					utuPluginMaterial.material_textures_relative_filenames_for_unsupported_shaders.Add(GetTexturePathAndAddItToGlobal(material, textureName));
                }
            }
			json.materials.Add(utuPluginMaterial);
			json.json_info.materials.Add(utuPluginMaterial.asset_name);
		}
	}

	private void AddTextureToGlobalMaterialList(string textureRelativeFilename, Texture texture) {
		if (!json.existing_textures.Contains(textureRelativeFilename) && textureRelativeFilename != "") {
			json.existing_textures.Add(textureRelativeFilename);
			UtuPluginTexture utuPluginTexture = new UtuPluginTexture();
			utuPluginTexture.asset_relative_filename = textureRelativeFilename;
			ErrorIfBadCharactersInPath(utuPluginTexture.asset_relative_filename);
			utuPluginTexture.asset_name = textureRelativeFilename.Split(UtuPluginPaths.slash).Last<string>();
			utuPluginTexture.texture_file_absolute_filename = UtuPluginPaths.unityFolder_Full_Project + textureRelativeFilename;
			json.textures.Add(utuPluginTexture);
			json.json_info.textures.Add(utuPluginTexture.asset_name);
            if (textureRelativeFilename.ToLower().EndsWith(".tif")) {
                UtuLog.Error("        .tif Texture Detected: '" + textureRelativeFilename + "'" +
                    "\n            Unreal does not support that texture format. This texture will not be imported.");
            }
		}
	}

	// Prefab -----------------------------------------------------------------------------------------------------------------------------------------------------------------------
	private UtuPluginActorPrefab CreateActorPrefab(GameObject go) {
		GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
		UtuPluginActorPrefab utuPluginActorPrefab = new UtuPluginActorPrefab();
		string asset_relative_filename = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(go));
		utuPluginActorPrefab.actor_prefab_relative_filename = asset_relative_filename;
		AddPrefabToGlobalPrefabList(utuPluginActorPrefab.actor_prefab_relative_filename, go);
        // Because Unreal does not give you the possibility of stretching the vertices and deform the mesh's default shape.
        Transform transform = go.transform;
        if (!UtuConst.NearlyEquals(transform.lossyScale.x, transform.lossyScale.y, 0.1f) || !UtuConst.NearlyEquals(transform.lossyScale.x, transform.lossyScale.z, 0.1f) || !UtuConst.NearlyEquals(transform.lossyScale.y, transform.lossyScale.z, 0.1f)) {
            // Non-Uniform Scaling Detected
            foreach (Transform t in transform) {
                if (t.gameObject.GetComponent<MeshFilter>()) {
                    Vector3 rot = t.rotation.eulerAngles;
                    if (!UtuConst.NearlyEquals(rot.x, 0.0f, 1.0f) || !UtuConst.NearlyEquals(rot.y, 0.0f, 1.0f) || !UtuConst.NearlyEquals(rot.y, 0.0f, 1.0f)) {
                        // Rotation Detected
                        UtuLog.Warning("        Non-Uniform Scaling Detected on a Parent of at least one Rotated Static Mesh! The Static Mesh may not be imported correctly in Unreal. \n            Scaled Prefab In Scene: " + transform.name + " \n            First Rotated Child Static Mesh Found: " + t.name);
                        break;
                    }
                }
            }
        }
        return utuPluginActorPrefab;
	}

	private void AddPrefabToGlobalPrefabList(string prefabRelativeFilename, GameObject prefabRoot) {
		if (!json.existing_prefabs.Contains(prefabRelativeFilename)) {
			json.existing_prefabs.Add(prefabRelativeFilename);
			// First Pass
			UtuPluginPrefabFirstPass utuPluginPrefabFirstPass = new UtuPluginPrefabFirstPass();
			utuPluginPrefabFirstPass.asset_relative_filename = prefabRelativeFilename;
			utuPluginPrefabFirstPass.asset_name = prefabRelativeFilename.Split(UtuPluginPaths.slash).Last<string>();
			json.prefabs_first_pass.Add(utuPluginPrefabFirstPass);
			json.json_info.prefabs.Add(utuPluginPrefabFirstPass.asset_name);
			utuPluginPrefabFirstPass.has_any_static_child = false;
			// Second Pass
			UtuPluginPrefabSecondPass utuPluginPrefabSecondPass = new UtuPluginPrefabSecondPass();
			utuPluginPrefabSecondPass.asset_relative_filename = prefabRelativeFilename;
			ErrorIfBadCharactersInPath(utuPluginPrefabSecondPass.asset_relative_filename);
			utuPluginPrefabSecondPass.asset_name = prefabRelativeFilename.Split(UtuPluginPaths.slash).Last<string>();
			GameObject contentsRoot = PrefabUtility.LoadPrefabContents(prefabRelativeFilename);
			// Little hack cause unity prefab system is dumb. Bref, have to reset to root transform sinc it's just a defaut value and it does not affect anything
			Vector3 rootPos = contentsRoot.transform.position;
			Quaternion rootRot = contentsRoot.transform.rotation;
			Vector3 rootScale = contentsRoot.transform.localScale;
			contentsRoot.transform.position = Vector3.zero;
			contentsRoot.transform.rotation = Quaternion.identity;
			contentsRoot.transform.localScale = Vector3.one;
			Transform[] children_transforms = contentsRoot.GetComponentsInChildren<Transform>();
			foreach (Transform transform in children_transforms) {
				UtuPluginActor component = ProcessActor(transform.gameObject, false);
				utuPluginPrefabFirstPass.has_any_static_child = utuPluginPrefabFirstPass.has_any_static_child || !component.actor_is_movable; 
				if (component != null)
				{ // Else it's probably just some components inside another nested prefab
					utuPluginPrefabSecondPass.prefab_components.Add(component);
				}
			}
			contentsRoot.transform.position = rootPos;
			contentsRoot.transform.rotation = rootRot;
			contentsRoot.transform.localScale = rootScale;
			PrefabUtility.UnloadPrefabContents(contentsRoot);
			json.prefabs_second_pass.Add(utuPluginPrefabSecondPass);
		}
	}

	private void ErrorIfBadCharactersInPath(string filename)
	{
		string error_message = "";
		foreach (char c in filename.ToCharArray())
		{
			if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.' || c == '/'))
            {
				if (error_message == "")
                {
					error_message = "Filename contains invalid characters: '" + filename + "' - Invalid characters:";
				}
				error_message += " " + c;
			}
		}
		if (error_message != "")
        {
			UtuLog.Error(error_message);
		}
	}
}