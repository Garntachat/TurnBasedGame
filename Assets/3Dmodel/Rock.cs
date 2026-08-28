using UnityEngine;

public class UpDownMovement : MonoBehaviour
{
    public float speed = 1f;     // how fast it moves
    public float height = 0.2f;    // how high it goes

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * height;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}