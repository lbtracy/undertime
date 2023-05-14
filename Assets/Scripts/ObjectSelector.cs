using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ObjectSelector : MonoBehaviour
{
    [Serializable]
    private class ListVector3
    {
        public List<Vector3> data;
    }

    [Header("相机"), Tooltip("相机")] public Camera targetCamera;

    [Tooltip("如果选中，就会使用鼠标的位置来选中物体，而不是相机中心点")]
    public bool useMouse;

    [Tooltip("可以被选中的物品名称前缀")] public List<string> selectableNamePrefix;
    [Tooltip("可以被选中的物品名称")] public List<string> selectableName;
    [Tooltip("物体被选中或取消之后触发的事件")] public UnityEvent<GameObject, bool> onSelectedOrUnSelected;

    [Header("选中时显示的边框"), Tooltip("将物体填充成某个颜色的材质")]
    public Material outlineFillMaterial; // 需要搭配使用

    [Tooltip("边框宽度")] public float outlineWidth = 6f;
    [Tooltip("将物体中填充的颜色去掉的材质")] public Material outlineMaskMaterial; // 需要搭配使用

    private GameObject _selectedObject;
    private Renderer _lastHitRenderer;

    [SerializeField, HideInInspector] private List<Mesh> bakeKeys = new();
    [SerializeField, HideInInspector] private List<ListVector3> bakeValues = new();
    private static readonly HashSet<Mesh> RegisteredMeshes = new();

    private static readonly int ZTest = Shader.PropertyToID("_ZTest");
    private static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");

    private void Awake()
    {
        outlineMaskMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat(OutlineWidth, outlineWidth);
    }

    void LoadSmoothNormal(MeshFilter meshFilter, Renderer r, SkinnedMeshRenderer skinnedMeshRenderer)
    {
        // Skip if smooth normals have already been adopted
        if (!RegisteredMeshes.Add(meshFilter.sharedMesh))
        {
            return;
        }

        Debug.Log("Loading smooth normal");

        // Retrieve or generate smooth normals
        var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
        var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);

        var meshFilterSharedMesh = meshFilter.sharedMesh;
        // Store smooth normals in UV3
        meshFilterSharedMesh.SetUVs(3, smoothNormals);

        // Combine child meshes
        CombineChildMeshes(meshFilterSharedMesh, r.sharedMaterials);

        // Clear UV3 on skinned mesh renderers
        if (System.Object.Equals(skinnedMeshRenderer, null)) return;
        // Skip if UV3 has already been reset
        if (!RegisteredMeshes.Add(skinnedMeshRenderer.sharedMesh))
        {
            return;
        }

        // Clear UV3
        var sharedMesh = skinnedMeshRenderer.sharedMesh;
        sharedMesh.uv4 = new Vector2[sharedMesh.vertexCount];

        // Combine child meshes
        CombineChildMeshes(sharedMesh, skinnedMeshRenderer.sharedMaterials);

        Debug.Log("Loaded smooth normal");
    }

    void CombineChildMeshes(Mesh mesh, Material[] materials)
    {
        // Skip meshes with a single child meshes
        if (mesh.subMeshCount == 1)
        {
            return;
        }

        // Skip if child meshes count exceeds material count
        if (mesh.subMeshCount > materials.Length)
        {
            return;
        }

        // Append combined child meshes
        mesh.subMeshCount++;
        mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }

    List<Vector3> SmoothNormals(Mesh mesh)
    {
        // Group vertices by location
        var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index))
            .GroupBy(pair => pair.Key);

        // Copy normals to a new list
        var smoothNormals = new List<Vector3>(mesh.normals);

        // Average normals for grouped vertices
        foreach (var group in groups)
        {
            // Skip single vertices
            if (group.Count() == 1)
            {
                continue;
            }

            // Calculate the average normal
            var smoothNormal = group.Aggregate(Vector3.zero, (current, pair) => current + smoothNormals[pair.Value]);

            smoothNormal.Normalize();

            // Assign smooth normal to each vertex
            foreach (var pair in group)
            {
                smoothNormals[pair.Value] = smoothNormal;
            }
        }

        return smoothNormals;
    }

    // Update is called once per frame
    void Update()
    {
        IsLookAtSpecifiedObject();
    }

    // 获取被光线击中的物体
    private GameObject GetSelectedObject()
    {
        if (useMouse)
        {
            var currentMousePosition = Mouse.current.position.ReadValue();
            var ray = targetCamera.ScreenPointToRay(new Vector3(currentMousePosition.x, currentMousePosition.y,
                targetCamera.nearClipPlane));
            return !Physics.Raycast(ray, out var hitInfoByMouse)
                ? null
                : hitInfoByMouse.transform.gameObject;
        }

        // 获取主相机看到的中心点
        var screenPoint = new Vector3(Screen.height / 2.0f, Screen.width / 2.0f, targetCamera.nearClipPlane);
        var target = targetCamera.ScreenToWorldPoint(screenPoint);
        return !Physics.Raycast(target, targetCamera.transform.forward, out var hitInfo, 10f)
            ? null
            : hitInfo.transform.gameObject;
    }

    // check if look at specified object
    private void IsLookAtSpecifiedObject()
    {
        _selectedObject = GetSelectedObject();
        // 没有任何物体被选中，返回
        if (System.Object.Equals(_selectedObject, null)) return;
        if (!_selectedObject.TryGetComponent<Renderer>(out var r)) return;
        // 选中的物体与上次选中的相同，返回
        if (_lastHitRenderer == r) return;
        // 上次选中的物体不为空，去掉边框，并判断是否需要触发事件
        if (!System.Object.Equals(_lastHitRenderer, null) && _lastHitRenderer)
        {
            OnUnselectObject(_lastHitRenderer.gameObject, _lastHitRenderer);
        }

        // 判断是否需要给当前选中的物体加上边框，并且触发事件
        OnSelectObject(_selectedObject, r);
    }

    private void AddOrRemoveOutline(Renderer r, bool add)
    {
        var ms = r.sharedMaterials.ToList();
        if (add)
        {
            ms.Add(outlineMaskMaterial);
            ms.Add(outlineFillMaterial);
        }
        else
        {
            ms.Remove(outlineMaskMaterial);
            ms.Remove(outlineFillMaterial);
        }

        r.materials = ms.ToArray();
    }

    private void OnUnselectObject(GameObject obj, Renderer r)
    {
        // 移除之前的物体的边框
        AddOrRemoveOutline(r, false);
        _lastHitRenderer = null;
        // 判断是否应该触发取消选中事件
        var objName = obj.name;
        if (selectableName.All(s => objName != s) &&
            selectableNamePrefix.All(s => !objName.StartsWith(s))) return;
        Debug.Log($"Unselected: {obj.name}");
        onSelectedOrUnSelected?.Invoke(obj, false);
    }

    private void OnSelectObject(GameObject obj, Renderer r)
    {
        var objName = obj.name;
        if (selectableName.All(s => objName != s) &&
            selectableNamePrefix.All(s => !objName.StartsWith(s))) return;
        Debug.Log($"Selected: {obj.name}");
        // 使边框变得平滑，尚未弄明白原理，暂时抄过来
        if (obj.TryGetComponent<MeshFilter>(out var meshFilter))
        {
            obj.TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer);
            LoadSmoothNormal(meshFilter, r, skinnedMeshRenderer);
        }

        AddOrRemoveOutline(r, true);

        onSelectedOrUnSelected?.Invoke(obj, true);
        _lastHitRenderer = r;
    }

    private void OnDisable()
    {
        if (System.Object.Equals(_lastHitRenderer, null)) return;
        OnUnselectObject(_lastHitRenderer.gameObject, _lastHitRenderer);
    }
}