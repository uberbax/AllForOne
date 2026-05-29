using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

//https://walkable-2020.com/unity/tilema-script/
namespace TmLib.CollTileTest
{
    public class TilemapCollTest : MonoBehaviour
    {
        [SerializeField] Tilemap m_tilemap = null;
        //[SerializeField] TilemapRenderer m_tmRendere = null;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log("---");
            for (int i=0;i< collision.contactCount; ++i)
            {
                Vector3Int iPos = m_tilemap.layoutGrid.WorldToCell(collision.contacts[i].point);
                Debug.Log(iPos.ToString());
            }
            Debug.Log("---");
        }
    }
}
