using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using MeshProcess;
using Util;

public class ConvexDecomposer
{
    static readonly string BUILD_COLLIDERS_NAME = "BuildColliders";
    public static void Build(GameObject target, VHACD vhacd, Optional<ColliderPrimitiveType> primitiveType = new Optional<ColliderPrimitiveType>())
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

            if (primitiveType.HasValue && primitiveType.Value == ColliderPrimitiveType.Box)
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
