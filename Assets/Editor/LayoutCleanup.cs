using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

public class LayoutCleanup {
    [MenuItem("MedTriage/Run Furnishings Cleanup")]
    public static void RunCleanup() {
        // 1. Load scene
        string scenePath = "Assets/MedTriage/Scenes/Hospital_Integration.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);
        if (!scene.IsValid()) {
            Debug.LogError("Failed to load scene at " + scenePath);
            return;
        }

        var furnishingsRoot = GameObject.Find("Furnishings");
        if (furnishingsRoot == null) {
            Debug.LogError("Furnishings root not found in scene!");
            return;
        }

        // 2. Gather floors
        var allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var floors = new List<Renderer>();
        foreach (var r in allRenderers) {
            if (r == null || r.transform.IsChildOf(furnishingsRoot.transform)) continue;
            string name = r.gameObject.name.ToLower();
            if (name.Contains("floor") || name.Contains("terrain") || name.Contains("ground") || name.Contains("paving") || name.Contains("podium")) {
                floors.Add(r);
            }
        }

        // 3. Register Undo for everything under Furnishings
        Undo.RegisterCompleteObjectUndo(furnishingsRoot, "Furnishings Cleanup Pass");

        // 4. Perform Room-by-Room Repositioning
        ProcessWaitingBay(furnishingsRoot.transform, floors);
        ProcessReception(furnishingsRoot.transform, floors);
        ProcessPatientRooms(furnishingsRoot.transform, floors);
        ProcessCardiacRoom(furnishingsRoot.transform, floors);
        ProcessExaminationRooms(furnishingsRoot.transform, floors);
        ProcessUtilityStorage(furnishingsRoot.transform, floors);
        ProcessExteriorEntrance(furnishingsRoot.transform, floors);

        // 5. Build/Cleanup Validation hierarchy for Manual Review Markers
        CleanupValidationMarkers();

        // 6. Save and dirty
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("HOSPITAL FURNITURE CLEANUP RUN COMPLETED AND SCENE SAVED.");
    }

    private static void ProcessWaitingBay(Transform root, List<Renderer> floors) {
        Transform waitingBay = root.Find("WaitingBay");
        if (waitingBay == null) return;

        // Chairs 01-03
        var chairs = new Transform[] {
            waitingBay.Find("WaitingBay_Chair_01"),
            waitingBay.Find("WaitingBay_Chair_02"),
            waitingBay.Find("WaitingBay_Chair_03")
        };
        float[] chairZs = new float[] { 4.8f, 6.2f, 7.6f };
        for (int i = 0; i < chairs.Length; i++) {
            if (chairs[i] != null) {
                SetPosRot(chairs[i], new Vector3(-18.0f, 0f, chairZs[i]), Quaternion.Euler(0f, 90f, 0f));
                AlignToFloor(chairs[i].gameObject, floors);
                ApplyStaticFlags(chairs[i].gameObject, false);
            }
        }

        // Benches 01-02
        var benches = new Transform[] {
            waitingBay.Find("WaitingBay_Bench_01"),
            waitingBay.Find("WaitingBay_Bench_02")
        };
        float[] benchZs = new float[] { 5.5f, 7.5f };
        for (int i = 0; i < benches.Length; i++) {
            if (benches[i] != null) {
                SetPosRot(benches[i], new Vector3(-11.0f, 0f, benchZs[i]), Quaternion.Euler(0f, 270f, 0f));
                AlignToFloor(benches[i].gameObject, floors);
                ApplyStaticFlags(benches[i].gameObject, true); // Benches can be static
            }
        }

        // Table
        Transform table = waitingBay.Find("WaitingBay_Table_01");
        if (table != null) {
            SetPosRot(table, new Vector3(-14.5f, 0f, 6.5f), Quaternion.identity);
            AlignToFloor(table.gameObject, floors);
            ApplyStaticFlags(table.gameObject, true); // Waiting coffee table is static
        }

        // Magazine
        Transform mag = waitingBay.Find("WaitingBay_Magazine_01");
        if (mag != null && table != null) {
            // Place flat on top of the table
            Bounds tableBounds = GetCombinedBounds(table.gameObject);
            SetPosRot(mag, new Vector3(-14.5f, tableBounds.max.y + 0.002f, 6.5f), Quaternion.Euler(0f, 30f, 0f));
            ApplyStaticFlags(mag.gameObject, false); // magazine is interactive/movable
        }

        // Wheelchair
        Transform wc = waitingBay.Find("WaitingBay_Wheelchair_01");
        if (wc != null) {
            SetPosRot(wc, new Vector3(-18.0f, 0f, 3.2f), Quaternion.Euler(0f, 90f, 0f));
            AlignToFloor(wc.gameObject, floors);
            ApplyStaticFlags(wc.gameObject, false); // Wheelchair is dynamic
        }

        // Ceiling lights & Exit Sign
        var lights = new Transform[] {
            waitingBay.Find("WaitingBay_Light_01"),
            waitingBay.Find("WaitingBay_Light_02"),
            waitingBay.Find("WaitingBay_ExitSign")
        };
        foreach (var l in lights) {
            if (l != null) {
                ApplyStaticFlags(l.gameObject, true);
            }
        }
    }

