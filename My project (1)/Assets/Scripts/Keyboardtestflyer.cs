/* KeyboardTestFlyer
   TEMPORARY development-only script for moving/aiming CC_HUB with a
   keyboard when no gamepad is connected. Safe to leave alongside
   DPadFlyer - that script already no-ops when Gamepad.current is null,
   so there's no conflict. Remove or disable this once you have a
   controller hooked up, or just leave it as a permanent keyboard fallback.

   Attach to CC_HUB (same object as DPadFlyer).

   Controls:
   W/A/S/D - move forward/back/strafe left/right
   Q/E     - move down/up
   Arrow Left/Right - yaw
   Arrow Up/Down    - pitch
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardTestFlyer : MonoBehaviour
{
    [Tooltip("Reference direction for movement (which way is 'forward'). Defaults to this object's own transform if left empty. Assign CC_HEAD for head-relative movement.")]
    public Transform directionReference;

    [Tooltip("Move speed in units per second.")]
    public float moveSpeed = 5f;

    [Tooltip("Rotation speed in degrees per second.")]
    public float rotateSpeed = 60f;

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        Transform dir = directionReference != null ? directionReference : transform;

        // Movement
        Vector3 move = Vector3.zero;
        if (kb.wKey.isPressed) move += dir.forward;
        if (kb.sKey.isPressed) move -= dir.forward;
        if (kb.aKey.isPressed) move -= dir.right;
        if (kb.dKey.isPressed) move += dir.right;
        if (kb.qKey.isPressed) move -= dir.up;
        if (kb.eKey.isPressed) move += dir.up;

        if (move.sqrMagnitude > 0f)
        {
            transform.position += move.normalized * moveSpeed * Time.deltaTime;
        }

        // Rotation (yaw around world up, pitch around dir's right axis)
        float yaw = 0f;
        if (kb.rightArrowKey.isPressed) yaw += 1f;
        if (kb.leftArrowKey.isPressed) yaw -= 1f;

        float pitch = 0f;
        if (kb.upArrowKey.isPressed) pitch -= 1f;
        if (kb.downArrowKey.isPressed) pitch += 1f;

        if (yaw != 0f) transform.Rotate(Vector3.up, yaw * rotateSpeed * Time.deltaTime, Space.World);
        if (pitch != 0f) transform.Rotate(dir.right, pitch * rotateSpeed * Time.deltaTime, Space.World);
    }
}

// /* KeyboardTestFlyer
//    TEMPORARY development-only script for aiming/positioning CC_HUB with a
//    keyboard when no gamepad is connected. Safe to leave alongside
//    DPadFlyer - that script already no-ops when Gamepad.current is null,
//    so there's no conflict.

//    Movement is intentionally restricted to the world X axis only (parallel
//    to the display wall) to preserve the fixed viewer-to-screen depth the
//    stereoscopic effect depends on. Look-around (pitch/yaw) is free since
//    rotating in place doesn't change your depth from the wall.

//    Attach to CC_HUB (same object as DPadFlyer).

//    Controls:
//    A/D - move left/right along world X only
//    Arrow Left/Right - yaw
//    Arrow Up/Down    - pitch
// */

// using UnityEngine;
// using UnityEngine.InputSystem;

// public class KeyboardTestFlyer : MonoBehaviour
// {
//     [Tooltip("Reference direction for pitch rotation axis. Defaults to this object's own transform if left empty. Assign CC_HEAD if you want pitch to rotate about the head's current right axis.")]
//     public Transform directionReference;

//     [Tooltip("Move speed in units per second, along world X only.")]
//     public float moveSpeed = 5f;

//     [Tooltip("Rotation speed in degrees per second.")]
//     public float rotateSpeed = 60f;

//     void Update()
//     {
//         Keyboard kb = Keyboard.current;
//         if (kb == null) return;

//         Transform dir = directionReference != null ? directionReference : transform;

//         // Movement - world X axis only, regardless of current facing direction.
//         float xMove = 0f;
//         if (kb.aKey.isPressed) xMove -= 1f;
//         if (kb.dKey.isPressed) xMove += 1f;

//         if (xMove != 0f)
//         {
//             transform.position += Vector3.right * xMove * moveSpeed * Time.deltaTime;
//         }

//         // Rotation - free look, does not affect position.
//         float yaw = 0f;
//         if (kb.rightArrowKey.isPressed) yaw += 1f;
//         if (kb.leftArrowKey.isPressed) yaw -= 1f;

//         float pitch = 0f;
//         if (kb.upArrowKey.isPressed) pitch -= 1f;
//         if (kb.downArrowKey.isPressed) pitch += 1f;

//         if (yaw != 0f) transform.Rotate(Vector3.up, yaw * rotateSpeed * Time.deltaTime, Space.World);
//         if (pitch != 0f) transform.Rotate(dir.right, pitch * rotateSpeed * Time.deltaTime, Space.World);
//     }
// }