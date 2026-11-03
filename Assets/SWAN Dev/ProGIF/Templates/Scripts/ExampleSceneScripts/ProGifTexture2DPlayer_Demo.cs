
using UnityEngine;
using UnityEngine.UI;

public class ProGifTexture2DPlayer_Demo : MonoBehaviour
{
    public ProGifPlayerTexture2D m_ProGifPlayerTexture2D;

    public RawImage m_RawImage;
    public Renderer m_Renderer;

    public RawImage m_RawImage2;
    public Renderer m_Renderer2;

    void Start ()
    {
        // Use gif Player Component directly: -----------------------------------------------------
        m_ProGifPlayerTexture2D.Play("https://media.giphy.com/media/M8yU8U19nTkRKj6p4d/giphy.gif", false);
        m_ProGifPlayerTexture2D.OnTexture2DCallback = (texture2D) =>
        {
            // get and display the decoded texture here:
            m_RawImage.texture = texture2D;
            m_Renderer.material.mainTexture = texture2D;

            // set the texture to other texture fields of the shader
            //m_Renderer.material.SetTexture("_MetallicGlossMap", texture2D);
        };


        // Use the PGif manager: ------------------------------------------------------ 
        PGif.iPlayGif("https://media.giphy.com/media/p4wvewkDf9OYWjIW2P/giphy.gif", m_RawImage2.gameObject, "MyGifPlayerName 01", (texture2D) => {
            // get and display the decoded texture here:
            m_RawImage2.texture = texture2D;
        });
        
        PGif.iPlayGif("https://media.giphy.com/media/GOPutjEbvhBHmH455X/giphy.gif", m_Renderer2.gameObject, "MyGifPlayerName 02", (texture2D) => {
            // get and display the decoded texture here:
            m_Renderer2.material.mainTexture = texture2D;
        });
    }
}
