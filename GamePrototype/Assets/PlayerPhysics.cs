using System;
using System.Collections;
using UnityEngine;

public class PlayerPhysics : MonoBehaviour
{
    //Variables
    public Rigidbody RB;
    public LayerMask layerMask;
    public Vector3 verticalVelocity => Vector3.Project(RB.linearVelocity, RB.transform.up);
    public Vector3 horizontalVelocity => Vector3.ProjectOnPlane(RB.linearVelocity, RB.transform.up);
    Vector3 normal;
    Vector3 point;
    public float gravity;
    public float jumpForce;
    public float groundDistance;
    public float speed;
    public float verticalSpeed => Vector3.Dot(RB.linearVelocity, RB.transform.up);
    bool ground;
    //Update
    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        Jump();
    }

    //Fixed Update
    void FixedUpdate()
    {
        Move();
        
    if (!ground)
        Gravity();
    
    

        StartCoroutine(LateFixedUpdateRoutine());

        IEnumerator LateFixedUpdateRoutine()
        {
            yield return new WaitForFixedUpdate();
            LateFixedUpdate();
        }
    }

    //Late Fixed Update
    void LateFixedUpdate()
    {
        Ground();
        Snap();

        
    }

    //Gravity
    void Gravity()
    {
        RB.linearVelocity -= Vector3.up * gravity * Time.deltaTime; 
    }

    //Jump
    void Jump()
    {
        if (!ground) return;
        RB.linearVelocity = (Vector3.up * jumpForce) + horizontalVelocity;

    }

    //Ground Detection
    void Ground()
    {
        float maxDistance = Mathf.Max(RB.centerOfMass.y, 0) + (RB.sleepThreshold * Time.fixedDeltaTime);
        if (ground && verticalSpeed < RB.sleepThreshold) maxDistance += groundDistance;
        ground = Physics.Raycast(RB.worldCenterOfMass, -RB.transform.up, out RaycastHit hit, groundDistance, layerMask, QueryTriggerInteraction.Ignore);
        point = ground ? hit.point : RB.transform.position;
        normal = ground ? hit.normal : Vector3.up;
    }

    //Movement
    void Move()
    {
        RB.linearVelocity = (Vector3.right * Input.GetAxis("Horizontal") * speed) + (Vector3.forward * Input.GetAxis("Vertical") * speed)
        + verticalVelocity;
    }

    //Snap to Ground
    void Snap()
    {
        transform.up = normal;
        Vector3 goal = point;
        Vector3 difference = goal - RB.transform.position;

    if (RB.SweepTest(difference, out _, difference.magnitude, QueryTriggerInteraction.Ignore)) return;
        RB.transform.position = goal;
    }
}
