using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Collections;


public class LobbyPlayer : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action<ulong> OnDataChanged;

    public static event Action<LobbyPlayer> OnPlayerSpawned;
    public static event Action<ulong> OnPlayerDespawned;

    private void Awake()
    {
        PlayerName.OnValueChanged += (_, _) => OnDataChanged?.Invoke(OwnerClientId);
        IsReady.OnValueChanged += (_, _) => OnDataChanged?.Invoke(OwnerClientId);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string desiredName = $"Player{OwnerClientId}";
            if (GameInstanceManager.Instance != null && GameInstanceManager.Instance.CurrentPlayer != null)
            {
                var n = GameInstanceManager.Instance.CurrentPlayer.Name;
                if (!string.IsNullOrEmpty(n))
                {
                    desiredName = n;
                }
            }
            if (IsServer)
            {
                PlayerName.Value = new FixedString32Bytes(desiredName);
            }
            else
            {
                SetPlayerNameServerRpc(new FixedString32Bytes(desiredName));
            }

            if (IsServer)
            {
                IsReady.Value = false;
            }
            //PlayerName.Value = GameInstanceManager.Instance.CurrentPlayer.Name;
            //Debug.Log($"LobbyPlayer Name: {PlayerName.Value} for ClientId: {OwnerClientId}"); 
            //IsReady.Value = false;
        }
        OnPlayerSpawned?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        OnPlayerDespawned?.Invoke(OwnerClientId);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetPlayerNameServerRpc(FixedString32Bytes name)
    {
        if (!IsServer) return;
        PlayerName.Value = name;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ToggleReadyServerRpc()
    {
        IsReady.Value = !IsReady.Value;
    }
}
