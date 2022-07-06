using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GameUIScript : MonoBehaviour
{
    public GameObject inGameUI;
    public GameObject pauseUI;
    public GameObject optionsMenuUI;

    public GameObject livesUIGroup;
    public GameObject gemsUIGroup;
    public GameObject spiritsUIGroup;

    private void Start()
    {
        
        inGameUI.SetActive(true);

        // Set lives, gems and spirits based on save data
        UpdateLivesCountOnUI();
        UpdateGemsCountOnUI();
        UpdateSpiritsCountOnUI();
        
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
        Toggle dofToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[1];
        dofToggle.isOn = optionsData.hasDOF;
        Toggle ssaoToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[2];
        ssaoToggle.isOn = optionsData.hasSSAO;
        Toggle bloomToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[3];
        bloomToggle.isOn = optionsData.hasBloom;
        Toggle fxaaToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[4];
        fxaaToggle.isOn = optionsData.hasFXAA;
        pauseUI.SetActive(false);
        optionsMenuUI.SetActive(false);
    }

    public void HandlePausedPressed(InputAction.CallbackContext context)
    {
        if (context.performed && !pauseUI.activeSelf)
        {
            ActivatePause();
        }
        else if (context.performed && pauseUI.activeSelf && !optionsMenuUI)
        {
            DeactivatePause();
        }
    }

    public void HandlePressingCancel(InputAction.CallbackContext context)
    {
        if (context.performed && GameManager.isPaused)
        {
            if (optionsMenuUI.activeSelf)
            {
                CloseAndSaveOptionsMenu();
            }
            else
            {
                DeactivatePause();
            }
        }
    }

    public void ActivatePause()
    {
        GameManager.isPaused = true;
        Time.timeScale = 0;
        AudioListener.pause = true;

        pauseUI.SetActive(true);
        Button resumeBtn = pauseUI.GetComponentsInChildren<Button>()[0];
        resumeBtn.Select();
    }

    public void DeactivatePause()
    {
        GameManager.isPaused = false;
        Time.timeScale = 1;
        AudioListener.pause = false;

        pauseUI.SetActive(false);
        optionsMenuUI.SetActive(false);
        print("**UN-PAUSED Time scale: " + Time.timeScale);
    }

    public void OpenOptionsMenu()
    {
        // Disable the main menu buttons
        Button[] mainMenuButtons = pauseUI.GetComponentsInChildren<Button>();
        for (int i = 0; i < mainMenuButtons.Length; i++)
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
        Toggle dofToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[1];
        updatedOptionsData.hasDOF = dofToggle.isOn;
        Toggle ssaoToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[2];
        updatedOptionsData.hasSSAO = ssaoToggle.isOn;
        Toggle bloomToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[3];
        updatedOptionsData.hasBloom = bloomToggle.isOn;
        Toggle fxaaToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[4];
        updatedOptionsData.hasFXAA = fxaaToggle.isOn;
        GameManager.SaveOptionsData(updatedOptionsData);

        optionsMenuUI.SetActive(false);

        // Re-enable the main menu buttons
        Button[] mainMenuButtons = pauseUI.GetComponentsInChildren<Button>();
        for (int i = 0; i < mainMenuButtons.Length; i++)
        {
            mainMenuButtons[i].interactable = true;
        }
        Button optionsButton = mainMenuButtons[1];
        optionsButton.Select();
    }

    public void SaveAndReturnToMenu()
    {
        SaveObject updatedSaveData = new SaveObject();
        updatedSaveData.playerLocation = GameManager.playerLocation;
        updatedSaveData.playerCheckpoint = GameManager.playerCheckpoint;
        updatedSaveData.gemsCount = GameManager.gemsCount;
        updatedSaveData.spiritCount = GameManager.spiritCount;
        updatedSaveData.livesCount = GameManager.livesCount;
        updatedSaveData.hasUpgrade1 = GameManager.hasUpgrade1;
        updatedSaveData.hasUpgrade2 = GameManager.hasUpgrade2;
        updatedSaveData.hasUpgrade3 = GameManager.hasUpgrade3;
        updatedSaveData.hasCompletedEvent1 = GameManager.hasCompletedEvent1;
        updatedSaveData.hasCompletedEvent2 = GameManager.hasCompletedEvent2;
        updatedSaveData.hasCompletedEvent3 = GameManager.hasCompletedEvent3;
        updatedSaveData.hasCompletedStory = GameManager.hasCompletedStory;
        GameManager.SaveSlotData(updatedSaveData, GameManager.currentlySelectedSaveSlot);

        Time.timeScale = 1;
        AudioListener.pause = false;
        GameManager.isPaused = false;

        GameManager.LoadScene(1, 0.3f);
    }

    public void UpdateLivesCountOnUI()
    {
        Text livesText = livesUIGroup.GetComponentsInChildren<Text>()[0];
        StartCoroutine(UIJumpAnimation(livesUIGroup, 0.07f));
        livesText.text = "x " + GameManager.livesCount;
    }

    public void UpdateGemsCountOnUI()
    {
        Text gemsText = gemsUIGroup.GetComponentsInChildren<Text>()[0];
        StartCoroutine(UIJumpAnimation(gemsUIGroup, 0.07f));
        gemsText.text = "x " + GameManager.gemsCount;
    }

    public void UpdateSpiritsCountOnUI()
    {
        Text spiritsText = spiritsUIGroup.GetComponentsInChildren<Text>()[0];
        StartCoroutine(UIJumpAnimation(spiritsUIGroup, 0.07f));
        spiritsText.text = "x " + GameManager.spiritCount;
    }

    private IEnumerator UIJumpAnimation(GameObject uiGroup, float duration)
    {
        Text text = uiGroup.GetComponentsInChildren<Text>()[0];
        RectTransform uiRectTransform = uiGroup.GetComponent<RectTransform>();
        print("start: " + uiRectTransform.anchoredPosition);
        float originalYPos = uiRectTransform.anchoredPosition.y;
        // Jump and color fade animation
        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            text.color = new Color(Mathf.Lerp(1,text.color.r,i), Mathf.Lerp(1, text.color.g, i), Mathf.Lerp(1, text.color.b, i), 255);
            uiRectTransform.anchoredPosition = new Vector2(uiRectTransform.anchoredPosition.x,
                (float)Mathf.Lerp(originalYPos, originalYPos+20, i));
            yield return null;
        }
        print("mid: " + uiRectTransform.anchoredPosition);

        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            uiRectTransform.anchoredPosition = new Vector2(uiRectTransform.anchoredPosition.x,
                (float)Mathf.Lerp(originalYPos+20, originalYPos, i));
            yield return null;
        }
        uiRectTransform.anchoredPosition = new Vector2(uiRectTransform.anchoredPosition.x, originalYPos);
    }
}