    private static void ProcessReception(Transform root, List<Renderer> floors) {
        Transform rec = root.Find("Reception");
        if (rec == null) return;

        Transform desk = rec.Find("Reception_Desk");
        Transform chair = rec.Find("Reception_Chair");
        Transform cab = rec.Find("Reception_Cabinet");

        if (desk != null) {
            // Keep at current position, ensure static
            ApplyStaticFlags(desk.gameObject, true);
        }

        if (chair != null) {
            // Move behind the desk and face it (South, 180 degrees)
            SetPosRot(chair, new Vector3(-14.5f, 0f, 11.8f), Quaternion.Euler(0f, 180f, 0f));
            AlignToFloor(chair.gameObject, floors);
            ApplyStaticFlags(chair.gameObject, false); // Chair is dynamic
        }

        if (cab != null) {
            // Move to back wall, facing South
            SetPosRot(cab, new Vector3(-12.8f, 0f, 12.0f), Quaternion.Euler(0f, 180f, 0f));
            AlignToFloor(cab.gameObject, floors);
            ApplyStaticFlags(cab.gameObject, true); // Cabinet is static
        }
    }

    private static void ProcessPatientRooms(Transform root, List<Renderer> floors) {
        Transform pr = root.Find("PatientRooms");
        if (pr == null) return;

        // Process Room 1 (Surgical), Room 2 (Trauma), Room 3 (Isolation)
        string[] rooms = new string[] { "PatientRoom_01", "PatientRoom_02", "PatientRoom_03" };
        float[] roomXs = new float[] { 5.0f, -5.0f, -15.0f };
        float[] cabinetXs = new float[] { 8.9f, -0.9f, -10.9f }; // Moved inwards slightly to prevent wall penetration
        float[] rotChairs = new float[] { 270f, 90f, 90f };

        for (int i = 0; i < rooms.Length; i++) {
            Transform rm = pr.Find(rooms[i]);
            if (rm == null) continue;

            Transform bed = rm.Find(rooms[i] + "_Bed");
            Transform cab = rm.Find(rooms[i] + "_BedsideCabinet");
            Transform chair = rm.Find(rooms[i] + "_VisitorChair");
            Transform pole = rm.Find(rooms[i] + "_IVPole");
            Transform bag = rm.Find(rooms[i] + "_IVBag");
            Transform stor = rm.Find(rooms[i] + "_StorageCabinet");
            Transform light = rm.Find(rooms[i] + "_Light");

            if (bed != null) {
                SetPosRot(bed, new Vector3(roomXs[i], 0f, -11.0f), Quaternion.identity);
                AlignToFloor(bed.gameObject, floors);
                ApplyStaticFlags(bed.gameObject, false); // Beds are dynamic
            }

            if (cab != null) {
                // Next to the head of the bed
                float offsetSide = (rooms[i] == "PatientRoom_01") ? -1.2f : -1.2f; // Left of bed
                if (rooms[i] == "PatientRoom_01") offsetSide = -1.2f;
                else offsetSide = -1.2f;
                SetPosRot(cab, new Vector3(roomXs[i] + offsetSide, 0f, -12.0f), Quaternion.Euler(0f, (rooms[i] == "PatientRoom_01") ? 90f : 270f, 0f));
                AlignToFloor(cab.gameObject, floors);
                ApplyStaticFlags(cab.gameObject, true);
            }

            if (chair != null) {
                // Facing the bed
                float offsetSide = (rooms[i] == "PatientRoom_01") ? 1.3f : 1.3f;
                if (rooms[i] == "PatientRoom_01") offsetSide = 1.3f;
                else offsetSide = 1.3f;
                SetPosRot(chair, new Vector3(roomXs[i] + offsetSide, 0f, -10.5f), Quaternion.Euler(0f, rotChairs[i], 0f));
                AlignToFloor(chair.gameObject, floors);
                ApplyStaticFlags(chair.gameObject, false);
            }

            if (pole != null) {
                // Near accessible side of the bed head
                float offsetSide = (rooms[i] == "PatientRoom_01") ? -1.2f : -1.2f;
                SetPosRot(pole, new Vector3(roomXs[i] + offsetSide, 0f, -11.0f), Quaternion.identity);
                AlignToFloor(pole.gameObject, floors);
                ApplyStaticFlags(pole.gameObject, false);

                // Align IV Bag to hook (x=0.29, y=1.9, z=0 relative to pole)
                if (bag != null) {
                    AlignIVBagToPole(pole, bag);
                }
            }

            if (stor != null) {
                // Flush against side wall, facing into room
                SetPosRot(stor, new Vector3(cabinetXs[i], 0f, -6.0f), Quaternion.Euler(0f, (rooms[i] == "PatientRoom_01") ? 270f : 90f, 0f));
                AlignToFloor(stor.gameObject, floors);
                ApplyStaticFlags(stor.gameObject, true);
            }

            if (light != null) {
                ApplyStaticFlags(light.gameObject, true);
            }
        }
    }

