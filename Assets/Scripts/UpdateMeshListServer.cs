using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class UpdateMeshListServer : MonoBehaviour
{
    private string _meshPath;
    private string _offlineMeshPath = Application.streamingAssetsPath + "/meshes";

    [SerializeField] private string ip = "192.168.229.42";
    [SerializeField] private string meshURL;
    [SerializeField] private TextMeshProUGUI ipInputField;
    [SerializeField] private VirtualizedScrollRectList listView;
    [SerializeField] private GameObject placeholder;
    [SerializeField] private bool localhost = false;
    private bool _onlineMode = true;
    private long _lastDownloadTime;

    [Serializable]
    public class FileData
    {
        public string name;
        public int size;
        public int LOD;
        public bool has_texture;
    }

    // Metodo di inizializzazione
    void Start()
    {
        ipInputField.text = ip;
        
        // Imposta l'URL del server
        meshURL = localhost ? "http://localhost:8080/" : "http://" + ip + ":8080/";
        Debug.Log("URL: " + meshURL);
        
        // Pulisce la cache locale
        _meshPath = Application.temporaryCachePath + "/";
        ClearCache();

        // Inizializza la lista delle mesh
        var meshListFile = "mesh_list.json";
        if (_onlineMode)
        {
            PrintManager.ShowMessage("Inizializzo la lista delle mesh dal server");
            StartCoroutine(DownloadFile(meshListFile, UpdateList));
        }
        else
        {
            UpdateList(meshListFile);
            PrintManager.ShowMessage("Inizializzo la lista delle mesh in locale");
        }
    }

    // Metodo per pulire la cache locale
    void ClearCache()
    {
        DirectoryInfo di = new DirectoryInfo(_meshPath);
        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }
    }

    // Metodo per aggiornare la lista delle mesh
    void UpdateList(string meshList)
    {
        if (listView == null)
        {
            Debug.LogError("VirtualizedScrollRectList non trovato");
            return;
        }

        string[] meshFiles;
        if (_onlineMode)
        {
            List<string> tmp = ReadMeshList();
            meshFiles = tmp.ToArray();
        }
        else
        {
            meshFiles = Directory.GetFiles(_offlineMeshPath, "*.drc");
        }

        listView.SetItemCount(meshFiles.Length);

        listView.OnVisible = (go, i) =>
        {
            foreach (var button in go.GetComponentsInChildren<PressableButton>())
            {
                button.gameObject.name = "mesh " + i;
                var meshfile = meshFiles[i];
                if (_onlineMode)
                    button.OnClicked.AddListener(() => StartCoroutine(DownloadFile(meshfile,ChangeMeshButton)));
                else
                {
                    button.OnClicked.AddListener(() =>
                    {
                        if (DracoMeshManager.GetInstances().Count == 0) NewMeshButton();
                        DracoMeshManager.GetInstances().Last().ChangeMesh(meshFiles[i]);
                    });
                }
            }

            foreach (var text in go.GetComponentsInChildren<TextMeshProUGUI>())
            {
                var meshName = _onlineMode ? meshFiles[i] : Path.GetFileName(meshFiles[i]);
                if (text.gameObject.name == "Text")
                    text.text = $"{meshName}";
            }
        };

        listView.OnInvisible = (go, i) =>
        {
            foreach (var button in go.GetComponentsInChildren<PressableButton>())
            {
                button.gameObject.name = "button";
                button.OnClicked.RemoveAllListeners();
            }
        };
    }

    // Metodo per scaricare un file dal server
    private IEnumerator DownloadFile(string file, Action<string> callback)
    {
        PrintManager.ShowMessage($"Download in corso di {file} da {meshURL + file}");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        stopwatch.Start();

        if (!File.Exists(_meshPath + file))
        {
            PrintManager.ShowMessage("File non trovato in locale, scarico il file dal server");
            var url = meshURL + file;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    HandleDownloadError(request);
                    yield break;
                }

                SaveDownloadedFile(file, request.downloadHandler.data);
            }
        }
        stopwatch.Stop();
        _lastDownloadTime = stopwatch.ElapsedMilliseconds;

        // Esegui il callback per ulteriori azioni
        callback?.Invoke(file);
    }

    // Metodo per gestire errori di download
    private void HandleDownloadError(UnityWebRequest request)
    {
        Debug.LogError("Errore durante il download del file: " + request.error);
        PrintManager.ShowMessage("Errore durante il download del file: " + request.error);
    }

    // Metodo per salvare il file scaricato
    private void SaveDownloadedFile(string file, byte[] data)
    {
        string filePath = _meshPath + file;
        File.WriteAllBytes(filePath, data);
        Debug.Log(filePath);
        PrintManager.ShowMessage("Mesh scaricata correttamente: " + filePath);
    }

    // Metodo per leggere la lista delle mesh
    List<string> ReadMeshList(string meshListFile = "mesh_list.json")
    {
        string filePath = _meshPath + meshListFile;
        if (File.Exists(filePath))
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                var json = reader.ReadToEnd();
                reader.Close();
                var fileDataArray = JsonConvert.DeserializeObject<List<FileData>>(json);
                var meshList = fileDataArray.Select(data => data.name).ToList();
                return new List<string>(meshList);
            }
        }
        else
        {
            Debug.LogError("Errore durante la lettura del file mesh_list.json");
            return null;
        }
    }

    // Metodo per resettare il bottone della mesh
    public void ResetMeshButton()
    {
        if (DracoMeshManager.GetInstances().Count == 0) return;
        DracoMeshManager.GetInstances().Last().ResetObject();
    }

    // Metodo per eliminare il bottone della mesh
    public void DeleteMeshButton()
    {
        if (DracoMeshManager.GetInstances().Count == 0) return;
        var draco = DracoMeshManager.GetInstances().Last();
        Destroy(DracoMeshManager.GetInstances().Last().gameObject);
        DracoMeshManager.GetInstances().Remove(draco);
    }

    // Metodo per creare un nuovo bottone per la mesh
    public void NewMeshButton()
    {
        if (placeholder == null) return;
        var go = Instantiate(placeholder, Vector3.zero, Quaternion.identity);
        var draco = go.GetComponent<DracoMeshManager>();
        if (draco != null)
        {
            DracoMeshManager.SetInstance(draco);
        }
        else
        {
            Debug.LogError("DracoMeshManager non trovato nel nuovo oggetto");
        }
    }

    // Metodo per impostare la modalità online
    public void SetOnlineMode(bool mode)
    {
        _onlineMode = mode;
        Start();
    }

    // Metodo per impostare l'IP
    public void SetIp()
    {
        ip = ipInputField.text;
        Start();
    }
    
    //metodo per il bottone che cambia la mesh
    public void ChangeMeshButton(string mesh)
    {
        if (DracoMeshManager.GetInstances().Count == 0) NewMeshButton();
        DracoMeshManager.GetInstances().Last().ChangeMesh(_meshPath + mesh);
        DracoMeshManager.GetInstances().Last().SetDownloadTime(_lastDownloadTime);
    }
}
