using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using File = UnityEngine.Windows.File;
using Newtonsoft.Json;

public class updatemeshlistserver : MonoBehaviour
{
    private string meshPath = Application.dataPath + "/meshStreaming/";
    
    [SerializeField] string meshURL = "http://localhost:8080/";
    public DracoMeshManager draco;
    [SerializeField] private VirtualizedScrollRectList listView;
    [SerializeField] private GameObject placeholder;

    
    [Serializable]
    public class FileData
    {
        public string name;
        public int size;
        public int LOD;
    }
    void Start()
    {
        //TODO operazione di demo
        //clear meshPath
        DirectoryInfo di = new DirectoryInfo(meshPath);
        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }
        
        if (draco == null)
        {
            draco = DracoMeshManager.instance;
            return;
        }
        
        
        
        
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
        if (!File.Exists(meshPath + file))
        {

            var url = meshURL + file;
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            // Controlla se c'è stato un errore durante il download
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Errore durante il download del file: " + request.error);
                yield return null;
            }

            // Salva il file scaricato nella directory locale
            string filePath = meshPath + file;
            System.IO.File.WriteAllBytes(filePath, request.downloadHandler.data);
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
            draco.ChangeMesh(meshPath + file);
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
        if (draco == null) return;
        draco.ResetObject();
    }
    
    public void DeleteMeshButton()
    {
        if (draco == null) return;
        var go = draco.gameObject;
        Destroy(go);
    }
    
    public void newMeshButton()
    {
        if(placeholder == null) return;
        var go = Instantiate(placeholder, new Vector3(0, 0, 0), Quaternion.identity);
        draco = go.GetComponent<DracoMeshManager>();
        if (draco!= null)
        {
            DracoMeshManager.instance = draco;
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
    }
}

