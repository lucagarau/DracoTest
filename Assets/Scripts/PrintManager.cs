using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PrintManager : MonoBehaviour
{
    private static string DisplayText ;
    private static TextMeshProUGUI DebugText;
    private static TextMeshProUGUI DownloadText;
    private static TextMeshProUGUI DecompressionText;
    private static TextMeshProUGUI VtxText;
    private static TextMeshProUGUI FacesText;
    private static TextMeshProUGUI SizeText;

    private void Start()
    {
        DebugText = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();
        DownloadText = GameObject.Find("DownloadTimeText").GetComponent<TextMeshProUGUI>();
        DecompressionText = GameObject.Find("DecoTimeText").GetComponent<TextMeshProUGUI>();
        VtxText = GameObject.Find("VtxText").GetComponent<TextMeshProUGUI>();
        FacesText = GameObject.Find("FacesText").GetComponent<TextMeshProUGUI>();
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
        DownloadText.text = draco.GetDownloadTime().ToString() + " ms";
        DecompressionText.text =draco.GetDecompressionTime().ToString() + " ms";
        VtxText.text = draco.GetVtxCount().ToString();
        FacesText.text = draco.GetFacesCount().ToString();
        //SizeText.text = draco.GetSize().ToString() + " Kb";
        
    }
}
