using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TmLib.HSprite
{
    public class SpawnEnemyCtrl : MonoBehaviour
    {
        [SerializeField] Texture2D[] m_enemyTexArr = null;
        [SerializeField] GameObject m_enemyPrefab = null;
        [SerializeField] Transform m_spawnTargetParentTr = null;
        [SerializeField] GameObject m_targetObj = null;
        [SerializeField, Range(0.05f, 5f)] float m_spawnFrequency = 1f;
        [SerializeField, Range(10, 1000)] int m_objPoolNum = 1000;
        EnemyCtrl[] m_objPoolArray;

        // Start is called before the first frame update
        void Start()
        {
            m_objPoolArray = new EnemyCtrl[m_objPoolNum];
            for(int i=0;i< m_objPoolArray.Length; ++i)
            {
                GameObject go = Instantiate(m_enemyPrefab, transform);
                go.SetActive(false);
                m_objPoolArray[i] = go.GetComponent<EnemyCtrl>();
            }

            StartCoroutine(EnemySpawnCo());
        }

        // Update is called once per frame
        void Update()
        {

        }

        EnemyCtrl pullWork()
        {
            EnemyCtrl ret = null;
            for (int i = 0; i < m_objPoolArray.Length; ++i)
            {
                if (!m_objPoolArray[i].gameObject.activeSelf)
                {
                    ret = m_objPoolArray[i];
                    break;
                }
            }
            return ret;
        }

        void pushWork(GameObject _obj)
        {
            _obj.SetActive(false);
        }


        IEnumerator EnemySpawnCo()
        {
            float timer = m_spawnFrequency;
            while (true)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer += m_spawnFrequency;
                    EnemyCtrl em = pullWork();
                    if (em != null)
                    {
                        Transform tr = m_spawnTargetParentTr.GetChild(Random.Range(0, m_spawnTargetParentTr.childCount));
                        em.transform.position = tr.position + Vector3.up * 1f;
                        em?.SetTargetAndStart(m_targetObj, m_enemyTexArr[Random.Range(0, m_enemyTexArr.Length)]);
                    }
                }
                yield return null;
            }
        }
    }
}
