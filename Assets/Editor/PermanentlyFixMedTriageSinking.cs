using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PermanentlyFixMedTriageSinking
{
    [MenuItem("MedTriage/Fix XR Sinking")]
    public static void ApplyFix()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play Mode before applying the sinking fix.");
            return;
        }

        GameObject origin = GameObject.Find("MedTriage_XR_Origin");

        if (!origin)
        {
            Debug.LogError("MedTriage_XR_Origin was not found.");
            return;
        }

        // Restore the intended starting position.
        origin.transform.position = new Vector3(15f, 0f, -8.3f);

        // Disable every XR Gravity Provider.
        foreach (Component component in
                 origin.GetComponentsInChildren<Component>(true))
        {
            if (component != null &&
                component.GetType().Name == "GravityProvider")
            {
                if (component is Behaviour behaviour)
                    behaviour.enabled = false;

                component.gameObject.SetActive(false);
            }
        }

        // Correct the XR Character Controller.
        CharacterController controller =
            origin.GetComponent<CharacterController>();

        if (!controller)
            controller = origin.AddComponent<CharacterController>();

        controller.enabled = true;
        controller.height = 1.7f;
        controller.radius = 0.25f;
        controller.center = new Vector3(0f, 0.85f, 0f);
        controller.skinWidth = 0.05f;
        controller.stepOffset = 0.2f;
        controller.slopeLimit = 45f;
        controller.minMoveDistance = 0f;

        // Add a large invisible floor collider.
        GameObject safetyFloor =
            GameObject.Find("MedTriage_GlobalSafetyFloor");

        if (!safetyFloor)
            safetyFloor = new GameObject("MedTriage_GlobalSafetyFloor");

        safetyFloor.transform.position =
            new Vector3(15f, -0.08f, -10.5f);

        safetyFloor.transform.rotation = Quaternion.identity;
        safetyFloor.transform.localScale = Vector3.one;

        BoxCollider floorCollider =
            safetyFloor.GetComponent<BoxCollider>();

        if (!floorCollider)
            floorCollider = safetyFloor.AddComponent<BoxCollider>();

        floorCollider.enabled = true;
        floorCollider.isTrigger = false;
        floorCollider.center = Vector3.zero;
        floorCollider.size = new Vector3(40f, 0.2f, 40f);

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();

        Debug.Log(
            "XR sinking fixed: gravity disabled and safety floor installed.");
    }
}