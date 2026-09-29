using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraDev.World
{
    /// <summary>Birinchi shaxs: oyoq nuqtasi saqlanadigan kapsula, yumshoq yurish va xavfsiz egilish.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class WorldPlayerController : MonoBehaviour
    {
        const float StandingHeight = 1.8f;
        const float CrouchingHeight = 1.1f;
        const float Radius = .3f;
        const float Gravity = -22f;
        const float JumpHeight = 1f;
        const float CoyoteSeconds = .12f;
        const float JumpBufferSeconds = .12f;
        /// <summary>Sezgirlik 1.00 da bir piksel sichqoncha siljishi necha daraja buradi.</summary>
        public const float MouseDegreesPerPixel = .085f;
        // InputManager.asset: "Mouse X/Y" sensitivity = 0.1, ya'ni GetAxisRaw = piksel * 0.1.
        // 10 ga ko'paytirib Input System'dagi Mouse.delta bilan bir xil piksel birligiga qaytaramiz.
        const float LegacyMouseAxisToPixels = 10f;

        [SerializeField] Camera viewCamera;
        [SerializeField] LayerMask collisionLayers = ~0;
        [SerializeField] float walkSpeed = 3.5f;
        [SerializeField] float sprintSpeed = 6f;
        [SerializeField] float crouchSpeed = 1.8f;
        [SerializeField] float acceleration = 22f;
        [SerializeField] float deceleration = 28f;

        readonly Collider[] clearanceHits = new Collider[32];
        readonly RaycastHit[] groundHits = new RaycastHit[16];
        CharacterController capsule;
        Vector3 planarVelocity;
        Vector3 groundNormal = Vector3.up;
        Vector3 spawnPosition;
        float spawnYaw;
        float verticalVelocity;
        float yaw;
        float pitch;
        float coyoteTime;
        float jumpBuffer;
        float bobPhase;
        float bobOffset;
        float stepCameraOffset;
        bool crouchLatched;
        bool previousCrouchInput;
        bool toggleCrouchMode;
        bool skipNextLook;
        bool paused;
        bool initialized;

        public Camera ViewCamera => viewCamera;
        public CharacterController Capsule => capsule != null ? capsule : GetComponent<CharacterController>();
        public bool Paused => paused;
        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public float Speed { get; private set; }
        public float Yaw => yaw;
        public float Pitch => pitch;
        public bool CanStand => HasClearance(StandingHeight);
        public bool TestMode { get; set; }
        public event Action<bool> PauseChanged;

        void Awake() => Initialize();

        void Start()
        {
            if (!TestMode) ApplyCursor();
        }

        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            capsule = GetComponent<CharacterController>();
            capsule.radius = Radius;
            capsule.height = StandingHeight;
            capsule.center = Vector3.up * (StandingHeight * .5f);
            capsule.slopeLimit = 45f;
            capsule.stepOffset = .3f;
            capsule.skinWidth = .035f;
            capsule.minMoveDistance = 0f;
            yaw = transform.eulerAngles.y;
            spawnYaw = yaw;
            spawnPosition = transform.position;
            toggleCrouchMode = WorldPreferences.ToggleCrouch;
        }

        public void Configure(Camera camera)
        {
            Initialize();
            viewCamera = camera;
            if (viewCamera == null) return;
            viewCamera.transform.SetParent(transform, false);
            viewCamera.transform.localPosition = new Vector3(0f, StandingHeight - .14f, 0f);
            viewCamera.transform.localRotation = Quaternion.identity;
            viewCamera.nearClipPlane = .06f;
            viewCamera.fieldOfView = WorldPreferences.FieldOfView;
            UpdateCamera(0f);
        }

        public void SetPaused(bool value)
        {
            bool changed = paused != value;
            paused = value;
            planarVelocity = Vector3.zero;
            Speed = 0f;
            IsSprinting = false;
            jumpBuffer = 0f;
            // Pauza/davom etishda toggle holati tushadi: joy bo'lsa o'yinchi tik turib qaytadi, past shift
            // ostida esa MoveStep baribir cho'kkalatib turadi. Pauzadan oldin ushlangan tugma yangi bosish emas.
            if (changed) crouchLatched = false;
            previousCrouchInput = true;
            skipNextLook = !value;
            if (!TestMode) ApplyCursor();
            if (changed) PauseChanged?.Invoke(value);
        }

        void ApplyCursor()
        {
            bool captured = !paused && Application.isFocused;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && !TestMode) SetPaused(true);
        }

        void OnApplicationPause(bool value)
        {
            if (value && !TestMode) SetPaused(true);
        }

        void OnDisable()
        {
            if (TestMode) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Teleport(Vector3 position, float yaw = 0f)
        {
            Initialize();
            if (!Finite(position.x) || !Finite(position.y) || !Finite(position.z)) return;
            bool wasEnabled = capsule.enabled;
            capsule.enabled = false;
            transform.position = position;
            this.yaw = Finite(yaw) ? Mathf.Repeat(yaw, 360f) : 0f;
            pitch = 0f;
            transform.rotation = Quaternion.Euler(0f, this.yaw, 0f);
            capsule.height = StandingHeight;
            capsule.center = Vector3.up * (StandingHeight * .5f);
            capsule.enabled = wasEnabled;
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
            coyoteTime = jumpBuffer = bobPhase = bobOffset = stepCameraOffset = 0f;
            crouchLatched = previousCrouchInput = false;
            IsGrounded = IsCrouching = IsSprinting = false;
            Speed = 0f;
            spawnPosition = position;
            spawnYaw = this.yaw;
            UpdateCamera(0f);
        }

        void Update()
        {
            if (TestMode || paused) return;
            if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked)
            {
                SetPaused(true);
                return;
            }
            ReadInput(out var move, out var look, out bool sprint, out bool crouch, out bool jump);
            if (skipNextLook)
            {
                look = Vector2.zero;
                skipNextLook = false;
            }
            Simulate(move, look, sprint, crouch, jump, Time.deltaTime);
        }

        /// <summary>O'yin ham, sinov ham bir xil fizik yo'ldan o'tadi; look — shu kadrdagi piksel siljishi.</summary>
        public void Simulate(Vector2 move, Vector2 look, bool sprint, bool crouch, bool jump, float dt)
        {
            Initialize();
            if (paused || !capsule.enabled || !Finite(dt) || dt <= 0f) return;
            dt = Mathf.Min(dt, .1f);
            if (!Finite(move.x) || !Finite(move.y)) move = Vector2.zero;
            if (!Finite(look.x) || !Finite(look.y)) look = Vector2.zero;
            move = Vector2.ClampMagnitude(move, 1f);
            float sensitivity = Mathf.Clamp(WorldPreferences.Sensitivity, WorldPreferences.MinSensitivity, WorldPreferences.MaxSensitivity);
            yaw = Mathf.Repeat(yaw + look.x * sensitivity * MouseDegreesPerPixel, 360f);
            pitch = Mathf.Clamp(pitch - look.y * sensitivity * MouseDegreesPerPixel *
                (WorldPreferences.InvertY ? -1f : 1f), -85f, 85f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            bool toggleMode = WorldPreferences.ToggleCrouch;
            if (toggleMode != toggleCrouchMode)
            {
                // Rejim almashsa (menyu, lobby yoki Reset) eski toggle holati qayta tiklanmaydi.
                toggleCrouchMode = toggleMode;
                crouchLatched = false;
            }
            if (toggleMode && crouch && !previousCrouchInput)
                crouchLatched = !crouchLatched;
            previousCrouchInput = crouch;
            bool wantsCrouch = toggleMode ? crouchLatched : crouch;
            if (jump) jumpBuffer = JumpBufferSeconds;

            // Uzun kadrda devordan o'tish va sakrash natijasining FPSga bog'liqligini kamaytiradi.
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / (1f / 90f)));
            float step = dt / steps;
            for (int i = 0; i < steps; i++) MoveStep(move, sprint, wantsCrouch, step);
            UpdateCamera(dt);
            if (transform.position.y < -40f) Teleport(spawnPosition, spawnYaw);
        }

        void MoveStep(Vector2 move, bool sprint, bool wantsCrouch, float dt)
        {
            bool onFloor = verticalVelocity <= 0f && ProbeGround(out groundNormal);
            IsGrounded = onFloor;
            coyoteTime = onFloor ? CoyoteSeconds : Mathf.Max(0f, coyoteTime - dt);
            bool crouching = wantsCrouch || !HasClearance(StandingHeight);
            float targetHeight = crouching ? CrouchingHeight : StandingHeight;
            float nextHeight = Mathf.MoveTowards(capsule.height, targetHeight, dt * 4.8f);
            if (nextHeight < capsule.height || HasClearance(nextHeight))
            {
                if (Mathf.Abs(nextHeight - capsule.height) > .0001f)
                {
                    capsule.height = nextHeight;
                    capsule.center = Vector3.up * (nextHeight * .5f);
                }
            }
            IsCrouching = crouching || capsule.height < StandingHeight - .01f;
            IsSprinting = sprint && !IsCrouching && move.y > .1f && move.sqrMagnitude > .01f;
            float targetSpeed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 desired = (transform.right * move.x + transform.forward * move.y) * targetSpeed;
            float change = (move.sqrMagnitude > .001f ? acceleration : deceleration) * (onFloor ? 1f : .38f);
            planarVelocity = Vector3.MoveTowards(planarVelocity, desired, change * dt);
            Vector3 movement = planarVelocity;
            if (onFloor && movement.sqrMagnitude > .0001f)
                movement = Vector3.ProjectOnPlane(movement, groundNormal).normalized * planarVelocity.magnitude;

            if (onFloor && verticalVelocity < 0f) verticalVelocity = -2f;
            if (jumpBuffer > 0f && coyoteTime > 0f && !IsCrouching)
            {
                verticalVelocity = Mathf.Sqrt(-2f * Gravity * JumpHeight);
                jumpBuffer = coyoteTime = 0f;
                IsGrounded = onFloor = false;
            }
            jumpBuffer = Mathf.Max(0f, jumpBuffer - dt);
            verticalVelocity = Mathf.Max(-45f, verticalVelocity + Gravity * dt);
            capsule.stepOffset = onFloor ? .3f : 0f;
            Vector3 before = transform.position;
            CollisionFlags collisions = capsule.Move((movement + Vector3.up * verticalVelocity) * dt);
            Vector3 actual = transform.position - before;
            Speed = new Vector2(actual.x, actual.z).magnitude / dt;
            if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
            if (verticalVelocity <= 0f && ProbeGround(out groundNormal))
            {
                IsGrounded = true;
                verticalVelocity = -2f;
            }
            else IsGrounded = false;

            // Past zinaga chiqish ko'zni sakratmaydi; haqiqiy sakrash esa kechikmaydi.
            if (onFloor && IsGrounded && actual.y > .025f && actual.y <= .34f)
                stepCameraOffset = Mathf.Clamp(stepCameraOffset - actual.y, -.32f, 0f);
            if (Speed < .05f) IsSprinting = false;
        }

        bool ProbeGround(out Vector3 normal)
        {
            normal = Vector3.up;
            // Zina burchagida spherecast ikki yuz orasidagi qiya normalni qaytarishi mumkin.
            // Oyoq markazidagi nur haqiqiy ustki yuzni topib, keyingi qadamni uzib qo'ymaydi.
            int supportCount = Physics.RaycastNonAlloc(transform.position + Vector3.up * .15f,
                Vector3.down, groundHits, .29f, collisionLayers, QueryTriggerInteraction.Ignore);
            float supportDistance = float.PositiveInfinity;
            bool supported = false;
            for (int i = 0; i < supportCount; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || OwnCollider(hit.collider) || hit.distance >= supportDistance) continue;
                if (Vector3.Angle(hit.normal, Vector3.up) > capsule.slopeLimit + .5f) continue;
                normal = hit.normal;
                supportDistance = hit.distance;
                supported = true;
            }
            if (supported) return true;
            float castRadius = capsule.radius * .93f;
            Vector3 origin = transform.position + Vector3.up * (capsule.radius + .065f);
            int count = Physics.SphereCastNonAlloc(origin, castRadius, Vector3.down, groundHits,
                .17f, collisionLayers, QueryTriggerInteraction.Ignore);
            float closest = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || OwnCollider(hit.collider) || hit.distance >= closest) continue;
                if (Vector3.Angle(hit.normal, Vector3.up) > capsule.slopeLimit + .5f) continue;
                normal = hit.normal;
                closest = hit.distance;
                found = true;
            }
            return found;
        }

        bool HasClearance(float height)
        {
            Initialize();
            // Faqat kengayadigan yuqori qismni tekshiradi: pol egilishni to'smasligi kerak.
            if (height <= capsule.height + .0001f) return true;
            float radius = Mathf.Max(.05f, capsule.radius - .012f);
            Vector3 bottom = transform.position + Vector3.up * Mathf.Max(radius, capsule.height - radius);
            Vector3 top = transform.position + Vector3.up * (height - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, clearanceHits,
                collisionLayers, QueryTriggerInteraction.Ignore);
            // To'lgan bufer xavfsiz tarafga og'adi; zich joyda shift ichiga turilmaydi.
            if (count == clearanceHits.Length) return false;
            for (int i = 0; i < count; i++)
                if (clearanceHits[i] != null && !OwnCollider(clearanceHits[i])) return false;
            return true;
        }

        bool OwnCollider(Collider other) => other == capsule || other.transform.IsChildOf(transform);

        void UpdateCamera(float dt)
        {
            if (viewCamera == null) return;
            float blend = dt <= 0f ? 1f : 1f - Mathf.Exp(-14f * dt);
            stepCameraOffset = Mathf.Lerp(stepCameraOffset, 0f, blend);
            float targetBob = 0f;
            if (WorldPreferences.HeadBob && IsGrounded && Speed > .1f && !paused)
            {
                bobPhase += dt * Speed * 2.25f;
                targetBob = Mathf.Sin(bobPhase * 2f) * (IsCrouching ? .009f : .018f);
            }
            bobOffset = Mathf.Lerp(bobOffset, targetBob, blend);
            viewCamera.transform.localPosition = new Vector3(0f, capsule.height - .14f + bobOffset + stepCameraOffset, 0f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, Mathf.Clamp(WorldPreferences.FieldOfView,
                WorldPreferences.MinFieldOfView, WorldPreferences.MaxFieldOfView), blend);
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static void ReadInput(out Vector2 move, out Vector2 look, out bool sprint, out bool crouch, out bool jump)
        {
            move = look = Vector2.zero;
            sprint = crouch = jump = false;
#if ENABLE_INPUT_SYSTEM
            var keys = Keyboard.current;
            if (keys != null)
            {
                move.x = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1f : 0f) -
                         (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1f : 0f);
                move.y = (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1f : 0f) -
                         (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1f : 0f);
                sprint = keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed;
                crouch = keys.leftCtrlKey.isPressed || keys.rightCtrlKey.isPressed || keys.cKey.isPressed;
                jump = keys.spaceKey.wasPressedThisFrame;
            }
            if (Mouse.current != null) look = Mouse.current.delta.ReadValue();
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > move.sqrMagnitude) move = stick;
                // Stik tezlikni bildiradi; sichqoncha esa tayyor siljishni beradi.
                look += gamepad.rightStick.ReadValue() * (1100f * Time.deltaTime);
                sprint |= gamepad.leftStickButton.isPressed || gamepad.leftShoulder.isPressed;
                crouch |= gamepad.rightStickButton.isPressed || gamepad.buttonEast.isPressed;
                jump |= gamepad.buttonSouth.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            move.x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                     (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            move.y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) -
                     (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * LegacyMouseAxisToPixels;
            sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            crouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.C);
            jump = Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}
