using Cubotris.Gameplay;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static partial class FeatureAuthoring
{
    public static void Practice()=>Complete(()=>
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");var canvas=GameObject.Find("Canvas").transform;Theme(canvas);
        var difficulty=canvas.Find("DifficultyPanel").GetComponent<DifficultyPanel>();
        var panel=Overlay("PracticePanel",canvas,1080,740,out var root);var controller=root.AddComponent<PracticePanel>();controller.difficultyPanel=difficulty;
        Label("Title",panel,"PRÁCTICA POR CARA",50,25,980,70,39).color=Mint;
        Label("Hint",panel,"Elige qué tablero quieres entrenar.",50,110,980,50,25);
        var face=Button("Face",panel,"",50,210,460,70,controller.NextFace);controller.faceText=face.GetComponentInChildren<Text>();
        var mode=Button("Mode",panel,"",570,210,460,70,controller.NextMode);controller.modeText=mode.GetComponentInChildren<Text>();
        controller.description=Label("Description",panel,"",50,330,980,150,26,TextAnchor.MiddleCenter);
        var begin=Button("Begin",panel,"INICIAR PRÁCTICA",50,520,980,70,controller.Begin);
        var back=Button("Back",panel,"VOLVER",50,630,980,65,controller.Close);Chain(face,mode,begin,back);controller.firstControl=face;
        controller.returnControl=canvas.Find("MenuButtons/ModeButton").GetComponent<Button>();
        var parent=difficulty.transform.Find("Panel");if(parent.Find("Practice"))Object.DestroyImmediate(parent.Find("Practice").gameObject);
        var open=Button("Practice",parent,"PRÁCTICA POR CARA",50,605,1060,55,controller.OpenPractice);
        Chain(parent.Find("Relaxed").GetComponent<Button>(),parent.Find("Normal").GetComponent<Button>(),parent.Find("Challenge").GetComponent<Button>(),open,parent.Find("Back").GetComponent<Button>());
        EditorSceneManager.SaveScene(scene);
        scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");canvas=GameObject.Find("GameCanvas").transform;Theme(canvas);
        var game=Object.FindAnyObjectByType<GameController>();var restart=game.GetComponent<PracticeRestartControl>()??game.gameObject.AddComponent<PracticeRestartControl>();restart.game=game;
        if(canvas.Find("PracticeRestartButton"))Object.DestroyImmediate(canvas.Find("PracticeRestartButton").gameObject);
        restart.button=Button("PracticeRestartButton",canvas,"REINICIAR PRÁCTICA",1120,35,440,40,restart.Restart);restart.button.transform.SetSiblingIndex(canvas.Find("GameInfoPanel").GetSiblingIndex());
        restart.button.navigation=new Navigation{mode=Navigation.Mode.None};restart.button.gameObject.SetActive(false);
        canvas.Find("GameInfoPanel/Title").GetComponent<Text>().fontSize=25;canvas.Find("GameInfoPanel/NextFace").GetComponent<Text>().fontSize=19;
        EditorSceneManager.SaveScene(scene);
    });
}
