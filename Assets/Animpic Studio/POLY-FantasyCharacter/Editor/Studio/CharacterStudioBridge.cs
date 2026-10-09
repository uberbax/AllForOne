using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Animpic.CharacterStudio.Editor
{
    [InitializeOnLoad]
    public static class CharacterStudioBridge
    {
        private static double nextPoll;
        static CharacterStudioBridge(){EditorApplication.update+=Tick;}
        private static void Tick()
        {
            if(EditorApplication.timeSinceStartup<nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            nextPoll=EditorApplication.timeSinceStartup+1;
            const string request="Library/CharacterStudio.request";if(!File.Exists(request))return;
            string command=File.ReadAllText(request).Trim();File.Delete(request);
            try
            {
                if(command=="surround" && EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying=false; File.WriteAllText(request,command); return; }
                if(command=="surround")StudioSurround.UpdateOpenStudio();
                else if(command=="layout")StudioChoiceLayout.Apply();
                else if(command=="brand")StudioBranding.Apply();
                else if(command=="smooth-before")StudioUIAntialiasing.Capture("before");
                else if(command=="smooth")StudioUIAntialiasing.Upgrade();
                else if(command=="build")CharacterStudioSetup.BuildStudio();
                else if(command=="render")CharacterStudioSetup.RenderExamples();
                else if(command=="refresh")AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                else if(command=="validate" || command=="play")
                {
                    var type=typeof(CharacterStudioBridge).Assembly.GetType("Animpic.CharacterStudio.Editor.CharacterStudioValidation");
                    if(type==null)throw new InvalidOperationException("Validation not yet installed.");
                    type.GetMethod(command=="validate"?"ValidateAll":"ValidatePlayMode",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
                }
                else if(command=="ui" || command=="ui-play" || command=="ui-current")
                {
                    var type=typeof(CharacterStudioBridge).Assembly.GetType("Animpic.CharacterStudio.Editor.CharacterStudioUIValidation");
                    type.GetMethod((command=="ui-play" || (command=="ui-current" && Application.isPlaying))?"ValidateRuntimeUI":"ValidateEditUI",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
                }
                else if(command=="window")EditorApplication.ExecuteMenuItem("Tools/Animpic Studio/Characters/Fantasy Character/Open Studio");
                else throw new ArgumentException("Unknown studio command: "+command);
                File.WriteAllText("Library/CharacterStudio.result.txt",command+" OK "+DateTime.Now.ToString("O"));
            }
            catch(Exception e){File.WriteAllText("Library/CharacterStudio.result.txt",e.ToString());Debug.LogException(e);}
        }
    }
}
