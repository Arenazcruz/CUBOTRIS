using Cubotris.Gameplay;
using Cubotris.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static partial class FeatureAuthoring
{
    public static void SaveGame()=>Complete(()=>
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");var canvas=GameObject.Find("Canvas").transform;Theme(canvas);
        var panel=Overlay("ContinuePanel",canvas,1060,650,out var root);var controller=root.AddComponent<ContinuePanel>();
        Label("Title",panel,"CONTINUAR PARTIDA",50,25,960,70,38).color=Mint;
        controller.message=Label("Message",panel,"",50,145,960,285,27,TextAnchor.MiddleCenter);
        controller.resumeButton=Button("Resume",panel,"CONTINUAR",50,500,455,75,controller.Resume);
        var back=Button("Back",panel,"VOLVER",555,500,455,75,controller.Close);controller.firstControl=back;Chain(controller.resumeButton,back);
        var parent=canvas.Find("MenuButtons");if(parent.Find("ContinueButton"))Object.DestroyImmediate(parent.Find("ContinueButton").gameObject);
        var open=Button("ContinueButton",parent,"CONTINUAR",248,480,520,80,controller.Open);controller.returnControl=open;
        Object.FindAnyObjectByType<MainMenuController>().continueButton=open;RelayoutMainMenu(canvas);EditorSceneManager.SaveScene(scene);
        scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");canvas=GameObject.Find("GameCanvas").transform;Theme(canvas);
        panel=Overlay("SaveAndExitPanel",canvas,1080,640,out root);var save=root.AddComponent<SaveAndExitPanel>();save.game=Object.FindAnyObjectByType<GameController>();
        Label("Title",panel,"GUARDAR Y SALIR",50,25,980,70,38).color=Mint;
        save.message=Label("Message",panel,"",50,145,980,245,28,TextAnchor.MiddleCenter);
        var confirm=Button("Confirm",panel,"GUARDAR Y SALIR",50,475,465,80,save.SaveAndExit);
        var cancel=Button("Cancel",panel,"CANCELAR",565,475,465,80,save.Close);save.firstControl=cancel;Chain(cancel,confirm);
        var pause=canvas.Find("PausePanel/PanelBackground");if(pause.Find("SaveButton"))Object.DestroyImmediate(pause.Find("SaveButton").gameObject);
        var saveOpen=Button("SaveButton",pause,"GUARDAR Y SALIR",0,0,600,65,save.Open);save.returnControl=saveOpen;
        var card=(RectTransform)pause;card.sizeDelta=new Vector2(860,760);
        Place(pause.Find("Title"),110,50,640,70);Place(pause.Find("Subtitle"),110,135,640,45);
        var names=new[]{"ResumeButton","RestartButton","OptionsButton","MainMenuButton","SaveButton"};var buttons=new Button[names.Length];
        for(int i=0;i<names.Length;i++)
        {
            buttons[i]=pause.Find(names[i]).GetComponent<Button>();Place(buttons[i].transform,110,210+i*95,640,75);
            var label=buttons[i].GetComponentInChildren<Text>();Place(label.transform,20,0,600,75);label.fontSize=26;label.alignment=TextAnchor.MiddleCenter;
        }
        Chain(buttons);EditorSceneManager.SaveScene(scene);
    });
}