    private static void ProcessCardiacRoom(Transform root, List<Renderer> floors) {
        Transform rm = root.Find("CardiacRoom_Reserved");
        if (rm == null) return;

        Transform bed = rm.Find("CardiacRoom_Bed");
        Transform cab = rm.Find("CardiacRoom_BedsideCabinet");
        Transform chair = rm.Find("CardiacRoom_VisitorChair");
        Transform pole = rm.Find("CardiacRoom_IVPole");
        Transform bag = rm.Find("CardiacRoom_IVBag");
        Transform stor = rm.Find("CardiacRoom_StorageCabinet");
        Transform light = rm.Find("CardiacRoom_Light");

        if (bed != null) {
            SetPosRot(bed, new Vector3(15.0f, 0f, -11.0f), Quaternion.identity);
            AlignToFloor(bed.gameObject, floors);
            ApplyStaticFlags(bed.gameObject, false);
        }

        if (cab != null) {
            SetPosRot(cab, new Vector3(13.8f, 0f, -12.0f), Quaternion.Euler(0f, 90f, 0f));
            AlignToFloor(cab.gameObject, floors);
            ApplyStaticFlags(cab.gameObject, true);
        }

        if (chair != null) {
            SetPosRot(chair, new Vector3(16.3f, 0f, -10.5f), Quaternion.Euler(0f, 270f, 0f));
            AlignToFloor(chair.gameObject, floors);
            ApplyStaticFlags(chair.gameObject, false);
        }

        if (pole != null) {
            SetPosRot(pole, new Vector3(13.8f, 0f, -11.0f), Quaternion.identity);
            AlignToFloor(pole.gameObject, floors);
            ApplyStaticFlags(pole.gameObject, false);

            if (bag != null) {
                AlignIVBagToPole(pole, bag);
            }
        }

        if (stor != null) {
            // Positioned at x=18.9 to prevent tree or wall penetration
            SetPosRot(stor, new Vector3(18.9f, 0f, -6.0f), Quaternion.Euler(0f, 270f, 0f));
            AlignToFloor(stor.gameObject, floors);
            ApplyStaticFlags(stor.gameObject, true);
        }

        if (light != null) {
            ApplyStaticFlags(light.gameObject, true);
        }
    }

