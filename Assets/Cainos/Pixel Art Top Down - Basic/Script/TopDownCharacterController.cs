using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cainos.PixelArtTopDown_Basic
{
    public class TopDownCharacterController : MonoBehaviour
    {
        public PlayerInput playerInput;
        public float speed = 3f;

        private Animator animator;
        private Rigidbody2D rb;

        private InputAction moveAction;
        private Vector2 moveInput;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            
        }

        

        private void OnMove(InputValue value)
        {
            Vector2 playerInput = new Vector2(value.Get<Vector2>().x, value.Get<Vector2>().y);
            moveInput = playerInput;
        }


        private void Update()
        {
            var dir = moveInput;
            bool isMoving = dir.sqrMagnitude > 0.0001f;
            animator?.SetBool("IsMoving", isMoving);
            if (isMoving)
            {
                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                {
                    animator?.SetInteger("Direction", dir.x > 0 ? 2 : 3); // 2 right and 3 is left
                }
                else
                {
                    animator?.SetInteger("Direction", dir.y > 0 ? 1 : 0);
                }
            }
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = speed * moveInput;
        }
    }
}
