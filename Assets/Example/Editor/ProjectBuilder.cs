using UnityEditor;

using UnityEngine;

namespace InjectableServiceLocator.Demo
{
    public class ProjectBuilder
    {
        public static void BuildAll()
        {
            Debug.Log("### BUILDING ###");

            var report = BuildPipeline.BuildPlayer(
                new[] {"Assets/Scenes/SampleScene.unity"},
                "C:/Users/Khamro/Desktop/1/build.apk",
                BuildTarget.Android,
                BuildOptions.None);

            Debug.Log("###   DONE   ###");

            Debug.Log(report);
        }
    }
}