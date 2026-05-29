using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Features
{
    public class ScreenFader : FeatureBase
    {
        Image faderObject;

        public override void Setup(Main main)
        {
            base.Setup(main);
            var instance = UnityUtil.QuickInstantiate("screen_fader", Vector3.zero);
            faderObject = instance.GetComponentInChildren<Image>();
            faderObject.color = Color.black;
        }

        public void SetFade(float a)
        {
            faderObject.color = new Color(0, 0, 0, a);
        }

        public void FadeOut(float delay = 0f)
        {
            faderObject.color = Color.black;
            faderObject.DOFade(0f, 1f).SetDelay(delay);
        }

        public void FadeIn()
        {
            faderObject.DOFade(1f, 0.5f);
        }
    }
}