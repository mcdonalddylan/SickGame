using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;

    // Loading variables
    private Image transitionImage;

    // Data variables
    public static int currentlySelectedSaveSlot = 1;
    public static string saveDirectory = "/SaveData/";
    public static string optionsFileName = "Options.txt";
    public static string saveSlotFileName = "SaveSlot";

    // Game state variables
    public static float timeScale = 1.0f;
    public static bool isTimeSlow = false;
    public static bool particleEffectsEnabled = false;
    public static bool isPaused = false;
    public static string playerLocation = "mainForest";
    public static int playerCheckpoint;
    public static int gemsCount;
    public static int spiritCount;
    public static int livesCount = 3;
    public static bool hasUpgrade1;
    public static bool hasUpgrade2;
    public static bool hasUpgrade3;
    public static bool hasCompletedEvent1;
    public static bool hasCompletedEvent2;
    public static bool hasCompletedEvent3;
    public static bool hasCompletedStory;

    // Multiplayer variables
    public static int activePlayers = 1;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            transitionImage = instance.GetComponentsInChildren<Image>()[0];
            print("save directory: " + Application.persistentDataPath + saveDirectory);
            transitionImage.rectTransform.sizeDelta = new Vector2(Screen.width + 200, Screen.height + 200);
            transitionImage.gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //----------------------
    // LOADING SCENE METHODS
    //----------------------
    public static void LoadScene(int buildIndexOfScene, float duration = 1)
    {
        instance.StartCoroutine(instance.FadeScene(buildIndexOfScene, duration));
    }

    private IEnumerator FadeScene(int buildIndexOfScene, float duration)
    {
        transitionImage.gameObject.SetActive(true);

        // Fade in animation
        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            transitionImage.color = new Color(0.05f, 0.05f, 0.05f, Mathf.Lerp(0, 1, i));
            yield return null;
        }

        AsyncOperation asyncSceneLoading = SceneManager.LoadSceneAsync(buildIndexOfScene);

        // Forces the game to wait before
        while (!asyncSceneLoading.isDone)
        {
            yield return null;
        }

        // Fade out animation
        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            transitionImage.color = new Color(0.05f, 0.05f, 0.05f, Mathf.Lerp(1, 0, i));
            yield return null;
        }
    }

    public static void LoadInGameScene()
    {
        SaveObject saveData = LoadSlotData(currentlySelectedSaveSlot);
        SetupLocalGameStateVariables(saveData);
        if (playerLocation == "mainForest" && !hasCompletedEvent1)
        {
            LoadScene(2, 0.5f);
        }
        else if (playerLocation == "mainForest")
        {
            LoadScene(3, 0.5f);
        }
    }

    //---------------------
    // GAME STATE VARIABLES
    //---------------------

    private static void SetupLocalGameStateVariables(SaveObject so)
    {
        playerLocation = so.playerLocation;
        playerCheckpoint = so.playerCheckpoint;
        gemsCount = so.gemsCount;
        spiritCount = so.spiritCount;
        livesCount = so.livesCount;
        hasUpgrade1 = so.hasUpgrade1;
        hasUpgrade2 = so.hasUpgrade2;
        hasUpgrade3 = so.hasUpgrade3;
        hasCompletedEvent1 = so.hasCompletedEvent1;
        hasCompletedEvent2 = so.hasCompletedEvent2;
        hasCompletedEvent3 = so.hasCompletedEvent3;
        hasCompletedStory = so.hasCompletedStory;
    }

    //------------------------
    // SAVING AND LOADING DATA
    //------------------------

    public static void SaveOptionsData(OptionsDataObject optionsData)
    {
        string fullDirectory = Application.persistentDataPath + saveDirectory;

        if (!Directory.Exists(fullDirectory))
        {
            Directory.CreateDirectory(fullDirectory);
        }

        string jsonData = JsonUtility.ToJson(optionsData);
        File.WriteAllText(fullDirectory + optionsFileName, jsonData);
    }

    public static OptionsDataObject LoadOptionsData()
    {
        string fullFilePath = Application.persistentDataPath + saveDirectory + optionsFileName;

        if (File.Exists(fullFilePath))
        {
            string jsonData = File.ReadAllText(fullFilePath);
            OptionsDataObject optionsData = JsonUtility.FromJson<OptionsDataObject>(jsonData);

            return optionsData;
        }
        else
        {
            Debug.Log("Options data file does not exit");
        }

        return null;
    }

    public static void SaveSlotData(SaveObject saveData, int saveSlotNumber)
    {
        if (!SaveDirectoryExists())
        {
            Directory.CreateDirectory(Application.persistentDataPath + saveDirectory);
        }
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        FileStream file = File.Create(GetFullPath(saveSlotNumber));
        binaryFormatter.Serialize(file, saveData);
        file.Close();
    }

    public static SaveObject LoadSlotData(int saveSlotNumber)
    {
        if (SaveSlotDataExists(saveSlotNumber))
        {
            try
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                FileStream file = File.Open(GetFullPath(saveSlotNumber), FileMode.Open);
                SaveObject saveData = (SaveObject)binaryFormatter.Deserialize(file);
                file.Close();

                return saveData;
            }
            catch (SerializationException)
            {
                Debug.Log("Failed to load save slot " + saveSlotNumber + " data");
            }
        }

        return null;
    }

    public static void DeleteSaveSlotData(int saveSlotNumber)
    {
        File.Delete(GetFullPath(saveSlotNumber));
    }

    public static bool SaveSlotDataExists(int saveSlotNumber)
    {
        return File.Exists(GetFullPath(saveSlotNumber));
    }

    private static bool SaveDirectoryExists()
    {
        return Directory.Exists(Application.persistentDataPath + saveDirectory);
    }

    private static string GetFullPath(int saveSlotNumber)
    {
        return Application.persistentDataPath + saveDirectory + saveSlotFileName + saveSlotNumber + ".dungo";
    }
}