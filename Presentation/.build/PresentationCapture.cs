using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PresentationCapture
{
    const string Key = "PresentationCapture.Active";
    static int step;
    static double next;
    static string Dir => Path.GetFullPath("Presentation/.build/screenshots");
    static PresentationCapture() { EditorApplication.update += Tick; }
    public static void Run()
    {
        Directory.CreateDirectory(Dir);
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            Debug.Log("[PPTCapture] Step " + step);
            switch (step++)
            {
                case 0: next=EditorApplication.timeSinceStartup+8; return;
                case 1: Capture("01_main_menu", false); break;
                case 2: SceneManager.LoadScene("NetFoldFree"); break;
                case 3: Capture("02_netfold_cube", false); Capture("03_netfold_hero", true); break;
                case 4: UnityEngine.Object.FindObjectOfType<NetFoldLab>().Unfold(); break;
                case 5: Capture("04_netfold_unfold", false); break;
                case 6: var n=UnityEngine.Object.FindObjectOfType<NetFoldLab>(); n.Fold(); n.ToggleProjection(); break;
                case 7: Capture("05_netfold_projection", false); break;
                case 8: UnityEngine.Object.FindObjectOfType<NetFoldLab>().ToggleSection(); break;
                case 9: Capture("06_netfold_section", false); break;
                case 10: var n2=UnityEngine.Object.FindObjectOfType<NetFoldLab>(); n2.ShowFreeShape(ShapeType.Cylinder); n2.Unfold(); break;
                case 11: Capture("07_netfold_cylinder", false); break;
                case 12: SceneManager.LoadScene("ParticleDriftFree"); break;
                case 13: Capture("08_drift_lab", false); UnityEngine.Object.FindObjectOfType<ExperimentController>().ToggleView(); break;
                case 14: Capture("09_drift_micro", false); break;
                case 15: SceneManager.LoadScene("ParticleDrift"); break;
                case 16: var d=UnityEngine.Object.FindObjectOfType<ExperimentController>(); d.NextStep(); d.NextStep(); break;
                case 17: Capture("10_drift_temperature", false); UnityEngine.Object.FindObjectOfType<ExperimentController>().NextStep(); break;
                case 18: Capture("11_drift_diffusion", false); break;
                case 19: SceneManager.LoadScene("LensCraftFree"); break;
                case 20: var l=UnityEngine.Object.FindObjectOfType<OpticalBenchController>(); l.ShowRays=true; l.ShowGuides=true; l.AutoScreen=true; break;
                case 21: Capture("12_lens_bench", false); Capture("13_lens_hero",true); break;
                case 22: var l2=UnityEngine.Object.FindObjectOfType<OpticalBenchController>(); l2.LockCandle=true; l2.ObjectDistance=.22f; l2.ApplyPlacement(); break;
                case 23: Capture("14_lens_magnification",false); break;
                case 24: var l3=UnityEngine.Object.FindObjectOfType<OpticalBenchController>(); l3.ObjectDistance=.09f; l3.ApplyPlacement(); break;
                case 25: Capture("15_lens_virtual",false); break;
                case 26: SceneManager.LoadScene("NetFoldChallenge"); break;
                case 27: Capture("16_challenge",false); break;
                default:
                    File.WriteAllText(Path.Combine(Dir,"complete.txt"), "Unity 2022.3.62f3 Play Mode. Real runtime scene cameras rendered at 1920x1080. UI overlay converted temporarily to camera canvas for capture. Hero images hide UI only. Source scenes unchanged.");
                    SessionState.SetBool(Key,false); EditorApplication.Exit(0); return;
            }
            next=EditorApplication.timeSinceStartup+3.2;
        }
        catch(Exception e) { Debug.LogException(e); File.WriteAllText(Path.Combine(Dir,"error.txt"),e.ToString()); SessionState.SetBool(Key,false); EditorApplication.Exit(1); }
    }
    static void Capture(string name, bool hideUI)
    {
        Camera cam = Camera.main;
        if(cam == null) cam=UnityEngine.Object.FindObjectsOfType<Camera>().FirstOrDefault(x=>x.enabled);
        if(cam == null) throw new Exception("No camera");
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);
        var oldRT=cam.targetTexture;
        cam.targetTexture=rt;
        var canvases=UnityEngine.Object.FindObjectsOfType<Canvas>().Where(c=>c.isRootCanvas && c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        foreach(var c in canvases)
        {
            if(hideUI) { c.enabled=false; continue; }
            c.renderMode=RenderMode.ScreenSpaceCamera; c.worldCamera=cam; c.planeDistance=.3f;
        }
        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active=rt;
        var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,1920,1080),0,0); tex.Apply();
        File.WriteAllBytes(Path.Combine(Dir,name+".png"), tex.EncodeToPNG());
        foreach(var c in canvases) { c.enabled=true; c.renderMode=RenderMode.ScreenSpaceOverlay; c.worldCamera=null; }
        cam.targetTexture=oldRT; RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
        Debug.Log("[PPTCapture] Saved " + name);
    }
}
