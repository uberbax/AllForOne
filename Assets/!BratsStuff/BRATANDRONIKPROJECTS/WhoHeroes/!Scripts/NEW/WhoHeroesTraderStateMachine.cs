using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WhoHeroesTraderStateMachine : ComponentBehavior
{
    public enum TraderState
    {
        Inactive,
        WaitingForPerk,
        MovingToCastle,
        WaitingAtCastle,
        Completed,
        Cancelled
    }

    [SerializeField] private TraderState state = TraderState.Inactive;

    private List<(float, float, float)> roadRoute;
    private float moveSpeed;
    private Action arrived;

    public TraderState State => state;

    public bool Initialize(
        List<(float, float, float)> route,
        float configuredMoveSpeed,
        bool waitForPerk,
        Action onArrived)
    {
        roadRoute = route;
        moveSpeed = configuredMoveSpeed;
        arrived = onArrived;

        if (roadRoute == null || roadRoute.Count == 0 || moveSpeed <= 0f || UtilsControl.Instance == null)
        {
            Cancel();
            return false;
        }

        state = waitForPerk ? TraderState.WaitingForPerk : TraderState.Inactive;
        if (!waitForPerk)
            ResumeAfterPerk();
        return true;
    }

    public void ResumeAfterPerk()
    {
        if (state != TraderState.WaitingForPerk && state != TraderState.Inactive)
            return;

        state = TraderState.MovingToCastle;
        UtilsControl.Instance.MoveToMany(transform, moveSpeed, roadRoute, 0, OnArrived);
    }

    public void WaitForPerk()
    {
        if (state == TraderState.Completed || state == TraderState.Cancelled ||
            state == TraderState.WaitingAtCastle)
            return;
        StopMovement();
        state = TraderState.WaitingForPerk;
    }

    public void Complete()
    {
        if (state == TraderState.Completed || state == TraderState.Cancelled)
            return;
        StopMovement();
        arrived = null;
        state = TraderState.Completed;
    }

    public void Cancel()
    {
        if (state == TraderState.Completed || state == TraderState.Cancelled)
            return;
        StopMovement();
        arrived = null;
        state = TraderState.Cancelled;
    }

    private void OnArrived()
    {
        if (state != TraderState.MovingToCastle)
            return;

        state = TraderState.WaitingAtCastle;
        var callback = arrived;
        arrived = null;
        callback?.Invoke();
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
        arrived = null;
    }
}
