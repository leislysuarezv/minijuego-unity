using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    public float minX, maxX;

    public bool followPlayer = true;

    void LateUpdate()
    {
        if (!followPlayer || player == null) return;

        float x = Mathf.Clamp(player.position.x, minX, maxX);

        // 👇 mantenemos Y FIJO
        transform.position = new Vector3(x, transform.position.y, -10);
    }
}


