using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CutsceneManager : MonoBehaviour
{
    public void HandlePressingStart(InputAction.CallbackContext context)
    {
        if (context.performed && GameManager.playerLocation.Equals("mainForest"))
        {
            SaveObject saveData = GameManager.LoadSlotData(GameManager.currentlySelectedSaveSlot);
            saveData.hasCompletedEvent1 = true;
            GameManager.SaveSlotData(saveData, GameManager.currentlySelectedSaveSlot);
            GameManager.LoadScene(3);
        }
    }
}
