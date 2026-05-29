using System;
using UnityEngine;
using UnityEngine.Analytics;

public class DragCard : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private bool isDrag = false;
    private Vector3 dlt;

    public Vector3 firstPos = Vector3.zero;
    private void OnMouseDown()
    {
        Debug.Log("OnMouseDown");
        isDrag = true;
        name += "_drag";
        
        var vec = MainStates.instance.otherCamera.ScreenToWorldPoint(Input.mousePosition);
        dlt = transform.position - vec;
        MainStates.instance.lastCard = transform;

        if (firstPos == Vector3.zero)
        {
            firstPos = transform.position;
        }
    }
    

    public void Released()
    {
        name = name.Replace("_drag", "");
        if (!isCasting && isDrag)
        {
            UtilsControl.Instance.MoveTo(transform, 100, firstPos, null, null);
        }
        else if (isDrag)
        {
            //its a cast ?
            SkillExecutor.instance.curTargeter.HandleExec();
        }
    }

    private bool isCasting = false;
    void LateUpdate()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Released();
            isDrag = false;
        }
        
        //place
        if (isDrag)
        {
            Vector3 screenPos = Input.mousePosition;
            //screenPos.z = 0.1f;
            var vec = MainStates.instance.otherCamera.ScreenToWorldPoint(screenPos);
            //Debug.Log(vec);
            transform.position = dlt + vec;
            
            //is surpress
            if (transform.position.y > MainStates.instance.cancelLine.position.y)
            {
                if (!isCasting)
                {
                    isCasting = true;
                    SkillExecutor.instance.CastSkill(MainStates.instance.mainPlayer, null, null, transform.GetComponent<FillCard>().skill);
                }
            }
            else
            {
                SkillExecutor.instance.CancelAction();
                isCasting = false;
            }
            
        }
        
    }
}
