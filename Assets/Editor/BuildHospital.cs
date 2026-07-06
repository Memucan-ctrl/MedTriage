using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class BuildHospital
{
    // ===== PATHS =====
    const string FbxFolder = "Assets/AssetsHospitalKit/FBX/FBX";
    const string MaterialsFolder = "Assets/AssetsHospitalKit/Materials";

    // ===== GRID (in tiles) =====
    const int W = 17;   // wider: corridor is now 3 tiles
    const int L = 24;   // rooms z0-17, open lobby z18-23

    static float tile = 2f;
    static float wallH = 3f;
    static Vector3 floorScale = Vector3.one;

    static int[,] room;
    class RoomInfo { public int id; public char type; public int x0,z0,x1,z1; }
    static Dictionary<int,RoomInfo> rooms;

    static readonly Dictionary<string,string> MatMap = new Dictionary<string,string>()
    {
        {"floor_tile_1","Floors_1"},{"floor_tile_2","Floors_1"},{"tile_corner","Floors_1"},
        {"tile_wall","Walls_1"},{"tile_wall_half","Walls_1"},{"pillar","Walls_1"},{"tile_window","Walls_1"},
        {"ceiling_tile","ceiling_1"},{"ceiling_light","Ceiling_light"},
        {"door_1","Door_1"},{"door_2","Door_1"},{"tile_doorway_1","Doorway_1"},{"tile_doorway_2","Doorway_1"},
        {"bed","bed"},{"chair","Chairs_table_1"},{"table","Chairs_table_1"},{"bench","Chairs_table_1"},
        {"cabinet_1","Chairs_table_1"},{"cabinet_2","Chairs_table_1"},{"cabinet_3","Chairs_table_1"},
        {"IV_Bag","Iv_bag"},{"IV_Bag_holder","Iv_pole"},
        {"Exit_sign","exit_sign"},{"Magazine1","Magazine"},{"wheel_chair","wheel_chair"},
    };

    [MenuItem("Tools/Hospital/3. Build Full Hospital")]
    public static void Build()
    {
        var floorP = LoadPiece("floor_tile_1");
        var wallP  = LoadPiece("tile_wall");
        if (floorP == null || wallP == null) { Debug.LogError("Missing floor/wall FBX. Check FbxFolder."); return; }
        Vector3 wsz = GetSize(wallP);
        tile = wsz.x; if (tile < 0.01f) tile = 2f;
        wallH = wsz.y; if (wallH < 0.01f) wallH = 3f;
        Vector3 fsz = GetSize(floorP);
        floorScale = new Vector3(tile / Mathf.Max(fsz.x, 0.01f), 1f, tile / Mathf.Max(fsz.z, 0.01f));

        Layout();

        string[] junk = { "Hospital", "Hospital_Room", "Floor", "Ceiling", "Walls", "Props", "Lighting", "Corners" };
        foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            if (System.Array.IndexOf(junk, go.name) >= 0) Object.DestroyImmediate(go);

       var root = new GameObject("Hospital");
        BaseSlab(root.transform);
        BuildFloor(root.transform);
        BuildWalls(root.transform);
        Corners(root.transform);
        Furnish(root.transform);
        Lighting(root.transform);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"✅ Hospital built. tile={tile}, wallH={wallH}, rooms={rooms.Count}");
    }
    static void BaseSlab(Transform parent)
{
    var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
    slab.name = "Floor_Slab";
    slab.transform.parent = parent;
    slab.transform.localScale = new Vector3(W*tile, 0.2f, L*tile);
    slab.transform.position = new Vector3((W-1)*tile*0.5f, -0.11f, (L-1)*tile*0.5f);
    var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/Floors_1.mat");
    if (mat != null) slab.GetComponent<Renderer>().sharedMaterial = mat;
}

    static void Layout()
    {
        room = new int[W,L];
        rooms = new Dictionary<int,RoomInfo>();
        Paint(1,'C', 7,0, 9,17);     // central corridor (3 wide)
        Paint(2,'w', 0,0, 6,5);      // left: ward
        Paint(3,'w', 0,6, 6,11);     // left: ward
        Paint(4,'t', 0,12, 6,17);    // left: treatment
        Paint(5,'w', 10,0, 16,5);    // right: ward
        Paint(6,'t', 10,6, 16,11);   // right: treatment
        Paint(7,'k', 10,12, 16,17);  // right: consult
        Paint(8,'L', 0,18, 16,23);   // open waiting lobby + bay
    }

    static void Paint(int id, char type, int x0,int z0,int x1,int z1)
    {
        rooms[id] = new RoomInfo{ id=id, type=type, x0=x0, z0=z0, x1=x1, z1=z1 };
        for (int x=x0;x<=x1;x++) for (int z=z0;z<=z1;z++) room[x,z]=id;
    }

    static bool Inside(int x,int z){ return x>=0 && x<W && z>=0 && z<L && room[x,z]!=0; }
    static int RoomAt(int x,int z){ return (x<0||x>=W||z<0||z>=L) ? 0 : room[x,z]; }
    static bool IsOpen(int a,int b){ return (a==1&&b==8)||(a==8&&b==1); } // corridor <-> lobby: no wall

    static bool WallBetween(int ax,int az,int bx,int bz)
    {
        int a=RoomAt(ax,az), b=RoomAt(bx,bz);
        if (a==b) return false;      // same region
        if (IsOpen(a,b)) return false;
        return true;                 // includes exterior (one side outside)
    }

    static void BuildFloor(Transform parent)
    {
        var fg = new GameObject("Floor"); fg.transform.parent=parent;
        var fp = LoadPiece("floor_tile_1");
        for (int x=0;x<W;x++) for (int z=0;z<L;z++)
        {
            if (!Inside(x,z)) continue;
            var go = Spawn(fp, fg.transform, new Vector3(x*tile,0,z*tile), 0, "floor_tile_1");
            if (go != null) go.transform.localScale = floorScale;
        }
    }

    static void BuildWalls(Transform parent)
    {
        var g = new GameObject("Walls"); g.transform.parent=parent;
        var wall = LoadPiece("tile_wall");
        var doorway = LoadPiece("tile_doorway_1") ?? wall;
        var doors = ChooseDoors();

        for (int x=0;x<W;x++) for (int z=0;z<L;z++)
        {
            if (!Inside(x,z)) continue;
            Edge(g.transform, wall, doorway, doors, x,z, x, z-1, 0);
            Edge(g.transform, wall, doorway, doors, x,z, x, z+1, 180);
            Edge(g.transform, wall, doorway, doors, x,z, x-1, z, 90);
            Edge(g.transform, wall, doorway, doors, x,z, x+1, z, 270);
        }
    }

    static void Edge(Transform parent, GameObject wall, GameObject doorway,
        HashSet<string> doors, int x,int z, int nx,int nz, float rot)
    {
        int myId = room[x,z];
        int nId = RoomAt(nx,nz);
        if (nId == myId) return;
        if (IsOpen(myId, nId)) return;           // open corridor->lobby passage
        if (nId != 0 && myId > nId) return;      // interior boundary: place only once
        int dir = (rot==0)?0 : (rot==180)?1 : (rot==90)?2 : 3;
        string key = $"{x},{z},{dir}";
        if (doors.Contains(key))
            Spawn(doorway, parent, new Vector3(x*tile,0,z*tile), rot, "tile_doorway_1");
        else
            Spawn(wall, parent, new Vector3(x*tile,0,z*tile), rot, "tile_wall");
    }

    // corner posts at every corner / T-junction / building corner (caps overshoot)
    static void Corners(Transform parent)
    {
        var pillar = LoadPiece("pillar");
        if (pillar == null) return;
        var g = new GameObject("Corners"); g.transform.parent=parent;
        for (int i=0;i<=W;i++) for (int j=0;j<=L;j++)
        {
            bool north = WallBetween(i-1,j,   i,j);
            bool south = WallBetween(i-1,j-1, i,j-1);
            bool east  = WallBetween(i,j,     i,j-1);
            bool west  = WallBetween(i-1,j,   i-1,j-1);
            int count = (north?1:0)+(south?1:0)+(east?1:0)+(west?1:0);
            if (count < 2) continue;
            bool straight = (count==2) && ((north&&south)||(east&&west));
            if (straight) continue;   // no post needed on a straight run
            Spawn(pillar, g.transform, new Vector3(i*tile - tile*0.5f, 0, j*tile - tile*0.5f), 0, "pillar");
        }
    }

    static HashSet<string> ChooseDoors()
    {
        var set = new HashSet<string>();
        foreach (var kv in rooms)
        {
            var r = kv.Value;
            if (r.type == 'C' || r.type == 'L') continue;
            var cands = new List<string>();
            for (int x=r.x0;x<=r.x1;x++) for (int z=r.z0;z<=r.z1;z++)
            {
                if (RoomAt(x-1,z)==1) cands.Add($"{x-1},{z},3");
                if (RoomAt(x+1,z)==1) cands.Add($"{x+1},{z},2");
                if (RoomAt(x,z-1)==1) cands.Add($"{x},{z-1},1");
                if (RoomAt(x,z+1)==1) cands.Add($"{x},{z+1},0");
            }
            if (cands.Count>0) set.Add(cands[cands.Count/2]); // centered door
        }
        set.Add($"7,{L-1},1");  // main entrance (3-wide, front of lobby)
        set.Add($"8,{L-1},1");
        set.Add($"9,{L-1},1");
        return set;
    }

    static void Furnish(Transform parent)
    {
        var g = new GameObject("Props"); g.transform.parent=parent;
        foreach (var kv in rooms)
        {
            var r = kv.Value;
            bool leftBlock = r.x1 <= 6;
            if (r.type=='w') Ward(g.transform, r, leftBlock);
            else if (r.type=='t') Treatment(g.transform, r, leftBlock);
            else if (r.type=='k') Consult(g.transform, r);
            else if (r.type=='C') Corridor(g.transform, r);
            else if (r.type=='L') Lobby(g.transform, r);
        }
    }

    static void Ward(Transform p, RoomInfo r, bool leftBlock)
    {
        var bed=LoadPiece("bed"); var iv=LoadPiece("IV_Bag_holder"); var cab=LoadPiece("cabinet_1");
        float outerX = leftBlock ? (r.x0+0.6f)*tile : (r.x1-0.6f)*tile;
        float rot = leftBlock ? 90 : 270;
        float ivOff = leftBlock ? 0.6f*tile : -0.6f*tile;
        for (int i=0;i<2;i++)
        {
            float z=(r.z0+1+i*2)*tile;
            if (bed!=null) Spawn(bed,p,new Vector3(outerX,0,z),rot,"bed");
            if (iv!=null)  Spawn(iv,p,new Vector3(outerX+ivOff,0,z+0.5f),0,"IV_Bag_holder");
        }
        if (cab!=null) Spawn(cab,p,new Vector3(outerX,0,(r.z1-0.5f)*tile),0,"cabinet_1");
    }

    static void Treatment(Transform p, RoomInfo r, bool leftBlock)
    {
        var bed=LoadPiece("bed"); var iv=LoadPiece("IV_Bag_holder"); var cab=LoadPiece("cabinet_1"); var wc=LoadPiece("wheel_chair");
        float cx=(r.x0+r.x1)*0.5f*tile, cz=(r.z0+r.z1)*0.5f*tile;
        if (bed!=null) Spawn(bed,p,new Vector3(cx,0,cz),0,"bed");
        float outerX = leftBlock ? (r.x0+0.4f)*tile : (r.x1-0.4f)*tile;
        for (int i=0;i<3;i++){ float z=(r.z0+1+i)*tile; if(iv!=null) Spawn(iv,p,new Vector3(outerX,0,z),0,"IV_Bag_holder"); }
        if (cab!=null) Spawn(cab,p,new Vector3(cx,0,(r.z0+0.6f)*tile),0,"cabinet_1");
        if (wc!=null) Spawn(wc,p,new Vector3(cx+tile,0,cz+tile),0,"wheel_chair");
    }

    static void Consult(Transform p, RoomInfo r)
    {
        var tbl=LoadPiece("table"); var ch=LoadPiece("chair"); var cab=LoadPiece("cabinet_1");
        float cx=(r.x0+r.x1)*0.5f*tile, cz=(r.z0+r.z1)*0.5f*tile;
        if (tbl!=null) Spawn(tbl,p,new Vector3(cx,0,cz),0,"table");
        if (ch!=null){ Spawn(ch,p,new Vector3(cx,0,cz-tile),0,"chair"); Spawn(ch,p,new Vector3(cx,0,cz+tile),180,"chair"); }
        if (cab!=null) Spawn(cab,p,new Vector3((r.x0+0.6f)*tile,0,(r.z0+0.6f)*tile),0,"cabinet_1");
    }

    static void Corridor(Transform p, RoomInfo r)
    {
        var exit=LoadPiece("Exit_sign");
        float mx=(r.x0+r.x1)*0.5f*tile;
        if (exit!=null) Spawn(exit,p,new Vector3(mx,wallH-0.6f,(r.z1)*tile),0,"Exit_sign");
    }

    static void Lobby(Transform p, RoomInfo r)
    {
        var chair=LoadPiece("chair"); var cab=LoadPiece("cabinet_1"); var bench=LoadPiece("bench");
        var bed=LoadPiece("bed"); var iv=LoadPiece("IV_Bag_holder"); var mag=LoadPiece("Magazine1"); var exit=LoadPiece("Exit_sign");
        float midX=(r.x0+r.x1)*0.5f*tile;
        if (cab!=null) Spawn(cab,p,new Vector3(midX,0,(r.z0+0.5f)*tile),0,"cabinet_1");
        for (int i=0;i<3;i++)
        {
            float z=(r.z0+2+i)*tile;
            if (chair!=null) Spawn(chair,p,new Vector3((r.x0+2)*tile,0,z),90,"chair");
            if (chair!=null) Spawn(chair,p,new Vector3((r.x0+3)*tile,0,z),270,"chair");
        }
        if (mag!=null) Spawn(mag,p,new Vector3((r.x0+2.5f)*tile,0,(r.z0+3)*tile),0,"Magazine1");
        for (int i=0;i<2;i++)
        {
            float z=(r.z0+2+i*2)*tile;
            if (bed!=null) Spawn(bed,p,new Vector3((r.x1-1)*tile,0,z),270,"bed");
            if (iv!=null)  Spawn(iv,p,new Vector3((r.x1-1.6f)*tile,0,z+0.5f),0,"IV_Bag_holder");
        }
        if (bench!=null) Spawn(bench,p,new Vector3(midX-2*tile,0,(r.z1-1)*tile),0,"bench");
        if (bench!=null) Spawn(bench,p,new Vector3(midX+2*tile,0,(r.z1-1)*tile),0,"bench");
        if (exit!=null) Spawn(exit,p,new Vector3(midX,wallH-0.6f,(r.z1)*tile),180,"Exit_sign");
    }

    static void Lighting(Transform parent)
    {
        var g=new GameObject("Lighting"); g.transform.parent=parent;
        foreach(var kv in rooms)
        {
            var r=kv.Value;
            for(int x=r.x0+1;x<=r.x1;x+=3) for(int z=r.z0+1;z<=r.z1;z+=3)
            {
                if(!Inside(x,z)) continue;
                var lgo=new GameObject("PL"); lgo.transform.parent=g.transform;
                lgo.transform.position=new Vector3(x*tile,wallH-0.5f,z*tile);
                var l=lgo.AddComponent<Light>();
                l.type=LightType.Point; l.range=tile*4.5f; l.intensity=1.15f;
                l.color=new Color(1f,0.96f,0.88f);
            }
        }
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(0.45f,0.46f,0.48f);
    }

    // ===== helpers =====
    static GameObject Spawn(GameObject prefab, Transform parent, Vector3 pos, float yRot, string piece)
    {
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.parent = parent; go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0,yRot,0);
        ApplyMat(go, piece); return go;
    }

    static GameObject LoadPiece(string name)
    {
        foreach (string gd in AssetDatabase.FindAssets(name+" t:Model", new[]{FbxFolder}))
        {
            string pth = AssetDatabase.GUIDToAssetPath(gd);
            if (Path.GetFileNameWithoutExtension(pth).ToLower()==name.ToLower())
                return AssetDatabase.LoadAssetAtPath<GameObject>(pth);
        }
        return null;
    }

    static Vector3 GetSize(GameObject go)
    {
        var t = (GameObject)PrefabUtility.InstantiatePrefab(go);
        Bounds b = new Bounds(); bool has=false;
        foreach (var rr in t.GetComponentsInChildren<Renderer>())
        { if(!has){b=rr.bounds;has=true;} else b.Encapsulate(rr.bounds); }
        Object.DestroyImmediate(t);
        return has ? b.size : Vector3.one;
    }

    static void ApplyMat(GameObject go, string piece)
    {
        if (!MatMap.TryGetValue(piece, out string matName)) return;
        var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{matName}.mat");
        if (mat == null) return;
        foreach (var rr in go.GetComponentsInChildren<Renderer>()) rr.sharedMaterial = mat;
    }
}