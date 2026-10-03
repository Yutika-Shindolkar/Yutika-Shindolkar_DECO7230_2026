using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Adds a real XR Origin (VR) rig to the World scene so this project can be built
// and run on a Quest headset. This is the "just get it running on-headset" pass -
// it does NOT rewire the existing mouse-driven interactions (DragNote, SpawnButton,
// PlusButtonClick, TourMode) to controllers. Those still only respond to
// mouse/keyboard for now. This just gets head tracking + basic locomotion working
// so the scene can be looked around in VR to confirm the build itself launches.
public class VRSetup
{
    const string XROriginPrefabPath =
        "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

    [MenuItem("Tools/VR Setup/Add XR Origin To World Scene")]
    public static void AddXROriginToWorldScene()
    {
        // Work on whichever scene is open if it's already World, otherwise open it -
        // avoids silently editing the wrong scene if something else was open.
        Scene active = EditorSceneManager.GetActiveScene();
        if (!active.path.EndsWith("World.unity"))
        {
            active = EditorSceneManager.OpenScene("Assets/Scenes/World.unity", OpenSceneMode.Single);
        }

        if (GameObject.Find("XR Origin (XR Rig)") != null)
        {
            Debug.Log("VRSetup: an 'XR Origin (XR Rig)' already exists in the World scene, skipping - delete it first if you want a fresh one.");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XROriginPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("VRSetup: couldn't find the XR Origin prefab at " + XROriginPrefabPath +
                " - check the XR Interaction Toolkit Starter Assets sample is still installed at that path.");
            return;
        }

        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab, active);
        rig.name = "XR Origin (XR Rig)";

        // Old desktop camera - find it, copy its position/rotation onto the new rig so
        // you start out looking at the same part of the whiteboard, then disable
        // (not delete) the old camera GameObject. PlayerMove.cs and the old Main
        // Camera stay in the scene, untouched, so the desktop version still works if
        // you re-enable that object later.
        GameObject oldCamera = GameObject.Find("Main Camera");
        if (oldCamera != null)
        {
            rig.transform.position = oldCamera.transform.position;
            rig.transform.rotation = Quaternion.Euler(0f, oldCamera.transform.eulerAngles.y, 0f);
            oldCamera.SetActive(false);
            Debug.Log("VRSetup: copied Main Camera's position/heading onto the XR Origin, then disabled the old Main Camera (not deleted - re-enable it to go back to desktop mode).");
        }
        else
        {
            Debug.LogWarning("VRSetup: no 'Main Camera' found to copy the starting position from - XR Origin placed at the world origin instead.");
        }

        EditorUtility.SetDirty(rig);
        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);

        Debug.Log("VRSetup: XR Origin (XR Rig) added to World.unity and scene saved. " +
            "Existing mouse-driven buttons/dragging/tour mode won't respond to controllers yet - " +
            "this pass only gets head tracking and basic teleport/move working so the build launches correctly on-headset.");
    }
}
