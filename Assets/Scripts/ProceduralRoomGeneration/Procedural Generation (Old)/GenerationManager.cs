using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/***********************************************************
 * GenerationState - This enum will help us to keep track of
 * the current state of generation that is being done. The
 * ones labeled "Special Cases" will only run once during
 * the whole generation process. This is to ensure there is
 * only ever one spawn point and one end point.
 ***********************************************************/
public enum GenerationState
{
    Idle,
    GeneratingRooms,
    GeneratingLighting,

    // Special Rooms
    GeneratingSpawn,
    GeneratingExit,
    
    // Set the world boundaries
    GeneratingBarrier
}

public class GenerationManager : MonoBehaviour
{

    /**************************************************************************************
     * Variables - Start
     **************************************************************************************/

    [Header("Room/Light Prefabs")]
    /***********************************************************
     * WorldGrid - This GameObject is the parent of the rooms
     ***********************************************************/
    [SerializeField] Transform WorldGrid;

    /***********************************************************
     * RoomPrefab - This GameObject is the template of the rooms
     ***********************************************************/
    [SerializeField] List<GameObject> RoomPrefabs;

    /***********************************************************
     * LightPrefabs - These are the prefabs that spawn in the 
     * rooms to give light
     ***********************************************************/
    [SerializeField] List<GameObject> LightPrefabs;

    [Header("Sliders/Buttons")]
    /***********************************************************
     * MapSizeSlider - This GameObject refernces the slider on 
     * the canvas which controls the size of the map
     ***********************************************************/
    [SerializeField] Slider MapSizeSlider;

    /***********************************************************
     * EmptinessSlider - This GameObject References the Map
     * Emptiness Slider on the canvas which controls how empty
     * the map will be
     ***********************************************************/
    [SerializeField] Slider EmptinessSlider;

    /***********************************************************
     * BrightnessSlider - This GameObject references the Map
     * Brightness slider on the canvas which controls how 
     * bright the map should be. To be exact, it controls the 
     * probability that lights spawn, so it isn't directly
     * how bright the map is.
     ***********************************************************/
    [SerializeField] Slider BrightnessSlider;

    /***********************************************************
     * GenerateButton - This GameObject references the button
     * on the canvas that is used to generate the world
     ***********************************************************/
    [SerializeField] Button GenerateButton;

    [Header("Special Rooms")]
    /***********************************************************
     * E_Room - This GameObject is a reference to the Empty
     * Room Type which has no walls and is essentially meant to
     * be like a hallway between rooms.
     ***********************************************************/
    [SerializeField] GameObject E_Room;

    /***********************************************************
     * B_Room - This GameObject references the BarrierRoom
     * prefab which is meant to serve as the boundaries to stop
     * us from falling out of the world.
     ***********************************************************/
    [SerializeField] GameObject B_Room;

    /***********************************************************
     * SpawnRoom - This GameObject holds a reference to the 
     * Spawn Room prefab which is where the player spawns in.
     ***********************************************************/
    [SerializeField] GameObject SpawnRoom;

    /***********************************************************
     * ExitRoom - This GameObject holds a reference to the 
     * Exit Room Prefab which is where the player must reach to
     * complete the game.
     ***********************************************************/
    [SerializeField] GameObject ExitRoom;

    /***********************************************************
     * GeneratedRooms - This list holds all of the rooms that
     * have been generated.
     ***********************************************************/
    public List<GameObject> GeneratedRooms;

    /***********************************************************
     * spawnRoom - THIS IS NOT THE SAME AS THE SpawnRoom object.
     * This is not holding the prefab, this holds a reference
     * to the actual room which gets created and added to the
     * GeneratedRooms list. We need a reference to it so that
     * we can move to player to this location on start.
     ***********************************************************/
    public GameObject spawnRoom;

    [Header("Gameplay View")]
    /***********************************************************
     * PlayerObject - This GameObject will hold a reference to
     * the Player Object
     ***********************************************************/
    [SerializeField] GameObject PlayerObject;

    /***********************************************************
     * MainCameraObject - This GameObject will hold a reference
     * to the MainCameraObject
     ***********************************************************/
    [SerializeField] GameObject MainCameraObject;
    
    [Header("Settings")]
    /***********************************************************
     * mapSize - This variable represents the size of the map,
     * meaning how many rooms there are.
     ***********************************************************
     * VERY IMPORTANT!! THIS MUST BE A SQUARE NUMBER OR THE GAME
     * WILL CRASH!! A square number means that the square root
     * of the number is an int, not a float or decimal.
     * Ex. 4, 9, 16, 25, etc
     ***********************************************************/
    [SerializeField] int mapSize = 16;

    /***********************************************************
     * mapEmptiness - This value represents the chance of an
     * E_Room spawning in. Basically how much of the backrooms
     * should be hallways and not rooms with walls
     ***********************************************************/
    public int mapEmptiness;

