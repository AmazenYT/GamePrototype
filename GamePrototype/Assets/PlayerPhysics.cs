using System;
using System.Collections;
using System.Drawing;
using UnityEditor.ShaderGraph;
using UnityEngine;

public class PlayerPhysics : MonoBehaviour
{
    //Variables
    public Rigidbody RB;
    public LayerMask layerMask;
    public Vector3 verticalVelocity => Vector3.Project(RB.linearVelocity, RB.transform.up);
    public Vector3 horizontalVelocity => Vector3.ProjectOnPlane(RB.linearVelocity, RB.transform.up);
    public float gravity;
    
    public float groundDistance;
    public float speed;
    public float verticalSpeed => Vector3.Dot(RB.linearVelocity, RB.transform.up);
    
    //Update
    

    //Fixed Update
    void FixedUpdate()
    {
        Move();
        
    if (!groundInfo.ground)
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

    public struct GroundInfo
    {
        public Vector3 point;
        public Vector3 normal;
        public bool ground;
    }

    [HideInInspector] public GroundInfo groundInfo;

    //Ground Detection
    void Ground()
    {
        float maxDistance = Mathf.Max(RB.centerOfMass.y, 0) + (RB.sleepThreshold * Time.fixedDeltaTime);
        if (groundInfo.ground && verticalSpeed < RB.sleepThreshold) maxDistance += groundDistance;
        groundInfo.ground = Physics.Raycast(RB.worldCenterOfMass, -RB.transform.up, out RaycastHit hit, groundDistance, layerMask, QueryTriggerInteraction.Ignore);
        groundInfo.point = groundInfo.ground ? hit.point : RB.transform.position;
        groundInfo.normal = groundInfo.ground ? hit.normal : Vector3.up;

        groundInfo = new PlayerPhysics.GroundInfo()
        {
            point = groundInfo.point,
            normal = groundInfo.normal,
            ground = groundInfo.ground
        };
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
        transform.up = groundInfo.normal;
        Vector3 goal = groundInfo.point;
        Vector3 difference = goal - RB.transform.position;

    if (RB.SweepTest(difference, out _, difference.magnitude, QueryTriggerInteraction.Ignore)) return;
        RB.transform.position = goal;
    }
}
