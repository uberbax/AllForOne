using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

    public class DragObject : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public static GameObject DraggableObject { get; set; }

        public Action<Transform> onEndDrag = null;
        public static Action<Transform, Vector3> onEndDragGlobal = null;

        
        public Transform par;
        public Transform iPar;
        public Action<GameObject> onClick;

        private CanvasGroup prev;
        private Button btnPrev; 
        
        public Inventary invCont;

        public bool dontInst = false;
        public bool noFinale = false;
        public bool noRemoveRaycast = false;
        
        Vector2 dlt = Vector2.zero;
        public static bool inDrag = false;
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (inDrag) return;
            
            //?
            if (dontInst) DraggableObject = eventData.pointerDrag;
            else
            {
                DraggableObject = Instantiate(eventData.pointerDrag, transform.parent);
                DraggableObject.name += "_drag";
            }
            //?
            prev = eventData.pointerDrag.GetComponent<CanvasGroup>();
            btnPrev = eventData.pointerDrag.GetComponent<Button>();
            if (prev != null)
            {
                prev.alpha = 0.5f;
            }

            if (btnPrev != null)
            {
                btnPrev.interactable = false;
            }

            //?
            //DraggableObject = eventData.pointerDrag;
            
            //?
            par = eventData.pointerDrag.transform.parent.parent.parent;
            iPar = eventData.pointerDrag.transform.parent;
            
            inDrag = true;
            //?
            DraggableObject.GetComponent<DragObject>().par = par;
            DraggableObject.GetComponent<DragObject>().iPar = iPar;
            
            DraggableObject.GetComponent<ObjHolder>().obj = eventData.pointerDrag.GetComponent<ObjHolder>().obj;
            DraggableObject.GetComponent<ObjHolder>().inDrag = true;
            //DraggableObject.GetComponent<DragObject>().invCont = eventData.pointerDrag.transform.GetComponentInParent<UnoChestUI>().inventary;
            //DraggableObject.GetComponent<DragObject>().chest = eventData.pointerDrag.GetComponent<DragObject>().chest;
            
            /*
            if (eventData.pointerDrag.GetComponent<ViewItem>() != null)
            {
                DraggableObject.GetComponent<ViewItem>().onClick =
                    eventData.pointerDrag.GetComponent<ViewItem>().onClick;

                DraggableObject.GetComponent<ViewItem>().item =
                    eventData.pointerDrag.GetComponent<ViewItem>().item;
            }
            */
            
            if (!noRemoveRaycast)
                DraggableObject.GetComponent<Image>().raycastTarget = false;
            
            //?
            if (!noFinale)
                DraggableObject.transform.parent = GameObject.FindGameObjectWithTag("Finale").transform;
            
            dlt = eventData.position - (Vector2)DraggableObject.transform.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            
            DraggableObject.GetComponent<RectTransform>().position = eventData.position - dlt;

        }

        public void OnEndDrag(PointerEventData eventData)
        {
            inDrag = false;
            if (DraggableObject!=null)
            {
                DraggableObject.GetComponent<ObjHolder>().inDrag = false;
                
                if (prev != null)
                    prev.alpha = 1;
                
                if (btnPrev != null)
                    btnPrev.interactable = true;

                
                if (onEndDrag != null) onEndDrag(DraggableObject.transform);
                if (onEndDragGlobal != null) onEndDragGlobal(DraggableObject.transform, dlt);
                

                if (!dontInst) Destroy(DraggableObject);
                DraggableObject = null;
            }
        }
    }
