using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEngine;
using Draco;
using MixedReality.Toolkit.SpatialManipulation;


public class DracoMeshManager : MonoBehaviour
{
    static private List<DracoMeshManager> Instances;
    
    [SerializeField] Camera mainCamera = null;
    [SerializeField] private Boolean isStatic = false;
    [SerializeField] private Mesh placeholderMesh = null;
    [SerializeField] private string meshPath = null;
    
    //metadati della mesh
    private string Name { set; get; }
    private uint Size { set; get; }
    private float DecompressionTime { set; get; }
    private float DownloadTime { set; get; }
    
    //variabili per la gestione della mesh
    private MeshFilter meshFilter;
    private Renderer renderer;
    private bool isVisible = false;
    
    //variabili per il ridimensionamento dell'oggetto
    private Bounds normalizedBounds;
    private Bounds bounds;
    private Vector3 startPosition;
    private Vector3 normalizedScale = Vector3.one;

    private void Start()
    {
        DecompressionTime = 0;
        DownloadTime = 0;
        
        startPosition = transform.position;
        meshFilter = GetComponent<MeshFilter>();
        renderer = GetComponent<Renderer>();
        normalizedBounds = renderer.bounds;
        if (isStatic && placeholderMesh != null)
        {
            GetComponent<MeshFilter>().mesh = placeholderMesh;
        }
        
        if(Instances == null)
        {
            Instances = new List<DracoMeshManager>();
        }
        
        GetComponent<ObjectManipulator>().selectEntered.AddListener((t0) =>
        {
           SetInstance(this);
        });
        SetInstance(this);
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
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        //var fullPath = Path.Combine(Application.streamingAssetsPath, path);
        var fullPath = Path.Combine(Application.temporaryCachePath, path);

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
            stopwatch.Stop();
            DecompressionTime = stopwatch.ElapsedMilliseconds;
            PrintManager.UpdateMeshInfo(this);
        }
        else
        {
            Debug.LogError("Errore nella decompressione della mesh");
        }
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
    
    public static void SetInstance(DracoMeshManager instance)
    {
        Debug.Log("Setto l'istanza a " + instance.gameObject.name );
        
        if (Instances == null)
        {
            Instances = new List<DracoMeshManager>();
        }

        if (Instances.Contains(instance))
            Instances.Remove(instance);
        Instances.Add(instance);
        PrintManager.UpdateMeshInfo(instance);
    }
    
    
    public static List<DracoMeshManager> GetInstances()
    {
        return Instances;
    }

    public void SetDownloadTime(float time)
    {
        DownloadTime = time;
    }
    
    public float GetDownloadTime()
    {
        return DownloadTime;
    }
    
    public float GetDecompressionTime()
    {
        return DecompressionTime;
    }
    
}


