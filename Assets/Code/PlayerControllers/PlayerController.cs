using Unity.Netcode.Components;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Globalization;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    private NetworkTransform _networkTransform;

    private Vector2 _moveInput;

    private void Awake()
    {
        _networkTransform = GetComponent<NetworkTransform>();
        if ( _networkTransform == null )
        {
            _networkTransform = gameObject.AddComponent<NetworkTransform>();
        }
        Debug.Log($"PlayerController Awake on {gameObject.name}, IsOwner: {IsOwner}, IsSpawned {IsSpawned}");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"Player spawned! ClientId: {OwnerClientId}, IsOwner: {IsOwner}, IsServer: {IsServer}, IsClient: {IsClient}");

        if (IsOwner)
        {
            GetComponent<SpriteRenderer>().color = Color.green;
            Debug.Log("This is my player (owner)");
        }
        else
        {
            GetComponent<SpriteRenderer>().color = Color.red;
            Debug.Log("This is a Remote player");
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
