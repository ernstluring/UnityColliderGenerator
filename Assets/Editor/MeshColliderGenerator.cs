using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using MeshProcess;
using Util;

public enum ColliderPrimitiveType
{
    Box = 0
}

public class MeshColliderGeneratorEditorWindow : EditorWindow
{
    VHACD _vhacd;
    GameObject _targetObject;
    MeshFilter _targetMesh;

    const int MAX_COLLIDERS_DEFAULT_VALUE = 10;
    const int MAX_VERTEX_DEFAULT_VALUE = 64;

    [SerializeField]
    [Range(1f, 1024f)]
    int _maxColliders = MAX_COLLIDERS_DEFAULT_VALUE;
    [SerializeField]
    [Range(3f, 1024f)]
    int _maxVertex = MAX_VERTEX_DEFAULT_VALUE;
    [SerializeField]
    bool _usePrimitives = false;
    [SerializeField]
    ColliderPrimitiveType _primitiveType;
    [SerializeField]
    bool _updatePrefabs = true;

    SerializedObject _serializedObject;
    SerializedProperty _maxCollidersProp;
    SerializedProperty _maxVertexProp;
    SerializedProperty _usePrimitivesProp;
    SerializedProperty _primitiveTypeProp;
    SerializedProperty _updatePrefabsProp;

    [MenuItem("Window/Mesh Collider Generator")]
    static void OpenWindow()
    {
        GetWindow<MeshColliderGeneratorEditorWindow>(false, "Mesh Collider Generator");
    }

    private void OnEnable()
    {
        if (_vhacd == null)
            _vhacd = FindObjectOfType<VHACD>();
        _serializedObject = new SerializedObject(this);
        _maxCollidersProp = _serializedObject.FindProperty("_maxColliders");
        _maxVertexProp = _serializedObject.FindProperty("_maxVertex");
        _usePrimitivesProp = _serializedObject.FindProperty("_usePrimitives");
        _primitiveTypeProp = _serializedObject.FindProperty("_primitiveType");
        _updatePrefabsProp = _serializedObject.FindProperty("_updatePrefabs");
    }

    private void OnGUI()
    {
        _serializedObject.Update();

        _targetObject = (GameObject)EditorGUILayout.ObjectField("Input GameObject", 
            _targetObject, typeof(GameObject), true);

        if (_targetObject == null)
        {
            EditorGUILayout.HelpBox("Select an input GameObject from the scene", MessageType.Info);
        }
        else
        {
            _targetMesh = _targetObject.GetComponent<MeshFilter>();
            if (_targetMesh == null)
            {
                EditorGUILayout.HelpBox("The input GameObject does not have a mesh", MessageType.Warning);
            }
        }

        EditorGUILayout.Separator();

        EditorGUILayout.PropertyField(_maxCollidersProp);
        EditorGUILayout.PropertyField(_maxVertexProp);

        EditorGUILayout.Separator();

        EditorGUILayout.PropertyField(_usePrimitivesProp);

        if (_usePrimitivesProp.boolValue)
            EditorGUILayout.PropertyField(_primitiveTypeProp);

        EditorGUILayout.PropertyField(_updatePrefabsProp);

        EditorGUILayout.Separator();

        if (_targetObject == null)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Build Colliders"))
        {
            _vhacd.m_parameters.m_maxConvexHulls = (uint)_maxColliders;
            _vhacd.m_parameters.m_maxNumVerticesPerCH = (uint)_maxVertex;
            Optional<ColliderPrimitiveType> usePrimitives = _usePrimitives ? _primitiveType : Optional<ColliderPrimitiveType>.NullOpt();
            Transform buildColliders = ConvexDecomposer.Build(_targetObject, _vhacd, usePrimitives);
            if (_updatePrefabs)
            {
                FindAndUpdatePrefabs(buildColliders);
            }
        }
        GUI.enabled = true;

        EditorGUILayout.Separator();

        if (GUILayout.Button("Reset Settings"))
        {
            _maxColliders = MAX_COLLIDERS_DEFAULT_VALUE;
            _maxVertex = MAX_VERTEX_DEFAULT_VALUE;
        }

        _serializedObject.ApplyModifiedProperties();
    }

    void FindAndUpdatePrefabs(Transform buildColliders)
    {
        string[] allPrefabPaths = FindAllPrefabPathsInProject();

        foreach (var path in allPrefabPaths)
        {
            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                // Find out if the prefab uses the input mesh
                Assert.IsNotNull(_targetMesh);
                var meshFilter = FindMeshFilterWithMeshRecursive(editingScope.prefabContentsRoot, _targetMesh.sharedMesh);
                if (meshFilter.HasValue)
                {
                    // Destroy already existing collider objects in the prefab
                    Transform colliders = meshFilter.Value.transform.Find(buildColliders.name);
                    if (colliders != null)
                    {
                        DestroyImmediate(colliders.gameObject);
                    }

                    // Copy the buildColliders Transform and attach to prefab
                    var clone = Instantiate(buildColliders);
                    clone.SetParent(meshFilter.Value.transform);
                    clone.gameObject.name = buildColliders.gameObject.name;
                    Debug.LogFormat("Updated prefab {0} with the generated colliders", editingScope.prefabContentsRoot.name);
                }
            }
        }
    }

    Optional<MeshFilter> FindMeshFilterWithMeshRecursive(GameObject obj, Mesh meshToFind)
    {
        var meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh == meshToFind)
        {
            return meshFilter;
        }

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            var child = obj.transform.GetChild(i);
            var childMeshFilter = FindMeshFilterWithMeshRecursive(child.gameObject, meshToFind);
            if (childMeshFilter.HasValue) 
                return childMeshFilter;
        }

        return null;
    }

    string[] FindAllPrefabPathsInProject()
    {
        return AssetDatabase.GetAllAssetPaths().Where(p => p.Contains(".prefab")).ToArray();
    }

}
