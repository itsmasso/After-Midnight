using UnityEngine;
using Unity.Netcode;

/*****************************************************************
 * RadioManager Script
 *****************************************************************
 * Author: Dylan Werelius
 *****************************************************************
 * Description:
    This script handles the actions of the radio. The radio allows
    the players to descend to the next dream level when they
    interact with it. When they have reached the deepest level of
    the dream, then it no longer allows them to descend. Instead
    it will play a soothing song to wake the dreamer up. The 
    players will have to find their way back to the spawn room
    before the song ends or else they will die.
 *****************************************************************/
public class RadioManager : NetworkSingleton<RadioManager>, IInteractable
{
    void Start()
    {
        Debug.Log("Start Radio Manger");
    }
    void Update()
    {

    }

/*****************************************************************
 * ReactToPlayerGaze - May not need
 *****************************************************************
 * Author: Dylan Werelius
 *****************************************************************
 * Description:
    This function is taken from the interface IReactToPlayerGaze
    It will handle the player interacting with the object when
    the player is looking at the radio. The player will look
    at the radio and press "E" (the interact key) to interact
    with it which will call the DescendDreamLevel or the 
    StartExtractionProtocol function depending on what dream 
    level the players are on.
 *****************************************************************/
    //public void ReactToPlayerGaze(NetworkObjectReference playerObjectRef) {}

/*****************************************************************
 * Interact
 *****************************************************************
 * Author: Dylan Werelius
 *****************************************************************
 * Description:
    This function will actually allow the player to interact
    with the radio when they press "E" (the interact key)
 *****************************************************************/
    public void Interact(NetworkObjectReference playerObjectRef)
    {
        Debug.Log("Interact called from RadioManager.cs");
        DescendDreamLevel();
    }

/*****************************************************************
 * PlaySong
 *****************************************************************
 * Author: Dylan Werelius
 *****************************************************************
 * Description:
    This function will start playing a song.
 *****************************************************************/
    private void PlaySong()
    {

    }

/*****************************************************************
 * DescendDreamLevel
 *****************************************************************
 * Author: Dylan Werelius
 *****************************************************************
 * Description:
    This function will be called when a player interacts with the 
    radio and they are not yet at the deepest level of the dream. 
    Players will descend down to the next dream lever.
    It will generate a new map and perform the actions
    necessary to spawn the players in the new map as well as 
    start everything that needs to be started.
 *****************************************************************/
    private void DescendDreamLevel()
    {
        Debug.Log("Descending Dream Level...");
        GameManager.Instance.OnNextLevel();
    }

/*****************************************************************
 * StartExtractionProtocol
 *****************************************************************
 * Author: Dylan Werelius
 *****************************************************************
 * Description:
    This function will be called when the players interact with 
    the radio and they are on the deepest level of the dream.
 *****************************************************************/
    private void StartExtractionProtocol()
    {
        Debug.Log("Starting Extraction Protocol...");
        PlaySong();
    }
}
