using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PresentationExtraCapture
{
    const string Key="PresentationExtraCapture.Active";
    const string ScoreKey="ScienceStudio.BestStars.NetFold";
    static int step;
    static double next;
    static bool scoreSaved,scoreExisted;
    static int score;
    static string Dir=>Path.GetFullPath("Presentation/.build/visual-extra");
    static PresentationExtraCapture(){EditorApplication.update+=Tick;}
    public static void Run(){Directory.CreateDirectory(Dir);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.EnterPlaymode();}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
        try{
            Debug.Log("[ExtraCapture] Step "+step);
            switch(step++){
                case 0:next=EditorApplication.timeSinceStartup+7;return;
                case 1:
                    UnityEngine.Object.FindObjectsOfType<Button>().First(b=>b.name=="Settings").onClick.Invoke();break;
                case 2:Capture("17_language_settings",false);break;
                case 3:SceneManager.LoadScene("NetFoldFree");break;
                case 4:UnityEngine.Object.FindObjectOfType<NetFoldLab>().ShowFreeShape(ShapeType.TriangularPrism);break;
                case 5:Capture("18_prism_hero",true,0.82f);break;
                case 6:UnityEngine.Object.FindObjectOfType<NetFoldLab>().ShowFreeShape(ShapeType.Cone);break;
                case 7:Capture("19_cone_hero",true,0.82f);break;
                case 8:SceneManager.LoadScene("ParticleDriftFree");break;
                case 9:UnityEngine.Object.FindObjectOfType<ExperimentController>().ToggleView();break;
                case 10:Capture("20_particle_hero",true);break;
                case 11:SceneManager.LoadScene("NetFoldChallenge");break;
                case 12:
                    scoreExisted=PlayerPrefs.HasKey(ScoreKey);score=PlayerPrefs.GetInt(ScoreKey);scoreSaved=true;
                    Resources.FindObjectsOfTypeAll<ResultPanel>().First(r=>r.gameObject.scene.isLoaded).Show(3,Loc.Get("stars.3"));break;
                case 13:Capture("21_challenge_result",false);RestoreScore();break;
                case 14:SceneManager.LoadScene("LensCraft");break;
                case 15:UnityEngine.Object.FindObjectOfType<OpticalBenchController>().NextStep();break;
                case 16:Capture("22_lens_lesson",false);break;
                default:
                    RestoreScore();File.WriteAllText(Path.Combine(Dir,"complete.txt"),"6 additional real Unity Play Mode screenshots at 1920x1080. Score preference restored. Source scenes unchanged.");SessionState.SetBool(Key,false);EditorApplication.Exit(0);return;
            }
            next=EditorApplication.timeSinceStartup+3;
        }catch(Exception e){RestoreScore();Debug.LogException(e);File.WriteAllText(Path.Combine(Dir,"error.txt"),e.ToString());SessionState.SetBool(Key,false);EditorApplication.Exit(1);}
    }
    static void RestoreScore(){if(!scoreSaved)return;if(scoreExisted)PlayerPrefs.SetInt(ScoreKey,score);else PlayerPrefs.DeleteKey(ScoreKey);PlayerPrefs.Save();scoreSaved=false;}
    static void Capture(string name,bool hideUI,float fovScale=1)
    {
        Camera cam=Camera.main;
        if(cam==null)cam=UnityEngine.Object.FindObjectsOfType<Camera>().FirstOrDefault(c=>c.enabled);
        if(cam==null)throw new Exception("No camera");
        float oldFov=cam.fieldOfView;cam.fieldOfView*=fovScale;
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);var oldRT=cam.targetTexture;cam.targetTexture=rt;
        var canvases=UnityEngine.Object.FindObjectsOfType<Canvas>().Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        foreach(var c in canvases){if(hideUI){c.enabled=false;continue;}c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=.3f;}
        Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
        File.WriteAllBytes(Path.Combine(Dir,name+".png"),tex.EncodeToPNG());
        foreach(var c in canvases){c.enabled=true;c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
        cam.targetTexture=oldRT;cam.fieldOfView=oldFov;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);
        Debug.Log("[ExtraCapture] Saved "+name);
    }
}
