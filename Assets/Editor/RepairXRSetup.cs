using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text;
using UnityEngine.UI;

[InitializeOnLoad]
public class RepairXRSetup {
    static RepairXRSetup() {
        EditorApplication.delayCall += RunRepair;
    }

    public static void ExecuteFromCommandLine() {
        RunRepair();
        EditorApplication.Exit(0);
    }

    static void RunRepair() {
        // Prevent running multiple times
        if (EditorPrefs.GetBool("RepairXRSetup_Done", false) && !Application.isBatchMode) {
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "XR_Setup_Test") {
            // Try to open it if not open
            string scenePath = "Assets/Scenes/XR_Setup_Test.unity";
            if (System.IO.File.Exists(scenePath)) {
                scene = EditorSceneManager.OpenScene(scenePath);
            } else {
                return;
            }
        }

        Debug.Log("Starting XR_Setup_Test Repair...");
        var testEnv = GameObject.Find("XR_TEST_ONLY");
        if (testEnv == null) testEnv = new GameObject("XR_TEST_ONLY");

        // 1. Delete old Simulator
        var oldSim = GameObject.Find("XR Device Simulator");
        if (oldSim != null) {
            Object.DestroyImmediate(oldSim);
            Debug.Log("Deleted old XR Device Simulator UI prefab.");
        }

        // 2. Instantiate Official Simulator
        var simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/XR Device Simulator/XR Device Simulator.prefab");
        GameObject simInst = null;
        if (simPrefab != null) {
            simInst = PrefabUtility.InstantiatePrefab(simPrefab) as GameObject;
            simInst.name = "XR Device Simulator";
            simInst.transform.parent = testEnv.transform;
            Debug.Log("Instantiated the complete XR Device Simulator prefab.");
        } else {
            Debug.LogError("Official Simulator prefab not found at expected path.");
        }

        // 3. Test Environment Visibility
        var light = GameObject.Find("Directional Light");
        if (light == null) {
            light = new GameObject("Directional Light");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.transform.parent = testEnv.transform;
            Debug.Log("Added Directional Light.");
        }

        // Materials
        var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        floorMat.color = Color.darkGray;
        var grabMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        grabMat.color = Color.red;
        var simpleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        simpleMat.color = Color.blue;

        var floor = GameObject.Find("Test_Floor");
        if (floor != null) {
            floor.transform.position = new Vector3(0, 0, 0); 
            var mr = floor.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = floorMat;
            Debug.Log("Adjusted Test_Floor to Y=0 and applied dark grey material.");
        }

        var grabCube = GameObject.Find("Test_GrabCube");
        if (grabCube != null) {
            grabCube.transform.position = new Vector3(0, 1.0f, 2.0f);
            var mr = grabCube.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = grabMat;
            Debug.Log("Adjusted Test_GrabCube to Y=1.0, Z=2.0 and applied red material.");
        }

        var simpleCube = GameObject.Find("Test_SimpleCube");
        if (simpleCube != null) {
            simpleCube.transform.position = new Vector3(0.5f, 1.0f, 2.0f);
            var mr = simpleCube.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = simpleMat;
            Debug.Log("Adjusted Test_SimpleCube beside GrabCube and applied blue material.");
        }

        // 4. Camera Offset
        var xrOrigin = GameObject.Find("XR Origin (VR)");
        if (xrOrigin != null) {
            xrOrigin.transform.position = Vector3.zero;
            
            var camOffset = xrOrigin.transform.Find("Camera Offset");
            if (camOffset != null) {
                camOffset.localPosition = new Vector3(0, 1.7f, 0);
                Debug.Log("Set Camera Offset Y to 1.7m.");
            }
        }

        // 5. Instruction Canvas
        var oldCanvas = GameObject.Find("Simulator_Instructions");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

        var canvasGo = new GameObject("Simulator_Instructions");
        canvasGo.transform.parent = testEnv.transform;
        canvasGo.transform.position = new Vector3(-1.5f, 1.5f, 3.5f);
        canvasGo.transform.rotation = Quaternion.Euler(0, 0, 0);
        
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(3, 2);
        canvasGo.transform.localScale = Vector3.one;
        
        var textGo = new GameObject("Text");
        textGo.transform.parent = canvasGo.transform;
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24; 
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        var rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = new Vector3(0.01f, 0.01f, 1f); 

        StringBuilder instr = new StringBuilder();
        instr.AppendLine("XR SIMULATOR BINDINGS:\n");

        if (simInst != null) {
            var simCompType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator, Unity.XR.Interaction.Toolkit");
            if (simCompType != null) {
                var simComp = simInst.GetComponent(simCompType);
                if (simComp != null) {
                    foreach (var field in simCompType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)) {
                        if (field.FieldType.Name.Contains("InputActionReference")) {
                            var val = field.GetValue(simComp);
                            if (val != null) {
                                var actionProp = val.GetType().GetProperty("action");
                                if (actionProp != null) {
                                    var action = actionProp.GetValue(val) as UnityEngine.InputSystem.InputAction;
                                    if (action != null) {
                                        string bindings = "";
                                        foreach (var b in action.bindings) {
                                            if (!b.isComposite) {
                                                bindings += b.ToDisplayString() + " | ";
                                            }
                                        }
                                        if (bindings.Length > 0) {
                                            string cleanName = field.Name.Replace("m_", "").Replace("Action", "");
                                            instr.AppendLine($"{cleanName}: {bindings}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        text.text = instr.ToString();
        Debug.Log("Generated Instruction Canvas with actual bindings.");
        
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        
        EditorPrefs.SetBool("RepairXRSetup_Done", true);
        Debug.Log("XR_Setup_Test Repair Complete!");
    }
}
