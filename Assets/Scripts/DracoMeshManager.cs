
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Draco;
using MixedReality.Toolkit.SpatialManipulation;

public class DracoMeshManager : MonoBehaviour
{
    private static List<DracoMeshManager> Instances;

    [SerializeField] private Camera mainCamera = null;
    [SerializeField] private bool isStatic = false;
    [SerializeField] private Mesh placeholderMesh = null;
    [SerializeField] private string meshPath = null;

    // Metadati della mesh
    private string Name { set; get; }
    private uint Size { set; get; }
    private float DecompressionTime { set; get; }
    private float DownloadTimeMesh;
    private float DownloadTimeTexture;
    private int VtxCount { set; get; }
    private int FacesCount { set; get; }

    // Variabili per la gestione della mesh
    private MeshFilter meshFilter;
    private Renderer renderer;
    private bool isVisible = false;

    // Variabili per il ridimensionamento dell'oggetto
    private Bounds normalizedBounds;
    private Bounds bounds;
    private Vector3 startPosition;
    private Vector3 normalizedScale = Vector3.one;

    private void Start()
    {
        DecompressionTime = 0;
        DownloadTimeMesh = 0;
        DownloadTimeTexture = 0;

        startPosition = transform.position;
        meshFilter = GetComponent<MeshFilter>();
        renderer = GetComponent<Renderer>();

        VtxCount = meshFilter.mesh.vertexCount;
        FacesCount = meshFilter.mesh.triangles.Length;

        normalizedBounds = renderer.bounds;
        if (isStatic && placeholderMesh != null)
        {
            meshFilter.mesh = placeholderMesh;
        }

        if (Instances == null)
        {
            Instances = new List<DracoMeshManager>();
        }

        GetComponent<ObjectManipulator>().selectEntered.AddListener((t0) =>
        {
            SetInstance(this);
        });
        SetInstance(this);
        // resizeObject();
    }

