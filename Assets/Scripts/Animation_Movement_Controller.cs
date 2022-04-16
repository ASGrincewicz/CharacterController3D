
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
        private bool _isMovementPressed;
        private bool _isRunPressed;
        [SerializeField] float _rotationFactorPerFrame = 2.0f;
        //Animator Parameters
        private readonly int _isWalkingHash = Animator.StringToHash("isWalking");
        private readonly int _isRunningHash = Animator.StringToHash("isRunning");
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

        private void HandleAnimation()
        {
            bool isWalking = _animator.GetBool(_isWalkingHash);
            bool isRunning = _animator.GetBool(_isRunningHash);
            
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
    }
}
