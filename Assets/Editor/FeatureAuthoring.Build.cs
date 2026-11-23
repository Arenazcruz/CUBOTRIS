using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static partial class FeatureAuthoring
{
    public static void BuildWindows()=>Complete(()=>
    {
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
            locationPathName="Builds/Delivery/CUBOTRIS.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(result.summary.result!=BuildResult.Succeeded)throw new System.Exception("La compilación no terminó correctamente: "+result.summary.result);
        UnityEngine.Debug.Log("DELIVERY_BUILD_OK "+result.summary.totalSize+" bytes");
    });
}
