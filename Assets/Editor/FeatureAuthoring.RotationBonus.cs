using Cubotris.Gameplay;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static partial class FeatureAuthoring
{
    public static void RotationBonus()=>Complete(()=>
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");var canvas=GameObject.Find("GameCanvas").transform;Theme(canvas);
        if(canvas.Find("RotationBonusPanel"))Object.DestroyImmediate(canvas.Find("RotationBonusPanel").gameObject);
        var panel=Node("RotationBonusPanel",canvas);Place(panel,1590,86,290,440);panel.SetSiblingIndex(canvas.Find("GameInfoPanel").GetSiblingIndex());
        var image=panel.gameObject.AddComponent<Image>();image.sprite=panelSprite;image.type=Image.Type.Sliced;image.color=Dark;image.raycastTarget=false;
        var view=panel.gameObject.AddComponent<RotationBonusView>();view.game=Object.FindAnyObjectByType<GameController>();
        Label("Title",panel,"META DE VUELTA",22,18,246,45,25).color=Mint;
        Label("Description",panel,"Limpia al menos una línea en cada cara de esta vuelta.",22,75,246,90,22).color=Muted;
        view.progress=Label("Progress",panel,"",22,180,246,135,23);
        view.notice=Label("Notice",panel,"",22,325,246,100,22);view.notice.color=Mint;
        EditorSceneManager.SaveScene(scene);
    });
}
