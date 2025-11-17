using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class GameUIScript : MonoBehaviour
{
    public GameObject inGameUI;
    public GameObject pauseUI;
    public GameObject optionsMenuUI;

    public CanvasGroup topUI;
    public GameObject livesUIGroup;
    public GameObject gemsUIGroup;
    public GameObject spiritsUIGroup;

    public Color32 TIME_CONTROLER_BAR_COLOR = new Color32(231, 177, 255, 255);
    private Coroutine timeBarAnimation = null;
    public GameObject player1FrontTimeControlBar;

    private Volume postprocessingVolume;

    private void Awake()
    {
        // Set lives, gems and spirits based on save data
        Text livesText = livesUIGroup.GetComponentsInChildren<Text>()[0];
        livesText.text = "x " + GameManager.livesCount;
        Text gemsText = gemsUIGroup.GetComponentsInChildren<Text>()[0];
        gemsText.text = "x " + GameManager.gemsCount;
        Text spiritsText = spiritsUIGroup.GetComponentsInChildren<Text>()[0];
        spiritsText.text = "x " + GameManager.spiritCount;

        try
        {
            postprocessingVolume = GameObject.FindGameObjectWithTag("GlobalVolume").GetComponent<Volume>();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("No global volume found in this scene");
        }

        player1FrontTimeControlBar.GetComponent<Image>().color = TIME_CONTROLER_BAR_COLOR;
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
        Toggle colorAdjToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[6];
        colorAdjToggle.isOn = optionsData.hasColorAdjustments;
        Toggle lensDistToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[7];
        lensDistToggle.isOn = optionsData.hasLensDistortion;
        Toggle lensFlareToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[8];
        lensFlareToggle.isOn = optionsData.hasScreenSpaceLensFlare;
        pauseUI.SetActive(false);
        optionsMenuUI.SetActive(false);
    }

    private void Update()
    {
        CheckForPause();
        CheckForCancel();
    }

    public void CheckForCancel()
    {
        if (InputManager.specialOrCancelWasPressed && GameManager.isPaused && optionsMenuUI.activeSelf)
        {
            CloseAndSaveOptionsMenu();
        }
        else if (InputManager.specialOrCancelWasPressed && GameManager.isPaused)
        {
            DeactivatePause();
        }
    }

    public void CheckForPause()
    {
        if (InputManager.pauseWasPressed && !pauseUI.activeSelf)
        {
            ActivatePause();
        }
        else if (InputManager.pauseWasPressed && pauseUI.activeSelf && !optionsMenuUI)
        {
            DeactivatePause();
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
        Toggle colorAdjToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[6];
        updatedOptionsData.hasColorAdjustments = colorAdjToggle.isOn;
        Toggle lensDistToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[7];
        updatedOptionsData.hasLensDistortion = lensDistToggle.isOn;
        Toggle lensFlareToggle = optionsMenuUI.GetComponentsInChildren<Toggle>()[8];
        updatedOptionsData.hasScreenSpaceLensFlare = lensFlareToggle.isOn;
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
        SaveObject updatedSaveData = new SaveObject
        {
            playerLocation = GameManager.playerLocation,
            playerCheckpoint = GameManager.playerCheckpoint,
            gemsCount = GameManager.gemsCount,
            spiritCount = GameManager.spiritCount,
            livesCount = GameManager.livesCount,
            hasUpgrade1 = GameManager.hasUpgrade1,
            hasUpgrade2 = GameManager.hasUpgrade2,
            hasUpgrade3 = GameManager.hasUpgrade3,
            hasCompletedEvent1 = GameManager.hasCompletedEvent1,
            hasCompletedEvent2 = GameManager.hasCompletedEvent2,
            hasCompletedEvent3 = GameManager.hasCompletedEvent3,
            hasCompletedStory = GameManager.hasCompletedStory
        };
        GameManager.SaveSlotData(updatedSaveData, GameManager.currentlySelectedSaveSlot);

        Time.timeScale = 1;
        AudioListener.pause = false;
        GameManager.isPaused = false;
        GameManager.timeScale = 1.0f;
        GameManager.nonPlayerTimeScale = 1.0f;
        GameManager.isTimeSlow = false;
        postprocessingVolume.profile.TryGet<ColorAdjustments>(out ColorAdjustments colorAdjustments);
        colorAdjustments.saturation.value = 0;
        postprocessingVolume.profile.TryGet<LensDistortion>(out LensDistortion lensDistortion);
        lensDistortion.intensity.value = 0;
        postprocessingVolume.profile.TryGet<ScreenSpaceLensFlare>(out ScreenSpaceLensFlare lensFlare);
        lensFlare.intensity.value = 0;

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

    public void SlowTimeBarAnimation(int playerNumber)
    {
        timeBarAnimation = StartCoroutine(PlayerTimeControlUISlow(0.25f, playerNumber));
    }

    public void ReturnNormalTimeBar(int playerNumber)
    {
        StopCoroutine(timeBarAnimation);
        player1FrontTimeControlBar.GetComponent<Image>().color = TIME_CONTROLER_BAR_COLOR;
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
        Color originalColor = new Color32(50,50,50,255);

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

    private IEnumerator PlayerTimeControlUISlow(float duration, int playerNumber)
    {
        Image timeBarImage = null;
        if (playerNumber == 1)
        {
            timeBarImage = player1FrontTimeControlBar.GetComponent<Image>();
        }

        while (true){
            for (float i = 0; i < 1; i += Time.deltaTime / duration)
            {
                timeBarImage.color = Color.Lerp(TIME_CONTROLER_BAR_COLOR, Color.white, i);
                yield return null;
            }
            timeBarImage.color = Color.white;

            for (float i = 0; i < 1; i += Time.deltaTime / duration)
            {
                timeBarImage.color = Color.Lerp(Color.white, TIME_CONTROLER_BAR_COLOR, i);
                yield return null;
            }
            timeBarImage.color = TIME_CONTROLER_BAR_COLOR;
        }
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
