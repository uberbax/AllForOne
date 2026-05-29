using UnityEngine;
using Util;

class GameplayScreen : ScreenBase
{
    public override void Init()
    {
        base.Init();
        
        var gameScreen = UnityUtil.QuickInstantiate("screen/game_screen", Vector3.zero);
        gameScreen.transform.parent = root.transform;
    }
}