using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;

public class StartMenuScript : MonoBehaviour
{
    // For enabling and disabling UI
    public GameObject startMenuUI;
    public GameObject saveSlotUI;
    public GameObject deleteSubMenuUI;
    public GameObject saveSlot1NoDataPanel;
    public GameObject saveSlot1DataPanel;
    public GameObject saveSlot2NoDataPanel;
    public GameObject saveSlot2DataPanel;
    public GameObject saveSlot3NoDataPanel;
    public GameObject saveSlot3DataPanel;

    // For adjusting post-processing
    public UniversalRenderPipelineAsset urpSettings;
    public ScriptableRendererFeature ssaoFeature;
    public Volume postprocessingVolume;
    private Camera mainCamera;

    void Start()
    {
        startMenuUI.SetActive(true);
        deleteSubMenuUI.SetActive(false);

        // load save slot 1 data and set the spirits on save slot ui
        SaveObject saveSlot1Data = GameManager.LoadSlotData(1);
        if (saveSlot1Data == null)
        {
            saveSlot1DataPanel.SetActive(false);
            saveSlot1NoDataPanel.SetActive(true);
        }
        else
        {
            saveSlot1DataPanel.SetActive(true);
            Text spiritsText = saveSlot1DataPanel.GetComponentsInChildren<Text>()[1];
            spiritsText.text = "x " + saveSlot1Data.spiritCount;
            saveSlot1NoDataPanel.SetActive(false);
        }
        // load save slot 2 data and set the spirits on save slot ui
        SaveObject saveSlot2Data = GameManager.LoadSlotData(2);
        if (saveSlot2Data == null)
        {
            saveSlot2DataPanel.SetActive(false);
            saveSlot2NoDataPanel.SetActive(true);
        }
        else
        {
            saveSlot2DataPanel.SetActive(true);
            Text spiritsText = saveSlot2DataPanel.GetComponentsInChildren<Text>()[1];
            spiritsText.text = "x " + saveSlot2Data.spiritCount;
            saveSlot2NoDataPanel.SetActive(false);
        }
        // load save slot 3 data and set the spirits on save slot ui
        SaveObject saveSlot3Data = GameManager.LoadSlotData(3);
        if (saveSlot3Data == null)
        {
            saveSlot3DataPanel.SetActive(false);
            saveSlot3NoDataPanel.SetActive(true);
        }
        else
        {
            saveSlot3DataPanel.SetActive(true);
            Text spiritsText = saveSlot3DataPanel.GetComponentsInChildren<Text>()[1];
            spiritsText.text = "x " + saveSlot3Data.spiritCount;
            saveSlot3NoDataPanel.SetActive(false);
        }
        saveSlotUI.SetActive(false);

        // Set the graphics settings according to the options.txt file
        OptionsDataObject optionsData = GameManager.LoadOptionsData();
        if (optionsData.resolutionScale == 7)
        {
            urpSettings.renderScale = 0.5f;
        }
        else if (optionsData.resolutionScale == 6)
        {
            urpSettings.renderScale = 0.25f;
        }
        else if (optionsData.resolutionScale == 5)
        {
            urpSettings.renderScale = 0.1f;
        }
        else
        {
            urpSettings.renderScale = (optionsData.resolutionScale) * 0.1f;
        }

        if (optionsData.hasQualityShadows)
        {
            urpSettings.shadowCascadeCount = 4;
            urpSettings.shadowDistance = 300;
        }
        else
        {
            urpSettings.shadowCascadeCount = 1;
            urpSettings.shadowDistance = 50;
        }

        if (optionsData.textureQuality == "Good")
        {
            QualitySettings.masterTextureLimit = 0;
        }
        else if (optionsData.textureQuality == "LOL")
        {
            QualitySettings.masterTextureLimit = 2;
        }

        ssaoFeature.SetActive(optionsData.hasSSAO);
        Bloom bloom;
        postprocessingVolume.profile.TryGet<Bloom>(out bloom);
        bloom.active = optionsData.hasBloom;
        DepthOfField dof;
        postprocessingVolume.profile.TryGet<DepthOfField>(out dof);
        dof.active = optionsData.hasDOF;
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        if (optionsData.hasFXAA)
        {
            mainCamera.GetComponent<UniversalAdditionalCameraData>().antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }
        else
        {
            mainCamera.GetComponent<UniversalAdditionalCameraData>().antialiasing = AntialiasingMode.None;
        }

        Button startButton = startMenuUI.GetComponentsInChildren<Button>()[0];
        startButton.Select();
    }

