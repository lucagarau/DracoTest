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

public class updatemeshlistserver : MonoBehaviour
{
    private string meshPath;
    
    [SerializeField] string meshURL = "http://192.168.229.42:8080/";
    [SerializeField] private VirtualizedScrollRectList listView;
    [SerializeField] private GameObject placeholder;
    [SerializeField] private Boolean localhost = false;
    
    [SerializeField] private TextMeshProUGUI DebugText;
    
    private string DisplayText ;

    
    [Serializable]
    public class FileData
    {
        public string name;
        public int size;
        public int LOD;
    }
    void Start()
    {
        if(localhost)
        {
            meshURL = "http://localhost:8080/";
        }
        meshPath = Application.temporaryCachePath + "/";
        DebugText = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();

        //TODO operazione di demo
        //clear meshPath
        // ShowMessage("Cancello i file nella cartella meshStreaming");
        // DirectoryInfo di = new DirectoryInfo(meshPath);
        // foreach (FileInfo file in di.GetFiles())
        // {
        //     file.Delete();
        // }
        // ShowMessage("Ho cancella i file nella cartella meshStreaming");
        
        ShowMessage("Inizializzo la lista delle mesh dal server");
        StartCoroutine(DownloadFile("mesh_list.json"));
    }

    void UpdateList()
    {
        if (listView == null)
        {
            Debug.LogError("VirtualizedScrollRectList not found");
            return;
        }

        List<string> tmp = ReadMeshList();
        string[] meshFiles = tmp.ToArray();
       
        listView.SetItemCount(meshFiles.Length);

        listView.OnVisible = (go, i) =>
        {
            foreach (var button in go.GetComponentsInChildren<PressableButton>())
            {
                button.gameObject.name = "mesh " + i;
                var meshfile = meshFiles[i];
                button.OnClicked.AddListener(() => StartCoroutine(DownloadFile(meshfile)));



            }

            foreach (var text in go.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (text.gameObject.name == "Text")
                    text.text = $"{meshFiles[i]}";
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
        ShowMessage($"Download in corso di {file} da {meshURL + file}");
        
        if (!File.Exists(meshPath + file))
        {
            ShowMessage("File non trovato in locale, scarico il file dal server");
            var url = meshURL + file;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                
                switch (request.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        Debug.LogError("Errore di connessione durante il download del file: " + request.error);
                        ShowMessage("Errore di connessione durante il download del file: " + request.error);
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("Errore durante il download del file: " + request.error);
                        ShowMessage("Errore durante il download del file: " + request.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("Errore durante il download del file: " + request.error);
                        ShowMessage("Errore durante il download del file: " + request.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        Debug.Log("Download completato");
                        ShowMessage("Download completato");
                        // Salva il file scaricato nella directory locale
                        string filePath = meshPath + file;
                        File.WriteAllBytes(filePath, request.downloadHandler.data);
                        Debug.Log(filePath);
                        ShowMessage("mesh scaricata correttamente: "+ filePath);
                        break;
                }
            }
            
          

            
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
        
        //place text
        Rect textArea = new Rect(10, 10, 200, 50);
        GUI.Label(textArea, DisplayText);
    }
    
    public void ShowMessage(string msg)
    {
        DisplayText = msg;
        DebugText.text = msg;
    }
}

