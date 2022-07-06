using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SaveSlotScript : MonoBehaviour, ISelectHandler
{
    public void OnSelect(BaseEventData eventData)
    {
        if (eventData.selectedObject.name.Equals("SaveSlot1Btn"))
        {
            GameManager.currentlySelectedSaveSlot = 1;
        }
        else if (eventData.selectedObject.name.Equals("SaveSlot2Btn"))
        {
            GameManager.currentlySelectedSaveSlot = 2;
        }
        else if (eventData.selectedObject.name.Equals("SaveSlot3Btn"))
        {
            GameManager.currentlySelectedSaveSlot = 3;
        }
    }
}
