using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            Optional<ColliderPrimitiveType> usePrimitives = _usePrimitives ? _primitiveType : Optional<ColliderPrimitiveType>.NullOpt();
            ConvexDecomposer.Build(_targetObject, _vhacd, usePrimitives);
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
