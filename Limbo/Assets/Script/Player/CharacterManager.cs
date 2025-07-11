using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 10f; // Stamina drained per second when running
    public float staminaRecoveryRate = 15f; // Stamina recovered per second when not running
    public float runStaminaThreshold = 40f; // Minimum stamina required to run

    [HideInInspector]
    public bool canRun = true;

    private void Start()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        HandleStamina();
    }

    private void HandleStamina()
    {
        if (canRun && IsRunning())
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }
        else
        {
            currentStamina += staminaRecoveryRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

            if (currentStamina >= runStaminaThreshold)
            {
                canRun = true;
            }
            else
            {
                canRun = false;
            }
        }
    }

    public bool IsRunning()
    {
        return Input.GetKey(KeyCode.LeftShift) && Input.GetAxisRaw("Vertical") > 0;
    }
}
