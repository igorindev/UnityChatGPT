using UnityEngine;

public class MoveObject : MonoBehaviour
{
    void Update()
    {
        GetComponent<Rigidbody>().AddForce(new Vector3(0, 5, 0) * 10f);
    }
}
