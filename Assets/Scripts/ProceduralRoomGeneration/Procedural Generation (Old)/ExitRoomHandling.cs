using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitRoomHandling : MonoBehaviour
{

    /***********************************************************
     * Function: OnTriggerEnter
     ***********************************************************
     * Description: This function is provided by unity. It will
     * detect when I touch the exit room. See the script which
     * defines the WinGame function (GenerationManager.cs) if
     * you want to see what will actually happen when the 
     * trigger happens.
     ***********************************************************
     * Parameters: None
     ***********************************************************
     * Returns: None
     ***********************************************************/
    public void OnTriggerEnter(Collider other) 
    {
        GenerationManager gm = FindObjectOfType<GenerationManager>();

        gm.WinGame();
    }
}
