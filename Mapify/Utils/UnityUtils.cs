using UnityEngine;

namespace Mapify
{
    // source: https://discussions.unity.com/t/instantiate-inactive-object/39830/5
    public static class UnityUtils
    {
        /// <summary>
        /// Will instantiate an object disabled preventing it from calling Awake/OnEnable.
        /// </summary>
        public static T InstantiateDisabled<T>(T original, Transform parent = null, bool worldPositionStays = false) where T : Object
        {
            if (!GetActiveState(original))
            {
                return Object.Instantiate(original, parent, worldPositionStays);
            }

            (GameObject coreObject, Transform coreObjectTransform) = CreateDisabledCoreObject(parent);
            T instance = Object.Instantiate(original, coreObjectTransform, worldPositionStays);
            SetActiveState(instance, false);
            SetParent(instance, parent, worldPositionStays);
            Object.Destroy(coreObject);
            return instance;
        }

        /// <summary>
        /// Will instantiate an object disabled preventing it from calling Awake/OnEnable.
        /// </summary>
        public static T InstantiateDisabled<T>(T original, Vector3 position, Quaternion rotation, Transform parent = null) where T : Object
        {
            if (!GetActiveState(original))
            {
                return Object.Instantiate(original, position, rotation, parent);
            }

            (GameObject coreObject, Transform coreObjectTransform) = CreateDisabledCoreObject(parent);
            T instance = Object.Instantiate(original, position, rotation, coreObjectTransform);
            SetActiveState(instance, false);
            SetParent(instance, parent, false);
            Object.Destroy(coreObject);
            return instance;
        }

        private static (GameObject coreObject, Transform coreObjectTransform) CreateDisabledCoreObject(Transform parent = null)
        {
            GameObject coreObject = new GameObject(string.Empty);
            coreObject.SetActive(false);
            Transform coreObjectTransform = coreObject.transform;
            coreObjectTransform.SetParent(parent);

            return (coreObject, coreObjectTransform);
        }

        private static bool GetActiveState<T>(T @object) where T : Object
        {
            switch (@object)
            {
                case GameObject gameObject:
                {
                    return gameObject.activeSelf;
                }
                case Component component:
                {
                    return component.gameObject.activeSelf;
                }
                default:
                {
                    return false;
                }
            }
        }

        private static void SetActiveState<T>(T @object, bool state) where T : Object
        {
            switch (@object)
            {
                case GameObject gameObject:
                {
                    gameObject.SetActive(state);

                    break;
                }
                case Component component:
                {
                    component.gameObject.SetActive(state);

                    break;
                }
            }
        }

        private static void SetParent<T>(T @object, Transform parent, bool worldPositionStays) where T : Object
        {
            switch (@object)
            {
                case GameObject gameObject:
                {
                    gameObject.transform.SetParent(parent, worldPositionStays);

                    break;
                }
                case Component component:
                {
                    component.transform.SetParent(parent, worldPositionStays);

                    break;
                }
            }
        }
    }
}
