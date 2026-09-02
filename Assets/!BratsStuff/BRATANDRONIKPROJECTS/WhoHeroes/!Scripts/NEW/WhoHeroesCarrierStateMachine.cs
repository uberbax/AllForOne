using System;
using UnityEngine;

public sealed class WhoHeroesCarrierStateMachine : ComponentBehavior
{
    public enum CarrierState
    {
        Inactive,
        ToMine,
        ToCastle,
        Completed,
        Cancelled
    }

    [SerializeField] private CarrierState state = CarrierState.Inactive;

    private Transform pickupTarget;
    private Transform castleTarget;
    private GameObject resourceIcon;
    private float speed;
    private Action completed;

    public CarrierState State => state;

    public bool Initialize(
        Transform pickup,
        Transform castle,
        GameObject icon,
        float moveSpeed,
        Action onCompleted,
        bool fastForward)
    {
        pickupTarget = pickup;
        castleTarget = castle;
        resourceIcon = icon;
        speed = moveSpeed;
        completed = onCompleted;

        if (pickupTarget == null || castleTarget == null || !fastForward && UtilsControl.Instance == null)
        {
            Cancel();
            return false;
        }

        if (fastForward)
        {
            FastForward();
            return true;
        }

        state = CarrierState.ToMine;
        MoveTo(pickupTarget, OnMineReached);
        return true;
    }

    public void FastForward()
    {
        if (state == CarrierState.Completed || state == CarrierState.Cancelled)
            return;

        StopMovement();
        resourceIcon?.SetActive(true);
        if (castleTarget != null)
            transform.position = castleTarget.position;
        Complete();
    }

    public void Cancel()
    {
        if (state == CarrierState.Completed || state == CarrierState.Cancelled)
            return;

        StopMovement();
        completed = null;
        state = CarrierState.Cancelled;
    }

    private void OnMineReached()
    {
        if (state != CarrierState.ToMine)
            return;

        resourceIcon?.SetActive(true);
        state = CarrierState.ToCastle;
        MoveTo(castleTarget, Complete);
    }

    private void Complete()
    {
        if (state == CarrierState.Completed || state == CarrierState.Cancelled)
            return;

        state = CarrierState.Completed;
        var callback = completed;
        completed = null;
        callback?.Invoke();
    }

    private void MoveTo(Transform target, Action callback)
    {
        UtilsControl.Instance.MoveTo(
            transform, speed, target.position, callback, target,
            useRight: false, ignoreFlip: true);
    }

    private void StopMovement()
    {
        var movement = GetComponent<MoveDir>();
        if (movement?.cr != null && UtilsControl.Instance != null)
        {
            UtilsControl.Instance.StopCoroutine(movement.cr);
            movement.cr = null;
        }
        name = name.Replace("_move", string.Empty);
    }

    private void OnDestroy()
    {
        completed = null;
    }
}
