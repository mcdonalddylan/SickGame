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

    public CanvasGroup topUI;
    public GameObject livesUIGroup;
    public GameObject gemsUIGroup;
    public GameObject spiritsUIGroup;

    private void Awake()
    {
        // Set lives, gems and spirits based on save data
        Text livesText = livesUIGroup.GetComponentsInChildren<Text>()[0];
        livesText.text = "x " + GameManager.livesCount;
        Text gemsText = gemsUIGroup.GetComponentsInChildren<Text>()[0];
        gemsText.text = "x " + GameManager.gemsCount;
        Text spiritsText = spiritsUIGroup.GetComponentsInChildren<Text>()[0];
        spiritsText.text = "x " + GameManager.spiritCount;
    }

    private void Start()
    {
        
        inGameUI.SetActive(true);
        
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
        StartCoroutine(UIJumpAnimation(livesUIGroup, 0.1f));
        livesText.text = "x " + GameManager.livesCount;
    }

    public void UpdateGemsCountOnUI()
    {
        Text gemsText = gemsUIGroup.GetComponentsInChildren<Text>()[0];
        StartCoroutine(UIJumpAnimation(gemsUIGroup, 0.1f));
        gemsText.text = "x " + GameManager.gemsCount;
    }

    public void UpdateSpiritsCountOnUI()
    {
        Text spiritsText = spiritsUIGroup.GetComponentsInChildren<Text>()[0];
        StartCoroutine(UIJumpAnimation(spiritsUIGroup, 0.1f));
        spiritsText.text = "x " + GameManager.spiritCount;
    }

    public void FadeInTopUI()
    {
        StartCoroutine(UIFadeIn(topUI, 1));
    }

    public void FadeOutTopUI()
    {
        StartCoroutine(UIFadeOut(topUI, 1));
    }

    private IEnumerator UIJumpAnimation(GameObject uiGroup, float duration)
    {
        Text text = uiGroup.GetComponentsInChildren<Text>()[0];
        RectTransform uiRectTransform = uiGroup.GetComponent<RectTransform>();

        float originalYPos = 200f;
        Color originalColor = text.color;
        // Jump and color fade animation
        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            text.color = Color.Lerp(Color.white, originalColor, i);
            uiRectTransform.anchoredPosition = new Vector2(uiRectTransform.anchoredPosition.x,
                (float)Mathf.Lerp(originalYPos, originalYPos+20, i));
            yield return null;
        }
        text.color = originalColor;

        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            uiRectTransform.anchoredPosition = new Vector2(uiRectTransform.anchoredPosition.x,
                (float)Mathf.Lerp(originalYPos+20, originalYPos, i));
            yield return null;
        }
        uiRectTransform.anchoredPosition = new Vector2(uiRectTransform.anchoredPosition.x, originalYPos);
    }

    private IEnumerator UIFadeIn(CanvasGroup canvasGroup, float duration)
    {
        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, i);
            yield return null;
        }
        canvasGroup.alpha = 1;
    }

    private IEnumerator UIFadeOut(CanvasGroup canvasGroup, float duration)
    {
        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, i);
            yield return null;
        }
        canvasGroup.alpha = 0;
    }
}
