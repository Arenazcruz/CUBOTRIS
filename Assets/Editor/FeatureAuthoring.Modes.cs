using System.Linq;
using Cubotris.Gameplay;
using Cubotris.UI;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static partial class FeatureAuthoring
{
    private static void RelayoutMainMenu(Transform canvas)
    {
        var parent=canvas.Find("MenuButtons");
        var names=new[]{"PlayButton","ContinueButton","ModeButton","RecordsButton","StatisticsButton","OptionsButton","ExitButton"};
        var buttons=names.Select(n=>parent.Find(n)).Where(t=>t!=null).Select(t=>t.GetComponent<Button>()).ToArray();
        float height=Mathf.Min(80,(450-(buttons.Length-1)*12)/buttons.Length);
        for(int i=0;i<buttons.Length;i++)
        {
            var t=buttons[i].transform;Place(t,248,480+i*(height+12),520,height);
            var label=t.Find("Label");if(label!=null){Place(label,25,0,420,height);label.GetComponent<Text>().fontSize=27;label.GetComponent<Text>().alignment=TextAnchor.MiddleLeft;}
            var arrow=t.Find("Arrow");if(arrow!=null)Place(arrow,465,0,40,height);
        }
        Chain(buttons);
    }
    public static void Modes()=>Complete(()=>
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var canvas=GameObject.Find("Canvas").transform;Theme(canvas);var menu=Object.FindAnyObjectByType<MainMenuController>();
        var panel=Overlay("DifficultyPanel",canvas,1160,800,out var root);var controller=root.AddComponent<DifficultyPanel>();
        Label("Title",panel,"DIFICULTAD",50,25,1060,65,40).color=Mint;
        controller.current=Label("Current",panel,"",50,105,1060,90,25);
        var relaxed=Button("Relaxed",panel,"RELAJADO",50,225,320,80,controller.Relaxed);
        var normal=Button("Normal",panel,"NORMAL",50,355,320,80,controller.Normal);
        var challenge=Button("Challenge",panel,"DESAFÍO",50,485,320,80,controller.Challenge);
        Label("RelaxedHelp",panel,DifficultyRules.Description(DifficultyMode.Relaxed),410,215,690,100,25);
        Label("NormalHelp",panel,DifficultyRules.Description(DifficultyMode.Normal),410,345,690,100,25);
        Label("ChallengeHelp",panel,DifficultyRules.Description(DifficultyMode.Challenge),410,475,690,100,25);
        var back=Button("Back",panel,"VOLVER",50,680,1060,65,controller.Close);controller.firstControl=normal;Chain(relaxed,normal,challenge,back);
        var parent=canvas.Find("MenuButtons");if(parent.Find("ModeButton"))Object.DestroyImmediate(parent.Find("ModeButton").gameObject);
        var open=Button("ModeButton",parent,"MODO",248,480,520,80,controller.Open);controller.returnControl=open;
        menu.modeText=open.GetComponentInChildren<Text>();UnityEventTools.AddPersistentListener(controller.changed,menu.RefreshBest);RelayoutMainMenu(canvas);
        var history=canvas.Find("RecordsPanel").GetComponent<ScoreHistoryPanel>();var content=history.transform.Find("Content");
        if(content.Find("ModeButton"))Object.DestroyImmediate(content.Find("ModeButton").gameObject);
        history.modeButton=Button("ModeButton",content,"MODO",1000,40,450,55,history.CycleMode);Place(content.Find("Title"),50,30,900,75);
        content.Find("ClearButton/Label").GetComponent<Text>().text="BORRAR ESTE MODO";
        EditorSceneManager.SaveScene(scene);
        scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var title=GameObject.Find("GameCanvas/GameInfoPanel/Title");title.GetComponent<Text>().fontSize=30;
        var modeLabel=title.GetComponent<SessionModeLabel>()??title.AddComponent<SessionModeLabel>();modeLabel.game=Object.FindAnyObjectByType<GameController>();
        EditorSceneManager.SaveScene(scene);
    });
}
