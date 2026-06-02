#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
#define UNITY_5_AND_LATER
#endif

#if UNITY_5_AND_LATER

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using PixelCloudGames;
#endif

/// <summary>
/// This is a dummy component to replace the problematic [MenuItem("Component/...")] entry, which needs an editor-restart to show up.
/// Note that this only applies to the Component menu, other menus (e.g. Window) still work fine.
/// This way we can add the dummy component, check for the condition and perform the add-task and remove the dummy component afterwards.
/// </summary>
[ExecuteInEditMode]
[AddComponentMenu("2D ColliderGen/Orthello Specific/Add AlphaMeshColliders To OTTileMap")]
public class ComponentMenu9AlphaMeshColliderOTTileMap : MonoBehaviour {

    [SerializeField]
    bool mIsInitialized = false;

#if UNITY_EDITOR
    void Update() {

        if (mIsInitialized) {
            return;
        }

        bool isValidOTTileMap = IsValidOTTileMap();
        if (isValidOTTileMap) {
            Component tileMapObject = this.GetComponent("OTTileMap");
            AddCollidersToOTTileMap(this.transform, tileMapObject);

            ComponentMenu2SelectAlphaMeshColliderChildren.SelectChildAlphaMeshColliders(Selection.gameObjects);
        }
        else {
            Debug.LogError("Not a valid OTTileMap object.");
        }

        DestroyImmediate(this, false); // note: we don't want this to be part of the undo

        mIsInitialized = true;
    }

    //-------------------------------------------------------------------------
    bool IsValidOTTileMap() {

        Component tileMapObject = this.GetComponent("OTTileMap");
		if (tileMapObject != null) {
			return true;
		}
		return false; // no OTTileMap component found.
    }

    //-------------------------------------------------------------------------
    static void AddCollidersToOTTileMap(Transform tileMapNode, Component otTileMap) {

        // OTTileMapLayer[]  otTileMap.layers
        System.Type otTileMapType = otTileMap.GetType();
        FieldInfo fieldLayers = otTileMapType.GetField("layers");
        if (fieldLayers == null) {
            Debug.LogError("Detected a missing 'layers' member variable at OTTileMap component - Is your Orthello package up to date? 2D ColliderGen might probably not work correctly with this version.");
            return;
        }

        // add a GameObject node named "AlphaMeshColliders"
        GameObject collidersNode = UndoAware.CreateGameObject("AlphaMeshColliders");
        collidersNode.transform.parent = tileMapNode;
        collidersNode.transform.localPosition = Vector3.zero;
        collidersNode.transform.localScale = Vector3.one;

        IEnumerable layersArray = (IEnumerable)fieldLayers.GetValue(otTileMap);
        int layerIndex = 0;
        foreach (object otTileMapLayer in layersArray) {

            System.Type otTileMapLayerType = otTileMapLayer.GetType();
            FieldInfo fieldName = otTileMapLayerType.GetField("name");
            if (fieldName == null) {
                Debug.LogError("Detected a missing 'name' member variable at OTTileMapLayer component - Is your Orthello package up to date? 2D ColliderGen might probably not work correctly with this version.");
                return;
            }
            string layerName = (string)fieldName.GetValue(otTileMapLayer);
            // add a GameObject node for each tilemap layer.
            GameObject layerNode = UndoAware.CreateGameObject(layerName);
            layerNode.transform.parent = collidersNode.transform;
            layerNode.transform.localPosition = Vector3.zero;
            layerNode.transform.localScale = Vector3.one;

            addColliderGameObjectsForOTTileMapLayer(layerNode.transform, otTileMap, otTileMapLayer, layerIndex);
            ++layerIndex;
        }
    }

