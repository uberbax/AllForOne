using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TmLib.HSprite
{
    public class AnimationChecker : MonoBehaviour
    {
        [System.Serializable]
        public class AminParams
        {
            public Texture2D mainTex;
            public Texture2D maskTex;
            public Vector2 maskScale=new Vector2(1f,1f);
        }

        [SerializeField] Animator m_quadAnimaotor = null;
        [SerializeField] Animator m_charAnimaotor = null;
        [SerializeField] SkinnedMeshRenderer m_quadSmr = null;
        [SerializeField] SkinnedMeshRenderer m_charSmr = null;
        [SerializeField] Text m_clipNameText = null;
        [SerializeField] Transform m_quadFlipRootTr = null;
        [SerializeField] Transform m_charFlipRootTr = null;
        [SerializeField] AminParams[] m_paramsArr = null;
        [SerializeField] AnimationClip[] m_AnimationClipArr=null;
        int m_animationPtr;
        int m_characterPtr;
        bool m_isFlip;

        // Start is called before the first frame update
        void Start()
        {
            m_animationPtr = 0;
            m_characterPtr = -1;
            m_isFlip = false;
            //m_quadAnimaotor.runtimeAnimatorController = m_charAnimaotor.runtimeAnimatorController;
            m_AnimationClipArr = m_charAnimaotor.runtimeAnimatorController.animationClips;

            OnNextMotionButton(0);
            OnCharacterChangeButton();
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void OnCharacterChangeButton()
        {
            m_characterPtr = (m_characterPtr + 1) % m_paramsArr.Length;
            setCharacter(m_characterPtr);
            OnPlayButton();
        }

        public void OnPlayButton()
        {
            if ((m_AnimationClipArr != null) && (m_AnimationClipArr.Length > m_animationPtr))
            {
                m_quadAnimaotor.Play(m_AnimationClipArr[m_animationPtr].name);
                m_charAnimaotor.Play(m_AnimationClipArr[m_animationPtr].name);
            }
        }
        public void OnNextMotionButton(int _jump)
        {
            if ((m_AnimationClipArr != null) && (m_AnimationClipArr.Length > 0))
            {
                m_animationPtr = (m_animationPtr + m_AnimationClipArr.Length + _jump) % m_AnimationClipArr.Length;
                m_clipNameText.text = m_AnimationClipArr[m_animationPtr].name;
                OnPlayButton();
            }
        }

        public void OnFlipButton()
        {
            m_isFlip = !m_isFlip;

#if false // Shader Flip don't flip position and rotation. 
            m_charSmr.material.SetVector("_Flip", new Vector4((m_isFlip ? -1 : 1), 1, 1, 1));
            m_quadSmr.material.SetVector("_Flip", new Vector4((m_isFlip ? -1 : 1), 1, 1, 1));
#else
            m_quadFlipRootTr.transform.localScale = new Vector3((m_isFlip ? -1 : 1), 1, 1);
            m_charFlipRootTr.transform.localScale = new Vector3((m_isFlip ? -1 : 1), 1, 1);
#endif
            OnPlayButton();
        }

        void setCharacter(int _id)
        {
            m_charSmr.material.SetTexture("_MainTex", m_paramsArr[_id].mainTex);
            m_charSmr.material.SetTexture("_MaskTex", m_paramsArr[_id].maskTex);
            m_charSmr.material.SetTextureScale("_MainTex", Vector2.one);
            m_charSmr.material.SetTextureScale("_MaskTex", m_paramsArr[_id].maskScale);

            m_quadSmr.material.SetTexture("_MaskTex", m_paramsArr[_id].maskTex);
            //m_quadSmr.material.SetTextureScale("_MainTex", Vector2.one);
            m_quadSmr.material.SetTextureScale("_MaskTex", m_paramsArr[_id].maskScale);
        }
    }
}
