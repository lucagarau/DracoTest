using System;
using System.IO;
using UnityEngine;
using Draco;
using UnityEditor;

public class DracoMeshManager : MonoBehaviour
{
    [SerializeField] Camera mainCamera = null;
    [SerializeField] float padding = 0.01f;
    [SerializeField] private Boolean isStatic = false;
    [SerializeField] private Mesh placeholderMesh = null;
    [SerializeField] private string meshPath = null;
    private Vector3 startPosition;
    
    private MeshFilter meshFilter;
    private Renderer renderer;

    private void Start()
    {
        startPosition = transform.position;
        
        meshFilter = GetComponent<MeshFilter>();
        renderer = GetComponent<Renderer>();

        if (isStatic && placeholderMesh != null)
        {
            GetComponent<MeshFilter>().mesh = placeholderMesh;
        }
    }

    public void FixedUpdate()
    {
        if (isStatic)
        {
            if (meshPath != null && meshFilter.mesh == placeholderMesh)
            {
                if (IsVisibleFromCamera(mainCamera, renderer))
                    ChangeMesh(meshPath);
            }
            else if (meshFilter.mesh != placeholderMesh && !IsVisibleFromCamera(mainCamera, renderer))
            {
                meshFilter.mesh = placeholderMesh;
            }
        }
    }

    public async void ChangeMesh(String path)
    {
        //var fullPath = Path.Combine(Application.streamingAssetsPath, path);
        var fullPath = Path.Combine(Application.dataPath, path);

        var data = await File.ReadAllBytesAsync(fullPath);
        if (data == null) return;

        var draco = new DracoMeshLoader();
        var meshDataArray = Mesh.AllocateWritableMeshData(1); //allocazione della memoria per una sola mesh
        
        //decode
        var result = await draco.ConvertDracoMeshToUnity(
            meshDataArray[0],
            data,
            requireNormals:true);
        
        //decode avvenuto con successo
        if (result.success)
        {
            var mesh = new Mesh();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray,mesh);
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
            
            GetComponent<MeshCollider>().sharedMesh = mesh;
            GetComponent<MeshCollider>().convex = true;
            
            
            if (!isStatic)
            {
                //ridimensionamento dell'oggetto
                resizeObject();
                //rotazione dell'oggetto
                rotateObject();
            }
        }
        else
        {
            Debug.LogError("Errore nella decompressione della mesh");
        }
    }   
    
    public void InvokeMethod(string method, string path)
    {
        var methodInfo = GetType().GetMethod(method);
        if (methodInfo == null)
        {
            Debug.LogError("Method not found");
            return;
        }
        methodInfo.Invoke(this, new object[] {path});
    }
    
    //TODO Funzioni di Debug per fare il test di decompressione
    public void load1()
    {
        ChangeMesh("meshes/m1.drc");
    }
    public void load2()
    {
        ChangeMesh("meshes/m2.drc");
    }

    public void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 50, 50),"Mesh 1"))
            load1();
        if (GUI.Button(new Rect(200, 10, 50, 50),"Mesh 2"))
            load2();
    }

    private void rotateObject()
    {
        var camera = Camera.main;
        var direction = camera.transform.position - transform.position;
        var rotation = Quaternion.LookRotation(direction);
        transform.rotation = rotation;
    }
    
    private void resizeObject()
    {
        if(mainCamera == null)
            mainCamera = Camera.main;
        
        //calcolo della dimensione della mesh
        var bounds = GetComponent<MeshRenderer>().bounds;
        var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        var distance = (size / (2.0f * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad))) + padding;
        transform.localScale = Vector3.one * distance;
    }
    
    public void resizeObject(float distance)
    {
        transform.localScale = Vector3.one * distance;
    }
    
    public void ResetObject()
    {
        resizeObject();
        rotateObject();
        transform.position = startPosition;

    }
    
    bool IsVisibleFromCamera(Camera camera, Renderer renderer)
    {
        if (camera == null)
            return false;
        // Verifica se il renderer è valido e attivo
        if (renderer != null && renderer.isVisible)
        {
            // Ottieni i piani di frustum della telecamera
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);

            // Ottieni il bounding box dell'oggetto
            Bounds bounds = renderer.bounds;

            // Verifica se il bounding box interseca i piani di frustum della telecamera
            if (GeometryUtility.TestPlanesAABB(planes, bounds))
            {
                return true; // L'oggetto è visibile nella telecamera
            }
        }
        return false; // L'oggetto non è visibile nella telecamera o non è valido
    }
    
    
}


