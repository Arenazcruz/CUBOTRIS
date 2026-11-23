using Cubotris.Gameplay;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static partial class FeatureAuthoring
{
    public static void Statistics()=>Complete(()=>
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");var canvas=GameObject.Find("Canvas").transform;Theme(canvas);
        var panel=Overlay("StatisticsPanel",canvas,1280,950,out var root);var controller=root.AddComponent<StatisticsPanel>();
        Label("Title",panel,"ESTADÍSTICAS PERSONALES",50,20,1180,65,39).color=Mint;
        var filter=Button("Filter",panel,"",50,105,1180,60,controller.TogglePractice);controller.filter=filter.GetComponentInChildren<Text>();
        controller.totals=Label("Totals",panel,"",50,205,1180,260,27);
        controller.hardest=Label("Hardest",panel,"",50,475,1180,55,27);controller.hardest.color=Mint;
        controller.faces=new Text[4];for(int i=0;i<4;i++)controller.faces[i]=Label("Face"+(i+1),panel,"",50,550+i*45,1180,40,26);
        controller.notice=Label("Notice",panel,"",50,760,1180,65,22);controller.notice.color=Muted;
        var back=Button("Back",panel,"VOLVER",50,850,1180,65,controller.Close);Chain(filter,back);controller.firstControl=filter;
        var parent=canvas.Find("MenuButtons");if(parent.Find("StatisticsButton"))Object.DestroyImmediate(parent.Find("StatisticsButton").gameObject);
        var open=Button("StatisticsButton",parent,"ESTADÍSTICAS",248,480,520,80,controller.Open);controller.returnControl=open;RelayoutMainMenu(canvas);
        EditorSceneManager.SaveScene(scene);
    });
}
