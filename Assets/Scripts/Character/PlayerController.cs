using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    #region Variables
    [SerializeField] Transform _Cam;
    [SerializeField] Transform axeContainer;
    [SerializeField] Transform handAxe;
    [SerializeField] Transform groundCheck;
    [SerializeField] Transform curvePoint;
    [SerializeField] Rigidbody _axeRigidbody;
    [SerializeField] GameObject _leviationAxe;
    [SerializeField] LeviationAxe _axeScript;
    [SerializeField] LayerMask groundMask;

    public float walkSpeed;
    public float runSpeed;
    public float speed;
    public float turningSpeed;
    public float groundDistance = 0.4f;
    public float axeThrowForce;
    [Range(1f, 3f)]
    public float speedOfReturning = 1f;
    [HideInInspector] public bool InHands;

    private float horizontalMove;
    private float verticalMove;
    private float targetSpeed;
    private float turnSmoothVelocity;
    private float axeReturnTime;

    private bool isSprinting;
    private bool isGrounded;
    private bool isFalling;
    private bool isArmed;
    private bool canMove;
    private bool isAiming;
    private bool hasThrown;
    private bool isPulling;
    private bool hasPulled;

    Vector3 weaponPosition;

    const float gravity = -9.81f;

    CharacterController _controller;
    Animator _animator;

    #endregion

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        SetValues();
    }

    private void Update()
    {
        InputManager();
        Move();
        Anim();
    }

    private void FixedUpdate()
    {
        PhysicsUpdate();
        AxeReturning();
    }

    private void AxeReturning()
    {
        if(isPulling)
        {
            if(axeReturnTime < 1)
            {
                _leviationAxe.transform.position = BezierCurve(handAxe.position, weaponPosition, curvePoint.position, axeReturnTime);
                axeReturnTime += Time.fixedDeltaTime * speedOfReturning;
            }
            else
            {
                AxeGrab();
            }
        }
    }

    private void PhysicsUpdate()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if(!isGrounded)
        {
            isFalling = true;
            Vector3 velocity = new Vector3(0f, gravity, 0f);
            _controller.Move(velocity * Time.deltaTime);
        }
        else
        {
            isFalling = false;
            _controller.Move(Vector3.zero * Time.deltaTime);
        }
    }

    private void SetValues()
    {
        isArmed = false;
        canMove = true;
        hasThrown = false;
        InHands = true;
    }

    private void InputManager()
    {
        horizontalMove = Input.GetAxisRaw("Horizontal");
        verticalMove = Input.GetAxisRaw("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        if(Input.GetKeyDown(KeyCode.R))
        {
            isPulling = true;
            AxeReturn();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            isArmed = !isArmed;
        }
        if(Input.GetKey(KeyCode.LeftControl) && isArmed)
        {
            isAiming = true;
            if(Input.GetMouseButton(0))
            {
                hasThrown = true;
            }
        }
        else
        {
            isAiming = false;
        }
    }

    private void Move()
    {
        Vector3 movementVec = new Vector3(horizontalMove, 0f, verticalMove);
        if(movementVec != Vector3.zero && canMove)
        {
            float targetAngle = (Mathf.Atan2(movementVec.x, movementVec.z) * Mathf.Rad2Deg)/2 + _Cam.eulerAngles.y;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, targetAngle, 0f), turningSpeed * Time.deltaTime);
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turningSpeed);
            //transform.rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            if (isSprinting && !isFalling)
            {
                targetSpeed = runSpeed;
            }
            else if (!isSprinting)
            {
                targetSpeed = walkSpeed;
            }
            _controller.Move(moveDir.normalized * targetSpeed * Time.deltaTime);
        }
        else
        {
            targetSpeed = 0f;
        }
    }

    private void Anim()
    {
        _animator.SetFloat("UnarmedSpeed", targetSpeed);
        _animator.SetBool("isFalling", isFalling);
        _animator.SetBool("isArmed", isArmed);
        _animator.SetBool("isAiming", isAiming);
        _animator.SetBool("hasThrown", hasThrown);
        _animator.SetBool("isPulling", isPulling);
        _animator.SetBool("hasPulled", hasPulled);
    }

    private void AxeThrow()
    {
        _leviationAxe.transform.parent = null;
        _axeRigidbody.isKinematic = false;
        _axeRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _axeScript.rotateSpeed = Mathf.Abs(_axeScript.rotateSpeed);
        _axeRigidbody.AddForce(Camera.main.transform.forward * axeThrowForce + transform.up * 2, ForceMode.Impulse);
        InHands = false;
    }

    private void AxeReturn()
    {
        weaponPosition = _leviationAxe.transform.position;
        _axeRigidbody.Sleep();
        _axeRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _axeRigidbody.isKinematic = true;
        _leviationAxe.transform.DORotate(new Vector3(-90, -90, 0), .2f).SetEase(Ease.InOutSine);
        _leviationAxe.transform.DOBlendableLocalRotateBy(Vector3.right * 90, .5f);
        axeReturnTime = 0;
        _axeScript.hasCollided = false;
        _axeScript.rotateSpeed = Mathf.Abs(_axeScript.rotateSpeed) * (-1);
    }

    private void AxeGrab()
    {
        isPulling = false;
        isArmed = true;
        InHands = true;
        hasThrown = false;
        AxeArmDisarmEvent();
        hasPulled = true;
        StartCoroutine(Pulled());
    }

    private void AxeArmDisarmEvent()
    {
        if (isArmed)
        {
            _leviationAxe.transform.parent = handAxe.transform;
        }
        else if (!isArmed)
        {
            _leviationAxe.transform.parent = axeContainer.transform;

        }
        _leviationAxe.transform.localPosition = Vector3.zero;
        _leviationAxe.transform.localRotation = Quaternion.identity;
    }

    private void MovementSwitch()
    {
        canMove = !canMove;
    }

    private Vector3 BezierCurve(Vector3 endingPosition, Vector3 startingPosition, Vector3 curvePoint, float fractionOfTime)
    {
        Vector3 positionReached = (Mathf.Pow((1 - fractionOfTime), 2) * startingPosition) + (2 * (1 - fractionOfTime) * fractionOfTime * curvePoint) + (Mathf.Pow(fractionOfTime, 2) * endingPosition);
        return positionReached;
    }

    IEnumerator Pulled()
    {
        yield return new WaitForSeconds(0.06f);
        hasPulled = false;
    }
}