    private static void ProcessExaminationRooms(Transform root, List<Renderer> floors) {
        Transform er = root.Find("ExaminationRooms");
        if (er == null) return;

        string[] rooms = new string[] { "ExaminationRoom_01", "ExaminationRoom_02" };
        float[] roomXs = new float[] { 5.0f, -5.0f };
        float[] rotChairs = new float[] { 270f, 90f };

        for (int i = 0; i < rooms.Length; i++) {
            Transform rm = er.Find(rooms[i]);
            if (rm == null) continue;

            Transform bed = rm.Find(rooms[i] + "_Bed");
            Transform cab = rm.Find(rooms[i] + "_BedsideCabinet");
            Transform chair = rm.Find(rooms[i] + "_VisitorChair");
            Transform pole = rm.Find(rooms[i] + "_IVPole");
            Transform bag = rm.Find(rooms[i] + "_IVBag");
            Transform light = rm.Find(rooms[i] + "_Light");

            if (bed != null) {
                // Rotated 180 (head near the top wall)
                SetPosRot(bed, new Vector3(roomXs[i], 0f, 11.0f), Quaternion.Euler(0f, 180f, 0f));
                AlignToFloor(bed.gameObject, floors);
                ApplyStaticFlags(bed.gameObject, false);
            }

            if (cab != null) {
                float offsetSide = (rooms[i] == "ExaminationRoom_01") ? -1.2f : -1.2f;
                SetPosRot(cab, new Vector3(roomXs[i] + offsetSide, 0f, 12.0f), Quaternion.Euler(0f, (rooms[i] == "ExaminationRoom_01") ? 90f : 270f, 0f));
                AlignToFloor(cab.gameObject, floors);
                ApplyStaticFlags(cab.gameObject, true);
            }

            if (chair != null) {
                float offsetSide = (rooms[i] == "ExaminationRoom_01") ? 1.3f : 1.3f;
                SetPosRot(chair, new Vector3(roomXs[i] + offsetSide, 0f, 11.5f), Quaternion.Euler(0f, rotChairs[i], 0f));
                AlignToFloor(chair.gameObject, floors);
                ApplyStaticFlags(chair.gameObject, false);
            }

            if (pole != null) {
                float offsetSide = (rooms[i] == "ExaminationRoom_01") ? -1.2f : -1.2f;
                SetPosRot(pole, new Vector3(roomXs[i] + offsetSide, 0f, 11.0f), Quaternion.identity);
                AlignToFloor(pole.gameObject, floors);
                ApplyStaticFlags(pole.gameObject, false);

                if (bag != null) {
                    AlignIVBagToPole(pole, bag);
                }
            }

            if (light != null) {
                ApplyStaticFlags(light.gameObject, true);
            }
        }
    }

