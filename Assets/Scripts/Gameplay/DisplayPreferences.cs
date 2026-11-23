using System;
using UnityEngine;
namespace Cubotris.Gameplay
{
    [Serializable] public struct DisplayChoice
    {
        public int width, height;
        public bool fullscreen;
        public DisplayChoice(int width,int height,bool fullscreen) { this.width=width;this.height=height;this.fullscreen=fullscreen; }
        public bool IsValid => width>=640 && height>=480 && width<=16384 && height<=16384;
    }
    public static class DisplayPreferences
    {
        public const string Key="Cubotris.Display.v1";
        public static DisplayChoice Current => new DisplayChoice(Screen.width,Screen.height,Screen.fullScreen);
        public static DisplayChoice Load(DisplayChoice fallback)
        {
            try { var choice=JsonUtility.FromJson<DisplayChoice>(PlayerPrefs.GetString(Key,""));return choice.IsValid?choice:fallback; }
            catch(Exception){return fallback;}
        }
        public static void Save(DisplayChoice value)
        { if(!value.IsValid)throw new ArgumentException("Resolución inválida");PlayerPrefs.SetString(Key,JsonUtility.ToJson(value));PlayerPrefs.Save(); }
        public static void Apply(DisplayChoice value)
        {
            if(!value.IsValid)return;
            // The Editor's Game view is configured by the Editor, not by player display settings.
            if(!Application.isEditor)Screen.SetResolution(value.width,value.height,value.fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() { if(PlayerPrefs.HasKey(Key))Apply(Load(Current)); }
    }
    public sealed class DisplayPreview
    {
        private readonly Action<DisplayChoice> apply,save;
        private DisplayChoice original, proposed;
        public bool IsActive { get; private set; }
        public float Remaining { get; private set; }
        public DisplayPreview(Action<DisplayChoice> apply,Action<DisplayChoice> save){this.apply=apply;this.save=save;}
        public void Begin(DisplayChoice current,DisplayChoice next)
        {
            if(!next.IsValid)throw new ArgumentException("Resolución inválida");
            Cancel();original=current;proposed=next;Remaining=10;IsActive=true;apply(next);
        }
        public void Tick(float unscaledSeconds)
        {if(!IsActive || unscaledSeconds<=0)return;Remaining=Mathf.Max(0,Remaining-unscaledSeconds);if(Remaining<=0)Cancel();}
        public void Confirm() {if(!IsActive)return;save(proposed);IsActive=false;}
        public void Cancel() {if(!IsActive)return;IsActive=false;apply(original);}
    }
}
