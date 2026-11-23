using System;
using System.Linq;
using Cubotris.Gameplay;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
public static partial class FeatureAuthoring
{
    private static Font font;
    private static Sprite panelSprite, buttonSprite;
    private static readonly Color Mint = new Color(.47f,.9f,.75f), Dark = new Color(.06f,.10f,.15f), Muted = new Color(.65f,.75f,.82f);
    private static void Theme(Transform canvas)
    {
        var button = canvas.GetComponentsInChildren<Button>(true).First();
        font = button.GetComponentInChildren<Text>(true).font; buttonSprite = button.GetComponent<Image>().sprite;
        panelSprite = canvas.GetComponentsInChildren<Image>(true).First(i => i.name == "PanelBackground").sprite;
    }
    private static RectTransform Node(string name, Transform parent)
    { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent,false); return (RectTransform)go.transform; }
    private static void Place(Transform transform, float x, float y, float width, float height)
    {
        var r=(RectTransform)transform; r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(width,height); r.localScale=Vector3.one;
    }
    private static Text Label(string name, Transform parent, string value, float x, float y, float width, float height, int size=25, TextAnchor align=TextAnchor.MiddleLeft)
    {
        var r=Node(name,parent);Place(r,x,y,width,height);var text=r.gameObject.AddComponent<Text>();
        text.font=font;text.fontSize=size;text.text=value;text.color=Color.white;text.alignment=align;text.raycastTarget=false;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
    }
    private static Button Button(string name, Transform parent, string value, float x, float y, float width, float height, UnityAction action)
    {
        var r=Node(name,parent);Place(r,x,y,width,height);var image=r.gameObject.AddComponent<Image>();image.sprite=buttonSprite;image.type=Image.Type.Sliced;image.color=Mint;
        var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=new Color(.8f,1,1);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.5f,.7f,.7f);button.colors=colors;
        var label=Label("Label",r,value,12,0,width-24,height,24,TextAnchor.MiddleCenter);label.color=Dark;
        if(action!=null)UnityEventTools.AddPersistentListener(button.onClick,action);return button;
    }
    private static RectTransform Overlay(string name, Transform parent, float width, float height, out GameObject root)
    {
        if(parent.Find(name)!=null)Object.DestroyImmediate(parent.Find(name).gameObject);
        var overlay=Node(name,parent); root=overlay.gameObject;root.SetActive(false);
        overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
        var shade=root.AddComponent<Image>();shade.color=new Color(.025f,.05f,.07f,.98f);
        var panel=Node("Panel",overlay);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.sizeDelta=new Vector2(width,height);
        var image=panel.gameObject.AddComponent<Image>();image.sprite=panelSprite;image.type=Image.Type.Sliced;image.color=Dark;return panel;
    }
    private static void Chain(params Selectable[] items)
    {
        for(int i=0;i<items.Length;i++)items[i].navigation=new Navigation { mode=Navigation.Mode.Explicit,
            selectOnUp=items[(i+items.Length-1)%items.Length],selectOnDown=items[(i+1)%items.Length],
            selectOnLeft=items[i] is Slider ? null : items[(i+items.Length-1)%items.Length],selectOnRight=items[i] is Slider ? null : items[(i+1)%items.Length] };
    }
    private static void Complete(Action action)
    {try { action();AssetDatabase.SaveAssets();Debug.Log("FEATURE_AUTHORING_OK");EditorApplication.Exit(0); }catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
    public static void Controls() => Complete(() =>
    {
        foreach(string sceneName in new[]{"MainMenu","Game"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+sceneName+".unity");
            var canvas=GameObject.Find(sceneName=="Game"?"GameCanvas":"Canvas").transform;Theme(canvas);
            var options=canvas.Find(sceneName=="Game"?"PauseOptionsPanel":"OptionsPanel");var background=options.Find("PanelBackground");
            var panel=Overlay("ControlsPanel",options,1120,850,out var root);var controller=root.AddComponent<ControlsPanel>();
            Label("Title",panel,"CONTROLES",50,25,1020,65,40).color=Mint;
            Label("Hint",panel,"Tecla principal y alternativa · ESC siempre permite volver",50,95,1020,45,23).color=Muted;
            int count=ControlBindings.Defaults().Length;controller.primaryButtons=new Button[count];controller.secondaryButtons=new Button[count];
            var navigation=new System.Collections.Generic.List<Selectable>();
            for(int i=0;i<count;i++)
            {
                Label("Action"+i,panel,ControlBindings.Label((GameAction)i),50,160+i*67,470,54,24);
                controller.primaryButtons[i]=Button("Primary"+i,panel,"",540,160+i*67,240,54,null);
                controller.secondaryButtons[i]=Button("Secondary"+i,panel,"",810,160+i*67,240,54,null);
                navigation.Add(controller.primaryButtons[i]);navigation.Add(controller.secondaryButtons[i]);
            }
            controller.status=Label("Status",panel,"",50,650,1020,60,22);controller.status.color=Muted;
            var reset=Button("Reset",panel,"RESTAURAR",50,745,480,65,controller.RestoreDefaults);var back=Button("Back",panel,"VOLVER",570,745,480,65,controller.Close);
            navigation.Add(reset);navigation.Add(back);Chain(navigation.ToArray());controller.firstControl=controller.primaryButtons[0];
            if(background.Find("ControlsButton")!=null)Object.DestroyImmediate(background.Find("ControlsButton").gameObject);
            var open=Button("ControlsButton",background,"CONTROLES",80,565,400,56,controller.Open);controller.returnControl=open;
            background.Find("SaveHint").gameObject.SetActive(false);
            Chain(background.Find("MusicVolume/Slider").GetComponent<Slider>(),background.Find("EffectsVolume/Slider").GetComponent<Slider>(),open,background.Find("BackButton").GetComponent<Button>());
            if(sceneName=="Game")
            {
                var hint=canvas.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.name=="Controls");
                if(hint!=null && hint.GetComponent<ControlsHint>()==null)hint.gameObject.AddComponent<ControlsHint>();
            }
            EditorSceneManager.SaveScene(scene);
        }
    });
}
