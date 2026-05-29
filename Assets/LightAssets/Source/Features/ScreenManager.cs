using System.Collections.Generic;
using UnityEngine;

public interface IScreen
{
    public void Init();
    public void Show();
    public void Tick(float dt);
    public void Hide();
}

public class ScreenBase : IScreen
{
    protected GameObject root;

    public virtual void Init()
    {
        root = new GameObject("screen_" + GetType());
        root.SetActive(false);
    }

    public virtual void Show()
    {
        root.SetActive(true);
    }

    public virtual void Tick(float dt)
    {
    }

    public virtual void Hide()
    {
        root.SetActive(false);
    }
}

class ScreenManager : FeatureBase
{
    List<IScreen> _all = new List<IScreen>();
    IScreen _current;

    public void AddScreen<T>() where T : IScreen, new()
    {
        var screen = new T();
        screen.Init();
        _all.Add(screen);
    }
    
    public void HideAll()
    {
        foreach (var s in _all)
            if (s == _current)
                s.Hide();
    }
    
    public void Show<T>() where T : IScreen
    {
        foreach (var s in _all)
            if (s is T)
            {
                _current = s;
                s.Show();
            }
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);
        _current?.Tick(dt);
    }
}