#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RumbleStartupAudit
{
    private const string Work = "C:/Users/s4032/Documents/Codex/2026-09-30/d-unity-projects-allforone-allforone-assets/work";
    [InitializeOnLoadMethod]
    private static void Requested()
    {
        if(File.Exists(Work+"/rumble-reader.request")) EditorApplication.delayCall += ReaderAudit;
        if (File.Exists(Work + "/rumble-startup-trace.request") && !File.Exists(Work + "/rumble-startup-audit.request"))
            EditorApplication.delayCall += StartTrace;
        if (!File.Exists(Work + "/rumble-startup-audit.request")) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlaying) return;
            File.Delete(Work + "/rumble-startup-audit.request");
            try { Audit(); if(File.Exists(Work + "/rumble-startup-trace.request")) StartTrace(); } catch (Exception e) { File.WriteAllText(Work + "/rumble-startup-audit.json", "FAIL: " + e); }
        };
    }
    [MenuItem("Tools/Diagnostics/Rumble startup audit")]
    public static void Audit()
    {
        var scene = SceneManager.GetActiveScene();
        var components = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c => c != null).ToArray();
        var configs = components.OfType<ConfigLoader>().ToArray();
        var report = new Dictionary<string, object>();
        report["scene"] = scene.path;
        report["sceneDirty"] = scene.isDirty;
        report["enterPlayModeOptions"] = EditorSettings.enterPlayModeOptions.ToString();
        report["objectCount"] = scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<Transform>(true).Length);
        report["activeBehaviours"] = components.Count(c => c.isActiveAndEnabled);
        report["activeBehaviourTypes"] = components.Where(c => c.isActiveAndEnabled).GroupBy(c => c.GetType().Name).OrderByDescending(g => g.Count()).Select(g => new { type=g.Key, count=g.Count() }).Take(30).ToArray();
        report["configs"] = configs.Select(c => new { path=AnimationUtility.CalculateTransformPath(c.transform,null), c.useLocalExcel, c.CONFIG_NAME, c.done, active=c.isActiveAndEnabled }).ToArray();
        var books = new List<object>();
        foreach (var name in configs.Where(c => c.isActiveAndEnabled && c.useLocalExcel).Select(c => c.CONFIG_NAME).Append("loc.xlsx").Distinct())
        {
            var timer = Stopwatch.StartNew();
            var file = Path.Combine(Application.streamingAssetsPath,name);
            if (!File.Exists(file)) { books.Add(new { name, error="File missing" }); continue; }
            Excel book;
            using (var input = new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
            using (var memory = new MemoryStream())
            {
                input.CopyTo(memory); memory.Position=0;
                book=ExcelHelper.LoadExcel(memory);
            }
            double loadMs=timer.Elapsed.TotalMilliseconds;
            var tables = new List<object>();
            foreach (var table in book.Tables)
            {
                timer.Restart(); var original = table.ToTSV(); double currentMs = timer.Elapsed.TotalMilliseconds;
                timer.Restart(); var equivalent = LegacyTsv(table); double previousMs = timer.Elapsed.TotalMilliseconds;
                tables.Add(new { name=table.TableName, rows=table.NumberOfRows, columns=table.NumberOfColumns, chars=original.Length, currentMs, previousMs, identical=original==equivalent });
            }
            books.Add(new { name, bytes=new FileInfo(file).Length, loadMs, tables });
        }
        report["excelBenchmarks"] = books;
        var frames = new List<object>();
        int first = UnityEditorInternal.ProfilerDriver.firstFrameIndex, last = UnityEditorInternal.ProfilerDriver.lastFrameIndex;
        report["profilerFrameRange"] = new[]{first,last};
        if (first >= 0)
        {
            for (int frame=Math.Max(first,last-180);frame<=last;frame++)
            {
                using (var view=UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(frame,0))
                {
                    if(!view.valid || view.frameTimeMs < 100) continue;
                    var samples=new List<object>();
                    for(int i=0;i<view.sampleCount;i++)
                    {
                        float ms=view.GetSampleTimeMs(i); if(ms<20) continue;
                        string n=view.GetSampleName(i);
                        samples.Add(new { name=n,ms });
                        if(samples.Count>=80) break;
                    }
                    frames.Add(new {frame, ms=view.frameTimeMs, samples});
                }
            }
        }
        report["slowRecordedFrames"] = frames;
        File.WriteAllText(Work + "/rumble-startup-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
    }
    [MenuItem("Tools/Diagnostics/Rumble Excel readers")]
    public static void ReaderAudit()
    {
        File.Delete(Work+"/rumble-reader.request");
        try
        {
            var reports=new List<object>();
            foreach(var name in new[]{"Config_exp.xlsx","loc.xlsx"})
            {
                byte[] bytes;
                using(var file=new FileStream(Path.Combine(Application.streamingAssetsPath,name),FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
                using(var buffer=new MemoryStream()) { file.CopyTo(buffer); bytes=buffer.ToArray(); }
                var timer=Stopwatch.StartNew();
                Excel legacy;
                double packageMs, copyMs;
                using(var stream=new MemoryStream(bytes))
                using(var package=new OfficeOpenXml.ExcelPackage(stream))
                {
                    packageMs=timer.Elapsed.TotalMilliseconds;
                    timer.Restart(); legacy=LegacyCopy(package.Workbook); copyMs=timer.Elapsed.TotalMilliseconds;
                    timer.Restart(); var optimized=new Excel(package.Workbook); double optimizedMs=timer.Elapsed.TotalMilliseconds;
                    var equal=legacy.Tables.Count==optimized.Tables.Count && legacy.Tables.Zip(optimized.Tables,(a,b)=>a.TableName==b.TableName && a.NumberOfRows==b.NumberOfRows && a.NumberOfColumns==b.NumberOfColumns && a.ToTSV()==b.ToTSV()).All(v=>v);
                    reports.Add(new {name,kind="constructor",previousMs=copyMs,currentMs=optimizedMs,identical=equal,tableCount=legacy.Tables.Count});
                }
                timer.Restart();
                Excel xml;
                using(var stream=new MemoryStream(bytes)) xml=ExcelHelper.LoadOpenXml(stream);
                double xmlMs=timer.Elapsed.TotalMilliseconds;
                var comparisons=new List<object>();
                foreach(var table in legacy.Tables)
                {
                    var other=xml.Tables.Find(t=>t.TableName==table.TableName);
                    string a=table.ToTSV(), b=other==null?null:other.ToTSV();
                    int firstDifference=-1;
                    if(a!=b && b!=null) { int i=0; while(i<Math.Min(a.Length,b.Length)&&a[i]==b[i])i++; firstDifference=i; }
                    comparisons.Add(new {name=table.TableName,identical=a==b,rows=table.NumberOfRows,columns=table.NumberOfColumns,xmlRows=other==null?-1:other.NumberOfRows,xmlColumns=other==null?-1:other.NumberOfColumns,firstDifference,
                        legacySample=firstDifference<0?null:a.Substring(Math.Max(0,firstDifference-30),Math.Min(90,a.Length-Math.Max(0,firstDifference-30))),
                        xmlSample=firstDifference<0?null:b.Substring(Math.Max(0,firstDifference-30),Math.Min(90,b.Length-Math.Max(0,firstDifference-30)))});
                }
                reports.Add(new {name,packageMs,copyMs,xmlMs,comparisons});
            }
            File.WriteAllText(Work+"/rumble-reader.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception e) { File.WriteAllText(Work+"/rumble-reader.json","FAIL: "+e); }
    }

    private static Excel LegacyCopy(OfficeOpenXml.ExcelWorkbook workbook)
    {
        var excel=new Excel();
        for(int i=1;i<=workbook.Worksheets.Count;i++)
        {
            var sheet=workbook.Worksheets[i];
            var table=new ExcelTable {TableName=sheet.Name, NumberOfRows=sheet.Dimension==null?0:sheet.Dimension.Rows, NumberOfColumns=sheet.Dimension==null?0:sheet.Dimension.Columns};
            for(int row=1;row<=table.NumberOfRows;row++)
            for(int col=1;col<=table.NumberOfColumns;col++)
            {
                string value="";
                if(sheet.Cells[row,col].Value!=null) value=sheet.Cells[row,col].Value.ToString();
                table.SetValue(row,col,value);
            }
            excel.Tables.Add(table);
        }
        return excel;
    }

    private static string LegacyTsv(ExcelTable table)
    {
        string result="";
        for(int row=1;row<=table.NumberOfRows;row++)
        {
            for(int col=1;col<=table.NumberOfColumns;col++)
            {
                var value=table.GetValue(row,col);
                if((value==null || value.ToString()=="") && col==1) break;
                if(col!=1) result+="\t";
                result+=string.Format("{0}",value);
            }
            result+="\n";
        }
        return result;
    }

    private static Stopwatch traceTimer;
    private static double enteredPlayMs;
    private static int traceUpdates;
    private static int firstTraceFrame;
    private static bool wasProfiling, wasEditorProfiling;

    [MenuItem("Tools/Diagnostics/Rumble startup trace")]
    public static void TraceStartup()
    {
        File.WriteAllText(Work+"/rumble-startup-trace.request","capture");
        StartTrace();
    }

    private static int traceArmUpdates;

    private static void StartTrace()
    {
        string request=Work+"/rumble-startup-trace.request";
        if(!File.Exists(request)) return;
        File.Delete(request);
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().name!="Game_Exp_Rumble")
        { File.WriteAllText(Work+"/rumble-startup-trace.json","Cannot trace: stop Play Mode and open Game_Exp_Rumble first."); return; }
        wasProfiling=UnityEditorInternal.ProfilerDriver.enabled;
        wasEditorProfiling=UnityEditorInternal.ProfilerDriver.profileEditor;
        UnityEditorInternal.ProfilerDriver.profileEditor=true;
        UnityEditorInternal.ProfilerDriver.enabled=true;
        traceArmUpdates=0;
        EditorApplication.update+=ArmTrace;
    }
    private static void ArmTrace()
    {
        if(++traceArmUpdates<3) return;
        EditorApplication.update-=ArmTrace;
        firstTraceFrame=UnityEditorInternal.ProfilerDriver.lastFrameIndex;
        enteredPlayMs=-1; traceUpdates=0; traceEvents.Clear(); traceTimer=Stopwatch.StartNew();
        EditorApplication.playModeStateChanged+=TraceMode;
        EditorApplication.update+=TraceUpdate;
        EditorApplication.EnterPlaymode();
    }
    private static readonly List<object> traceEvents=new List<object>();
    private static void TraceMode(PlayModeStateChange change)
    {
        traceEvents.Add(new {name=change.ToString(),ms=traceTimer.Elapsed.TotalMilliseconds});
        if(change==PlayModeStateChange.EnteredPlayMode) enteredPlayMs=traceTimer.Elapsed.TotalMilliseconds;
    }
    private static void TraceUpdate()
    {
        if(enteredPlayMs<0 || ++traceUpdates<20) return;
        EditorApplication.update-=TraceUpdate;
        EditorApplication.playModeStateChanged-=TraceMode;
        try
        {
            int first=UnityEditorInternal.ProfilerDriver.firstFrameIndex, last=UnityEditorInternal.ProfilerDriver.lastFrameIndex;
            var frames=new List<object>();
            for(int f=Math.Max(first,Math.Max(firstTraceFrame,last-90)); f<=last; f++)
            using(var view=UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(f,0))
            {
                if(!view.valid || view.frameTimeMs<30) continue;
                var samples=new List<object>();
                for(int i=0;i<view.sampleCount;i++)
                {
                    float ms=view.GetSampleTimeMs(i);
                    if(ms<10) continue;
                    samples.Add(new {name=view.GetSampleName(i),ms});
                    if(samples.Count>=180) break;
                }
                frames.Add(new {frame=f,ms=view.frameTimeMs,samples});
            }
            File.WriteAllText(Work+"/rumble-startup-trace.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {enterPlayMs=enteredPlayMs,totalCaptureMs=traceTimer.Elapsed.TotalMilliseconds,events=traceEvents,frames},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception e) { File.WriteAllText(Work+"/rumble-startup-trace.json","FAIL: "+e); }
        finally
        {
            UnityEditorInternal.ProfilerDriver.enabled=wasProfiling;
            UnityEditorInternal.ProfilerDriver.profileEditor=wasEditorProfiling;
        }
    }
}
#endif

