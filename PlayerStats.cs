using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PlayerStats : MonoBehaviour
{
    PlayerInput playerInput;
    InputAction sprintAction;

    public UnityEngine.UI.Image staminaBar;
    public UnityEngine.UI.Image hungerBar;
    public UnityEngine.UI.Image depletedStaminaBar;

    /*
     * Hunger will act as a temporary buff, and so should be its own stat bar essentially
     * Stamina will be the total amount of spriting that the player can do, dependent upon other stats
    */

    /* The limits for stamina and hunger */
    public float maxStamina = 100.0f; // The cap on the stamina bar
    public float minStamina = 20.0f; // The lowest that the stamina drain can get to

    public float maxHunger = 50.0f; // The limit that the buff bar can grow to
    public float minHunger = 0.0f; // The lower end to the buff bar, which is to say that you have no buff

    /* Stats that will actually change */
    public float currentHunger = 0.0f; // Acts as a bonus sprint bar
    public float currentStamina = 100.0f; // Acts as the current limit to stamina
    public float sprintGauge = 100.0f; // The actual sprint meter

    /* Various time and rates used for modifying values */
    public float decayAmount = 5.0f; // Amount that the stamina will drain by
    const float DefDecayTime = 60.0f;
    public float decayTime = DefDecayTime; // Time before stamina drains (default of 60 seconds)

    public float sprintDrainRate = 10f; // Drain rate while sprinting
    public float sprintRecoverRate = 20f; // Rate that sprint gauge recovers
    const float DefSprintCooldownTime = 3.0f;
    public float sprintCooldownTime = DefSprintCooldownTime; // Time the player has to wait before they can sprint after draining
    const float DefRecoveryCooldown = 2.0f;
    public float recoveryCooldownTime = DefRecoveryCooldown; // Time buffer before the sprint gauge starts recovering

    public bool outOfStamina = false; // Bool for if the player can no longer sprint


    /*      TO-DO List
     *  Optimize the UI updater so that it only triggers when change is actually needed
     *  Look into the bug where the stamina bar will "jump" when the hunger bar gets updated
     *  Add a stamina cost to jumping
     *  Replace all "GetComponent" with public elements
     * 
     * 
    */

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInput = GetComponent<PlayerInput>(); // Initializes player input
        sprintAction = playerInput.actions.FindAction("Sprint"); // Initializes player sprint
        sprintGauge = currentStamina + currentHunger; // Initializes the player's current available sprint

        DisplayUI(); // Loads the UI at default values
    }

    // Update is called once per frame
    void Update()
    {
        StaminaDecay();
        SprintHandler();
    }

    // Drains the stamina bar by a set amount after an arbitrary amount of time
    void StaminaDecay()
    {
        if (decayTime <= 0.0f) // If the timer has ended, perform code
        {
            currentStamina -= decayAmount; // Removes a set amount from the stamina

            currentStamina = Mathf.Clamp(currentStamina, minStamina, maxStamina); // Performs a clamp on the value to prevent going under/over limits
            sprintGauge = Mathf.Clamp(sprintGauge, 0f, currentStamina + currentHunger); // Clamps the sprint gauge to the new currentStamina, plus currentHunger to account for it

            decayTime = 60.0f; // Resets the drain timer

            UpdateUI(); // Updates the UI since there has been a change
        }
        else // Takes time off the timer otherwise
        {
            decayTime -= Time.deltaTime;
        }
    }

    // Handles all of the sprinting logic
    void SprintHandler()
    {
        /* The sprint handler function should control sprint recovery, sprint cooldown, and sprint drain
         * Sprint Drain should only occur when the player is off-cooldown and attempting to sprint
         * Sprint Cooldown should occur only when the player is still 'out of stamina', but independent of when the player is attempting to sprint
         * Sprint Recovery should happen anytime the sprint gauge is less than currentStamina, subject to its own cooldown as well
        */

        float isSprinting = sprintAction.ReadValue<float>(); // Value is 1.0 if the player is sprinting


        if (outOfStamina) { SprintDepleted(); } // Called to handle the runoff of the timer

        if ((isSprinting == 1) && (!outOfStamina)) { SprintDrain(); } // Called when the player is trying to and able to sprint

        if ((recoveryCooldownTime <= 0f) && (sprintGauge < (currentStamina + currentHunger))) // It has been long enough since the player last sprinted and the gauge is not full
        {
            SprintRecovery();
        } 
        else if (recoveryCooldownTime > 0f) // Takes time off the timer
        { 
            SprintRecoveryCooldown();
        }

        UpdateUI(); // Updates the UI
    }

    // Drains the sprint gauge and the hunger bar if necessary
    void SprintDrain()
    {
        recoveryCooldownTime = DefRecoveryCooldown; // Resets the recovery cooldown as the player has now sprinted

        // We first need to check for if we are running on "reserve" stamina, with the hunger bar
        if (sprintGauge <= currentHunger) // If the sprint gauge drops below the hunger amount, then we start draining from the hunger
        {
            sprintGauge -= sprintDrainRate * Time.deltaTime; // Drains the sprint bar
            currentHunger = sprintGauge; // Sets the currentHunger to sprintGauge to drain it

            currentHunger = Mathf.Clamp(currentHunger, minHunger, maxHunger); // Clamps the hunger bar
            sprintGauge = Mathf.Clamp(sprintGauge, 0f, currentStamina + currentHunger); // Clamps the sprint gauge
        }
        else // Otherwise we just drain from the sprint gauge as normal
        {
            sprintGauge -= sprintDrainRate * Time.deltaTime; // Drains the sprint bar
            sprintGauge = Mathf.Clamp(sprintGauge, 0f, currentStamina + currentHunger); // Clamps the sprint gauge
        }

        if (sprintGauge <= 0f) { outOfStamina = true; } // Checks if the player ran out of stamina while sprinting
    }

    // Recovers any missing stamina on the sprint gauge, if off cooldown
    void SprintRecovery()
    {
        sprintGauge += sprintRecoverRate * Time.deltaTime; // Recovers a set amount of sprint
        sprintGauge = Mathf.Clamp(sprintGauge, 0f, currentStamina + currentHunger); // Clamps it to the bounds to prevent overflow
    }

    // Takes time off the timer for if the player has sprinted recently
    void SprintRecoveryCooldown()
    {
        recoveryCooldownTime -= 1.0f * Time.deltaTime; // Takes time off the cooldown timer
        recoveryCooldownTime = Mathf.Clamp(recoveryCooldownTime, 0f, 60f); // Clamps to prevent negatives
    }

    // Takes time off the timer for if the player depeleted all stamina, and handles reseting player sprint ability
    void SprintDepleted()
    {
        sprintCooldownTime -= 1.0f * Time.deltaTime; // Takes time off the cooldown timer

        if (sprintCooldownTime <= 0f) // Checks if the timer is up
        {
            sprintCooldownTime = DefSprintCooldownTime; // Resets the timer
            outOfStamina = false; // Lets the player sprint again
        }
    }

    // Loads and displays the player UI on startup
    void DisplayUI()
    {
        UnityEngine.Debug.Log("--Initializing UI Display--\n");
    }

    // Constantly updates the UI elements for player stats
    void UpdateUI()
    {
        staminaBar.fillAmount = ((sprintGauge - currentHunger) / maxStamina); // Sets the bar at the ratio of current stamina to total stamina
        hungerBar.fillAmount = (currentHunger / maxHunger); // Sets the hunger bar proportional to remaining stamina
        depletedStaminaBar.fillAmount = (maxStamina - currentStamina) / 100; // Sets the depleted stamina bar to the missing stamina amount
    }
}
