using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TmLib.HSprite
{
    public class EnemyCtrl : MonoBehaviour
    {
        public enum MoveState { idle=0, walk, damage, die };
        [SerializeField, Range(1f, 10f)] float m_moveSpeed = 2f;
        [SerializeField] Transform m_flipRootTr = null;
        [SerializeField] Rigidbody2D m_rb = null;
        [SerializeField] Animator m_animaotor = null;
        [SerializeField] SkinnedMeshRenderer m_smr = null;
        [SerializeField, Range(0f, 10000f)] float m_hitPoint = 10f;
        [SerializeField] Collider2D m_collider = null;
        PlayerCtrl m_targetPlayer = null;
        MoveState m_moveState = MoveState.idle;
        bool m_isInDamage;
        AudioSource m_ac;
        float m_walkTimer;
        Vector2 m_walkDir;

        private void Awake()
        {
            gameObject.SetActive(false);
            m_isInDamage = false;
        }
        // Start is called before the first frame update
        void Start()
        {
            m_ac = m_targetPlayer.gameObject.GetComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {
            if (m_isInDamage)
                return;

            if (m_targetPlayer == null)
                return;

            if (m_moveState != MoveState.walk)
                return;

            m_walkTimer -= Time.deltaTime;
            if (m_walkTimer <= 0f)
            {
                m_walkTimer += Random.Range(0.1f,2f);
                m_walkDir = m_targetPlayer.transform.position - transform.position;
                if (m_walkDir.sqrMagnitude > 0f)
                {
                    m_walkDir.Normalize();
                    //m_smr.material.SetVector("_Flip", new Vector3((m_walkDir.x > 0f) ? -1f : 1f, 1f, 1f));
                    m_flipRootTr.localScale = new Vector3((m_walkDir.x > 0f) ? -1f : 1f, 1f, 1f);
                }
            }
            Vector2 pos = transform.position;
            pos += m_walkDir * m_moveSpeed * Time.deltaTime;
            m_rb.MovePosition(pos);
        }

        public void SetTargetAndStart(GameObject _obj, Texture2D _tex)
        {
            gameObject.SetActive(true);
            m_targetPlayer = _obj.GetComponent<PlayerCtrl>();
            m_moveState = MoveState.idle;
            m_collider.enabled = true;
            m_smr.material.SetTexture("_MainTex", _tex);

            StartCoroutine(spawnCo());
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            toDamageCk(collision);
        }

        void toDamageCk(Collider2D collision)
        {
            if (!m_isInDamage)
            {
                StopAllCoroutines();
                StartCoroutine(damageCo(collision, Random.Range(1f, 30f)));
            }
        }
        IEnumerator spawnCo()
        {
            m_moveState = MoveState.idle;
            m_smr.material.SetColor("_Color", Color.gray);
            m_isInDamage = true;
            m_animaotor.Play("M_Spawn");
            yield return new WaitForSeconds(1f);
            m_isInDamage = false;
            m_moveState = MoveState.walk;
            m_animaotor.Play("H_Walk");
        }

        IEnumerator idleCo()
        {
            m_moveState = MoveState.idle;
            m_animaotor.Play("H_Idle");
            m_smr.material.SetColor("_Color", Color.gray);
            m_isInDamage = false;
            yield return new WaitForSeconds(Random.Range(0.2f, 1f));
            m_moveState = MoveState.walk;
            m_animaotor.Play("H_Walk");
            m_walkTimer = 0f;
        }

        IEnumerator damageCo(Collider2D collision, float _datage)
        {
            Vector3 moveDir = (transform.position - collision.transform.root.position).normalized;
            m_rb.AddForce(moveDir * 10f, ForceMode2D.Impulse);
            m_isInDamage = true;
            m_hitPoint -= _datage;
            m_smr.material.SetColor("_Color", Color.white);
            //m_smr.material.SetVector("_Flip", new Vector3((moveDir.x > 0) ? 1f : -1f, 1f, 1f));
            m_flipRootTr.localScale = new Vector3((m_walkDir.x > 0f) ? -1f : 1f, 1f, 1f);
            m_ac?.Play();
            if (m_hitPoint > 0f)
            {
                m_animaotor.Play("H_Damage");
                yield return new WaitForSeconds(0.5f);
                m_smr.material.SetColor("_Color", Color.gray);
                m_isInDamage = false;
                StartCoroutine(idleCo());
                yield break;
            }
            else
            {
                m_targetPlayer?.AddKillCount(1);
                m_animaotor.Play("M_Die");
                m_collider.enabled = false;
                yield return new WaitForSeconds(1f);
                m_smr.material.SetColor("_Color", Color.gray);
                m_isInDamage = false;
                gameObject.SetActive(false);
                yield break;
            }
        }
    }
}
