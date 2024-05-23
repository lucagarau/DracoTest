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
//using File = UnityEngine.Windows.File;
using File = System.IO.File;
using Newtonsoft.Json;
using UnityEngine.Android;
using UnityEngine.UI;

public class updatemeshlistserver : MonoBehaviour
{
    private string meshPath;
    private string _OfflineMeshPath = Application.streamingAssetsPath + "/meshes";

    [SerializeField] private string ip = "192.168.229.42";
    [SerializeField] private string meshURL;
    [SerializeField] private TextMeshProUGUI ipInputField;
    
    [SerializeField] private VirtualizedScrollRectList listView;
    [SerializeField] private GameObject placeholder;
    [SerializeField] private Boolean localhost = false;
    private Boolean _onlineMode = true;
    
    [Serializable]
    public class FileData
    {
        public string name;
        public int size;
        public int LOD;
    }
    void Start()
    {
        ipInputField.text = ip;
        
        if(localhost)
        {
            meshURL = "http://localhost:8080/";
        }
        else
        {
            meshURL = "http://" + ip + ":8080/";
        }
        Debug.Log("URL: " + meshURL);
        meshPath = Application.temporaryCachePath + "/";

        //TODO operazione di demo
        DirectoryInfo di = new DirectoryInfo(meshPath);
        foreach (FileInfo file in di.GetFiles())
        {
             file.Delete();
        }
        
        if (_onlineMode)
        {
            PrintManager.ShowMessage("Inizializzo la lista delle mesh dal server");
            StartCoroutine(DownloadFile("mesh_list.json"));   
        }

        else
        {
            UpdateList();
            PrintManager.ShowMessage("Inizializzo la lista delle mesh in locale");
        }
    }

    void UpdateList()
    {
        if (listView == null)
        {
            Debug.LogError("VirtualizedScrollRectList not found");
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
           meshFiles = Directory.GetFiles(_OfflineMeshPath, "*.drc");
        }

        listView.SetItemCount(meshFiles.Length);

        listView.OnVisible = (go, i) =>
        {
            foreach (var button in go.GetComponentsInChildren<PressableButton>())
            {
                button.gameObject.name = "mesh " + i;
                var meshfile = meshFiles[i];
                if(_onlineMode) 
                    button.OnClicked.AddListener(() => StartCoroutine(DownloadFile(meshfile)));
                else
                {
                    button.OnClicked.AddListener(() =>
                    {
                        if (DracoMeshManager.GetInstances().Count == 0) newMeshButton();
                        DracoMeshManager.GetInstances().Last().ChangeMesh(meshFiles[i]);
                    });
                }



            }

            foreach (var text in go.GetComponentsInChildren<TextMeshProUGUI>())
            {
                var meshName = _onlineMode ? meshFiles[i] : meshFiles[i].Substring(meshFiles[i].LastIndexOf('/') + 1);
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

    private IEnumerator DownloadFile(string file)
    {
        var stopWatch = new System.Diagnostics.Stopwatch();
        
        PrintManager.ShowMessage($"Download in corso di {file} da {meshURL + file}");
        
        if (!File.Exists(meshPath + file))
        {
            stopWatch.Start();
            PrintManager.ShowMessage("File non trovato in locale, scarico il file dal server");
            var url = meshURL + file;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                
                switch (request.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        Debug.LogError("Errore di connessione durante il download del file: " + request.error);
                        PrintManager.ShowMessage("Errore di connessione durante il download del file: " + request.error);
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("Errore durante il download del file: " + request.error);
                        PrintManager.ShowMessage("Errore durante il download del file: " + request.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("Errore durante il download del file: " + request.error);
                        PrintManager.ShowMessage("Errore durante il download del file: " + request.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        Debug.Log("Download completato");
                        PrintManager.ShowMessage("Download completato");
                        // Salva il file scaricato nella directory locale
                        string filePath = meshPath + file;
                        File.WriteAllBytes(filePath, request.downloadHandler.data);
                        Debug.Log(filePath);
                        PrintManager.ShowMessage("mesh scaricata correttamente: "+ filePath);
                        break;
                }
            }
            stopWatch.Stop();
        }

        if (file == "mesh_list.json")
        {
            if (!File.Exists(meshPath + "mesh_list.json"))
            {
                Debug.LogError("Errore durante il download del file mesh_list.json, la connessione con il server potrebbe non essere riuscita");
                yield return null;
            }

            UpdateList();
        }
        else
        {
            Debug.Log("Cambio della mesh: " + file);
            if (DracoMeshManager.GetInstances().Count == 0) newMeshButton();
            DracoMeshManager.GetInstances().Last().ChangeMesh(meshPath + file);
            DracoMeshManager.GetInstances().Last().SetDownloadTime(stopWatch.ElapsedMilliseconds);
        }


    }
    
    List<string> ReadMeshList()
    {
        string filePath = meshPath + "mesh_list.json";
        if (File.Exists(filePath))
        {
            using(StreamReader reader = new StreamReader(filePath))
            {
                var json = reader.ReadToEnd();
                reader.Close();
                var fileDataArray = JsonConvert.DeserializeObject<List<FileData>>(json);
                                
                var meshList = new List<string>();
                foreach (var data in fileDataArray)
                {
                    meshList.Add(data.name);
                }
                return new List<string>(meshList);
            }
        }
        else
        {
            Debug.LogError("Errore durante la lettura del file mesh_list.json");
            return null;
        }
    }

    public void ResetMeshButton()
    {
        if (DracoMeshManager.GetInstances().Count == 0) return;
        DracoMeshManager.GetInstances().Last().ResetObject();
    }
    
    public void DeleteMeshButton()
    {
        if (DracoMeshManager.GetInstances().Count == 0) return;
        var draco = DracoMeshManager.GetInstances().Last();
       Destroy(DracoMeshManager.GetInstances().Last().gameObject);
       DracoMeshManager.GetInstances().Remove(draco);
    }
    
    public void newMeshButton()
    {
        if(placeholder == null) return;
        var go = Instantiate(placeholder, new Vector3(0, 0, 0), Quaternion.identity);
        var draco = go.GetComponent<DracoMeshManager>();
        if (draco!= null)
        {
            DracoMeshManager.SetInstance(draco);
        }
        else
        {
            Debug.LogError("DracoMeshManager non trovato nel nuovo oggetto");
        }
    }
    
    public void SetOnlineMode(bool mode)
    {
        _onlineMode = mode;
        Start();
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 100, 50, 50), "Reset Mesh"))
        {
            ResetMeshButton();
        }
        if (GUI.Button(new Rect(10, 150, 50, 50), "Delete Mesh"))
        {
            DeleteMeshButton();
        }
        if (GUI.Button(new Rect(10, 200, 50, 50), "New Mesh"))
        {
            newMeshButton();
        }
        
    }
    
    public void SetIp()
    {
        ip = ipInputField.text;
        Start();
    }
    
}

