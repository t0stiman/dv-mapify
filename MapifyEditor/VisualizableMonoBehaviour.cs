using System.Collections;
using System.Collections.Generic;
using Mapify.Editor.Utils;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace Mapify.Editor
{
    public abstract class VisualizableMonoBehaviour : MonoBehaviour
    {
        [Header("Editor Visualization")]
#pragma warning disable CS0649
        [SerializeField]
        internal GameObject visualPrefab;
#pragma warning restore CS0649

        protected void UpdateVisuals<T>(T[] things, Transform reference)
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null || EditorUtility.IsPersistent(gameObject) || visualPrefab == null)
                return;
            StartCoroutine(UpdateVisualsCoroutine(things, reference));
        }

        private IEnumerator UpdateVisualsCoroutine<T>(IReadOnlyCollection<T> things, Transform reference)
        {
            yield return null;
            DestroyVisuals();

            for (int i = 0; i < things.Count; i++)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
                go.tag = "EditorOnly";
                PositionThing(reference, go.transform, i);
            }
        }

        private void DestroyVisuals()
        {
            foreach (Transform child in transform.FindChildrenByName(visualPrefab.name))
                DestroyImmediate(child.gameObject);
        }

        public abstract void PositionThing(Transform reference, Transform toMove, int count);
    }
}