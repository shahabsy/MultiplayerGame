using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Collections;


public class LobbyPlayer : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>();

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
            PlayerName.Value = GameInstanceManager.Instance.CurrentPlayer.Name;
            IsReady.Value = false;
        }
        OnPlayerSpawned?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        OnPlayerDespawned?.Invoke(OwnerClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ToggleReadyServerRpc()
    {
        IsReady.Value = !IsReady.Value;
    }
}
