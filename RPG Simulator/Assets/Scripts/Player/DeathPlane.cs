using UnityEngine;

public class DeathPlane : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            collision.transform.position = new Vector3(collision.transform.position.x, 10, collision.transform.position.z);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            collision.transform.position = new Vector3(collision.transform.position.x, 10, collision.transform.position.z);
        }
    }
}
