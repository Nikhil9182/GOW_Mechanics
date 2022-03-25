using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeviationAxe : MonoBehaviour
{
    [SerializeField] PlayerController _controller;

    public float rotateSpeed;
    [HideInInspector] 
    public bool hasCollided;

    Rigidbody _rigibody;

    private bool hasToReturn;

    private void Start()
    {
        _rigibody = GetComponent<Rigidbody>();
        SetValue();
    }

    private void SetValue()
    {
        hasCollided = false;
        hasToReturn = false;
    }

    private void Update()
    {
        RotateAxe();
    }

    private void RotateAxe()
    {
        if(!hasCollided && !_controller.InHands)
        {
            transform.localEulerAngles += Vector3.forward * rotateSpeed * Time.deltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == 6)
        {
            _rigibody.Sleep();
            _rigibody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigibody.isKinematic = true;
            hasCollided = true;
        }
    }
}
