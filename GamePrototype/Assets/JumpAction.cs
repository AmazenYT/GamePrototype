using UnityEngine;
using UnityEngine.InputSystem;

public class JumpAction : MonoBehaviour
{
    public float jumpForce;
    [SerializeField] PlayerPhysics playerPhysics;
    Rigidbody RB => playerPhysics.RB;
    PlayerPhysics.GroundInfo groundInfo => playerPhysics.groundInfo;

    public void OnJump(InputAction.CallbackContext callbackContext)
    {
        if (callbackContext.performed)
        {
            Debug.Log("OnJump: performed");
            Jump();
        }
    }

    //Jump
    void Jump()
    {
        if (!playerPhysics.groundInfo.ground) return;
        RB.linearVelocity = (Vector3.up * jumpForce) + playerPhysics.horizontalVelocity;

    }
}
