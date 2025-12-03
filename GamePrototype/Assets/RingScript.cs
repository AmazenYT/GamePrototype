using UnityEngine;

public class RingScript : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0f, 180f, 0f);

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
