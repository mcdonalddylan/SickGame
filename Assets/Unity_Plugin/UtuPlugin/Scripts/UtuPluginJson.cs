// Copyright Alex Quevillon. All Rights Reserved.

using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum UtuActorType { Empty, StaticMesh, SkeletalMesh, PointLight, DirectionalLight, SpotLight, Camera, Prefab };

[System.Serializable]
public enum UtuShaderType { Standard, StandardSpecular, UnlitColor, UnlitTexture, UnlitTransparent, UnlitCutout, MobileDiffuse, LegacyDiffuse, Unsupported };

[System.Serializable]
public enum UtuShaderOpacity { Opaque, Masked, Translucent };

[System.Serializable]
public class UtuPluginSubmesh {
	public string submesh_name;
	public Vector3 submesh_relative_location;
	public Quaternion submesh_relative_rotation;
	public Vector3 submesh_relative_scale;
}

			[System.Serializable]
			public class UtuPluginActorCamera {
				public Quaternion camera_viewport_rect; // XYWH
				public float camera_near_clip_plane;
				public float camera_far_clip_plane;
				public float camera_aspect_ratio;
				public bool camera_is_perspective;
				public float camera_ortho_size;
				public float camera_persp_field_of_view;
				public bool camera_is_physical;
				public float camera_phys_focal_length;
				public Vector2 camera_phys_sensor_size;
			}

			[System.Serializable]
			public class UtuPluginActorLight {
				public string light_color; // Hex
				public float light_intensity;
				public float light_range;
				public float light_spot_angle;
				public bool light_is_casting_shadows;
				//public float light_bounce_intensity;
				//public float light_shadow_strength;
			}

			[System.Serializable]
			public class UtuPluginActorMesh {
				public string actor_mesh_relative_filename; // Warning: if == "", means that the mesh is invalid / empty
				public string actor_mesh_relative_filename_if_separated; // Warning: if == "", means that the mesh is invalid / empty
				public List<string> actor_mesh_materials_relative_filenames = new List<string>();
			}

			[System.Serializable]
			public class UtuPluginActorPrefab {
				public string actor_prefab_relative_filename; // Warning: if == "", means that the mesh is invalid / empty
			}

		[System.Serializable]
		public class UtuPluginActor {
			public int actor_id;
			public int actor_parent_id;
			public string actor_display_name;
			public string actor_tag;
			public bool actor_is_visible;
			public Vector3 actor_world_location;
			public Vector3 actor_world_location_if_separated;
			public Quaternion actor_world_rotation;
			public Vector3 actor_world_scale;
			public Vector3 actor_relative_location;
			public Vector3 actor_relative_location_if_separated;
			public Quaternion actor_relative_rotation;
			public Vector3 actor_relative_scale;
			public bool actor_is_movable;
			public List<UtuActorType> actor_types = new List<UtuActorType>();
			public UtuPluginActorMesh actor_mesh;
			public UtuPluginActorPrefab actor_prefab;
			public UtuPluginActorLight actor_light;
			public UtuPluginActorCamera actor_camera;
		}

	[System.Serializable]
	public class UtuPluginAsset {
		public string asset_name;
		public string asset_relative_filename;
	}

	[System.Serializable]
	public class UtuPluginScene : UtuPluginAsset {
		public List<UtuPluginActor> scene_actors = new List<UtuPluginActor>();
	}

	[System.Serializable]
	public class UtuPluginMesh : UtuPluginAsset {
		public string mesh_file_absolute_filename;
		public Vector3 mesh_import_position_offset;
		public Quaternion mesh_import_rotation_offset;
		public Vector3 mesh_import_scale_offset;
		public float mesh_import_scale_factor;
        public List<string> mesh_materials_relative_filenames = new List<string>();
        public bool is_skeletal_mesh;
        public bool use_file_scale;
        public List<string> skeletal_mesh_animations_relative_filenames = new List<string>();
        public List<UtuPluginSubmesh> submeshes = new List<UtuPluginSubmesh>();
	}

	[System.Serializable]
	public class UtuPluginMaterial : UtuPluginAsset {
		public UtuShaderType shader_type;
		// Standard
		public string albedo_relative_filename;
		public Quaternion albedo_tiling_and_offset;
		public string albedo_multiply_color; // Hex
		public float smoothness;
		public string normal_relative_filename;
		public Quaternion normal_tiling_and_offset;
		public float normal_intensity;
		public string height_relative_filename;
		public Quaternion height_tiling_and_offset;
		public string occlusion_relative_filename;
		public Quaternion occlusion_tiling_and_offset;
		public float occlusion_intensity;
		public bool is_emissive;
		public string emission_relative_filename;
		public Quaternion emission_tiling_and_offset;
		public string emission_color; // Hex
	    public string detail_mask_relative_filename;
		public Quaternion detail_mask_tiling_and_offset;
		public string detail_albedo_relative_filename;
		public Quaternion detail_albedo_tiling_and_offset;
		public string detail_normal_relative_filename;
		public Quaternion detail_normal_tiling_and_offset;
		public float detail_normal_intensity;
		public UtuShaderOpacity shader_opacity;
		// Metallic
		public string metallic_relative_filename;
		public Quaternion metallic_tiling_and_offset;
		public float metallic_intensity;
		// Specular
		public string specular_relative_filename;
		public Quaternion specular_tiling_and_offset;
		public float specular_intensity;
		public string specular_color; // Hex, even though it's not supported by Unreal
		// Unsupported Shaders
		public List<string> material_textures_relative_filenames_for_unsupported_shaders = new List<string>();
	}

	[System.Serializable]
	public class UtuPluginTexture : UtuPluginAsset {
		public string texture_file_absolute_filename;
	}

	[System.Serializable]
	public class UtuPluginPrefabSecondPass : UtuPluginAsset {
		public List<UtuPluginActor> prefab_components = new List<UtuPluginActor>();
	}
	[System.Serializable]
	public class UtuPluginPrefabFirstPass : UtuPluginAsset {
		public bool has_any_static_child;
	}

