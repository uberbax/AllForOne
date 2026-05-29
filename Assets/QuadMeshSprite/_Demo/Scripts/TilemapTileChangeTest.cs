using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Events;

namespace TmLib.TileChangeTest
{
    public enum PositionType{ tile=0,world,character };
    [System.Serializable]
    public class CheckTileInfo
    {
        public Tilemap tilemap;
        public TileBase tileBase;
        public TileBase hitTileBase; // Set if Change Tile
        public GameObject hitObjPrefab; // Set if Use Effect by GameObject
        public Texture2D texture;
        public Vector3 offset;
        public PositionType type;
        public AudioClip audioClip;
        public UnityEvent tileChangeHandler;
    }



    public class TilemapTileChangeTest : MonoBehaviour
    {
        [SerializeField] CheckTileInfo[] m_ckTileInfoArr = null;
        Vector3Int[] m_posArr;
        SkinnedMeshRenderer m_smr;
        AudioSource m_audio;
        float m_clipRate;

        // Start is called before the first frame update
        void Start()
        {
            m_smr = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            m_audio = gameObject.GetComponentInChildren<AudioSource>();
            m_posArr = new Vector3Int[m_ckTileInfoArr.Length];
            for(int i=0;i< m_posArr.Length; ++i)
            {
                m_posArr[i] = new Vector3Int(-1, -1);
            }
            m_clipRate = 0f;
        }

        // Update is called once per frame
        void Update()
        {
            for(int i=0;i< m_ckTileInfoArr.Length; ++i)
            {
                tileUpdate(i);
            }
        }

        public void OnHideLowerBody(float _clipRate)
        {
            if (_clipRate != m_clipRate)
            {
                m_clipRate = _clipRate;
                m_smr?.material?.SetFloat("_Clip", _clipRate);
                // need setDirty? only UDP?
                m_smr?.gameObject.SetActive(false);
                m_smr?.gameObject.SetActive(true);
            }
        }

        bool tileUpdate(int _infoId)
        {
            CheckTileInfo info = m_ckTileInfoArr[_infoId];
            bool ret = false;
            if ((info != null) && (info.tilemap != null)&&(info.tilemap.layoutGrid!=null))
            {
                Vector3Int iPos = info.tilemap.layoutGrid.WorldToCell(transform.position - Vector3.up * 1.5f);
                if ((m_posArr[_infoId] - iPos) != Vector3Int.zero)
                {
                    m_posArr[_infoId] = iPos;
                    ret = true;

                    if (info.tilemap.HasTile(iPos))
                    {
                        TileBase tile = info.tilemap.GetTile(iPos);
                        if ((tile != null)&&(tile.name == info.tileBase.name))
                        {
                            if ((m_audio != null) && (info.audioClip != null))
                            {
                                m_audio.PlayOneShot(info.audioClip);
                                //m_audio.Play();
                            }
                            if (info.hitTileBase != null)
                            {
                                StartCoroutine(moveGrassCo(info, iPos));
                            }
                            else if(info.hitObjPrefab!= null)
                            {
                                GameObject go = Instantiate(info.hitObjPrefab);
                                if (info.texture != null)
                                {
                                    SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                                    if (smr != null)
                                    {
                                        smr.material.SetTexture("_MainTex", info.texture);
                                    }
                                }
                                if (info.type == PositionType.tile)
                                {
                                    go.transform.parent = info.tilemap.transform;
                                    Vector3 pos = info.tilemap.layoutGrid.GetCellCenterWorld(iPos);
                                    pos.z = info.tilemap.transform.position.z;
                                    go.transform.position = pos + info.offset;
                                }
                                else
                                {
                                    go.transform.position = transform.position + info.offset + new Vector3(0f, -1f, -3f);
                                    if (info.type == PositionType.character)
                                    {
                                        go.transform.parent = transform;
                                    }
                                }
                                //go.GetComponent<TilemapGrassAnim>().SetParams(tile);
                            }

                            if ((info.tileChangeHandler != null) && (info.tileChangeHandler.GetPersistentEventCount() > 0))
                            {
                                info.tileChangeHandler?.Invoke();
                            }
                        }
                    }
                }
            }
            return ret;
        }

        IEnumerator moveGrassCo(CheckTileInfo _info, Vector3Int _iPos)
        {
            _info.tilemap.SetTile(_iPos, _info.hitTileBase);
            yield return new WaitForSeconds(1f);
            _info.tilemap.SetTile(_iPos, _info.tileBase);
        }
    }
}
