using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TmLib.HSprite
{
    public class PlBullet00Thunder : MonoBehaviour
    {
        [SerializeField] CapsuleCollider2D m_attackColl = null;
        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(attackCo());
        }

        // Update is called once per frame
        void Update()
        {

        }

        IEnumerator attackCo()
        {
            while (true)
            {
                m_attackColl.enabled = true;
                yield return new WaitForSeconds(0.9f);
                m_attackColl.enabled = false;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