    /***********************************************************
     * mapBrightness - This represents the chance of a light 
     * type spawning in.
     ***********************************************************/
    public int mapBrightness;

    /***********************************************************
     * mapSizeSquare - This is the square root of mapSize. As 
     * stated before, this has to be an int, not a float!
     * This is because it is impossible to create 1.5 rooms. You
     * have to create a whole number of rooms.
     ***********************************************************/
    private int mapSizeSquare;

    /***********************************************************
     * currentPos - This is the current position of the room to
     * be generated.
     ***********************************************************/
    private Vector3 currentPos;

    /***********************************************************
     * currentPosX - This will keep track of our position X of
     * the room to be generated.
     ***********************************************************/
    private float currentPosX;

    /***********************************************************
     * currentPosZ - This will keep track of our position Z
     * of the room to be generated.
     ***********************************************************/
    private float currentPosZ;

    /***********************************************************
     * currentPosTracker - This will keep track of how many
     * rooms we have created.
     ***********************************************************/
    private float currentPosTracker;

    /***********************************************************
     * currentRoom - This will keep track of the current room
     * that we are in. We will use this to set up the borders
     * of the world.
     ***********************************************************/
    private int currentRoom;

    /***********************************************************
     * positionTracker - This keeps track of the position of 
     * out generator
     ***********************************************************/
     private int positionTracker;

    /***********************************************************
     * roomSize - This variable keeps track of the size of the 
     * rooms that we are generating.
     ***********************************************************/
     public float roomSize = 7;

    /***********************************************************
     * currentState - This enum variable keeps track of what 
     * state of generation we are in.
     ***********************************************************/
     public GenerationState currentState;

     /***********************************************************
     * enumLength - This will keep track of how many states exist
     * inside of the GenerationState enum. We use this value in
     * loops throughout the code.
     ***********************************************************/
     private int enumLength = Enum.GetNames(typeof(GenerationState)).Length;

    /**************************************************************************************
     * Variables - End
     **************************************************************************************/





    /**************************************************************************************
     * Functions - Begin
     **************************************************************************************/

    /***********************************************************
     * Function: Update (unity provided function)
     ***********************************************************
     * Description: This purpose of having this is to set the
     * mapSizeSquare varable to the square root of mapSize.
     * Be mindful that it is possible to crash the game if the
     * square root of map size comes out to be a float value not
     * an int value.
     ***********************************************************
     * Parameters: None
     ***********************************************************
     * Returns: None
     ***********************************************************/
     private void Update()
     {
        // This will raise the value of the slider to the power of 4
        // which will automatically make it a square number. So if
        // We use the slider to set the mapSize to 2, then this will
        // set mapSize = to 2^4 which equals 16, which is a square number.
        mapSize = (int)Mathf.Pow(MapSizeSlider.value, 4);

        mapSizeSquare = (int)Mathf.Sqrt(mapSize);

        mapEmptiness = (int)EmptinessSlider.value;

        mapBrightness = (int)BrightnessSlider.value;
     }

