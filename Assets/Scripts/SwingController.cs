using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
public class SwingController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform handPoint;
    public ChainVisual dynamicChain;

    [Header("Input")]
    public KeyCode grabKey = KeyCode.E;
    public KeyCode pullInKey = KeyCode.R;
    public KeyCode letOutKey = KeyCode.F;

    [Header("Grab")]
    public float maxGrabDistance = 35f;
    public bool allowProximityGrab = true;
    public float closeGrabRadius = 5.5f;
    public float maxChainEndHeightDifference = 4.5f;
    public LayerMask swingMask = ~0;

    [Header("Swing")]
    public float minRopeLength = 3.5f;
    public float maxRopeLength = 28f;
    public float ropeAdjustSpeed = 8f;
    public float swingKick = 3.25f;
    public float releaseBoost = 0.42f;
    public float maxReleaseSpeed = 8.5f;

    [Header("Realistic Limits")]
    [Tooltip("Prevents the player from swinging over the anchor and doing full 360 loops.")]
    public float maxSwingAngleFromVertical = 68f;

    [Tooltip("Keeps the swing close to the first pendulum plane so it cannot orbit 360 degrees around the sphere.")]
    public float maxSideAngleFromGrabPlane = 38f;

    [Tooltip("Damps swing energy a little every frame for a heavier, more realistic chain feel.")]
    [Range(0.9f, 1f)] public float swingEnergyDamping = 0.988f;

    [Tooltip("Maximum external swing speed before release.")]
    public float maxSwingSpeed = 12f;

    [Tooltip("How strongly the player is corrected back onto the rope each frame.")]
    [Range(0.3f, 1f)] public float ropeCorrectionStrength = 0.82f;

    private PlayerMotor motor;
    private SwingAnchor currentAnchor;
    private float ropeLength;
    private bool isSwinging;
    private Vector3 anchorPoint;
    private Vector3 swingPlaneNormal;

    public bool IsSwinging { get { return isSwinging; } }
    public float RopeLength { get { return ropeLength; } }
    public SwingAnchor CurrentAnchor { get { return currentAnchor; } }

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();

        if (handPoint == null)
        {
            GameObject hand = new GameObject("SwingHandPoint");
            hand.transform.SetParent(transform);
            hand.transform.localPosition = new Vector3(0.2f, 1.15f, 0.35f);
            handPoint = hand.transform;
        }

        if (dynamicChain == null)
        {
            dynamicChain = GetComponent<ChainVisual>();
            if (dynamicChain == null) dynamicChain = gameObject.AddComponent<ChainVisual>();
        }
    }

    private void Update()
    {
        if (!isSwinging && Input.GetKey(grabKey))
        {
            TryGrabSwingAnchor();
        }

        if (Input.GetKeyUp(grabKey) && isSwinging)
        {
            Release(false);
        }

        if (!isSwinging)
        {
            if (dynamicChain != null) dynamicChain.SetChain(Vector3.zero, Vector3.zero, false);
            return;
        }

        if (currentAnchor == null)
        {
            Release(false);
            return;
        }

        anchorPoint = currentAnchor.PivotPosition;

        if (Input.GetKey(pullInKey))
        {
            ropeLength = Mathf.Max(minRopeLength, ropeLength - ropeAdjustSpeed * Time.deltaTime);
        }

        if (Input.GetKey(letOutKey))
        {
            ropeLength = Mathf.Min(maxRopeLength, ropeLength + ropeAdjustSpeed * Time.deltaTime);
        }

        if (Input.GetButtonDown("Jump"))
        {
            Release(true);
            return;
        }

        Vector3 toPlayer = transform.position - anchorPoint;
        Vector3 ropeDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector3.down;
        Vector3 forwardTangent = Vector3.ProjectOnPlane(playerCamera.transform.forward, ropeDir).normalized;
        if (forwardTangent.sqrMagnitude > 0.01f)
        {
            float forward = Input.GetAxisRaw("Vertical");
            motor.AddExternalVelocity(forwardTangent * forward * swingKick * Time.deltaTime);
        }

        motor.ExternalVelocity = Vector3.ClampMagnitude(motor.ExternalVelocity * Mathf.Pow(swingEnergyDamping, Time.deltaTime * 60f), maxSwingSpeed);

        if (dynamicChain != null)
        {
            dynamicChain.SetChain(anchorPoint, handPoint.position, true);
        }
    }

    private void LateUpdate()
    {
        if (!isSwinging || currentAnchor == null) return;
        EnforceRopeConstraint();
    }

    private bool TryGrabSwingAnchor()
    {
        SwingAnchor anchor = FindAimedAnchor();
        if (anchor == null && allowProximityGrab)
        {
            anchor = FindClosestNearbyAnchor();
        }

        if (anchor == null) return false;

        currentAnchor = anchor;
        anchorPoint = currentAnchor.PivotPosition;
        ropeLength = Mathf.Clamp(Vector3.Distance(transform.position, anchorPoint), minRopeLength, maxRopeLength);
        isSwinging = true;
        motor.IsSwinging = true;
        motor.StopVerticalFall();
        currentAnchor.HideStaticChain();
        BuildSwingPlane();

        Vector3 firstKick = Vector3.ProjectOnPlane(playerCamera.transform.forward, transform.position - anchorPoint).normalized;
        if (firstKick.sqrMagnitude > 0.01f)
        {
            motor.AddExternalVelocity(firstKick * 0.85f);
        }

        return true;
    }

    private void BuildSwingPlane()
    {
        Vector3 flatForward = playerCamera != null ? Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up) : Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.01f)
        {
            flatForward = Vector3.ProjectOnPlane(transform.position - anchorPoint, Vector3.up);
        }
        if (flatForward.sqrMagnitude < 0.01f)
        {
            flatForward = transform.forward;
        }

        flatForward.Normalize();
        swingPlaneNormal = Vector3.Cross(Vector3.down, flatForward).normalized;
        if (swingPlaneNormal.sqrMagnitude < 0.01f)
        {
            swingPlaneNormal = transform.right;
        }
    }

    private SwingAnchor FindAimedAnchor()
    {
        if (playerCamera == null) return null;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, swingMask, QueryTriggerInteraction.Collide))
        {
            return null;
        }

        return hit.collider.GetComponentInParent<SwingAnchor>();
    }

    private SwingAnchor FindClosestNearbyAnchor()
    {
        SwingAnchor[] anchors = FindObjectsOfType<SwingAnchor>();
        SwingAnchor best = null;
        float bestDistance = float.MaxValue;
        Vector3 playerPosition = transform.position;

        for (int i = 0; i < anchors.Length; i++)
        {
            SwingAnchor anchor = anchors[i];
            if (anchor == null) continue;

            Vector3 grabPoint = anchor.HangingEndPosition;
            float heightDifference = Mathf.Abs(playerPosition.y - grabPoint.y);
            float distance = Vector3.Distance(playerPosition, grabPoint);

            if (distance <= closeGrabRadius && heightDifference <= maxChainEndHeightDifference && distance < bestDistance)
            {
                best = anchor;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void EnforceRopeConstraint()
    {
        anchorPoint = currentAnchor.PivotPosition;
        Vector3 toPlayer = transform.position - anchorPoint;
        float distance = toPlayer.magnitude;
        if (distance < 0.001f) return;

        Vector3 dir = toPlayer / distance;
        dir = ClampDirectionToRealisticSwing(dir);

        Vector3 targetPosition = anchorPoint + dir * ropeLength;
        Vector3 correction = (targetPosition - transform.position) * ropeCorrectionStrength;
        motor.MoveCorrection(correction);

        Vector3 velocity = motor.ExternalVelocity;

        // Remove radial velocity so the rope behaves taut instead of stretching.
        velocity -= Vector3.Project(velocity, dir);

        // If the player pushes into the side limit, remove only that outward side velocity.
        if (swingPlaneNormal.sqrMagnitude > 0.01f)
        {
            float sideDot = Vector3.Dot(dir, swingPlaneNormal);
            float maxSideSin = Mathf.Sin(maxSideAngleFromGrabPlane * Mathf.Deg2Rad);
            if (Mathf.Abs(sideDot) > maxSideSin * 0.96f)
            {
                float sideVelocity = Vector3.Dot(velocity, swingPlaneNormal) * Mathf.Sign(sideDot);
                if (sideVelocity > 0f)
                {
                    velocity -= swingPlaneNormal * Vector3.Dot(velocity, swingPlaneNormal);
                }
            }
        }

        motor.ExternalVelocity = Vector3.ClampMagnitude(velocity, maxSwingSpeed);
    }

    private Vector3 ClampDirectionToRealisticSwing(Vector3 dir)
    {
        // Limit how high the rope can rise. This prevents over-the-top 360 loops.
        float angleFromDown = Vector3.Angle(Vector3.down, dir);
        if (angleFromDown > maxSwingAngleFromVertical)
        {
            Vector3 axis = Vector3.Cross(Vector3.down, dir);
            if (axis.sqrMagnitude < 0.001f) axis = transform.right;
            dir = Quaternion.AngleAxis(maxSwingAngleFromVertical, axis.normalized) * Vector3.down;
        }

        // Limit orbiting around the anchor by keeping the player near the first vertical swing plane.
        if (swingPlaneNormal.sqrMagnitude > 0.01f)
        {
            Vector3 planarDir = Vector3.ProjectOnPlane(dir, swingPlaneNormal);
            if (planarDir.sqrMagnitude > 0.001f)
            {
                planarDir.Normalize();
                float sideDot = Vector3.Dot(dir, swingPlaneNormal);
                float maxSideSin = Mathf.Sin(maxSideAngleFromGrabPlane * Mathf.Deg2Rad);

                if (Mathf.Abs(sideDot) > maxSideSin)
                {
                    float sign = Mathf.Sign(sideDot);
                    float side = maxSideSin * sign;
                    float planar = Mathf.Sqrt(1f - maxSideSin * maxSideSin);
                    dir = (planarDir * planar + swingPlaneNormal.normalized * side).normalized;
                }
            }
        }

        return dir.normalized;
    }

    private void Release(bool boosted)
    {
        Vector3 releaseVelocity = motor.ExternalVelocity;

        if (boosted && releaseVelocity.sqrMagnitude > 0.25f)
        {
            Vector3 boostDirection = playerCamera != null ? playerCamera.transform.forward : transform.forward;
            releaseVelocity += boostDirection.normalized * releaseBoost;
            releaseVelocity += Vector3.up * releaseBoost * 0.15f;
        }

        motor.ExternalVelocity = Vector3.ClampMagnitude(releaseVelocity, maxReleaseSpeed);

        isSwinging = false;
        motor.IsSwinging = false;

        if (currentAnchor != null)
        {
            currentAnchor.ShowStaticChain();
        }

        currentAnchor = null;

        if (dynamicChain != null) dynamicChain.SetChain(Vector3.zero, Vector3.zero, false);
    }
}
