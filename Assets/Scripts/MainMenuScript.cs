using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainMenuScript : MonoBehaviour
{
    public GameObject mainMenuUI;
    public GameObject optionsMenuUI;
    public Text spiritsText;

    public GameObject Adventurer2Image;
    public GameObject Adventurer3Image;
    public GameObject Adventurer4Image;

    public GameObject menuGlowParticles;
    void Start()
    {
        // Set deafult number of active players
        Adventurer2Image.SetActive(false);
        Adventurer3Image.SetActive(false);
        Adventurer4Image.SetActive(false);

        mainMenuUI.SetActive(true);

        SaveObject saveData = GameManager.LoadSlotData(GameManager.currentlySelectedSaveSlot);
        spiritsText.text = "x " + saveData.spiritCount;

        // Set options UI to match loaded data
        OptionsDataObject optionsData = GameManager.LoadOptionsData();
        if (optionsData == null)
        {
            GameManager.SaveOptionsData(new OptionsDataObject());
        }
        Slider resolutionScaleSlider = optionsMenuUI.GetComponentsInChildren<Slider>()[0];
        resolutionScaleSlider.value = optionsData.resolutionScale;

        Dropdown textureQualityDropdown = optionsMenuUI.GetComponentsInChildren<Dropdown>()[0];
        if (optionsData.textureQuality.Equals("Good"))
        {
            textureQualityDropdown.value = 0;
        }
        else if (optionsData.textureQuality.Equals("LOL"))
        {
            textureQualityDropdown.value = 1;
        }
        Toggle shadowToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[0];
        shadowToggle.isOn = optionsData.hasQualityShadows;
        Toggle particlesToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[1];
        particlesToggle.isOn = optionsData.particleEffectsEnabled;
        Toggle dofToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[2];
        dofToggle.isOn = optionsData.hasDOF;
        Toggle ssaoToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[3];
        ssaoToggle.isOn = optionsData.hasSSAO;
        Toggle bloomToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[4];
        bloomToggle.isOn = optionsData.hasBloom;
        Toggle fxaaToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[5];
        fxaaToggle.isOn = optionsData.hasFXAA;
        optionsMenuUI.SetActive(false);

        if (GameManager.particleEffectsEnabled)
        {
            menuGlowParticles.SetActive(true);
        }

        Button adventureButton = mainMenuUI.GetComponentsInChildren<Button>()[0];
        adventureButton.Select();
    }

    public void AddNewLocalPlayer()
    {
        int activePlayers = GameManager.activePlayers;
        if (activePlayers == 1)
        {
            Adventurer2Image.SetActive(true);
            GameManager.activePlayers = 2;
        }
        else if (activePlayers == 2)
        {
            Adventurer3Image.SetActive(true);
            GameManager.activePlayers = 2;
        }
        else if (activePlayers == 3)
        {
            Adventurer4Image.SetActive(true);
            GameManager.activePlayers = 4;
        }
    }

    public void RemoveLocalPlayer()
    {
        int activePlayers = GameManager.activePlayers;
        if (activePlayers == 2)
        {
            Adventurer2Image.SetActive(false);
            GameManager.activePlayers = 1;
        }
        else if (activePlayers == 3)
        {
            Adventurer3Image.SetActive(false);
            GameManager.activePlayers = 2;
        }
        else if (activePlayers == 4)
        {
            Adventurer4Image.SetActive(false);
            GameManager.activePlayers = 3;
        }
    }

    public void OpenOptionsMenu()
    {
        // Disable the main menu buttons
        Button[] mainMenuButtons = mainMenuUI.GetComponentsInChildren<Button>();
        for (int i = 0; i< mainMenuButtons.Length; i++)
        {
            mainMenuButtons[i].interactable = false;
        }

        optionsMenuUI.SetActive(true);
        Slider resolutionScaleSlider = optionsMenuUI.GetComponentsInChildren<Slider>()[0];
        resolutionScaleSlider.Select();
    }

    public void CloseAndSaveOptionsMenu()
    {
        OptionsDataObject updatedOptionsData = new OptionsDataObject();

        Slider resolutionScaleSlider = optionsMenuUI.GetComponentsInChildren<Slider>()[0];
        updatedOptionsData.resolutionScale = (int)resolutionScaleSlider.value;
        Dropdown textureQualityDropdown = optionsMenuUI.GetComponentsInChildren<Dropdown>()[0];
        if (textureQualityDropdown.value == 1)
        {
            updatedOptionsData.textureQuality = "LOL";
        }
        else if (textureQualityDropdown.value == 0)
        {
            updatedOptionsData.textureQuality = "Good";
        }
        Toggle shadowToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[0];
        updatedOptionsData.hasQualityShadows = shadowToggle.isOn;
        Toggle particlesToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[1];
        updatedOptionsData.particleEffectsEnabled = particlesToggle.isOn;
        Toggle dofToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[2];
        updatedOptionsData.hasDOF = dofToggle.isOn;
        Toggle ssaoToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[3];
        updatedOptionsData.hasSSAO = ssaoToggle.isOn;
        Toggle bloomToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[4];
        updatedOptionsData.hasBloom = bloomToggle.isOn;
        Toggle fxaaToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[5];
        updatedOptionsData.hasFXAA = fxaaToggle.isOn;
        GameManager.SaveOptionsData(updatedOptionsData);

        optionsMenuUI.SetActive(false);

        // Re-enable the main menu buttons
        Button[] mainMenuButtons = mainMenuUI.GetComponentsInChildren<Button>();
        for (int i = 0; i < mainMenuButtons.Length; i++)
        {
            mainMenuButtons[i].interactable = true;
        }
        Button optionsButton = mainMenuButtons[2];
        optionsButton.Select();
    }

    public void HandlePressingCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (optionsMenuUI.activeSelf)
            {
                CloseAndSaveOptionsMenu();
            }
            else
            {
                GameManager.LoadScene(0, 0.2f);
            }
        }
    }

    public void SelectingAdventure()
    {
        GameManager.LoadInGameScene();
    }

    public void RemoveBGParticles()
    {
        menuGlowParticles.SetActive(false);
    }

    public void ReAddBGParticles()
    {
        menuGlowParticles.SetActive(true);
    }
}
