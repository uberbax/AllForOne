using Features;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityGameOver : MonoBehaviour
{
    bool done;
    
    void Update()
    {
        if (done)
            return;
        
        if (GetComponent<CanvasGroup>().alpha == 0)
            return;
        
        if (Input.GetMouseButtonDown(0))
        {
            done = true;
            
            UnityShell.main.Get<ScreenFader>().FadeIn();
            Invoke(nameof(Load), 0.5f);

        }
    }

    void Load()
    {
        SceneManager.LoadScene("main");
    }
}