using System.Collections;
using System.Collections.Generic;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class updatemeshlist : MonoBehaviour
{
    private string meshPath = Application.dataPath+"/meshes";
    public DracoMeshManager draco;
    [SerializeField] private VirtualizedScrollRectList listView;
    void Start()
    {
        UpdateList();
    }

    void UpdateList()
    {
       if (listView == null)
       {
           Debug.LogError("VirtualizedScrollRectList not found");
           return;
       }
       
       string[] meshFiles = System.IO.Directory.GetFiles(meshPath, "*.drc");
       listView.SetItemCount(meshFiles.Length);
       
       listView.OnVisible = (go, i) =>
       {
           foreach (var button in go.GetComponentsInChildren<PressableButton>())
           {
               button.gameObject.name = "mesh " + i;
               button.OnClicked.AddListener(() => draco.ChangeMesh(meshFiles[i]));

               
               
           }
           foreach (var text in go.GetComponentsInChildren<TextMeshProUGUI>())
           {
               var meshName = meshFiles[i].Substring(meshFiles[i].LastIndexOf('/') + 1);
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
    
    
}