    //-------------------------------------------------------------------------
    static void addColliderGameObjectsForOTTileMapLayer(Transform layerNode, Component otTileMap, object otTileMapLayer, int layerIndex) {

        // read tileMapSize = OTTileMap.mapSize (UnityEngine.Vector2)
        System.Type otTileMapType = otTileMap.GetType();
        FieldInfo fieldMapSize = otTileMapType.GetField("mapSize");
        if (fieldMapSize == null) {
            Debug.LogError("Detected a missing 'mapSize' member variable at OTTileMap component - Is your Orthello package up to date? 2D ColliderGen might probably not work correctly with this version.");
            return;
        }
        Vector2 tileMapSize = (UnityEngine.Vector2)fieldMapSize.GetValue(otTileMap);
        int tileMapWidth = (int)tileMapSize.x;
        int tileMapHeight = (int)tileMapSize.y;
        // read mapTileSize = OTTileMap.mapTileSize (UnityEngine.Vector2)
        FieldInfo fieldMapTileSize = otTileMapType.GetField("mapTileSize");
        if (fieldMapTileSize == null) {
            Debug.LogError("Detected a missing 'mapTileSize' member variable at OTTileMap component - Is your Orthello package up to date? 2D ColliderGen might probably not work correctly with this version.");
            return;
        }
        Vector2 mapTileSize = (UnityEngine.Vector2)fieldMapTileSize.GetValue(otTileMap);
        Vector3 mapTileScale = new Vector3(1.0f / tileMapSize.x, 1.0f / tileMapSize.y, 1.0f / tileMapSize.x);

        System.Collections.Generic.Dictionary<int, object> tileSetAtTileIndex = new System.Collections.Generic.Dictionary<int, object>();


        Vector2 bottomLeftTileOffset = new Vector2(-0.5f, -0.5f);

        // read tileIndices = otTileMapLayer.tiles (int[])
        System.Type otTileMapLayerType = otTileMapLayer.GetType();
        FieldInfo fieldTiles = otTileMapLayerType.GetField("tiles");
        if (fieldTiles == null) {
            Debug.LogError("Detected a missing 'tiles' member variable at OTTileMapLayer component - Is your Orthello package up to date? 2D ColliderGen might probably not work correctly with this version.");
            return;
        }
        int[] tileIndices = (int[])fieldTiles.GetValue(otTileMapLayer);
        System.Collections.Generic.Dictionary<int, Transform> groupNodeForTileIndex = new System.Collections.Generic.Dictionary<int, Transform>();
        Transform tileGroupNode = null;

        object tileSet = null;

        for (int y = 0; y < tileMapHeight; ++y) {
            for (int x = 0; x < tileMapWidth; ++x) {
                int tileIndex = tileIndices[y * tileMapWidth + x];
                if (tileIndex != 0) {

                    if (groupNodeForTileIndex.ContainsKey(tileIndex)) {
                        tileGroupNode = groupNodeForTileIndex[tileIndex];
                        tileSet = tileSetAtTileIndex[tileIndex];
                    }
                    else {
                        // create a group node
                        GameObject newTileGroup = UndoAware.CreateGameObject("Tile Type " + tileIndex);
                        newTileGroup.transform.parent = layerNode;
                        newTileGroup.transform.localPosition = Vector3.zero;
                        newTileGroup.transform.localScale = Vector3.one;
                        tileGroupNode = newTileGroup.transform;
                        groupNodeForTileIndex[tileIndex] = tileGroupNode;
                        // get tileset for tile index
                        tileSet = AlphaMeshCollider.GetOTTileSetForTileIndex(otTileMap, tileIndex);
                        tileSetAtTileIndex[tileIndex] = tileSet;
                    }
                    // read tileSet.tileSize (Vector2)
                    System.Type otTileSetType = tileSet.GetType();
                    FieldInfo fieldTileSize = otTileSetType.GetField("tileSize");
                    if (fieldTileSize == null) {
                        Debug.LogError("Detected a missing 'tileSize' member variable at OTTileSet class - Is your Orthello package up to date? 2D ColliderGen might probably not work correctly with this version.");
                        return;
                    }
                    Vector2 tileSize = (UnityEngine.Vector2)fieldTileSize.GetValue(tileSet);
                    Vector3 tileScale = new Vector3(mapTileScale.x / mapTileSize.x * tileSize.x, mapTileScale.y / mapTileSize.y * tileSize.y, mapTileScale.z);
                    Vector2 tileCenterOffset = new Vector3(tileScale.x * 0.5f, tileScale.x * 0.5f);

                    // add a GameObject for each enabled tile with name "tile y x"
                    GameObject alphaMeshColliderNode = UndoAware.CreateGameObject("tile " + y + " " + x);
                    alphaMeshColliderNode.transform.parent = tileGroupNode;
                    AlphaMeshCollider alphaMeshColliderComponent = UndoAware.AddComponent<AlphaMeshCollider>(alphaMeshColliderNode);
                    alphaMeshColliderComponent.SetOTTileMap(otTileMap, layerIndex, x, y, tileMapWidth);

                    // set the position of the tile collider according to its (x,y) pos in the map.
                    alphaMeshColliderNode.transform.localPosition = new Vector3(x * mapTileScale.x + bottomLeftTileOffset.x + tileCenterOffset.x, (tileMapSize.y - 1 - y) * mapTileScale.y + bottomLeftTileOffset.y + tileCenterOffset.y, 0.0f);
                    alphaMeshColliderNode.transform.localScale = tileScale;
                }
            }
        }
    }

    //-------------------------------------------------------------------------
    static void AddAlphaMeshColliderToOTTilesSprite(Transform tilesSpriteNode, Component otTilesSprite) {

        AlphaMeshCollider alphaMeshColliderComponent = tilesSpriteNode.GetComponent<AlphaMeshCollider>();
        if (alphaMeshColliderComponent == null) {
            alphaMeshColliderComponent = UndoAware.AddComponent<AlphaMeshCollider>(tilesSpriteNode.gameObject);
            alphaMeshColliderComponent.SetOTTilesSprite(otTilesSprite);
        }
    }
#endif
}

#endif
