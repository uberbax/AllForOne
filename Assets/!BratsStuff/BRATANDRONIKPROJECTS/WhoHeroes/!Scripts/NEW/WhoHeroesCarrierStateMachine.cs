using System;
using System.Collections.Generic;
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
    private GameObject sourceResourceIcon;
    private GameObject resourceIcon;
    private List<(float, float, float)> routeToMine;
    private List<(float, float, float)> routeToCastle;
    private float speed;
    private Action completed;

    public CarrierState State => state;

    public bool Initialize(
        Transform pickup,
        Transform castle,
        GameObject sourceIcon,
        GameObject icon,
        List<(float, float, float)> toMine,
        List<(float, float, float)> toCastle,
        float moveSpeed,
        Action onCompleted,
        bool fastForward)
    {
        pickupTarget = pickup;
        castleTarget = castle;
        sourceResourceIcon = sourceIcon;
        resourceIcon = icon;
        routeToMine = toMine;
        routeToCastle = toCastle;
        speed = moveSpeed;
        completed = onCompleted;

        if (pickupTarget == null || castleTarget == null ||
            !fastForward && (UtilsControl.Instance == null || routeToMine == null || routeToMine.Count == 0 ||
                             routeToCastle == null || routeToCastle.Count == 0))
        {
            Cancel();
            return false;
        }

        if (fastForward)
        {
            FastForward();
            return true;
        }

        sourceResourceIcon?.SetActive(true);
        resourceIcon?.SetActive(false);
        state = CarrierState.ToMine;
        MoveAlong(routeToMine, OnMineReached);
        return true;
    }

    public void FastForward()
    {
        if (state == CarrierState.Completed || state == CarrierState.Cancelled)
            return;

        StopMovement();
        sourceResourceIcon?.SetActive(false);
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
        sourceResourceIcon?.SetActive(false);
        completed = null;
        state = CarrierState.Cancelled;
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

    private void OnMineReached()
    {
        if (state != CarrierState.ToMine)
            return;

        sourceResourceIcon?.SetActive(false);
        resourceIcon?.SetActive(true);
        state = CarrierState.ToCastle;
        MoveAlong(routeToCastle, Complete);
    }

    private void MoveAlong(List<(float, float, float)> route, Action callback)
    {
        UtilsControl.Instance.MoveToMany(transform, speed, route, 0, callback);
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
