using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PrintManager : MonoBehaviour
{
    private static string logFile ;
    private static string DisplayText;
    private static TextMeshProUGUI DebugText;
    private static TextMeshProUGUI _meshDownloadTime, _textureDownloadTime;
    private static TextMeshProUGUI _decompressionMeshTime, _decompressionTextureTime;
    private static TextMeshProUGUI _vtxCount, _facesCount;
    private void Start()
    {
        DebugText = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();
        _meshDownloadTime = GameObject.Find("MeshDownloadTime").GetComponent<TextMeshProUGUI>();
        _textureDownloadTime = GameObject.Find("TextureDownloadTime").GetComponent<TextMeshProUGUI>();
        _decompressionMeshTime = GameObject.Find("MeshDecompressionTime").GetComponent<TextMeshProUGUI>();
        _decompressionTextureTime = GameObject.Find("TextureDecompressionTime").GetComponent<TextMeshProUGUI>();
        _vtxCount = GameObject.Find("MeshVerticesNumber").GetComponent<TextMeshProUGUI>();
        _facesCount = GameObject.Find("MeshPolyNumber").GetComponent<TextMeshProUGUI>();
        
        
        logFile = Application.dataPath + "/log.txt";
        if (!System.IO.File.Exists(logFile))
        {
            System.IO.File.Create(logFile);
        }
        
        Debug.Log("Log file created at: " + logFile);
    }

    public static void ShowMessage(string msg)
    {
        DisplayText = msg;
        DebugText.text = msg;
    }

    /*private void OnGUI()
    {
        //place text
        Rect textArea = new Rect(10, 10, 200, 50);
        GUI.Label(textArea, DisplayText);
    }*/

    public static void UpdateMeshInfo(DracoMeshManager draco)
    {
        if (draco == null) return;
        _vtxCount.text = draco.GetVtxCount().ToString();
        _facesCount.text = draco.GetFacesCount().ToString();
        
        var now = DateTime.Now;
        System.IO.File.AppendAllText(logFile,
            "[" + now.ToString("yyyy-MM-dd HH:mm:ss") + "] " +
            "Mesh:"+
            draco.gameObject.name +
            "\n\tDownload Time:" +
            draco.GetDownloadTime().ToString() +
            "\n\tDecompressione Time:" +
            draco.GetDecompressionTime()
                .ToString() +
            "\n\tVertices:" +
            draco.GetVtxCount()
                .ToString() +
            "\n\tFaces:" +
            draco.GetFacesCount()
                .ToString() +
            "\n");
    }

    public static void setDownloadTime(float time, string type)
    {
        switch (type)
        {   
            case "mesh":
                _meshDownloadTime.text = time + "ms";
                break;
            case "texture":
                _textureDownloadTime.text = time + "ms";
                break;
            default:
                Debug.LogError("Tipo non riconosciuto o non ancora implementato");
                break;
        }
    }
    
    public static void setDecompressionTime(float time, string type)
    {
        switch (type)
        {   
            case "mesh":
                _decompressionMeshTime.text = time + "ms";
                break;
            case "texture":
                _decompressionTextureTime.text = time + "ms";
                break;
            default:
                Debug.LogError("Tipo non riconosciuto o non ancora implementato");
                break;
        }
    }
    
    
}