[System.Serializable]
public class UtuPluginJsonInfo {
	public string export_name;
	public string export_datetime;
	public string export_timestamp;
	public string json_file_fullname;
	public List<string> scenes = new List<string>();
	public List<string> meshes = new List<string>();
	public List<string> materials = new List<string>();
	public List<string> textures = new List<string>();
	public List<string> prefabs = new List<string>();
}

[System.Serializable]
public class UtuPluginJson {
	public UtuPluginJsonInfo json_info;
	public List<UtuPluginScene> scenes = new List<UtuPluginScene>();
	public List<UtuPluginMesh> meshes = new List<UtuPluginMesh>();
	public List<UtuPluginMaterial> materials = new List<UtuPluginMaterial>();
	public List<UtuPluginTexture> textures = new List<UtuPluginTexture>();
	public List<UtuPluginPrefabFirstPass> prefabs_first_pass = new List<UtuPluginPrefabFirstPass>();
	public List<UtuPluginPrefabSecondPass> prefabs_second_pass = new List<UtuPluginPrefabSecondPass>();
	[System.NonSerialized] public List<string> existing_meshes = new List<string>();
	[System.NonSerialized] public List<string> existing_materials = new List<string>();
	[System.NonSerialized] public List<string> existing_textures = new List<string>();
	[System.NonSerialized] public List<string> existing_prefabs = new List<string>();
}



public class UtuPluginJsonUtilities {

	public static void DumpExportJsonToFile(UtuPluginJson json, string timestamp) {
		UtuLog.Log("        Exporting Generated Data to file for future Import in Unreal...");
		string path = UtuPluginPaths.pluginFolder_Full_Exports + UtuPluginPaths.slash + timestamp + UtuPluginPaths.slash + "UtuPlugin.json";
		json.json_info.json_file_fullname = path;
		File.WriteAllText(path, JsonUtility.ToJson(json, true));
		string infoPath = UtuPluginPaths.pluginFolder_Full_Exports + UtuPluginPaths.slash + timestamp + UtuPluginPaths.slash + "UtuPluginInfo.json";
		File.WriteAllText(infoPath, JsonUtility.ToJson(json.json_info, true));
		UtuLog.Log("        Generated Data Exported!");
		UtuLog.Log("            Data: \"" + path + "\"");
		UtuLog.Log("            Info: \"" + infoPath + "\"");
		string logPath = UtuPluginPaths.pluginFolder_Full_Exports + UtuPluginPaths.slash + timestamp + UtuPluginPaths.slash + "UnityExport.log";
		UtuLog.Log("            Log:  \"" + infoPath + "\"");
	}

	public static List<string> GetAvailableExportJsons() {
		List<string> ret = new List<string>();
		if (Directory.Exists(UtuPluginPaths.pluginFolder_Full_Exports)) {
			string[] timestamps = Directory.GetDirectories(UtuPluginPaths.pluginFolder_Full_Exports);
			Array.Sort(timestamps);
			Array.Reverse(timestamps);
			foreach (string x in timestamps) {
				ret.Add(x + UtuPluginPaths.slash + "UtuPlugin.json");
			}
		}
		return ret;
	}
}
