
namespace ResourceViewer
{
    using IngameDebugConsole;
    using UnityEngine;
    using UnityEngine.InputSystem;

    /// <summary>
    /// A simple free camera to be added to a Unity game object.
    ///
    /// Keys:
    ///	wasd / arrows	- movement
    ///	q/e 			- up/down (local space)
    ///	r/f 			- up/down (world space)
    ///	pageup/pagedown	- up/down (world space)
    ///	hold shift		- enable fast movement mode
    ///	right mouse  	- enable free look
    ///	mouse			- free look / rotation
    ///
    /// </summary>
    public class FreeCameraController : MonoBehaviour
    {
        /// <summary>
        /// Normal speed of camera movement.
        /// </summary>
        public float movementSpeed = 20f;

        /// <summary>
        /// Speed of camera movement when shift is held down,
        /// </summary>
        public float fastMovementSpeed = 100f;

        /// <summary>
        /// Sensitivity for free look.
        /// </summary>
        public float freeLookSensitivity = 0.1f;

        /// <summary>
        /// Amount to zoom the camera when using the mouse wheel.
        /// </summary>
        public float zoomSensitivity = 0.1f;

        /// <summary>
        /// Set to true when free looking (on right mouse button).
        /// </summary>
        private bool _looking = false;
        private bool _lookingStarted = false;
        private bool _enable = true;

        private void OnEnable()
        {
            DebugLogManager.Instance.OnLogWindowShown += OnDebugWindowShown;
            DebugLogManager.Instance.OnLogWindowHidden += OnDebugWindowHidden;
        }

        private void OnDisable()
        {
            DebugLogManager.Instance.OnLogWindowShown -= OnDebugWindowShown;
            DebugLogManager.Instance.OnLogWindowHidden -= OnDebugWindowHidden;
            StopLooking();
        }

        private void OnDebugWindowShown()
        {
            StopLooking();
            _enable = false;
        }

        private void OnDebugWindowHidden()
        {
            _enable = true;
        }

        void Update()
        {
            if (!_enable) return;

            var fastMode = Keyboard.current.leftShiftKey.isPressed||
                               Keyboard.current.rightShiftKey.isPressed;

            var movementSpeed = fastMode ? fastMovementSpeed : this.movementSpeed;

            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
            {
                transform.position += (-transform.right * movementSpeed * Time.deltaTime);
            }

            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
            {
                transform.position += (transform.right * movementSpeed * Time.deltaTime);
            }

            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed)
            {
                transform.position += (transform.forward * movementSpeed * Time.deltaTime);
            }

            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed)
            {
                transform.position += (-transform.forward * movementSpeed * Time.deltaTime);
            }

            if (Keyboard.current.qKey.isPressed)
            {
                transform.position += (transform.up * movementSpeed * Time.deltaTime);
            }

            if (Keyboard.current.eKey.isPressed)
            {
                transform.position += (-transform.up * movementSpeed * Time.deltaTime);
            }

            if (Keyboard.current.rKey.isPressed ||
                Keyboard.current.pageUpKey.isPressed)
            {
                transform.position += (Vector3.up * movementSpeed * Time.deltaTime);
            }

            if (Keyboard.current.fKey.isPressed ||
                Keyboard.current.pageDownKey.isPressed)
            {
                transform.position += (-Vector3.up * movementSpeed * Time.deltaTime);
            }

            float yAxis = Mouse.current.scroll.y.ReadValue();
            if (yAxis != 0)
            {
                transform.position += transform.forward * yAxis * this.zoomSensitivity;
            }

            if (_looking)
            {
                var delta = Mouse.current.delta.ReadValue();
                if (delta.x != 0 || delta.y != 0)
                {
                    if (!_lookingStarted)
                    {
                        _lookingStarted = true; // This is to fix a bug on MacOS
                    }
                    else
                    {
                        float newRotationX = transform.localEulerAngles.y + delta.x * freeLookSensitivity;
                        float newRotationY = transform.localEulerAngles.x - delta.y * freeLookSensitivity;
                        transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
                    }
                }
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                StartLooking();
            }
            else if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                StopLooking();
            }
        }

        /// <summary>
        /// Enable free looking.
        /// </summary>
        private void StartLooking()
        {
            _looking = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>
        /// Disable free looking.
        /// </summary>
        private void StopLooking()
        {
            _looking = false;
            _lookingStarted = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}