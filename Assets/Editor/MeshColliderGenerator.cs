using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using MeshProcess;

public enum ColliderPrimitiveType
{
    Box = 0
}

public class ConvexDecomposer
{
    static readonly string BUILD_COLLIDERS_NAME = "BuildColliders";
    public static void Build(GameObject target, VHACD vhacd, bool usePrimitives, ColliderPrimitiveType primitiveType)
    {
        Assert.IsNotNull(vhacd, "The vhacd object cannot be null");

        // Get or create the wrapper object
        Transform wrapperObjectTrans = GetOrCreateWrapperObject(target);

        // If there already are collider objects, we need destroy them to create new ones
        while (wrapperObjectTrans.childCount != 0)
        {
            GameObject childObj = wrapperObjectTrans.GetChild(0).gameObject;
            Object.DestroyImmediate(childObj);
        }

        
        Mesh targetMesh = target.GetComponent<MeshFilter>().sharedMesh;
        if (targetMesh == null)
        {
            Debug.LogErrorFormat("The given target GameObject {0} has no mesh", target);
        }

        // Convert the target mesh to a list of convex meshes with VHACD
        List<Mesh> convexMeshes = vhacd.GenerateConvexMeshes(targetMesh);

        int triangles = convexMeshes.Sum(x => x.triangles.Length);
        Debug.LogFormat("Generated {0} meshes with {1} triangles", convexMeshes.Count, triangles);

        // Create the collision mesh objects for the target object
        for (int i = 0; i < convexMeshes.Count; i++)
        {
            Mesh convexMesh = convexMeshes[i];
            convexMesh.name = target.name + "collision mesh" + i;

            var go = new GameObject(target.name + " collider " + i);

            if (usePrimitives && primitiveType == ColliderPrimitiveType.Box)
            {
                var boxCol = go.AddComponent<BoxCollider>();
                boxCol.center = convexMesh.bounds.center;
                boxCol.size = convexMesh.bounds.size;
                go.transform.SetParent(wrapperObjectTrans, false);
            }
            else
            {
                var meshCol = go.AddComponent<MeshCollider>();
                meshCol.sharedMesh = convexMesh;
                meshCol.convex = true;
                go.transform.SetParent(wrapperObjectTrans, false);
            }

        }
    }

    private static Transform GetOrCreateWrapperObject(GameObject target)
    {
        // First, try to find the wrapper object as a child of the target object.
        Transform wrapperTrans = target.transform.Find(BUILD_COLLIDERS_NAME);
        if (wrapperTrans == null)
        {
            // If there is no existing wrapper object, create it.
            var wrapperObject = new GameObject(BUILD_COLLIDERS_NAME);
            wrapperTrans = wrapperObject.transform;
            wrapperTrans.SetParent(target.transform, false);
        }
        wrapperTrans.localPosition = Vector3.zero;
        wrapperTrans.localRotation = Quaternion.identity;
        return wrapperTrans;
    }
}

public class MeshColliderGeneratorEditorWindow : EditorWindow
{
    VHACD _vhacd;
    GameObject _targetObject;
    MeshFilter _targetMesh;

    const int MAX_COLLIDERS_DEFAULT_VALUE = 10;
    const int MAX_VERTEX_DEFAULT_VALUE = 64;

    [SerializeField]
    [Range(1, 1024)]
    int _maxColliders = MAX_COLLIDERS_DEFAULT_VALUE;
    [SerializeField]
    [Range(3, 1024)]
    int _maxVertex = MAX_VERTEX_DEFAULT_VALUE;
    [SerializeField]
    bool _usePrimitives = false;
    [SerializeField]
    ColliderPrimitiveType _primitiveType;

    SerializedObject _serializedObject;
    SerializedProperty _maxCollidersProp;
    SerializedProperty _maxVertexProp;
    SerializedProperty _usePrimitivesProp;
    SerializedProperty _primitiveTypeProp;

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

        EditorGUILayout.Separator();

        if (_targetObject == null)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Build Colliders"))
        {
            _vhacd.m_parameters.m_maxConvexHulls = (uint)_maxColliders;
            _vhacd.m_parameters.m_maxNumVerticesPerCH = (uint)_maxVertex;
            ConvexDecomposer.Build(_targetObject, _vhacd, _usePrimitives, _primitiveType);
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

}
