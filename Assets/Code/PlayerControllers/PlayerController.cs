using Unity.Netcode.Components;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    private NetworkTransform _networkTransform;

    private void Awake()
    {
        _networkTransform = GetComponent<NetworkTransform>();
        if ( _networkTransform == null )
        {
            _networkTransform = gameObject.AddComponent<NetworkTransform>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) { return; }

        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            if(Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                moveInput.y += 1f;
            }
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                moveInput.y -= 1f;
            }
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                moveInput.x -= 1f;
            }
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                moveInput.x += 1f;
            }
        }

        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }

        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * _moveSpeed * Time.deltaTime;
        transform.Translate(movement);
    }
}
