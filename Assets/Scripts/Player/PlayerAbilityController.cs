using System;
using UnityEngine;

// Tracks which ability, if any, currently has exclusive control over the player's movement.
// This does NOT contain ability logic itself - each ability is its own component that asks
// this controller for permission to take over, drives the rigidbody while active, then
// releases control. Keeping this dumb on purpose: it's just a gate, not a behaviour tree.
public enum AbilityState
{
    None,
    Dashing,
    WallClinging,
    WallJumping,
    StretchArm,
    PuddleForm,
    HeavyForm,
    GatlingForm
}

[RequireComponent(typeof(PlayerMovement))]
public class PlayerAbilityController : MonoBehaviour
{
    public AbilityState CurrentState { get; private set; } = AbilityState.None;

    // (previousState, newState) - lets VFX/animation/camera react to any ability transition
    // without each ability script needing to know about them directly.
    public event Action<AbilityState, AbilityState> OnStateChanged;

    [SerializeField] private PlayerMovement playerMovement;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    public bool IsFree => CurrentState == AbilityState.None;

    public bool IsInState(AbilityState state) => CurrentState == state;

    // Abilities call this before starting. Returns false if something else already has control -
    // the ability should just no-op in that case rather than fighting for control.
    public bool TryEnterState(AbilityState newState)
    {
        if (CurrentState != AbilityState.None)
            return false;

        AbilityState previous = CurrentState;
        CurrentState = newState;
        playerMovement.SetMovementLocked(true);

        OnStateChanged?.Invoke(previous, newState);
        return true;
    }

    // Abilities call this when they finish (naturally or cancelled). Pass in the state you
    // entered with as a safety check, so a stale/late call from an old ability can't
    // accidentally clear a different ability's control.
    public void ExitState(AbilityState state)
    {
        if (CurrentState != state)
            return;

        AbilityState previous = CurrentState;
        CurrentState = AbilityState.None;
        playerMovement.SetMovementLocked(false);

        OnStateChanged?.Invoke(previous, AbilityState.None);
    }
}