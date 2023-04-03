using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Mapify.Editor.Validators
{
    public abstract class SceneValidator : Validator
    {
        public override IEnumerator<Result> Validate()
        {
            string scenePath = GetScenePath();
            Scene scene = EditorSceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid())
            {
                yield return Result.Error($"Failed to find {GetPrettySceneName()} scene! It should be located at \"{scenePath}\"");
                yield break;
            }

            bool isSceneLoaded = scene.isLoaded;
            if (!isSceneLoaded)
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            IEnumerator<Result> validateRailwayScene = ValidateScene(scene);
            while (validateRailwayScene.MoveNext()) yield return validateRailwayScene.Current;

            if (!isSceneLoaded)
                EditorSceneManager.UnloadSceneAsync(scenePath);
        }

        protected abstract IEnumerator<Result> ValidateScene(Scene scene);

        protected string GetPrettySceneName()
        {
            return GetScenePath().Split('/').Last().Split('.').First();
        }

        public abstract string GetScenePath();
    }
}