using UnityEngine;
using UnityEngine.InputSystem;

// Gates the whole game behind a "press A to start" instructions screen.
// While the game hasn't started yet, movement, the hotbar, and all other
// piece interaction are disabled - DPadFlyer, KeyboardTestFlyer, and
// PlaceableObjectController each check HasStarted at the top of their own
// Update()/FixedUpdate() and simply do nothing until it becomes true.
//
// Attach this to any object in the scene (once), and assign the
// instructions box/text as instructionsPanel.
public class GameStartGate : MonoBehaviour
{
    // other scripts read this directly, e.g. "if (!GameStartGate.HasStarted) return;"
    public static bool HasStarted { get; private set; } = false;

    [Tooltip("The instructions box/text shown before the game starts. Gets hidden the moment A is pressed.")]
    public GameObject instructionsPanel;

    void Awake()
    {
        // always begin in the not-started state, showing instructions,
        // regardless of whatever HasStarted happened to be left at from
        // a previous play session in the Editor
        HasStarted = false;

        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);
        }
    }

    void Update()
    {
        if (HasStarted)
        {
            return;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad == null)
        {
            return;
        }

        if (gamepad.buttonSouth.wasPressedThisFrame)
        {
            HasStarted = true;

            if (instructionsPanel != null)
            {
                instructionsPanel.SetActive(false);
            }
        }
    }
}