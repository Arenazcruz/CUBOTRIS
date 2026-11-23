using System.Linq;
using Cubotris.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static partial class FeatureAuthoring
{
    public static void Hold() => Complete(() =>
    {
        BuildControls();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var canvas = GameObject.Find("GameCanvas").transform; Theme(canvas);
        if(canvas.Find("HoldPanel")) Object.DestroyImmediate(canvas.Find("HoldPanel").gameObject);
        var panel = Node("HoldPanel",canvas); Place(panel,1120,720,440,220);
        panel.SetSiblingIndex(canvas.Find("NextPiecePanel").GetSiblingIndex()+1);
        var background=panel.gameObject.AddComponent<Image>(); background.sprite=panelSprite; background.type=Image.Type.Sliced; background.color=Dark;
        var view=panel.gameObject.AddComponent<HoldView>(); view.game=Object.FindAnyObjectByType<GameController>();
        Label("Title",panel,"RESERVA",25,10,390,45,27).color=Mint;
        view.status=Label("Status",panel,"",25,170,390,40,21,TextAnchor.MiddleCenter);
        var preview=Node("PiecePreview",panel);Place(preview,220,115,0,0);
        view.blocks=new Image[4];
        for(int i=0;i<4;i++) { var block=Node("Block"+i,preview);block.sizeDelta=Vector2.one*34;view.blocks[i]=block.gameObject.AddComponent<Image>();view.blocks[i].raycastTarget=false; }
        var serialized=new SerializedObject(Object.FindAnyObjectByType<GameHudView>());var sprites=serialized.FindProperty("pieceSprites");
        view.sprites=Enumerable.Range(0,sprites.arraySize).Select(i=>(Sprite)sprites.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var controls=canvas.Find("Controls");Place(controls,1120,950,440,100);
        while(controls.childCount>0)Object.DestroyImmediate(controls.GetChild(0).gameObject);
        var hint=Label("Hint",controls,"",15,5,410,90,19,TextAnchor.MiddleLeft);hint.gameObject.AddComponent<ControlsHint>();
        EditorSceneManager.SaveScene(scene);
    });
}
