using Cubotris.Gameplay;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static partial class FeatureAuthoring
{
    public static void Combo()=>Complete(()=>
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");var canvas=GameObject.Find("GameCanvas").transform;Theme(canvas);
        if(canvas.Find("ComboPanel"))Object.DestroyImmediate(canvas.Find("ComboPanel").gameObject);
        var panel=Node("ComboPanel",canvas);Place(panel,1590,550,290,265);panel.SetSiblingIndex(canvas.Find("GameInfoPanel").GetSiblingIndex());
        var image=panel.gameObject.AddComponent<Image>();image.sprite=panelSprite;image.type=Image.Type.Sliced;image.color=Dark;image.raycastTarget=false;
        var view=panel.gameObject.AddComponent<ComboView>();view.game=Object.FindAnyObjectByType<GameController>();
        Label("Title",panel,"COMBO",22,18,246,45,25).color=Mint;
        view.value=Label("Value",panel,"",22,75,246,45,28);
        view.detail=Label("Detail",panel,"",22,130,246,115,21);view.detail.color=Muted;
        EditorSceneManager.SaveScene(scene);
    });
}
