/*****************************************************************
 * GFClockManager Script
 *****************************************************************
 * Author: Dylan Werelius
 *****************************************************************
 * Description:
    The purpose of this script is to act as a controller for the
    GFClock which helps control the Mannequin monsters. This 
    script is a "timer" script which counts down the seconds until
    the GFClock starts to chime. This
    script does not directly control the Mannequin monsters, but
    the actions of the Mannequin monsters are based on the enum
    MQThreatLevel (Mannequin Threat Level). This script only tells
    them when they can be active and roaming.
 *****************************************************************/
using UnityEngine;

// This enum will handle what stage the timer is at
public enum MQThreatLevel
{
    PASSIVE,
    ACTIVATING,
    AWAKENED
}

public class GFClockManager : NetworkSingleton<GFClockManager>
{
    private const float TOTAL_TIME = 30;
    private const float TIME_TO_DANGER = 10;
    private float currentTime;
    private bool timeRunning;
    private MQThreatLevel currentThreatLevel = MQThreatLevel.PASSIVE;
    private bool canBeWound = false;

    // Developer Variables
    private bool printedActivating = false;
    private bool printedAwakened = false;
    protected override void Awake()
	{
		base.Awake();
	}
    void Start()
    {
        currentTime = TOTAL_TIME;
        timeRunning = true;
        //Debug.Log("Timer Started");
    }

    // Update is called once per frame
    void Update()
    {
        // Decrement the timer by one second if the clock is running and the currentTime > -1
        currentTime = (timeRunning && currentTime > -1) ? currentTime -= Time.deltaTime : currentTime;

        // This will interpret the timer and update the level of danger based on how much time is left
        if (timeRunning && currentTime <= 0)
        {
            // At this time, the GF Clock stops chiming and the lights go out
            currentThreatLevel = MQThreatLevel.AWAKENED;
            timeRunning = false;
            
            // This is just for the sake of making sure the timer is running
            if (!printedAwakened)
            {
                Debug.Log("Monsters have Awakened");
                printedAwakened = true;
            }
            //Debug.Log("Current Time: " + currentTime);
        } 
        else if (timeRunning && currentTime <= TIME_TO_DANGER) 
        {
            // At this time, the GF Clock will start to chime and lights begin to flicker
            currentThreatLevel = MQThreatLevel.ACTIVATING;

            // This is just for the sake of making sure the timer is running
            if (!printedActivating)
            {
                Debug.Log("Monsters are Activating");
                printedActivating = true;
            }
            //Debug.Log("Current Time: " + currentTime);
        }
    }

    /*****************************************************************
    * InstantResetTimer
    *****************************************************************
    * Author: Dylan Werelius
    *****************************************************************
    * Description:
        This function will reset the timer back to the TOTAL_TIME and
        start it.
    *****************************************************************/
    private void InstantResetTimer()
    {
        currentTime = TOTAL_TIME;
        timeRunning = true;
    }

    public MQThreatLevel GetMQThreatLevel()
    {
        return currentThreatLevel;
    }

    // This will be used to rewind the timer. Note that you can only rewind the timer back to TOTAL_TIME
    private void RewindTime()
    {
        timeRunning = true;
        if (currentTime >= TOTAL_TIME)
        {
            currentTime = TOTAL_TIME;
        } else {
            currentTime = currentTime + 5;
        }
    }
}
