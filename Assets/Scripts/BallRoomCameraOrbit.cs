using UnityEngine;

public class BallRoomCameraOrbit : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float followHeight = 0.35f;

    [Header("Orbit")]
    public float distance = 7.2f;
    public float minDistance = 4.5f;
    public float maxDistance = 8.5f;
    public float yaw = 0f;
    public float pitch = 18f;
    public float minPitch = 10f;
    public float maxPitch = 48f;
    public float orbitSensitivity = 3.2f;
    public float zoomSensitivity = 3.5f;

    [Header("Room Camera Limits")]
    [Tooltip("Keeps the camera inside the room so walls never block the ball at Play start.")]
    public bool keepInsideRoom = true;
    public float roomHalfSize = 8.15f;
    public float minCameraHeight = 1.25f;
    public float maxCameraHeight = 6.35f;

    [Header("Smoothing")]
    public bool useSmoothing = true;
    public float positionSharpness = 18f;
    public float rotationSharpness = 22f;

    private bool hasSnappedOnce;

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * orbitSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSensitivity, minDistance, maxDistance);
        }

        ApplyCameraTransform(false);
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        ApplyCameraTransform(true);
        hasSnappedOnce = true;
    }

    private void ApplyCameraTransform(bool instant)
    {
        Vector3 focus = target.position + Vector3.up * followHeight;
        Vector3 desiredPosition = CalculateDesiredPosition(focus);
        Quaternion desiredRotation = Quaternion.LookRotation(focus - desiredPosition, Vector3.up);

        if (instant || !useSmoothing || !hasSnappedOnce)
        {
            transform.position = desiredPosition;
            transform.rotation = desiredRotation;
        }
        else
        {
            float positionLerp = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            float rotationLerp = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionLerp);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationLerp);
        }
    }

    private Vector3 CalculateDesiredPosition(Vector3 focus)
    {
        float yawRad = yaw * Mathf.Deg2Rad;
        float pitchRad = pitch * Mathf.Deg2Rad;

        Vector3 horizontalDirection = new Vector3(Mathf.Sin(yawRad), 0f, -Mathf.Cos(yawRad));
        Vector3 offset = horizontalDirection * (Mathf.Cos(pitchRad) * distance);
        offset += Vector3.up * (Mathf.Sin(pitchRad) * distance);

        Vector3 desiredPosition = focus + offset;

        if (keepInsideRoom)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, -roomHalfSize, roomHalfSize);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, -roomHalfSize, roomHalfSize);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minCameraHeight, maxCameraHeight);
        }

        return desiredPosition;
    }
}
