using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.InputSystem;

// https://forpro.unity3d.jp/unity_pro_tips/2021/05/20/1957/
namespace TmLib.HSprite
{
    public class PlayerCtrl : MonoBehaviour
    {
        [SerializeField, Range(1f, 10f)] float m_moveSpeed = 2f;
        [SerializeField] Transform m_flipRootTr = null;
        [SerializeField] Transform m_particlePivotTr = null;
        [SerializeField] Rigidbody2D m_rb = null;
        [SerializeField] Animator m_animaotor = null;
        [SerializeField] SkinnedMeshRenderer m_smr = null;
        [SerializeField, Range(0f, 1000f)] float m_hitPoint = 100f;
        [SerializeField] Image m_hpImage = null;
        [SerializeField] TMPro.TMP_Text m_text = null;
        Vector2 m_moveDir;
        Quaternion m_fireRot = Quaternion.identity;
        float m_maxHitPoint;
        int m_killCount;
        bool m_isDied;

        // Start is called before the first frame update
        void Start()
        {
            m_isDied = false;
            m_maxHitPoint = m_hitPoint;
            m_hpImage.fillAmount = 1f;
            m_killCount = 0;
            AddKillCount(0);
        }

#if false // UnityEngine.InputSystem
        public void OnInputMove(InputAction.CallbackContext context)
        {
            m_moveDir = context.ReadValue<Vector2>();
        }
        void getInputValue(){}
#else
        void getInputValue()
        {
            Vector2 dir = Vector2.zero;
            if (Input.GetKey(KeyCode.DownArrow))    { dir.y -= 1f; }
            if (Input.GetKey(KeyCode.UpArrow))      { dir.y += 1f; }
            if (Input.GetKey(KeyCode.LeftArrow))    { dir.x -= 1f; }
            if (Input.GetKey(KeyCode.RightArrow))   { dir.x += 1f; }
            m_moveDir = (dir.sqrMagnitude > 0f) ? dir.normalized : Vector2.zero;
        }
#endif

        // Update is called once per frame
        void FixedUpdate()
        {
            if (m_isDied)
                return;

            getInputValue();
            Vector2 dir = m_moveDir;

            if (dir.sqrMagnitude > 0f)
            {
                dir.Normalize();
                if (dir.x > 0.1f)
                {
                    //m_smr.material.SetVector("_Flip", new Vector3(-1f,1f,1f));
                    m_flipRootTr.localScale = new Vector3(-1f, 1f, 1f);
                }
                else if (dir.x < -0.1f)
                {
                    //m_smr.material.SetVector("_Flip", new Vector3(1f, 1f, 1f));
                    m_flipRootTr.localScale = new Vector3(1f, 1f, 1f);
                }
                m_animaotor.Play("H_Walk");

                float angle = -Mathf.Atan2(dir.x, dir.y);
                m_fireRot = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);
                m_particlePivotTr.rotation = Quaternion.Lerp(m_particlePivotTr.rotation, m_fireRot, 0.1f);
                Vector2 pos = transform.position;
                pos += dir * m_moveSpeed * Time.fixedDeltaTime;
                m_rb.linearVelocity = Vector2.zero; // fixing a bug of 2021.3.0 
                m_rb.MovePosition(pos);
            }
            else
            {
                m_animaotor.Play("H_Idle");
            }
        }

        public void AddKillCount(int _num)
        {
            m_killCount += _num;
            m_text?.SetText(m_killCount.ToString());
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (m_isDied)
                return;

            if (collision.gameObject.GetComponent<EnemyCtrl>() != null)
            {
                StopAllCoroutines();
                StartCoroutine(damageCo());
            }
        }

        IEnumerator damageCo()
        {
            m_hitPoint = Mathf.Max(0f, m_hitPoint - 10);
            m_hpImage.fillAmount = m_hitPoint / m_maxHitPoint;
            if (m_hitPoint <= 0f)
            {
                m_isDied = true;
                m_animaotor.Play("M_Die");
                m_smr.material.SetColor("_Color", Color.red);
                m_particlePivotTr.gameObject.SetActive(false);
            }
            m_smr.material.SetColor("_Color", Color.red);
            yield return new WaitForSeconds(0.2f);
            m_smr.material.SetColor("_Color", Color.gray);
        }
    }
}
