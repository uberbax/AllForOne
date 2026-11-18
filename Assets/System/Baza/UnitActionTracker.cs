using System;
using System.Collections;
using UnityEngine;

// Tracks asynchronous work belonging to a turn without scanning the world every frame.
public sealed class UnitActionTracker : MonoBehaviour
{
    private int pending;
    private int generation;
    public bool Busy => pending > 0 && isActiveAndEnabled;

    private sealed class Lease : IDisposable
    {
        private UnitActionTracker tracker;
        private readonly int generation;
        public Lease(UnitActionTracker tracker)
        {
            this.tracker = tracker;
            generation = tracker.generation;
            tracker.pending++;
        }
        public void Dispose()
        {
            if (tracker != null && tracker.generation == generation)
                tracker.pending = Mathf.Max(0, tracker.pending - 1);
            tracker = null;
        }
    }

    public static UnitActionTracker For(RObj unit)
    {
        if (unit?.main == null) return null;
        return unit.main.GetComponent<UnitActionTracker>() ?? unit.main.AddComponent<UnitActionTracker>();
    }

    public static IDisposable Begin(RObj unit)
    {
        if (MainStates.instance == null || !MainStates.instance.InIteration ||
            unit?.main == null || !unit.main.activeInHierarchy) return null;
        return new Lease(For(unit));
    }

    public static void Schedule(RObj unit, Action action, float delay)
    {
        var lease = Begin(unit);
        if (lease == null)
        {
            FunctionTimer.Create(action, delay);
            return;
        }
        var tracker = For(unit);
        if (delay <= 0)
        {
            try { action?.Invoke(); }
            finally { lease.Dispose(); }
        }
        else tracker.StartCoroutine(InvokeLater(action, delay, lease));
    }

    private static IEnumerator InvokeLater(Action action, float delay, IDisposable lease)
    {
        try
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
        finally { lease.Dispose(); }
    }

    public static void WatchProjectile(RObj owner, GameObject projectile)
    {
        var lease = Begin(owner);
        if (lease != null) For(owner).StartCoroutine(WaitForProjectile(projectile, lease));
    }

    private static IEnumerator WaitForProjectile(GameObject projectile, IDisposable lease)
    {
        try { while (projectile != null) yield return null; }
        finally { lease.Dispose(); }
    }

    public static void WatchAnimation(RObj owner)
    {
        var lease = Begin(owner);
        if (lease == null) return;
        var animator = owner.visuals.TryGetValue("animator", out var view) && view != null
            ? view.GetComponentInChildren<XDanimator>() : null;
        var video = owner.visMain != null ? owner.visMain.GetComponent<UiLoopVideo>() : null;
        var slotter = owner.visMain != null ? owner.visMain.GetComponentInChildren<Slotter>() : null;
        For(owner).StartCoroutine(WaitForAnimation(owner, animator, video, slotter, lease));
    }

    private static IEnumerator WaitForAnimation(RObj owner, XDanimator animator, UiLoopVideo video, Slotter slotter, IDisposable lease)
    {
        try
        {
            float deadline = Time.unscaledTime + (video != null ? video.GetAnimationDuration("attack") + 10f : 5f);
            while (owner.main != null && owner.main.activeInHierarchy && owner.GetPar("health") > 0 &&
                ((animator != null && animator.prevState == "attack") || (slotter != null && slotter.IsAttacking) ||
                    (video != null && video.IsReactionPlaying)) &&
                Time.unscaledTime < deadline)
            {
                if (video != null && video.IsAnimationFinished("attack") && !video.IsReactionPlaying &&
                    (slotter == null || !slotter.IsAttacking)) break;
                yield return null;
            }
        }
        finally { lease.Dispose(); }
    }

    private void OnDisable()
    {
        generation++;
        pending = 0;
        StopAllCoroutines();
    }
}
