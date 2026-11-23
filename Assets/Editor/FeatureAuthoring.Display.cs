using Cubotris.Gameplay;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static partial class FeatureAuthoring
{
    public static void Display() => Complete(() =>
    {
        foreach(string sceneName in new[]{"MainMenu","Game"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+sceneName+".unity");
            var canvas=GameObject.Find(sceneName=="Game"?"GameCanvas":"Canvas").transform;Theme(canvas);
            var options=canvas.Find(sceneName=="Game"?"PauseOptionsPanel":"OptionsPanel");var background=options.Find("PanelBackground");
            var panel=Overlay("DisplayPanel",options,1120,760,out var root);var controller=root.AddComponent<DisplayPanel>();
            Label("Title",panel,"PANTALLA",50,25,1020,65,40).color=Mint;
            Label("ResolutionLabel",panel,"RESOLUCIÓN",50,135,1020,40,25).color=Muted;
            controller.resolutionText=Label("Resolution",panel,"",300,195,520,70,38,TextAnchor.MiddleCenter);
            controller.previousButton=Button("Previous",panel,"ANTERIOR",50,200,230,60,controller.Previous);
            controller.nextButton=Button("Next",panel,"SIGUIENTE",840,200,230,60,controller.Next);
            controller.modeButton=Button("Mode",panel,"",50,320,1020,70,controller.ToggleMode);controller.modeText=controller.modeButton.GetComponentInChildren<Text>();
            controller.status=Label("Status",panel,"",50,430,1020,90,25,TextAnchor.MiddleCenter);controller.status.color=Muted;
            controller.applyButton=Button("Apply",panel,"APLICAR",50,550,1020,65,controller.Apply);
            controller.keepButton=Button("Keep",panel,"CONSERVAR",50,550,490,65,controller.Keep);
            controller.revertButton=Button("Revert",panel,"REVERTIR",580,550,490,65,controller.Revert);
            controller.backButton=Button("Back",panel,"VOLVER",50,655,1020,65,controller.Close);controller.firstControl=controller.previousButton;
            if(background.Find("DisplayButton")!=null)Object.DestroyImmediate(background.Find("DisplayButton").gameObject);
            var open=Button("DisplayButton",background,"PANTALLA",560,565,400,56,controller.Open);controller.returnControl=open;
            Chain(background.Find("MusicVolume/Slider").GetComponent<Slider>(),background.Find("EffectsVolume/Slider").GetComponent<Slider>(),
                background.Find("ControlsButton").GetComponent<Button>(),open,background.Find("BackButton").GetComponent<Button>());
            EditorSceneManager.SaveScene(scene);
        }
    });
}
