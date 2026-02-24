using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEmotes : NetworkBehaviour
{
    [SerializeField] private NetworkAnimator emoteAnimator;
    [SerializeField] private GameObject deathPrefab;
    int height = 50;

    public bool canDoEmotes = true;

    [Header("Animation Hashes")]
    private readonly int _isWavingBool = Animator.StringToHash("IsWaving");

    [Header("Animation States")]
    private bool isWaving;

    private void Update()
    {
    }

    public void OnSpawnDeath()
    {
        if (!canDoEmotes) return;

        if (IsHost) SpawnDeath();
        else SpawnDeathServerRpc();
    }

    [ServerRpc]
    private void SpawnDeathServerRpc()
    {
        SpawnDeath();
    }

    private void SpawnDeath()
    {
        Debug.Log("Spawning Liquid Death");

        GameObject death = Instantiate(deathPrefab, transform.position + Vector3.up * height, Quaternion.identity);
        NetworkObject deathObject = death.GetComponent<NetworkObject>();
        deathObject.Spawn();
        
        // set random rotation
        death.GetComponent<Rigidbody>().angularVelocity = Random.insideUnitSphere * 5f;
    }

    public void OnWave(InputValue value)
    {
        if (value.isPressed && canDoEmotes) emoteAnimator.Animator.SetBool(_isWavingBool, true);
        else if (!value.isPressed) emoteAnimator.Animator.SetBool(_isWavingBool, false);
    }

    public void OnThumbsUp()
    {
        if (!canDoEmotes) return;
    }

    public void OnPush()
    {
        if (!canDoEmotes) return;

    }

    public void OnClap()
    {
        if (!canDoEmotes) return;
    }

    public void OnDance()
    {
        if (!canDoEmotes) return;

    }
}
