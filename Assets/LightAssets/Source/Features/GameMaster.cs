using Features;

class GameMaster : FeatureBase
{
    public override void Setup(Main main)
    {
        base.Setup(main);
        
        var screenManager = main.Get<ScreenManager>();
        screenManager.AddScreen<GameplayScreen>();
    }

    public override void Start()
    {
        base.Start();
        
        var screenManager = main.Get<ScreenManager>();
        screenManager.Show<GameplayScreen>();

        var screenFader = main.Get<ScreenFader>();
        screenFader.FadeOut(1f);
    }
}