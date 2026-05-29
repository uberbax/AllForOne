using UnityEngine;

public class PrAnimat : MonoBehaviour
{
    public string state = "idle";
    public float bounceAmplitude = 0.05f;
    public float bounceFrequency = 10f;
    private float savedY = 1;
    public void CrossFade(string what, float tm = 0.2f)
    {
        state = what;
    }

    void Start()
    {
        savedY = transform.localScale.y;
    }
        
    string prevState = "";
    // Update is called once per frame
    void Update()
    {
        
        var f = GetComponent<SpriterAnim>();
        if (f != null)
        {
            //enabled = false;
            return;
        }
        
        
        if (state == "idle")
        {
            // breathing effect
            float bounce = 1 + Mathf.Sin(Time.time * bounceFrequency) * bounceAmplitude;
            transform.localScale = new Vector3(transform.localScale.x, bounce * savedY, 1f);
            transform.localPosition = Vector3.zero;
        }
        else if (state == "walk")
        {
            Vector3 walkbounce = new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 20f) * 0.05f), 0);
            transform.localPosition = walkbounce;
        }
        else if (state == "attack")
        {
            if (prevState != state)
            {
                //Debug.Log(Time.time);
                UtilsControl.Instance.ApplyCurve(transform, AnimationCurve.EaseInOut(0, 0, 1, 1),
                    UtilsControl.CurveType.ShaderVal, null, 0.33f, 3, 1,
                    0, Color.white, true, was: 0, now: -1f, str: "_ShearX");
            }
        }
        
        prevState = state;
    }
}
