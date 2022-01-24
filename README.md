# Unity Collider Generator

A Unity tool that can be used for generating convex MeshColliders and convex primitive colliders (currently only BoxColliders) for a Mesh by using convex decomposition.
VHACD is used as the convex decomposition library, found here: https://github.com/Unity-Technologies/VHACD 


## How to use?
1. Open the editor by going to Window -> Mesh Collider Generator.
2. Select a gameobject with a Mesh in the input field.
3. Optionally, set custom build settings values.
4. Optionally, choose to only use primitive colliders (BoxCollider)
5. Press the Build Colliders button.
