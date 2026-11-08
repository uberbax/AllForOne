#if UNITY_EDITOR
using System.IO;
using UnityEditor;
public static class PubEditorTaskPump
{
    static double nextRefresh;
    [MenuItem("Tools/Diagnostics/Manual Requests/Start Pub Task Pump")]
    private static void StartPump()
    {
        EditorApplication.update -= Tick;
        if (File.Exists(PubInspector.Work + "/keepalive.request"))
            EditorApplication.update += Tick;
    }
    static void Tick()
    {
        if(!File.Exists(PubInspector.Work+"/keepalive.request")){EditorApplication.update-=Tick;return;}
        EditorApplication.QueuePlayerLoopUpdate();
        if(!EditorApplication.isPlaying&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&EditorApplication.timeSinceStartup>=nextRefresh)
        {nextRefresh=EditorApplication.timeSinceStartup+4;AssetDatabase.Refresh();}
    }
}
#endif
