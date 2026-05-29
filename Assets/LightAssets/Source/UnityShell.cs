using Features;
using UnityEngine;

public class UnityShell : MonoBehaviour
{
    public static Main main;

    void Start()
    {
        main = new Main();

        main.Add<ScreenManager>();
        main.Add<ScreenFader>();
        main.Add<GameMaster>();
        
        main.Start();
    }

    void FixedUpdate()
    {
        main.Tick(Time.fixedDeltaTime);
    }
}