    /***********************************************************
     * Function: ReloadWorld
     ***********************************************************
     * Description: This function will reload the world if a 
     * new one is needed for whatever reason.
     ***********************************************************
     * Parameters: None
     ***********************************************************
     * Returns: None
     ***********************************************************/
    public void ReloadWorld()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /***********************************************************
     * Function: GenerateWorld
     ***********************************************************
     * Description: This function will create the world when it
     * is clicked.
     ***********************************************************
     * Parameters: None
     ***********************************************************
     * Returns: None
     ***********************************************************/
    public void GenerateWorld()
    {
        for (int count = 0; count < mapEmptiness; count++)
        {
            // This will add empty rooms to the RoomPrefabs list
            RoomPrefabs.Add(E_Room);
        }

        // This ensures we can only generate once
        GenerateButton.interactable = false;

        for (int state = 0; state < enumLength; state++)
        {
            for (int count = 0; count < mapSize; count++)
            {
                // This will help make sure we make a square grid of rooms,
                // and not a line of rooms
                if (currentPosTracker == mapSizeSquare)
                {
                    // This is for the right side of the map
                    if (currentState == GenerationState.GeneratingBarrier) GenerateBarrier();

                    // Reset the column count
                    currentPosX = 0;
                    currentPosTracker = 0;

                    // Move to the next row
                    currentPosZ += roomSize;

                    // This is for the left side of the map
                    if (currentState == GenerationState.GeneratingBarrier) GenerateBarrier();
                }

                // This will instantiate our room type at the current position instead of the world grid
                currentPos = new(currentPosX, 0, currentPosZ);

                // This will control the generation of Rooms (not special rooms), Lighting, and Barriers
                switch(currentState)
                {
                    // If the state is GeneratingRooms, then generate the rooms
                    case GenerationState.GeneratingRooms:
                        GeneratedRooms.Add(Instantiate(RoomPrefabs[UnityEngine.Random.Range(0, RoomPrefabs.Count)], currentPos, Quaternion.identity, WorldGrid));
                    break;

                    // If the state is GeneratingLighting, then generate the lighting
                    case GenerationState.GeneratingLighting:

                        // This will help us randomly generate if a light spawns
                        int lightSpawn = UnityEngine.Random.Range(-1, mapBrightness);

                        if (lightSpawn == 0)
                        {
                            Instantiate(LightPrefabs[UnityEngine.Random.Range(0, LightPrefabs.Count)], currentPos, Quaternion.identity, WorldGrid);
                        }
                    break;

                    case GenerationState.GeneratingBarrier:
                        if (currentRoom <= mapSizeSquare && currentRoom >= 0)
                        {
                            // This is for the bottom of the map
                            GenerateBarrier();
                        }

                        if (currentRoom <= mapSize && currentRoom >= mapSize - mapSizeSquare)
                        {
                            // This is for the top of the map
                            GenerateBarrier();
                        }
                    break;
                }

                // This will ensure the rooms do not spawn on top of each other
                currentRoom++;
                currentPosTracker++;
                currentPosX += roomSize;
            } // End of for (int count = 0; count < mapSize; count++)
            NextState();

            // Handle the Exit and Spawn rooms
            switch (currentState)
            {
                case GenerationState.GeneratingExit:

                    // This will select a random room that was already generated to make the exit room
                    int roomToReplace = UnityEngine.Random.Range(GeneratedRooms.Count - (int)(GeneratedRooms.Count / 4), GeneratedRooms.Count);

                    // Duplicate the room to create the exit and the destroy the original room
                    GameObject exitRoom = Instantiate(ExitRoom, GeneratedRooms[roomToReplace].transform.position, Quaternion.identity, WorldGrid);
                    Destroy(GeneratedRooms[roomToReplace]);
                    GeneratedRooms[roomToReplace] = exitRoom;
                break;

                case GenerationState.GeneratingSpawn:
                    int _roomToReplace = UnityEngine.Random.Range(0, (int)(GeneratedRooms.Count / 4));
                    spawnRoom = Instantiate(SpawnRoom, GeneratedRooms[_roomToReplace].transform.position, Quaternion.identity, WorldGrid);
                    Destroy(GeneratedRooms[_roomToReplace]);
                    GeneratedRooms[_roomToReplace] = spawnRoom;
                break;
            }

        } // End of for (int state = 0; state < 3; state++)
    } // End of GenerateWorld

    /***********************************************************
     * Function: NextState
     ***********************************************************
     * Description: This function will increment the Generation
     * State enum to the next phase of generation. It will also
     * reset all of the position tracking variables back to 
     * zero. The purpose of this is to first generate the rooms
     * and then generate the lights as opposed to running the
     * processes side by side.
     ***********************************************************
     * Parameters: None
     ***********************************************************
     * Returns: None
     ***********************************************************/
     public void NextState()
     {
        // Go to next state
        currentState++;

        // Reset the variables
        currentPosX = 0;
        currentPosZ = 0;
        currentPosTracker = 0;
        currentRoom = 0;
        currentPos  = Vector3.zero;
     }

    /***********************************************************
     * Function: SpawnPlayer
     ***********************************************************
     * Description: This function will allow the player to move
     * around, and it will lock the main camera.
     ***********************************************************
     * Parameters: None
     ***********************************************************
     * Returns: None
     ***********************************************************/
     public void SpawnPlayer()
     {
        // The player object has to be inactive before we can move it
        PlayerObject.SetActive(false);

        // move the player into the spawn room
        PlayerObject.transform.position = new Vector3(spawnRoom.transform.position.x, 1.8f, spawnRoom.transform.position.z);

        PlayerObject.SetActive(true);
        MainCameraObject.SetActive(false);
     }

     /***********************************************************
     * Function: WinGame
     ***********************************************************
     * Description: This function is called from the
     * ExitRoomHandling script when the script detects that I 
     * have touched the exit room totem.
     ***********************************************************
     * Parameters: None
     ***********************************************************
     * Returns: None
     ***********************************************************/
     public void WinGame()
     {
        MainCameraObject.SetActive(true);
        PlayerObject.SetActive(false);

        // Make the cursor viewable again
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Player has exited and won the game!");
     }

     /***********************************************************
     * Function: GenerateBarrier
     ***********************************************************
     * Description: This function is used to generate the 
     * barrier that runs around the border of the world to
     * ensure that we do not run off the map. It will generate
     * the barrier at the current position that is passed in.
     ***********************************************************
     * Parameters: None
     ***********************************************************
     * Returns: None
     ***********************************************************/
     public void GenerateBarrier()
     {
        currentPos = new(currentPosX, 0, currentPosZ);

        Instantiate(B_Room, currentPos, Quaternion.identity, WorldGrid);
     }

    /**************************************************************************************
     * Functions - End
     **************************************************************************************/

}