    public void HandlePressingCancel(InputAction.CallbackContext context)
    {
        if (context.performed && deleteSubMenuUI.activeSelf)
        {
            ReturnToSaveSlotMenu();
        }
        else if (context.performed && saveSlotUI.activeSelf)
        {
            ReturnToStartMenu();
        }
        else if (context.performed)
        {
            Application.Quit();
        }
    }

    public void HandlePressingStart(InputAction.CallbackContext context)
    {
        if (context.performed && !saveSlotUI.activeSelf)
        {
            EnterSaveSlotMenu();
        }
    }

    public void HandlePressingDelete(InputAction.CallbackContext context)
    {
        if (context.performed && !deleteSubMenuUI.activeSelf && GameManager.SaveSlotDataExists(GameManager.currentlySelectedSaveSlot))
        {
            deleteSubMenuUI.SetActive(true);
            Button nahBtn = deleteSubMenuUI.GetComponentsInChildren<Button>()[0];
            nahBtn.Select();
        }
    }

    public void DeleteSaveSlotData()
    {
        int selectedSave = GameManager.currentlySelectedSaveSlot;
        GameManager.DeleteSaveSlotData(selectedSave);

        if (selectedSave == 1)
        {
            saveSlot1DataPanel.SetActive(false);
            saveSlot1NoDataPanel.SetActive(true);
        }
        else if (selectedSave == 2)
        {
            saveSlot2DataPanel.SetActive(false);
            saveSlot2NoDataPanel.SetActive(true);
        }
        else if (selectedSave == 3)
        {
            saveSlot3DataPanel.SetActive(false);
            saveSlot3NoDataPanel.SetActive(true);
        }
        ReturnToSaveSlotMenu();
    }

    public void ReturnToSaveSlotMenu()
    {
        deleteSubMenuUI.SetActive(false);
        Button selectedSlotBtn = saveSlotUI.GetComponentsInChildren<Button>()[GameManager.currentlySelectedSaveSlot - 1];
        selectedSlotBtn.Select();
    }

    public void EnterSaveSlotMenu()
    {
        // play some exit animation for start menu

        // play some intro animation for the save slot ui
        saveSlotUI.SetActive(true);
        Button slot1Button = saveSlotUI.GetComponentsInChildren<Button>()[0];
        slot1Button.Select();
        startMenuUI.SetActive(false);
    }

    public void ReturnToStartMenu()
    {
        // play some exit animation for save slot menu

        // play some intro animation for the start menu ui
        startMenuUI.SetActive(true);
        Button startButton = startMenuUI.GetComponentsInChildren<Button>()[0];
        startButton.Select();
        saveSlotUI.SetActive(false);
    }

    public void SelectSaveSlot1()
    {
        // create save data for slot 1 if it doesn't exist
        if (saveSlot1NoDataPanel.activeSelf)
        {
            GameManager.SaveSlotData(new SaveObject(), 1);
        }

        // load main menu scene
        GameManager.LoadScene(1, 0.2f);
    }

    public void SelectSaveSlot2()
    {
        // create save data for slot 1 if it doesn't exist
        if (saveSlot2NoDataPanel.activeSelf)
        {
            GameManager.SaveSlotData(new SaveObject(), 2);
        }

        // load main menu scene
        GameManager.LoadScene(1, 0.2f);
    }

    public void SelectSaveSlot3()
    {
        // create save data for slot 1 if it doesn't exist
        if (saveSlot3NoDataPanel.activeSelf)
        {
            GameManager.SaveSlotData(new SaveObject(), 3);
        }

        // load main menu scene
        GameManager.LoadScene(1, 0.2f);
    }
}