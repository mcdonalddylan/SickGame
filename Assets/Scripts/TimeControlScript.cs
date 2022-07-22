using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class TimeControlScript : MonoBehaviour
{
    private Volume postprocessingVolume;
    private ColorAdjustments colorAdjustments = null;
    private PlayerControllerScript playerScript;
    private GameUIScript inGameUI;
    private RectTransform playerFrontTimeBar;
    private Vector3 playerTimeBarLocalScale;

    private bool decreasePlayerTimeValue = false;
    private bool increasePlayerTimeValue = false;

    private void Awake()
    {
        try
        {
            postprocessingVolume = GameObject.FindGameObjectWithTag("GlobalVolume").GetComponent<Volume>();
            postprocessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments);
        }
        catch (NullReferenceException e)
        {
            Debug.Log("No global volume found in this scene");
        }
        playerScript = GetComponent<PlayerControllerScript>();
        inGameUI = GameObject.FindGameObjectWithTag("InGameUI").GetComponent<GameUIScript>();
        if (playerScript.playerNumber == 1)
        {
            playerFrontTimeBar = inGameUI.player1FrontTimeControlBar.GetComponent<RectTransform>();
        }

    }

    private void Update()
    {
        if (increasePlayerTimeValue)
        {
            playerScript.currentTimeSlowValue += 0.05f * Time.deltaTime;
        }
        else if (decreasePlayerTimeValue)
        {
            playerScript.currentTimeSlowValue -= 0.175f * Time.deltaTime;
        }

        if(playerScript.currentTimeSlowValue < 0)
        {
            ReturnNormalTime(0.35f, playerScript.playerNumber);
        }
        else if(playerScript.currentTimeSlowValue > 1)
        {
            playerScript.currentTimeSlowValue = 1;
            increasePlayerTimeValue = false;
        }
        playerTimeBarLocalScale = new Vector3(playerScript.currentTimeSlowValue, 1, 1);
        playerFrontTimeBar.localScale = playerTimeBarLocalScale;
    }

    // ------------------
    // Public Functions |
    // ------------------
    public void StartTimeSlow(float duration, int playerNumber)
    {
        increasePlayerTimeValue = false;
        StartCoroutine(SlowTimeAnimation(duration));
        decreasePlayerTimeValue = true;
        inGameUI.SlowTimeBarAnimation(playerNumber);
    }

    public void ReturnNormalTime(float duration, int playerNumber)
    {
        
        StartCoroutine(ReturnNormalTimeAnimation(duration));
        StartCoroutine(TemporarilyHaltIncrease(1f));
        inGameUI.ReturnNormalTimeBar(playerNumber);
    }

    // ------------------
    // Coroutines       |
    // ------------------
    private IEnumerator SlowTimeAnimation(float duration)
    {
        GameManager.isTimeSlow = true;
        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            GameManager.timeScale = Mathf.Lerp(1f, 0.1f, i);
            colorAdjustments.saturation.value = Mathf.Lerp(0f, -20f, i);
            yield return null;
        }
        colorAdjustments.saturation.value = -20;
        GameManager.timeScale = 0.1f;
    }

    private IEnumerator ReturnNormalTimeAnimation(float duration)
    {
        GameManager.isTimeSlow = false;
        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            GameManager.timeScale = Mathf.Lerp(0.1f, 1f, i);
            colorAdjustments.saturation.value = Mathf.Lerp(-20f, 0f, i);
            yield return null;
        }
        colorAdjustments.saturation.value = 0;
        GameManager.timeScale = 1.0f;
    }

    private IEnumerator TemporarilyHaltIncrease(float duration)
    {
        playerScript.timeHalted = true;
        decreasePlayerTimeValue = false;
        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            yield return null;
        }
        increasePlayerTimeValue = true;
        playerScript.timeHalted = false;
    }
}
