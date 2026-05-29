using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewSHLglobe : MonoBehaviour
{
    public TextMeshProUGUI statText;
    
    public Image cur1;
    public Image cur2;

    // Update is called once per frame
    void Update()
    {
        var s1 = MainStates.instance.mainPlayer.GetPar("shield");
        var s2 = MainStates.instance.mainPlayer.GetPar("max_health");
        
        statText.text = s1.ToString();
        float ratio = s1 / s2;
        cur1.fillAmount = ratio;
        cur2.fillAmount = ratio;
    }
}
