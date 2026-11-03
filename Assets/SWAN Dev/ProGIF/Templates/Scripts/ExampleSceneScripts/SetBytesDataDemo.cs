
using UnityEngine;

public class SetBytesDataDemo : MonoBehaviour
{
    public ProGifPlayerImage m_PlayerImageBytesSource;

    public ProGifPlayerImage m_PlayerImageBytesDestination;

    public void SetBytesFromSourceToDestination()
    {
        m_PlayerImageBytesDestination.SetBytes(m_PlayerImageBytesSource.GetBytes(), play:true);
    }

}
