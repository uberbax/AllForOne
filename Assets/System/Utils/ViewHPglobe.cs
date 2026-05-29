using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewHPglobe : MonoBehaviour
{
    public string stat;
    
    public TextMeshProUGUI statText;
    
    public Image currentHealthGlobe;

    // Update is called once per frame
    void Update()
    {
        var s1 = MainStates.instance.mainPlayer.GetPar(stat);
        var s2 = MainStates.instance.mainPlayer.GetPar("max_" + stat);
        statText.text = s1 + "/" + s2;
        float ratio = s1 / s2;
        
        currentHealthGlobe.rectTransform.localPosition = new Vector3(0, currentHealthGlobe.rectTransform.rect.height * ratio - currentHealthGlobe.rectTransform.rect.height, 0);
    }
}