    private static void ProcessUtilityStorage(Transform root, List<Renderer> floors) {
        Transform ut = root.Find("UtilityStorage");
        if (ut == null) return;

        Transform desk = ut.Find("Office_Table");
        Transform chair1 = ut.Find("Office_Chair_01");
        Transform chair2 = ut.Find("Office_Chair_02");
        Transform cab1 = ut.Find("Office_StorageCabinet_01");
        Transform cab2 = ut.Find("Office_StorageCabinet_02");

        if (desk != null) {
            SetPosRot(desk, new Vector3(15.0f, 0f, 8.0f), Quaternion.identity);
            AlignToFloor(desk.gameObject, floors);
            ApplyStaticFlags(desk.gameObject, true);
        }

        if (chair1 != null) {
            SetPosRot(chair1, new Vector3(15.0f, 0f, 6.8f), Quaternion.identity); // facing desk
            AlignToFloor(chair1.gameObject, floors);
            ApplyStaticFlags(chair1.gameObject, false);
        }

        if (chair2 != null) {
            SetPosRot(chair2, new Vector3(15.0f, 0f, 9.2f), Quaternion.Euler(0f, 180f, 0f)); // facing desk
            AlignToFloor(chair2.gameObject, floors);
            ApplyStaticFlags(chair2.gameObject, false);
        }

        if (cab1 != null) {
            SetPosRot(cab1, new Vector3(11.0f, 0f, 4.8f), Quaternion.Euler(0f, 90f, 0f));
            AlignToFloor(cab1.gameObject, floors);
            ApplyStaticFlags(cab1.gameObject, true);
        }

        if (cab2 != null) {
            SetPosRot(cab2, new Vector3(11.0f, 0f, 7.2f), Quaternion.Euler(0f, 90f, 0f));
            AlignToFloor(cab2.gameObject, floors);
            ApplyStaticFlags(cab2.gameObject, true);
        }
    }

    private static void ProcessExteriorEntrance(Transform root, List<Renderer> floors) {
        Transform ext = root.Find("ExteriorEntrance");
        if (ext == null) return;

        Transform bench1 = ext.Find("Exterior_Bench_01");
        Transform bench2 = ext.Find("Exterior_Bench_02");
        Transform wc = ext.Find("Exterior_Wheelchair_01");

        // Move to flat ground areas (x < -21 or x > -9) to clear the steps podium entirely
        if (bench1 != null) {
            SetPosRot(bench1, new Vector3(-22.0f, 0f, 18.0f), Quaternion.identity);
            AlignToFloor(bench1.gameObject, floors);
            ApplyStaticFlags(bench1.gameObject, true);
        }

        if (bench2 != null) {
            SetPosRot(bench2, new Vector3(-8.0f, 0f, 18.0f), Quaternion.identity);
            AlignToFloor(bench2.gameObject, floors);
            ApplyStaticFlags(bench2.gameObject, true);
        }

        if (wc != null) {
            SetPosRot(wc, new Vector3(-24.0f, 0f, 18.0f), Quaternion.Euler(0f, 180f, 0f));
            AlignToFloor(wc.gameObject, floors);
            ApplyStaticFlags(wc.gameObject, false);
        }
    }

    private static void AlignIVBagToPole(Transform pole, Transform bag) {
        // Target attachment point in pole's local space is (0.29, 1.90, 0.0)
        string attachName = "IVBag_AttachPoint";
        Transform attachPoint = pole.Find(attachName);
        if (attachPoint == null) {
            var go = new GameObject(attachName);
            attachPoint = go.transform;
            Undo.RegisterCreatedObjectUndo(go, "Create IV Bag Attach Point");
            Undo.SetTransformParent(attachPoint, pole, "Parent Attach Point");
            attachPoint.localPosition = new Vector3(0.29f, 1.90f, 0f);
            attachPoint.localRotation = Quaternion.identity;
            attachPoint.localScale = Vector3.one;
        }

        // Align bag topmost bounds vertex to hook
        Bounds bagBounds = GetCombinedBounds(bag.gameObject);
        Vector3 bagTopOffsetInWrapperSpace = bag.InverseTransformPoint(new Vector3(bagBounds.center.x, bagBounds.max.y, bagBounds.center.z));
        
        // Reparent bag to attach point
        Undo.SetTransformParent(bag, attachPoint, "Parent IV Bag");
        
        // Position bag relative to hook (small offset to hang below hook, e.g. -0.05m y-offset to prevent intersection)
        bag.localRotation = Quaternion.identity;
        bag.localScale = Vector3.one;
        
        // We set its wrapper local position such that the top of its bounds matches the hook (with a 5cm buffer)
        Vector3 localAttachTarget = -bagTopOffsetInWrapperSpace + new Vector3(0f, -0.05f, 0f);
        bag.localPosition = localAttachTarget;
    }

