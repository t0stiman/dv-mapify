using System;
using System.Collections.Generic;
using System.Linq;
using DV;
using DV.CabControls.NonVR;
using DV.Common;
using DV.InventorySystem;
using DV.UI.Inventory;
using DV.Utils;
using Mapify.Editor.Utils;
using Mapify.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Mapify.BuildMode
{
    public class BuildMode: MonoBehaviour
    {
        private bool placeMode = false;
        // private List<GameObject> placedGameObjects;

        private InventoryUIController inventoryUIController;

        private const int LEFT_MOUSE_BUTTON = 0;

        private void Awake()
        {
            // placedGameObjects = new List<GameObject>();
        }

        private void Start()
        {
            if (!FindInventoryUIController())
            {
                Mapify.LogError("Failed to find InventoryUIController");
            }
        }

        /// <summary>
        /// Find the InventoryUIController
        /// </summary>
        /// <returns>true if we found the InventoryUIController</returns>
        private bool FindInventoryUIController()
        {
            foreach (var rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                inventoryUIController = rootGameObject.GetComponentInChildren<InventoryUIController>(true);
                if (inventoryUIController != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                TogglePlaceMode();
            }

            //TODO check application window focus
            // if (placeMode && !SingletonBehaviour<AppUtil>.Instance.IsPauseMenuOpen)
            // {
            //     UpdatePlaceMode();
            // }
        }

        private void TogglePlaceMode()
        {
            if (placeMode)
            {
                ExitPlaceMode();
            }
            else
            {
                EnterPlaceMode();
            }
        }

        private void EnterPlaceMode()
        {
            placeMode = true;
            // SelectObject();

            InitializeInventory();
        }

        private void InitializeInventory()
        {
            EmptyInventory();

            var controller = StartingItemsController.Instance;

            // controller.InstantiateStorageItems(inventoryStorageItemDatas, true, ref inventoryBelongingData, itemSaveDataCollection, itemRigidBodiesToToggleOffKinematicState);

            var inventoryBelongingData = new Dictionary<GameObject, StorageItemData>();

            //TODO meer
            for (var index = 0; index < 5; index++)
            {
                var asset = BuildingAssetsRegistry.Assets.ToArray()[index];
                inventoryBelongingData.Add(InstantiateItem(asset, index), new StorageItemData(
                    asset.Key,
                    Vector3.zero,
                    Quaternion.identity,
                    true,
                    inventorySlotIndex:index
                ));
            }

            // controller.FinalizeItemStateLoading(transformRelatedWorldItemStates, worldParent, itemSaveDataCollection, itemRigidBodiesToToggleOffKinematicState);

            var inventoryData = controller.CreateInventoryData(inventoryBelongingData);
            controller.InitializeInventory(inventoryData);
        }

        private GameObject InstantiateItem(KeyValuePair<string, GameObject> asset, int instantiatedItemCount)
        {
            var vector3 = StartingItemsController.ITEM_INSTANTIATION_SAFETY_OFFSET * instantiatedItemCount;
            var itemObject = Instantiate(asset.Value, StartingItemsController.ITEM_INSTANTIATION_SAFETY_POSITION + vector3, Quaternion.identity);
            itemObject.name = asset.Value.name;

            //TODO make item smaller
            itemObject.transform.localScale /= 10;

            var itemSpec = itemObject.AddComponent<InventoryItemSpec>();
            SetupItemSpec(ref itemSpec, asset);

            var rigidBody = itemObject.AddComponent<Rigidbody>();
            rigidBody.isKinematic = true;

            //TODO do we need this?
            // itemObject.AddComponent<ItemSaveData>();

            var itemNonVR = itemObject.AddComponent<ItemNonVR>();
            itemNonVR.Setup();

            return itemObject;
        }

        private void SetupItemSpec(ref InventoryItemSpec spec, KeyValuePair<string, GameObject> asset)
        {
            //name of the item
            spec.localizationKeyName = Locale.DV_ASSET_PREFIX + asset.Key;

            //description of the item
            spec.localizationKeyDescription = spec.localizationKeyName;

            spec.BelongsToPlayer = true;
            spec.immuneToDumpster = false;
            spec.isEssential = false;

            spec.itemPrefabName = asset.Key;

            var placeHolder = FindObjectOfType<Sprite>(); //TODO

            //this is the old black and white inventory icon from DV Overhauled. Not used anymore but i'm gonna set it anyway just in case.
            spec.itemIconSprite = placeHolder;

            //this is the currently used inventory icon
            spec.itemIconSpriteStandard = placeHolder;
            //this icon will be shown in your inventory when you've dropped the item. For vanilla game items, this is a blue (#72A2B3) silhouette of the item.
            spec.itemIconSpriteDropped = placeHolder;

            //this preview is shown when you hold R to place an item
            spec.previewPrefab = CreatePreviewObject(asset.Value);
            //idk what this is
            spec.previewBounds = new Bounds(Vector3.zero, new Vector3(0.2f, 0.2f, 0.2f));
        }

        private static void EmptyInventory()
        {
            var inventory = SingletonBehaviour<Inventory>.Instance;
            inventory.inventoryItemData = new InventoryItemData[inventory.inventoryItemData.Length];
        }

        // ======================================

        private void ExitPlaceMode()
        {
            placeMode = false;
            // Destroy(previewObject);
            RestoreNormalInventory();
        }

        private void RestoreNormalInventory()
        {
            //TODO
            // inventoryUIController.InitializeInventory();
        }

        // private void SelectObject()
        // {
        //     var keysList = BuildingAssetsRegistry.Assets.Keys.ToList();
        //     var newKey = keysList[assetIndex];
        //
        //     originalObject = BuildingAssetsRegistry.Assets[newKey];
        //
        //     Destroy(previewObject);
        // }

        private static GameObject CreatePreviewObject(GameObject originalObject)
        {
            var previewObject = Instantiate(originalObject);
            foreach (var collider in previewObject.GetComponentsInChildren<Collider>())
            {
                DestroyImmediate(collider);
            }
            return previewObject;
        }

        //Like the modulo operator but it works with negative numbers
        public static int BetterModulo(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
