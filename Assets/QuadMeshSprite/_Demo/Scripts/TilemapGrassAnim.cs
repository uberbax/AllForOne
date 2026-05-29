using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TmLib.TileChangeTest
{
    public class TilemapGrassAnim : MonoBehaviour
    {
        [SerializeField] Animator m_anm = null;
        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(animGrassCo());
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void SetParams(TileBase _tile)
        {
            /*
            if (tile != null)
            {
                SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
                smr.material.SetTexture("_MainTex", tile.sprite.texture);
                smr.material.SetTextureOffset("_MainTex", tile.sprite.textureRect.position);
                smr.material.SetTextureScale("_MainTex", tile.sprite.textureRect.size);
            }
            */
        }

        IEnumerator animGrassCo()
        {
            SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
            m_anm.Play("M_Shake");
            Color col = smr.material.GetColor("_Color");
            float timer = 0f;
            while (timer < 1f)
            {
                timer = Mathf.Clamp01(timer + Time.deltaTime * 5f);
                col.a = timer;
                smr.material.SetColor("_Color", col);
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            while (timer > 0f)
            {
                timer = Mathf.Clamp01(timer - Time.deltaTime * 5f);
                col.a = timer;
                smr.material.SetColor("_Color", col);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}

