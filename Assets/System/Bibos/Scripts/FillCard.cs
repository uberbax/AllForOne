using System;
using TMPro;
using UnityEngine;

public class FillCard : MonoBehaviour
{
    public SpriteRenderer icon;
    public SpriteRenderer cost;
    
    public TMP_Text costText;
    public TMP_Text nameText;
    
    
    public string skill = "";
    public RObj parsedSkill = null;

    public GameObject cardEffect;
    public GameObject back;
    public GameObject front;
    private void OnEnable()
    {
        Fill();
    }

    public void Fill()
    {
        if (!ConfigLoader.parseEnded)
        {
            Invoke("Fill", 0.1f);
            return;
        }
        
        parsedSkill = DatabaseAll.instance.CreateProjectile(MainStates.instance.mainPlayer, skill, transform.position, false, false);
        icon.sprite = ResourceHolder.instance.skills[skill];
        costText.text = parsedSkill.GetPar("mana_req").ToString();
        nameText.text = ConfigLoader.Instance.GetMeLocale(skill);

        icon.transform.localScale *= 5 * 30.0f / icon.sprite.texture.width;

    }

    public void Deactivate()
    {
        back.SetActive(false);
        front.SetActive(false);
    }


}
