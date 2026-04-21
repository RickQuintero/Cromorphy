using UnityEngine;
using UnityEngine.InputSystem;


    /// <summary>
    /// Polls the New Input System every frame via PlayerInput.actions.
    /// 
    /// ── WHY NO CALLBACKS ──
    ///   Callbacks (performed/started/canceled) fire outside Update() order,
    ///   causing frame-order ambiguity and making it hard to reason about
    ///   when state changed. Polling gives deterministic, per-frame snapshots.
    ///
    /// ── SETUP ──
    ///   1. Add PlayerInput component to the Player GameObject.
    ///   2. Assign an InputActionAsset with actions: Move, Jump, Crouch, Attack.
    ///   3. Set Behavior to "Send Messages" OR "Invoke Unity Events" — but we
    ///      won't use either; we poll directly. Set to "None" if possible.
    ///   4. This component reads via: playerInput.actions["ActionName"]
    ///
    /// ── REQUIRED ACTION NAMES (in your InputActionAsset) ──
    ///   "Move"   → Vector2 (WASD / Left Stick)
    ///   "Jump"   → Button
    ///   "Crouch" → Button (hold)
    ///   "Attack" → Button
    /// </summary>

    public class InputReader : MonoBehaviour
    {
        // ── Action name constants ───────────────────────────────────────────────
        // Using constants prevents typo bugs when referencing string keys.
        private const string ACTION_MOVE   = "Move";
        private const string ACTION_JUMP   = "Jump";
        private const string ACTION_CROUCH = "Crouch";
        private const string ACTION_ATTACK = "Attack";

        private const string ACTION_AIM = "Interact";

        // ── Cached InputAction references ──────────────────────────────────────
        // Caching avoids dictionary lookup overhead on every ReadValue call.
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction;
        private InputAction _attackAction;

        private InputAction _aimAction;

        // ── Polled values (public, read-only properties) ───────────────────────

        /// <summary>Horizontal + Vertical input. Range: [-1, 1] per axis.</summary>
        public Vector2 MoveInput   { get; private set; }

        /// <summary>True only in the frame the jump button was pressed.</summary>
        public bool JumpDown       { get; private set; }

        /// <summary>True while jump is held (for variable jump height).</summary>
        public bool JumpHeld       { get; private set; }

        /// <summary>True in the frame the jump button was released.</summary>
        public bool JumpUp         { get; private set; }

        /// <summary>True while crouch/crawl is held.</summary>
        public bool CrouchHeld     { get; private set; }

        /// <summary>True only in the frame attack was pressed.</summary>
        public bool AttackDown     { get; private set; }

        /// <summary>True only in the frame aim (right-click) was pressed.</summary>
        public bool AimDown        { get; private set; }

        /// <summary>True while aim is held.</summary>
        public bool AimHeld        { get; private set; }

        /// <summary>True in the frame aim was released.</summary>
        public bool AimUp          { get; private set; }

        // ── Raw direction helpers ──────────────────────────────────────────────

        public float Horizontal => MoveInput.x;
        public float Vertical   => MoveInput.y;
        public bool  MovingLeft  => MoveInput.x < -0.1f;
        public bool  MovingRight => MoveInput.x >  0.1f;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            var playerInput = GetComponent<PlayerInput>();

            // Cache references once — no string lookup per frame after this
            _moveAction   = playerInput.actions[ACTION_MOVE];
            _jumpAction   = playerInput.actions[ACTION_JUMP];
            _crouchAction = playerInput.actions[ACTION_CROUCH];
            _attackAction = playerInput.actions[ACTION_ATTACK];
            _aimAction    = playerInput.actions[ACTION_AIM];
        }

        private void Update()
        {
            // ── Polling: read current frame state ─────────────────────────────
            // ReadValue<T>(), WasPressedThisFrame(), IsPressed() are all
            // synchronous polling methods — no callbacks involved.

            MoveInput  = _moveAction.ReadValue<Vector2>();
            JumpHeld   = _jumpAction.IsPressed();
            JumpDown   = _jumpAction.WasPressedThisFrame();
            JumpUp     = _jumpAction.WasReleasedThisFrame();
            CrouchHeld = _crouchAction.IsPressed();
            AttackDown = _attackAction.WasPressedThisFrame();
            AimDown    = _aimAction.WasPressedThisFrame();
            AimHeld    = _aimAction.IsPressed();
            AimUp      = _aimAction.WasReleasedThisFrame();
        }
    }

