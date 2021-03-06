
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VeganimusStudios
{
    public class Animation_Movement_Controller : MonoBehaviour
    {
        [SerializeField] private float _movementSpeed = 2.0f;
        [SerializeField] private float _runMultiplier = 2.0f;
        private PlayerControls _playerControls;
        private CharacterController _characterController;
        private Animator _animator;
        //Player Input Values
        private Vector2 _currentMovementInput;
        private Vector3 _currentMovement;
        private Vector3 _currentRunMovement;
        private bool _isMovementPressed = false;
        private bool _isRunPressed = false;
        private bool _isJumpPressed = false;
        [SerializeField] float _rotationFactorPerFrame = 2.0f;
        //Jumping Parameters
        [SerializeField]private float _initialJumpVelocity;
        [SerializeField] private float _maxJumpHeight = 1.0f;
        [SerializeField]private float _maxJumpTime = 0.5f;
        private bool _isJumping = false;
        [SerializeField] int _jumpCount = 0;
        private float _jumpTimer = 0.5f;
        private WaitForSeconds _jumpDelay;
        private Dictionary<int, float> _initialJumpVelocities = new Dictionary<int, float>();
        private Dictionary<int, float> _jumpGravities = new Dictionary<int, float>();
        private Coroutine _currentJumpResetRoutine = null;
        //Animator Parameters
        private readonly int _isWalkingHash = Animator.StringToHash("isWalking");
        private readonly int _isRunningHash = Animator.StringToHash("isRunning");
        private readonly int _isJumpingHash = Animator.StringToHash("isJumping");
        private readonly int _jumpCountHash = Animator.StringToHash("jumpCount");
        private bool _isJumpAnimating = false;
        //Gravity Parameters
        private float _groundedGravity = -0.05f;
        private float _gravity = -9.8f;
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            _playerControls = new PlayerControls();
            _playerControls.CharacterControls.Move.started += OnMovementInput;
            _playerControls.CharacterControls.Move.performed += OnMovementInput;
            _playerControls.CharacterControls.Move.canceled += OnMovementInput;
            _playerControls.CharacterControls.Run.started += OnRun;
            _playerControls.CharacterControls.Run.canceled += OnRun;
            _playerControls.CharacterControls.Jump.started += OnJump;
            _playerControls.CharacterControls.Jump.canceled += OnJump;
            SetupJumpVariables();
        }
        private void OnEnable() => _playerControls.CharacterControls.Enable();

        private void OnDisable() => _playerControls.CharacterControls.Disable();

        private void Update()
        {
            HandleAnimation();
            HandleRotation();
           
            if (_isRunPressed)
                _characterController.Move(_currentRunMovement * (_movementSpeed * Time.deltaTime));
            else
                _characterController.Move(_currentMovement *(_movementSpeed * Time.deltaTime));
            HandleGravity();
            HandleJump();
        }

        private void SetupJumpVariables()
        {
            float timeToApex = _maxJumpTime / 2;
            _gravity = (-2 * _maxJumpHeight) / Mathf.Pow(timeToApex, 2);
            _initialJumpVelocity = 2 * _maxJumpHeight / timeToApex;
            float secondJumpGravity = (-2 * _maxJumpHeight + 2) / Mathf.Pow(timeToApex * 1.25f, 2);
            float secondJumpVelocity = 2 * _maxJumpHeight + 2 / timeToApex * 1.25f;
            float thirdJumpGravity = (-2 * _maxJumpHeight + 3) / Mathf.Pow(timeToApex * 1.5f, 2);
            float thirdJumpVelocity = 2 * _maxJumpHeight + 3 / timeToApex * 1.5f;
            
            _initialJumpVelocities.Add(1,_initialJumpVelocity);
            _initialJumpVelocities.Add(2,secondJumpVelocity);
            _initialJumpVelocities.Add(3,thirdJumpVelocity);
            
            _jumpGravities.Add(0,_gravity);
            _jumpGravities.Add(1,_gravity);
            _jumpGravities.Add(2, secondJumpGravity);
            _jumpGravities.Add(3, thirdJumpGravity);

            _jumpDelay = new WaitForSeconds(_jumpTimer);

        }
        private void OnMovementInput(InputAction.CallbackContext context)
        {
            _currentMovementInput = context.ReadValue<Vector2>();
            _currentMovement.x = _currentMovementInput.x;
            _currentMovement.z = _currentMovementInput.y;
            _currentRunMovement.x = _currentMovement.x * _runMultiplier;
            _currentRunMovement.z = _currentMovement.z * _runMultiplier;
            _isMovementPressed = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;
        }

        private void OnRun(InputAction.CallbackContext context) => _isRunPressed = context.ReadValueAsButton();
        private void OnJump(InputAction.CallbackContext context)
        {
            _isJumpPressed = context.ReadValueAsButton();
            Debug.Log(_isJumpPressed);
        }

        private void HandleAnimation()
        {
            bool isWalking = _animator.GetBool(_isWalkingHash);
            bool isRunning = _animator.GetBool(_isRunningHash);
            bool isJumping = _animator.GetBool(_isJumpingHash);
            
            if(_isMovementPressed && !isWalking)
                _animator.SetBool(_isWalkingHash, true);
            else if(!_isMovementPressed && isWalking)
                _animator.SetBool(_isWalkingHash, false);
            if(_isRunPressed && !isRunning)
                _animator.SetBool(_isRunningHash, true);
            else if(!_isRunPressed && isRunning)
                _animator.SetBool(_isRunningHash, false);
        }

        private void HandleRotation()
        {
            Vector3 positionToLookAt;
            positionToLookAt.x = _currentMovement.x;
            positionToLookAt.y = 0.0f;
            positionToLookAt.z = _currentMovement.z;
            Quaternion currentRotation = transform.rotation;
            if (_isMovementPressed)
            {
                Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
                transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, _rotationFactorPerFrame * Time.deltaTime);
            }
        }

        private void HandleGravity()
        {
            bool isFalling = _currentMovement.y <= 0.0f || !_isJumpPressed;
            float fallMultiplier = 2.0f;
            if (_characterController.isGrounded)
            {
                if (_isJumpAnimating)
                {
                    _animator.SetBool(_isJumpingHash, false);
                    _isJumpAnimating = false;
                    _currentJumpResetRoutine = StartCoroutine(JumpResetRoutine());
                    if (_jumpCount == 3)
                    {
                        _jumpCount = 0;
                        _animator.SetInteger(_jumpCountHash, _jumpCount);
                    }
                }
                _currentMovement.y = _groundedGravity;
                _currentRunMovement.y = _groundedGravity;
            }
            else if (isFalling)
            {
                float previousYVelocity = _currentMovement.y;
                float newYVelocity = _currentMovement.y + (_jumpGravities[_jumpCount] * fallMultiplier * Time.deltaTime);
                float nextYVelocity = Mathf.Max((previousYVelocity + newYVelocity) * .5f, -20.0f);
                _currentMovement.y = nextYVelocity;
                _currentRunMovement.y = nextYVelocity;
            }
            else
            {
                float previousYVelocity = _currentMovement.y;
                float newYVelocity = _currentMovement.y + (_jumpGravities[_jumpCount] * Time.deltaTime);
                float nextYVelocity = (previousYVelocity + newYVelocity) * .5f;
                _currentMovement.y = nextYVelocity;
                _currentRunMovement.y = nextYVelocity;
            }
        }

        private void HandleJump()
        {
            if (!_isJumping && _isJumpPressed && _characterController.isGrounded)
            {
                if (_jumpCount < 3 && _currentJumpResetRoutine != null)
                {
                    StopCoroutine(_currentJumpResetRoutine);
                }
                _animator.SetBool(_isJumpingHash, true);
                _isJumping = true;
                _isJumpAnimating = true;
                _jumpCount++;
                _animator.SetInteger(_jumpCountHash, _jumpCount);
                _currentMovement.y = _initialJumpVelocities[_jumpCount] * 0.5f;
                _currentRunMovement.y = _initialJumpVelocities[_jumpCount] * 0.5f;
            }
            else if (!_isJumpPressed && _isJumping && _characterController.isGrounded)
            {
                _isJumping = false;
            }
        }

        private IEnumerator JumpResetRoutine()
        {
            yield return _jumpDelay;
            _jumpCount = 0;
        }
    }
}
