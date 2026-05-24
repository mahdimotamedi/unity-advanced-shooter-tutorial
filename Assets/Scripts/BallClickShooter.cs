using UnityEngine;

public class BallClickShooter : MonoBehaviour
{
    [Header("References")]
    public Camera sceneCamera;
    public Rigidbody ball;
    public Transform resetPoint;
    public BallRoomCameraOrbit cameraOrbit;

    [Header("Click And Charge")]
    public LayerMask hitLayers = ~0;
    public float fullChargeTime = 1.35f;
    public float minImpulse = 3.5f;
    public float maxImpulse = 28f;
    public float upwardBias = 0.1f;
    public float cameraDirectionBlend = 0.32f;
    public float extraSpinMultiplier = 0.45f;

    [Header("Click Assist")]
    [Tooltip("Makes the ball easier to click while it is moving. 1 means exact collider radius; higher values allow near-misses.")]
    public float clickAssistRadiusMultiplier = 1.85f;
    public float maxClickDistance = 80f;

    [Header("Safety")]
    public float maxBallSpeed = 28f;
    public float maxAngularSpeed = 45f;
    public float autoResetBelowY = -8f;

    public bool IsCharging { get; private set; }
    public float CurrentCharge01 { get; private set; }
    public Vector3 LastClickPoint { get; private set; }
    public Vector3 LastForceDirection { get; private set; }

    private float chargeStartTime;
    private Vector3 heldClickPoint;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Collider ballCollider;

    private void Awake()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;

        if (ball != null)
        {
            ballCollider = ball.GetComponent<Collider>();
            startPosition = resetPoint != null ? resetPoint.position : ball.position;
            startRotation = resetPoint != null ? resetPoint.rotation : ball.rotation;
            ball.maxAngularVelocity = maxAngularSpeed;
        }
    }

    private void Update()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (ball == null || sceneCamera == null) return;
        if (ballCollider == null) ballCollider = ball.GetComponent<Collider>();

        if (Input.GetMouseButtonDown(0))
        {
            TryBeginCharge();
        }

        if (IsCharging && Input.GetMouseButton(0))
        {
            CurrentCharge01 = Mathf.Clamp01((Time.time - chargeStartTime) / Mathf.Max(0.05f, fullChargeTime));
        }

        if (IsCharging && Input.GetMouseButtonUp(0))
        {
            ReleaseShot();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetBall();
        }

        if (ball.position.y < autoResetBelowY)
        {
            ResetBall();
        }
    }

    private void FixedUpdate()
    {
        if (ball == null) return;

        if (ball.velocity.magnitude > maxBallSpeed)
        {
            ball.velocity = ball.velocity.normalized * maxBallSpeed;
        }
    }

    private void TryBeginCharge()
    {
        Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxClickDistance, hitLayers, QueryTriggerInteraction.Ignore))
        {
            Rigidbody hitBody = hit.rigidbody != null ? hit.rigidbody : hit.collider.attachedRigidbody;
            if (hitBody == ball)
            {
                BeginChargeAt(hit.point);
                return;
            }
        }

        Vector3 assistedPoint;
        if (TryGetAssistedBallClick(ray, out assistedPoint))
        {
            BeginChargeAt(assistedPoint);
        }
    }

    private void BeginChargeAt(Vector3 clickPoint)
    {
        IsCharging = true;
        CurrentCharge01 = 0f;
        chargeStartTime = Time.time;
        heldClickPoint = clickPoint;
        LastClickPoint = clickPoint;
    }

    private bool TryGetAssistedBallClick(Ray ray, out Vector3 clickPoint)
    {
        clickPoint = Vector3.zero;
        if (ball == null) return false;

        Vector3 center = ball.worldCenterOfMass;
        float radius = GetBallRadius();
        float assistedRadius = Mathf.Max(radius, radius * clickAssistRadiusMultiplier);

        Vector3 originToCenter = ray.origin - center;
        float b = Vector3.Dot(originToCenter, ray.direction);
        float c = Vector3.Dot(originToCenter, originToCenter) - assistedRadius * assistedRadius;
        float discriminant = b * b - c;

        if (discriminant < 0f)
        {
            return false;
        }

        float sqrt = Mathf.Sqrt(discriminant);
        float t = -b - sqrt;
        if (t < 0f) t = -b + sqrt;
        if (t < 0f || t > maxClickDistance) return false;

        Vector3 pointOnAssistedSphere = ray.origin + ray.direction * t;
        Vector3 fromCenter = pointOnAssistedSphere - center;
        if (fromCenter.sqrMagnitude < 0.0001f)
        {
            fromCenter = -ray.direction;
        }

        clickPoint = center + fromCenter.normalized * radius;
        return true;
    }

    private float GetBallRadius()
    {
        if (ballCollider != null)
        {
            Vector3 extents = ballCollider.bounds.extents;
            return Mathf.Max(0.15f, Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z)));
        }

        return 0.7f;
    }

    private void ReleaseShot()
    {
        IsCharging = false;

        Vector3 center = ball.worldCenterOfMass;
        Vector3 clickInfluence = center - heldClickPoint;
        if (clickInfluence.sqrMagnitude < 0.0001f)
        {
            clickInfluence = sceneCamera.transform.forward;
        }

        Vector3 clickDirection = clickInfluence.normalized;
        Vector3 cameraDirection = sceneCamera.transform.forward.normalized;
        Vector3 forceDirection = Vector3.Slerp(clickDirection, cameraDirection, Mathf.Clamp01(cameraDirectionBlend));
        forceDirection += Vector3.up * upwardBias;
        forceDirection.y = Mathf.Max(forceDirection.y, -0.18f);
        forceDirection.Normalize();

        float impulse = Mathf.Lerp(minImpulse, maxImpulse, CurrentCharge01);
        ball.WakeUp();
        ball.AddForceAtPosition(forceDirection * impulse, heldClickPoint, ForceMode.Impulse);

        Vector3 radius = heldClickPoint - center;
        if (radius.sqrMagnitude > 0.0001f)
        {
            Vector3 torque = Vector3.Cross(radius.normalized, forceDirection) * impulse * extraSpinMultiplier;
            ball.AddTorque(torque, ForceMode.Impulse);
        }

        LastForceDirection = forceDirection;
        CurrentCharge01 = 0f;
    }

    public void ResetBall()
    {
        if (ball == null) return;

        ball.velocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;
        ball.position = resetPoint != null ? resetPoint.position : startPosition;
        ball.rotation = resetPoint != null ? resetPoint.rotation : startRotation;
        ball.Sleep();

        IsCharging = false;
        CurrentCharge01 = 0f;

        if (cameraOrbit != null)
        {
            cameraOrbit.SnapToTarget();
        }
    }
}
