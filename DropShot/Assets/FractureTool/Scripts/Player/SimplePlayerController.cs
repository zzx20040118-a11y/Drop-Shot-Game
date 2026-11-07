using UnityEngine;

namespace FractureTool
{
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float mouseSensitivity = 2f;
        public float gravity = 9.8f;
        public float interactDistance = 50f;
        [Tooltip("Left click breaks the object under the crosshair. Disabled while a weapon is in use.")]
        public bool clickToFracture = true;

        private CharacterController controller;
        private Camera playerCamera;
        private float verticalRotation = 0;
        private float verticalSpeed = 0;
        private int ignoreLookFrames = 0;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            playerCamera = GetComponentInChildren<Camera>();
            LockCursor();
        }

        void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            // Locking the cursor can report a large mouse delta on the next frames
            ignoreLookFrames = 3;
        }

        void Update()
        {
            // Movement
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            Vector3 move = (transform.right * x + transform.forward * z) * moveSpeed;

            // Gravity
            verticalSpeed = controller.isGrounded ? -1f : verticalSpeed - gravity * Time.deltaTime;
            move.y = verticalSpeed;

            controller.Move(move * Time.deltaTime);

            // Release the cursor with Escape, click again to capture it
            if (Input.GetKeyDown(KeyCode.Escape))
                Cursor.lockState = CursorLockMode.None;

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Input.GetMouseButtonDown(0))
                    LockCursor();
                return;
            }

            if (ignoreLookFrames > 0)
            {
                ignoreLookFrames--;
                return;
            }

            // Mouse look
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            verticalRotation = Mathf.Clamp(verticalRotation - mouseY, -90f, 90f);
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
            transform.Rotate(Vector3.up * mouseX);

            // Click to fracture what the camera is looking at
            if (clickToFracture && Input.GetMouseButtonDown(0))
            {
                Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
                {
                    FractureController fracture = hit.collider.GetComponent<FractureController>();
                    if (fracture != null)
                        fracture.TriggerFracture();
                }
            }
        }
    }
}
