using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Collections;


public class LobbyPlayer : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>();

    public event Action<ulong> OnDataChanged;

    private void Awake()
    {
        PlayerName.OnValueChanged += (_, _) => OnDataChanged?.Invoke(OwnerClientId);
        IsReady.OnValueChanged += (_, _) => OnDataChanged?.Invoke(OwnerClientId);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            PlayerName.Value = GameInstanceManager.Instance.CurrentPlayer.Name;
            IsReady.Value = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc()
    {
        IsReady.Value = !IsReady.Value;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
