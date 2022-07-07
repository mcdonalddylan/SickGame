using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class OptionsMenuScript : MonoBehaviour
{
    // UI components
    public Slider resolutionScaleSlider;
    public Dropdown textureDropdown;
    public Toggle shadowToggle;
    public Toggle particlesToggle;
    public Toggle dofToggle;
    public Toggle ssaoToggle;
    public Toggle bloomToggle;
    public Toggle fxaaToggle;

    // For adjusting post-processing
    public UniversalRenderPipelineAsset urpSettings;
    public ScriptableRendererFeature ssaoFeature;
    private Volume postprocessingVolume;
    private Camera mainCamera;

    // For enabling and disabling particle effects
    private MainMenuScript mainUIScript;

    private void Awake()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        try
        {
            postprocessingVolume = GameObject.FindGameObjectWithTag("GlobalVolume").GetComponent<Volume>();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("No global volume found in this scene");
        }
        try
        {
            mainUIScript = GameObject.FindGameObjectWithTag("mainUIScript").GetComponent<MainMenuScript>();
        } catch (NullReferenceException e)
        {
            Debug.Log("No main menu script found in this scene");
        }
    }

    private void Start()
    {
        resolutionScaleSlider.onValueChanged.AddListener(delegate { ResolutionScaleChanged(); });
        textureDropdown.onValueChanged.AddListener(delegate { TextureQualityChanged(); });
        shadowToggle.onValueChanged.AddListener(delegate { ShadowQualityChanged(); });
        particlesToggle.onValueChanged.AddListener(delegate { ToggleParticles(); });
        dofToggle.onValueChanged.AddListener(delegate { ToggleDof(); });
        ssaoToggle.onValueChanged.AddListener(delegate { ToggleSSAO(); });
        bloomToggle.onValueChanged.AddListener(delegate { ToggleBloom(); });
        fxaaToggle.onValueChanged.AddListener(delegate { ToggleFXAA(); });
    }

    private void ResolutionScaleChanged()
    {
        if (resolutionScaleSlider.value == 7)
        {
            urpSettings.renderScale = 0.5f;
        }
        else if (resolutionScaleSlider.value == 6)
        {
            urpSettings.renderScale = 0.25f;
        }
        else if (resolutionScaleSlider.value == 5)
        {
            urpSettings.renderScale = 0.1f;
        }
        else
        {
            urpSettings.renderScale = (resolutionScaleSlider.value) * 0.1f;
        }
    }

    private void TextureQualityChanged()
    {
        if (textureDropdown.value == 0)
        {
            QualitySettings.masterTextureLimit = 0;
        }
        else if (textureDropdown.value == 1)
        {
            QualitySettings.masterTextureLimit = 2;
        }
    }

    private void ShadowQualityChanged()
    {
        if (shadowToggle.isOn)
        {
            urpSettings.shadowCascadeCount = 4;
            urpSettings.shadowDistance = 300;
        }
        else
        {
            urpSettings.shadowCascadeCount = 1;
            urpSettings.shadowDistance = 50;
        }
    }

    private void ToggleParticles()
    {
        if (particlesToggle.isOn)
        {
            GameManager.particleEffectsEnabled = true;
            if(mainUIScript != null)
            {
                mainUIScript.ReAddBGParticles();
            }
        }
        else
        {
            GameManager.particleEffectsEnabled = false;
            if (mainUIScript != null)
            {
                mainUIScript.RemoveBGParticles();
            }
        }
        print(GameManager.particleEffectsEnabled);
    }

    private void ToggleDof()
    {
        DepthOfField dof;
        postprocessingVolume.profile.TryGet<DepthOfField>(out dof);
        dof.active = dofToggle.isOn;
    }

    private void ToggleSSAO()
    {
        ssaoFeature.SetActive(ssaoToggle.isOn);
    }

    private void ToggleBloom()
    {
        Bloom bloom;
        postprocessingVolume.profile.TryGet<Bloom>(out bloom);
        bloom.active = bloomToggle.isOn;
    }

    private void ToggleFXAA()
    {
        if (fxaaToggle.isOn)
        {
            mainCamera.GetComponent<UniversalAdditionalCameraData>().antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }
        else
        {
            mainCamera.GetComponent<UniversalAdditionalCameraData>().antialiasing = AntialiasingMode.None;
        }
    }
}
