using System;
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

    private Transform destination;
    private float travelSeconds;
    private Action arrived;

    public TraderState State => state;

    public bool Initialize(Transform castleDestination, float configuredTravelSeconds, bool waitForPerk, Action onArrived)
    {
        destination = castleDestination;
        travelSeconds = Mathf.Max(0.01f, configuredTravelSeconds);
        arrived = onArrived;

        if (destination == null || UtilsControl.Instance == null)
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
        UtilsControl.Instance.MoveTo(
            transform, 1f, destination.position, OnArrived, destination,
            z0: 0f, useRight: false, travelTm: travelSeconds);
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
