using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public float distance = 7.0f;
    public float height = 2.5f;
    public float damping = 5.0f;
    public bool followBehind = true;
    public float rotationDamping = 100.0f;

    void LateUpdate()
    {
        // Player's position
        Transform target = GameManager.PLAYER_TRANSFORM;

        // No position
        if (target == null) return;

        Vector3 wantedPosition;

        if (followBehind) {
            wantedPosition = target.TransformPoint(0, height, -distance);
        }
        else {
            wantedPosition = target.TransformPoint(0, height, distance);
        }

        transform.position = Vector3.Lerp(transform.position, wantedPosition, Time.deltaTime * damping);

        Quaternion wantedRotation = Quaternion.LookRotation(target.position - transform.position, target.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, wantedRotation, Time.deltaTime * rotationDamping);
    }
}
