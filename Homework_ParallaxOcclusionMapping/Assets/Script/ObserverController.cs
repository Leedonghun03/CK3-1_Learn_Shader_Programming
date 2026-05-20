using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObserverController : MonoBehaviour
{
    [Header ("플레이어 속도 값")]
    [SerializeField] private float MoveSpeed = 1.0f;
    [SerializeField] private float LookSpeed = 20.0f;
    [SerializeField] private float VerticalSpeed = 3.0f;

    [Header ("플레이어 들어온 입력 값")]
    [SerializeField] private Vector2 Movement;
    [SerializeField] private Vector2 LookInput;

    [SerializeField] private float Pitch = 0.0f;
    [SerializeField] private float Yaw = 0.0f;

    [Header("마우스 잠금 상태")]
    [SerializeField] private bool IsCursorLocked = true;

    void Start()
    {
        Movement  = Vector2.zero;
        LookInput = Vector2.zero;

        LockCursor(true);
    }

    void Update()
    {
        ToggleCursor();

        ProcessTranslation();
        ProcessRotation();
        ProcessVerticalMovement();
    }

    public void OnMove(InputValue Value)
    {
        Movement = Value.Get<Vector2>();
    }

    public void OnLook(InputValue Value)
    {
        LookInput = Value.Get<Vector2>();
    }

    private void ToggleCursor()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            IsCursorLocked = !IsCursorLocked;
            LockCursor(IsCursorLocked);
        }
    }

    private void LockCursor(bool Lock)
    {
        if (Lock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void ProcessTranslation()
    {
        Vector3 Move = new Vector3(Movement.x, 0.0f, Movement.y);
        transform.Translate(Move * MoveSpeed * Time.deltaTime);
    }

    private void ProcessVerticalMovement()
    {
        float Vertical = 0.0f;

        if (Keyboard.current.qKey.isPressed)
        {
            Vertical += 1.0f;
        }

        if (Keyboard.current.eKey.isPressed)
        {
            Vertical -= 1.0f;
        }

        Vector3 MoveY = new Vector3(0.0f, Vertical, 0.0f);

        transform.Translate(MoveY * VerticalSpeed * Time.deltaTime, Space.World);
    }

    private void ProcessRotation()
    {
        if (!IsCursorLocked)
            return;

        Yaw += LookInput.x * LookSpeed * Time.deltaTime;
        Pitch -= LookInput.y * LookSpeed * Time.deltaTime;

        Pitch = Mathf.Clamp(Pitch, -80.0f, 80.0f);

        transform.rotation = Quaternion.Euler(Pitch, Yaw, 0f);
    }
}
