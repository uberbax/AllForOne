#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class PickupNotificationPlayCheck
{
    const string Flag="Assets/System/UI/Editor/.pickup-ui-check";
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state==PlayModeStateChange.EnteredPlayMode && File.Exists(Flag)) { File.Delete(Flag); Run(); }
        };
    }
    static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
    static async void Run()
    {
        PickupNotificationStack stack=null; float oldScale=Time.timeScale;
        try {
            await Task.Delay(1500);
            var icon=ResourceHolder.instance.GetIcon("gold");
            PickupNotificationStack.Show("pickup_test_0","Золото",10,icon);
            stack=UnityEngine.Object.FindFirstObjectByType<PickupNotificationStack>();
            Require(stack!=null,"Resources prefab did not instantiate");
            for(int i=1;i<8;i++) stack.Enqueue("pickup_test_"+i,"Тестовый предмет "+i,i,icon);
            stack.Enqueue("pickup_test_0","Золото",25,icon);
            Require(stack.VisibleCount==5 && stack.PendingCount==3,"Visible stack / queue counts");
            var rows=stack.transform.Find("SafeArea/Rows");
            var first=rows.Cast<Transform>().First(t=>t.gameObject.activeSelf);
            Require(first.Find("Amount").GetComponent<TextMeshProUGUI>().text=="+35","Duplicate pickup merge");
            Require(stack.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"HUD blocks UI input");
            Require(stack.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay && stack.transform.lossyScale.x>0,"Canvas setup");
            Time.timeScale=0;
            await Task.Delay(350);
            var visible=rows.Cast<Transform>().Where(t=>t.gameObject.activeSelf).Select(t=>(RectTransform)t).ToArray();
            Require(visible.Length==5 && first.GetComponent<CanvasGroup>().alpha>.99f,"Pause-time fade in");
            for(int i=1;i<visible.Length;i++) {
                var a=new Vector3[4]; var b=new Vector3[4]; visible[i-1].GetWorldCorners(a); visible[i].GetWorldCorners(b);
                Require(a[0].y>b[1].y,"Notification rows overlap");
            }
            await Task.Delay(3000);
            Require(stack.PendingCount==0 && stack.VisibleCount==3,"Queue not drained after expiry during pause");
            await Task.Delay(3300);
            Require(stack.VisibleCount==0,"Notifications did not expire");
            int allocated=rows.childCount;
            stack.Enqueue("pool_test","Повторное использование",1,icon);
            Require(stack.VisibleCount==1 && rows.childCount==allocated,"Row pool not reused");
            File.WriteAllText(Path.Combine(Path.GetTempPath(),"pickup-ui-check.txt"),"PASS: lazy Resources prefab, 5 visible / 3 queued, duplicate amount +35, no UI interception, non-overlapping screen rows, unscaled fade and queue expiry at timeScale 0, pooled row reuse. Allocated rows="+(allocated-1));
        } catch(Exception e) { File.WriteAllText(Path.Combine(Path.GetTempPath(),"pickup-ui-check.txt"),"FAIL: "+e); Debug.LogException(e); }
        finally { Time.timeScale=oldScale; if(stack!=null) UnityEngine.Object.Destroy(stack.gameObject); }
    }
}
#endif