    private static void CleanupValidationMarkers() {
        var valRoot = GameObject.Find("Validation");
        if (valRoot == null) {
            valRoot = new GameObject("Validation");
            Undo.RegisterCreatedObjectUndo(valRoot, "Create Validation Root");
        }

        // Clear existing markers under validation
        var children = new List<Transform>();
        foreach (Transform t in valRoot.transform) {
            children.Add(t);
        }
        foreach (var child in children) {
            Undo.DestroyObjectImmediate(child.gameObject);
        }

        // Create editor-only markers for manual review
        CreateMarker(valRoot.transform, "ManualReview_ExteriorEntrance", new Vector3(-15f, 0.5f, 17f), "Exterior plaza step clearance review");
        CreateMarker(valRoot.transform, "ManualReview_WaitingBay", new Vector3(-14.5f, 0.5f, 6.5f), "Waiting bay aisle spacing review");
    }

    private static void CreateMarker(Transform parent, string name, Vector3 pos, string reason) {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Marker " + name);
        Undo.SetTransformParent(go.transform, parent, "Parent Marker");
        go.transform.position = pos;
        go.tag = "EditorOnly";
        Debug.Log($"[MARKER CREATED] {name} created at {pos} | Reason: {reason}");
    }

    private static void SetPosRot(Transform t, Vector3 worldPos, Quaternion worldRot) {
        Undo.RecordObject(t, "Modify transform of " + t.name);
        t.position = worldPos;
        t.rotation = worldRot;
    }

    private static void AlignToFloor(GameObject go, List<Renderer> floors) {
        Bounds bounds = GetCombinedBounds(go);
        float floorY = FindFloorY(go.transform.position, floors);
        
        float bottomY = bounds.min.y;
        float offset = floorY - bottomY;
        
        Vector3 pos = go.transform.position;
        pos.y += offset;
        go.transform.position = pos;
    }

    private static float FindFloorY(Vector3 position, List<Renderer> floors) {
        // Try Raycast inside room (from y = 2.0 down to floor)
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(position.x, 2.0f, position.z), Vector3.down, out hit, 10f)) {
            string hitName = hit.collider.gameObject.name.ToLower();
            if (hitName.Contains("floor") || hitName.Contains("terrain") || hitName.Contains("ground") || hitName.Contains("paving") || hitName.Contains("podium")) {
                return hit.point.y;
            }
        }
        
        // Fallback: search floors by bounding box in X/Z
        float bestY = 0f;
        float minDistance = float.MaxValue;
        bool found = false;
        foreach (var floor in floors) {
            Bounds b = floor.bounds;
            if (position.x >= b.min.x && position.x <= b.max.x &&
                position.z >= b.min.z && position.z <= b.max.z) {
                float dist = Mathf.Abs(position.y - b.max.y);
                if (dist < minDistance) {
                    minDistance = dist;
                    bestY = b.max.y;
                    found = true;
                }
            }
        }
        return found ? bestY : 0f;
    }

    private static Bounds GetCombinedBounds(GameObject go) {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) {
            b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }

    private static void ApplyStaticFlags(GameObject go, bool isStatic) {
        var flags = isStatic ? StaticEditorFlags.OccluderStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI : 0;
        GameObjectUtility.SetStaticEditorFlags(go, flags);
        foreach (Transform t in go.transform) {
            // Only apply recursively if it's not a separate wrapper instance (like IV bag under IV pole)
            if (t.name == "Model" || t.GetComponent<Renderer>() != null) {
                ApplyStaticFlags(t.gameObject, isStatic);
            }
        }
    }
}
