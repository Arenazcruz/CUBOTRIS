using System.Collections;
using System.IO;
using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Cubotris.Tests
{
    public sealed partial class GameIntegrationTests
    {
        [UnityTest,Category("Visual")]
        public IEnumerator CaptureFeatureScreensAt1080And720()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)Assert.Ignore("La comprobación visual requiere un dispositivo gráfico.");
            var game=Object.FindAnyObjectByType<GameController>();
            for(int i=0;i<4;i++){game.Model.Board.TryLock(TetrominoType.I,Enumerable.Range(0,10).Select(x=>new Vector2Int(x,0)).ToArray());game.HardDrop();yield return Settled(game);}
            game.Hold();yield return null;
            yield return CaptureViews("game");game.HardDrop();yield return Settled(game);game.Session.AdvanceActiveTime(29);yield return CaptureViews("garbage-warning");
            yield return Tap(Key.Escape);yield return CaptureViews("pause");
            ButtonAt("PausePanel/PanelBackground/OptionsButton").onClick.Invoke();yield return null;yield return CaptureViews("options");
            ButtonAt("PauseOptionsPanel/PanelBackground/ControlsButton").onClick.Invoke();yield return null;yield return CaptureViews("controls");
            Object.FindAnyObjectByType<ControlsPanel>().Close();yield return null;
            ButtonAt("PauseOptionsPanel/PanelBackground/DisplayButton").onClick.Invoke();yield return null;yield return CaptureViews("display");
            Object.FindAnyObjectByType<DisplayPanel>().Close();yield return null;
            ButtonAt("PauseOptionsPanel/PanelBackground/BackButton").onClick.Invoke();yield return null;
            ButtonAt("PausePanel/PanelBackground/SaveButton").onClick.Invoke();yield return null;yield return CaptureViews("save");
            Object.FindAnyObjectByType<SaveAndExitPanel>().Close();yield return null;yield return Tap(Key.Escape);
            yield return LoseGame(game);yield return null;yield return CaptureViews("game-over");
            BestScoreStore.History.TryAdd(game.FinishedRecord,"Ana",out _);
            GameSave.Store.TrySave(new CubotrisSession(mode:DifficultyMode.Challenge),out _);
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;yield return CaptureViews("main-menu");
            GameObject.Find("Canvas/MenuButtons/StatisticsButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return CaptureViews("statistics");
            Object.FindAnyObjectByType<StatisticsPanel>().Close();yield return null;
            GameObject.Find("Canvas/MenuButtons/ContinueButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return CaptureViews("continue");
            Object.FindAnyObjectByType<ContinuePanel>().Close();yield return null;
            GameObject.Find("Canvas/MenuButtons/ModeButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return CaptureViews("difficulty");
            GameObject.Find("Canvas/DifficultyPanel/Panel/Practice").GetComponent<Button>().onClick.Invoke();yield return null;yield return CaptureViews("practice");
            Object.FindAnyObjectByType<PracticePanel>().Close();yield return null;
            GameObject.Find("Canvas/MenuButtons/RecordsButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return CaptureViews("records");
        }
        private static IEnumerator CaptureViews(string name)
        {
            var game=Object.FindAnyObjectByType<GameController>();bool enabled=game!=null && game.enabled;if(game!=null)game.enabled=false;
            try
            {
                foreach(int width in new[]{1920,1280})
                {
                    int height=width*9/16;var canvas=Object.FindAnyObjectByType<Canvas>();var camera=Camera.main;var scaler=canvas.GetComponent<CanvasScaler>();
                    var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldPlane=canvas.planeDistance;int oldOrder=canvas.sortingOrder;var scaleMode=scaler.uiScaleMode;float scale=scaler.scaleFactor;var oldTarget=camera.targetTexture;float oldAspect=camera.aspect;
                    var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();Texture2D texture=null;
                    try
                    {
                        camera.targetTexture=target;camera.aspect=(float)width/height;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=32767;
                        scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=width/1920f;
                        yield return null;foreach(var graphic in canvas.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                        var previous=RenderTexture.active;RenderTexture.active=target;texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();RenderTexture.active=previous;
                        string directory=Path.GetFullPath("Logs/VisualEvidence");Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,name+"-"+width+"x"+height+".png"),texture.EncodeToPNG());
                        Assert.That(canvas.GetComponentsInChildren<Component>(true).All(c=>c!=null),Is.True,"Missing component in "+name);
                    }
                    finally
                    {
                        canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;canvas.sortingOrder=oldOrder;scaler.uiScaleMode=scaleMode;scaler.scaleFactor=scale;camera.targetTexture=oldTarget;camera.aspect=oldAspect;
                        target.Release();Object.Destroy(target);if(texture!=null)Object.Destroy(texture);
                    }
                }
            }
            finally{if(game!=null)game.enabled=enabled;}
        }
    }
}
