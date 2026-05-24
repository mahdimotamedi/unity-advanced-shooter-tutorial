using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot;
    public Camera playerCamera;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2.2f;
    public float minPitch = -70f;
    public float maxPitch = 80f;

    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintMultiplier = 1.55f;
    public float jumpHeight = 1.45f;
    public float gravity = -24f;
    public float groundedExternalDamping = 13.5f;
    public float airExternalDamping = 0.12f;
    public float swingAirControl = 16f;

    [Header("Landing Stability")]
    [Tooltip("Small downward value that keeps the CharacterController grounded without bouncing.")]
    public float groundedStickVelocity = -2.5f;

    [Tooltip("External slide speed is capped on the ground to stop landing wobble.")]
    public float groundedExternalMaxSpeed = 5.5f;

    [Tooltip("Small leftover landing velocities below this value are snapped to zero.")]
    public float groundSettleThreshold = 0.18f;

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private Vector3 externalVelocity;
    private float pitch;

    public bool IsSwinging { get; set; }
    public bool IsGrounded { get; private set; }
    public Vector3 PlanarInputWorld { get; private set; }
    public Vector3 ExternalVelocity
    {
        get { return externalVelocity; }
        set { externalVelocity = value; }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraPivot == null)
        {
            GameObject pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(transform);
            pivot.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            cameraPivot = pivot.transform;
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX, Space.World);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        bool groundedBeforeMove = controller.isGrounded;
        IsGrounded = groundedBeforeMove;

        if (groundedBeforeMove && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = groundedStickVelocity;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 input = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        PlanarInputWorld = input;

        float speed = walkSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
        Vector3 desiredPlanarVelocity = input * speed;

        if (Input.GetButtonDown("Jump") && groundedBeforeMove && !IsSwinging)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        if (IsSwinging)
        {
            // While swinging, WASD gives controlled pumping instead of instantly overriding the rope physics.
            externalVelocity += desiredPlanarVelocity * swingAirControl * Time.deltaTime;
            desiredPlanarVelocity = Vector3.zero;
        }
        else
        {
            float damping = groundedBeforeMove ? groundedExternalDamping : airExternalDamping;
            externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, damping * Time.deltaTime);

            if (groundedBeforeMove)
            {
                CleanGroundExternalVelocity();
            }
        }

        verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 totalVelocity = desiredPlanarVelocity + verticalVelocity + externalVelocity;
        CollisionFlags flags = controller.Move(totalVelocity * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity.y > 0f)
        {
            verticalVelocity.y = 0f;
        }

        bool groundedAfterMove = (flags & CollisionFlags.Below) != 0 || controller.isGrounded;
        IsGrounded = groundedAfterMove;

        if (groundedAfterMove && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = groundedStickVelocity;
        }

        if (groundedAfterMove && !IsSwinging)
        {
            CleanGroundExternalVelocity();
        }
    }

    private void CleanGroundExternalVelocity()
    {
        externalVelocity = Vector3.ProjectOnPlane(externalVelocity, Vector3.up);
        externalVelocity = Vector3.ClampMagnitude(externalVelocity, groundedExternalMaxSpeed);

        if (externalVelocity.sqrMagnitude < groundSettleThreshold * groundSettleThreshold)
        {
            externalVelocity = Vector3.zero;
        }
    }

    public void AddExternalVelocity(Vector3 velocityDelta)
    {
        externalVelocity += velocityDelta;
    }

    public void MoveCorrection(Vector3 correction)
    {
        if (correction.sqrMagnitude > 0.000001f)
        {
            controller.Move(correction);
        }
    }

    public void StopVerticalFall()
    {
        if (verticalVelocity.y < 0f)
        {
            verticalVelocity.y = 0f;
        }
    }
}