    private void FixedUpdate()
    {
        if (isStatic)
        {
            if (meshPath != null && !isVisible)
            {
                if (IsVisibleFromCamera(mainCamera, renderer))
                {
                    Debug.Log($"Cambio la mesh con {meshPath}");
                    StartCoroutine(ChangeMeshCoroutine(meshPath));
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

    private IEnumerator ChangeMeshCoroutine(string path)
    {
        Debug.Log("inzio decompressione di " + path);
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        var fullPath = Path.Combine(Application.temporaryCachePath, path);

        Task<byte[]> dataTask = ReadFileAsync(fullPath);
        yield return new WaitUntil(() => dataTask.IsCompleted);

        var data = dataTask.Result;
        if (data == null) yield break;

        var draco = new DracoMeshLoader();
        var meshDataArray = Mesh.AllocateWritableMeshData(1); // Allocazione della memoria per una sola mesh

        // Decode
        Task<DracoMeshLoader.DecodeResult> resultTask = draco.ConvertDracoMeshToUnity(
            meshDataArray[0],
            data,
            requireNormals: true);
        yield return new WaitUntil(() => resultTask.IsCompleted);
        

        var result = resultTask.Result;

        // Decode avvenuto con successo
        if (result.success)
        {
            var mesh = new Mesh();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            bounds = mesh.bounds;
            mesh.RecalculateTangents();

            meshFilter.mesh = mesh;

            /*
            var meshCollider = GetComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;*/
            
            var boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null)
                this.gameObject.AddComponent<BoxCollider>();
            boxCollider.size = bounds.size;

            if (!isStatic)
            {
                // Ridimensionamento dell'oggetto
                resizeObject();
                // Rotazione dell'oggetto
                rotateObject();
            }
            stopwatch.Stop();
            DecompressionTime = stopwatch.ElapsedMilliseconds;
            PrintManager.setDecompressionTime(DecompressionTime,"mesh");
            VtxCount = mesh.vertexCount;
            FacesCount = mesh.triangles.Length;
            
            //todo prova per uv
            RotateUV();

            PrintManager.UpdateMeshInfo(this);
            Debug.Log("Decompressione completata con successo");
        }
        else
        {
            Debug.LogError("Errore nella decompressione della mesh");
        }
    }

    private async Task<byte[]> ReadFileAsync(string path)
    {
        return await Task.Run(() => File.ReadAllBytes(path));
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

    private bool IsVisibleFromCamera(Camera camera, Renderer renderer)
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

    public static void SetInstance(DracoMeshManager instance)
    {
        Debug.Log("Setto l'istanza a " + instance.gameObject.name);

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

    public void SetDownloadTime(float time, string type)
    {
        switch (type)
        {
            case "mesh":
                DownloadTimeMesh = time;
                break;
            case "texture":
                DownloadTimeTexture = time;
                break;
        }
    }

    public float GetDownloadTime()
    {
        return DownloadTimeMesh + DownloadTimeTexture;
    }

    public float GetDecompressionTime()
    {
        return DecompressionTime;
    }

    public int GetVtxCount()
    {
        return VtxCount;
    }

    public int GetFacesCount()
    {
        return FacesCount;
    }

    public uint GetSize()
    {
        return Size;
    }
    
    public void ChangeMesh(string path)
    {
        Debug.Log("Cambio la mesh con " + path);
        StartCoroutine(ChangeMeshCoroutine(path));
    }
    public void ChangeTexture(string path)
    {
        StartCoroutine(ChangeTextureCoroutine(path));
    }
    
    private IEnumerator ChangeTextureCoroutine(string path)
    {
        var stopWatch = new System.Diagnostics.Stopwatch();
        stopWatch.Start();
        //todo prova per heic
        /*if (path.EndsWith(".heic"))
        {
            var newPath = path.Replace(".heic", ".png");
            Utilities.ConvertHeicToPng(path, newPath);
            path = newPath;
        }*/
        Debug.Log("Cambio la texture con " + path);
        var fullPath = Path.Combine(Application.temporaryCachePath, path);

        // Lettura asincrona del file per evitare blocchi nel thread principale
        byte[] data = null;
        yield return ReadFileAsync(fullPath, result => data = result);

        if (data == null)
        {
            Debug.LogError("Errore nel caricamento della texture dal path: " + fullPath);
            yield break;
        }

        // Caricamento della texture
        var texture = new Texture2D(1, 1);
        texture.LoadImage(data);
        texture.Apply();
        renderer.material.mainTexture = texture;
        stopWatch.Stop();
        Debug.Log("Decompressione completata con successo in " + stopWatch.ElapsedMilliseconds + "ms");
        PrintManager.setDecompressionTime(stopWatch.ElapsedMilliseconds,"texture");

        yield return null;
    }

    private IEnumerator ReadFileAsync(string path, Action<byte[]> callback)
    {
        byte[] data = null;
        bool isDone = false;

        // Legge il file in un task separato per evitare il blocco del thread principale
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                data = File.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                Debug.LogError("Errore durante la lettura del file: " + e.Message);
            }
            finally
            {
                isDone = true;
            }
        });

        // Aspetta che il task termini
        while (!isDone)
        {
            yield return null;
        }

        // Esegue il callback con i dati letti
        callback(data);
    }
    
    public void ChangeMaterial(string path)
    {
        Debug.Log("Cambio il materiale con " + path);
        StartCoroutine(ChangeMaterialCoroutine(path));
    }
    
    private IEnumerator ChangeMaterialCoroutine(string path)
    {
        Debug.Log("Cambio il materiale con " + path);
        var fullPath = Path.Combine(Application.temporaryCachePath, path);
        var material = Utilities.LoadMTL(fullPath);
        renderer.material = material;
        yield return null;
    }

    private void RotateUV()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        Vector2[] uvs = mesh.uv;
        
        Debug.Log(uvs.Length);

        for (int i = 0; i < uvs.Length; i++)
        {
            //uvs[i].x = 1.0f - uvs[i].x;
            uvs[i].y = -1.0f * uvs[i].y;
        }

        mesh.uv = uvs;
    }
}
