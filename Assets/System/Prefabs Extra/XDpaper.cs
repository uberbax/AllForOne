using TMPro;
using UnityEngine;

public class XDpaper : MonoBehaviour
{

    public TextMeshProUGUI text;
    public void Activate(string s)
    {
        text.text = ConfigLoader.Instance.GetMeLocale(s);
    }
}
