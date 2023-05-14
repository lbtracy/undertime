using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour, INeedSave, IPauseable, INeedReset
    {
        public static GameObject playerLookingAt;

        [Header("Player")] [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;

        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)] [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.1f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")] public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.5f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        [Header("Inputs")] public InputAction Sprint;
        public InputAction Look;
        public InputAction Moving;
        public InputAction Jump;

        [Header("Character Input Values")] public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;

        [Header("Movement Settings")] public bool analogMovement;

        // should pause to response action
        private bool paused;

        // player
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private CharacterController _controller;

        private GameObject _mainCamera;

        // 相机组件
        private Camera _mainCameraComponent;

        // 碰撞箱
        public BoxCollider bedCollider;

        private const float _threshold = 0.01f;

        // 最后一个被射线击中的物体的渲染器
        private Renderer _lastHitRenderer;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
                // TODO: 返回当前设备是否是鼠标
                return true;
#else
				return false;
#endif
            }
        }

        public void PauseOrResume(bool isPaused)
        {
            paused = isPaused;
            if (!isPaused) return;
            // reset all
            look = new Vector2();
            move = new Vector2();
            sprint = false;
            jump = false;
        }

        public void SaveData(ref GameData gameData)
        {
            gameData.playerPosition = gameObject.transform.position;
            gameData.playerCameraRotation = _mainCamera.transform.rotation;
        }

        public void LoadData(GameData gameData)
        {
            transform.Translate(gameData.playerPosition);
            _mainCamera.transform.Rotate(gameData.playerCameraRotation.eulerAngles.x, 0, 0);
            transform.Rotate(0, gameData.playerCameraRotation.eulerAngles.y, 0);
        }

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
                // 获取主相机的相机组件
                _mainCameraComponent = _mainCamera.GetComponent<Camera>();
            }
        }

        private void OnJump(InputAction.CallbackContext obj)
        {
            if (paused)
                return;
            jump = obj.action.WasPressedThisFrame();
            Debug.Log("On Jump");
        }

        private void OnLook(InputAction.CallbackContext obj)
        {
            if (paused)
                return;
            look = obj.ReadValue<Vector2>();
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            Look.performed += OnLook;
            Jump.performed += OnJump;
        }

        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();
            Move();
            // IsLookAtSpecifiedObject();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (paused)
            {
                return;
            }

            // if there is an input
            if (look.sqrMagnitude >= _threshold)
            {
                //Don't multiply mouse input by Time.deltaTime
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                // rotate the player left and right
                transform.Rotate(Vector3.up * (look.x * RotationSpeed * deltaTimeMultiplier));
                _mainCamera.transform.Rotate(Vector3.left * -(look.y * RotationSpeed * deltaTimeMultiplier));
            }
        }

        private void Move()
        {
            if (paused)
                return;
            move = Moving.ReadValue<Vector2>();
            sprint = Sprint.IsPressed();
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = analogMovement ? move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // normalise input direction
            Vector3 inputDirection = new Vector3(move.x, 0.0f, move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (move != Vector2.zero)
            {
                // move
                inputDirection = transform.right * move.x + transform.forward * move.y;
            }

            // move the player
            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                // if we are not grounded, do not jump
                jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        /// <summary>
        /// 让玩家到床上去
        /// </summary>
        public void GoToBed()
        {
            // 禁用床的碰撞箱
            bedCollider.enabled = false;
            // 让玩家到床上去
            gameObject.transform.position = new Vector3(0.9f, 0, 0);
            // 修改相机位置
            var t = _mainCamera.transform;
            t.localPosition = new Vector3(0.2f, 0.6f, 0);
            // 修改相机视角
            var ePlayer = t.eulerAngles;
            transform.Rotate(0, 180 - ePlayer.y, 0);
            t.Rotate(0 - _mainCamera.transform.eulerAngles.x, 0, 0);
        }

        private void OnEnable()
        {
            Jump.Enable();
            Sprint.Enable();
            Moving.Enable();
            Look.Enable();
        }

        private void OnDisable()
        {
            Jump.Disable();
            Sprint.Disable();
            Moving.Disable();
            Look.Disable();
        }

        public void Reset()
        {
            // 禁用角色控制器，阻止角色控制器将玩家位置设置回之前的值
            _controller.enabled = false;
            // 重置玩家位置
            gameObject.transform.position = new Vector3(-0.5f, 0);
            // 重置相机位置
            var t = _mainCamera.transform;
            t.localPosition = new Vector3(0.2f, 1.35f, 0);
            // 重置相机视角
            transform.Rotate(0, -transform.eulerAngles.y, 0);
            _mainCamera.transform.Rotate(0, 0, 0);
            // 启用床的碰撞箱
            if (bedCollider) bedCollider.enabled = true;
            // 重新启用角色控制器
            _controller.enabled = true;
        }
    }
}