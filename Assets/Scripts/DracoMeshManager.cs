using System;
using System.IO;
using UnityEngine;
using Draco;


public class DracoMeshManager : MonoBehaviour
{
    [SerializeField] Camera mainCamera = null;
    [SerializeField] float padding = 0.01f;
    [SerializeField] private Boolean isStatic = false;
    [SerializeField] private Mesh placeholderMesh = null;
    [SerializeField] private string meshPath = null;
    private Vector3 startPosition;
    private Vector3 normalizedScale = Vector3.one;
    
    static public DracoMeshManager instance;
    
    private MeshFilter meshFilter;
    private Renderer renderer;
    private bool isVisible = false;
    
    private Bounds normalizedBounds;
    private Bounds bounds;

    private void Start()
    {
        startPosition = transform.position;
        
        meshFilter = GetComponent<MeshFilter>();
        renderer = GetComponent<Renderer>();
        normalizedBounds = renderer.bounds;
        if (isStatic && placeholderMesh != null)
        {
            GetComponent<MeshFilter>().mesh = placeholderMesh;
        }
        
        //resizeObject();
    }

    public void FixedUpdate()
    {
        if (isStatic)
        {
            if (meshPath != null && !isVisible)
            {
                if (IsVisibleFromCamera(mainCamera, renderer))
                {
                    Debug.Log($"Cambio la mesh con {meshPath}");
                    ChangeMesh(meshPath);
                    isVisible = true;
                }
            }
            else if (isVisible && !IsVisibleFromCamera(mainCamera, renderer))
            {
                meshFilter.mesh = placeholderMesh;
                isVisible = false;
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
            mesh.RecalculateBounds();
            bounds = mesh.bounds;
            mesh.RecalculateTangents();
            
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
    
    private void rotateObject()
    {
        var camera = Camera.main;
        var direction = camera.transform.position - transform.position;
        var rotation = Quaternion.LookRotation(direction);
        transform.rotation = rotation;
    }
    
    private void resizeObject()
    {
        
        // Ottieni il bounding box della mesh
        
        var scaleFactor = normalizedBounds.size.magnitude / (bounds.size.magnitude);
        transform.localScale = Vector3.one * scaleFactor;
        normalizedScale = transform.localScale;
        
    }
    
    public void ResetObject()
    {
        rotateObject();
        transform.position = startPosition;
        transform.localScale = normalizedScale;
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
                return true; // L'oggetto è visibile nella 
            }
        }
        return false; // L'oggetto non è visibile nella telecamera o non è valido
    }
    
    public void OnTouch()
    {
        Debug.Log("Touched");
        DracoMeshManager.instance = this;
    }

    //resize dell'oggetto per far in modo che sia visibile per intero nella telecamera
    
    
    
   
    
    
}


