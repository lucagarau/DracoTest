using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using TMPro;
using UnityEngine;
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
    [SerializeField] private DracoMeshManager dummy;
    [SerializeField] private TextMeshProUGUI descriptionText;
 
    [SerializeField] private bool localhost = false;
    //private bool _onlineMode = true;
    private long _lastDownloadTime;
    private List<FileData> fileDataArray;
    

    [Serializable]
    public class FileData
    {
        public string drc;
        public string mtl;
        public string texture;
        public string path;
        public string description;
    }

    // Metodo di inizializzazione
    void Start()
    {
        ipInputField.text = ip;
        
        if (dummy == null)
        {
            dummy = GameObject.Find("Dummy").GetComponent<DracoMeshManager>();
        }
        
        // Imposta l'URL del server
        meshURL = localhost ? "http://localhost:8080/" : "http://" + ip + ":8080/";
        Debug.Log("URL: " + meshURL);
        
        // Pulisce la cache locale
        _meshPath = Application.temporaryCachePath + "/";
        ClearCache();

        // Inizializza la lista delle mesh
        var meshListFile = "mesh_list.json";
       
        PrintManager.ShowMessage("Inizializzo la lista delle mesh dal server");
        StartCoroutine(Utilities.DownloadFile(meshListFile,meshURL, _meshPath, UpdateList));
        
    }

    // Metodo per pulire la cache locale
    void ClearCache()
    {
        DirectoryInfo di = new DirectoryInfo(_meshPath);
        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }

        foreach (var subfolder in di.GetDirectories())
        {
            subfolder.Delete(true);
        }
        
    }

    // Metodo per aggiornare la lista delle mesh
    void UpdateList(string fileServerList)
    {
        if (listView == null)
        {
            Debug.LogError("VirtualizedScrollRectList non trovato");
        }
        
        ReadMeshList();
        fileDataArray = Utilities.getFileDataArray();
        
        listView.SetItemCount(fileDataArray.Count);
        
        listView.OnVisible = (buttons, i) =>
        {
            foreach (var text in buttons.GetComponentsInChildren<TextMeshProUGUI>())
            {
                var meshName = fileDataArray[i].drc;
                if (text.gameObject.name == "Text")
                    text.text = $"{meshName}";
            }
            
            foreach (var button in buttons.GetComponentsInChildren<PressableButton>())
            {
                button.gameObject.name = "mesh " + i;

                var mesh = fileDataArray[i];
                button.OnClicked.AddListener(()=>{
                    StartCoroutine(Utilities.DownloadFile(mesh.path + "/"+mesh.drc, meshURL , _meshPath, ChangeMeshButton));
                   // StartCoroutine(Utilities.DownloadFile(mesh.mtl, meshURL + mesh.path + "/", _meshPath, ChangeMaterialButton, mesh.texture));
                    StartCoroutine(Utilities.DownloadFile(mesh.path + "/"+mesh.texture, meshURL, _meshPath, ChangeTextureButton));
                    if (mesh.description != null && descriptionText != null)
                        StartCoroutine(Utilities.DownloadFile(mesh.path + "/" + mesh.description, meshURL, _meshPath,
                            ChangeDescription));
                    else
                        descriptionText.text = "";
                });

            }

            
        };
        
        listView.OnInvisible = (buttons, i) =>
        {
            foreach (var button in buttons.GetComponentsInChildren<PressableButton>())
            {
                button.gameObject.name = "button";
                button.OnClicked.RemoveAllListeners();
            }
        };
        
        

       
    }

    // Metodo per leggere la lista delle mesh
    void ReadMeshList(string meshListFile = "mesh_list.json")
    {
        string filePath = _meshPath + meshListFile;
        if (File.Exists(filePath))
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                var json = reader.ReadToEnd();
                reader.Close();
                fileDataArray = JsonConvert.DeserializeObject<List<FileData>>(json);
                Utilities.setFileDataArray(fileDataArray);
            }
        }
        else
        {
            Debug.LogError("Errore durante la lettura del file mesh_list.json");
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
        //_onlineMode = mode;
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
        DracoMeshManager.GetInstances().Last().SetDownloadTime(_lastDownloadTime, mesh);
        dummy.ChangeMesh(_meshPath + mesh);
    }
    
    //metodo per il bottone che cambia il materiale
    public void ChangeMaterialButton(string material)
    {
        if (DracoMeshManager.GetInstances().Count == 0) NewMeshButton();
        DracoMeshManager.GetInstances().Last().ChangeMaterial(_meshPath + material);
        dummy.ChangeMaterial(_meshPath + material);
    }
    
    //metodo per il bottone che cambia la texture
    public void ChangeTextureButton(string texture)
    {
        if (DracoMeshManager.GetInstances().Count == 0) NewMeshButton();
        DracoMeshManager.GetInstances().Last().ChangeTexture(_meshPath + texture);
        DracoMeshManager.GetInstances().Last().SetDownloadTime(_lastDownloadTime, texture);
        dummy.ChangeTexture(_meshPath + texture);
    }

    public void ChangeDescription(string file)
    {
        //read txt file
        string filePath = _meshPath + file;
        if (File.Exists(filePath))
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                descriptionText.text = reader.ReadToEnd();
                reader.Close();
            }
        }
        else
        {
            Debug.LogError("Errore durante la lettura del file " + file);
        }
    }
}
