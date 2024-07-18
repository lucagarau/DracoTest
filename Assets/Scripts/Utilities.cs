using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class Utilities : MonoBehaviour
{
    private static float _lastDownloadTime;
    private static List<UpdateMeshListServer.FileData> _fileDataArray;
    
    public static IEnumerator DownloadFile(string file, string url, string internalPath, Action<string> callback = null, string another = null)
    {
        PrintManager.ShowMessage($"Download in corso di {file} da {url + file}");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        stopwatch.Start();

        if (!File.Exists(internalPath + file))
        {
            PrintManager.ShowMessage("File non trovato in locale, scarico il file dal server");
            var drcUrl = url + file;

            using (UnityWebRequest request = UnityWebRequest.Get(drcUrl))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    HandleDownloadError(request);
                    yield break;
                }

                SaveDownloadedFile(file, request.downloadHandler.data, internalPath);
                stopwatch.Stop();
                _lastDownloadTime = stopwatch.ElapsedMilliseconds;

                //update download time
                if (file.Contains(".drc"))
                {
                    PrintManager.setDownloadTime(_lastDownloadTime, "mesh");
                }
                else if (file.Contains(".png"))
                {
                    PrintManager.setDownloadTime(_lastDownloadTime, "texture");
                }
            }
        }
        
        if (another != null)
        {
            if (!File.Exists(internalPath + another))
            {
                PrintManager.ShowMessage("File non trovato in locale, scarico il file dal server");
                var drcUrl = url + another;

                using (UnityWebRequest request = UnityWebRequest.Get(drcUrl))
                {
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        HandleDownloadError(request);
                        yield break;
                    }

                    SaveDownloadedFile(another, request.downloadHandler.data, internalPath);
                }
            }
        }
        
        stopwatch.Stop();
        _lastDownloadTime = stopwatch.ElapsedMilliseconds;

        // Esegui il callback per ulteriori azioni
        callback?.Invoke(file);
    } 
    

    // Metodo per gestire errori di download
    private static void HandleDownloadError(UnityWebRequest request)
    {
        Debug.LogError("Errore durante il download del file: " + request.error);
        PrintManager.ShowMessage("Errore durante il download del file: " + request.error);
    }

    // Metodo per salvare il file scaricato
    private static void SaveDownloadedFile(string file, byte[] data, string internalPath)
    {
        string filePath = internalPath + file;
        
        var folder = Path.GetDirectoryName(filePath);
        if(!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        File.WriteAllBytes(filePath, data);
        Debug.Log(filePath);
        PrintManager.ShowMessage("Mesh scaricata correttamente: " + filePath);
    }
    
    
    
    public static List<UpdateMeshListServer.FileData> getFileDataArray()
    {
        return _fileDataArray;
    }
    
    public static void setFileDataArray(List<UpdateMeshListServer.FileData> fileDataArray)
    {
        _fileDataArray = fileDataArray;
    }
    
    public static Material LoadMTL(string path)
    {
        Material material = new Material(Shader.Find("Standard"));

        string[] lines = File.ReadAllLines(path);
        foreach (string line in lines)
        {
            string[] tokens = line.Split(' ');
            if (tokens.Length < 2) continue;

            switch (tokens[0])
            {
                case "newmtl":
                    material.name = tokens[1];
                    break;
                case "Kd":
                    material.color = new Color(float.Parse(tokens[1]), float.Parse(tokens[2]), float.Parse(tokens[3]));
                    break;
                case "Ks":
                    material.SetColor("_SpecColor", new Color(float.Parse(tokens[1]), float.Parse(tokens[2]), float.Parse(tokens[3])));
                    break;
                case "Ka":
                    material.SetColor("_EmissionColor", new Color(float.Parse(tokens[1]), float.Parse(tokens[2]), float.Parse(tokens[3])));
                    break;
                case "d":
                    material.SetFloat("_Mode", float.Parse(tokens[1]));
                    break;
                case "Ns":
                    material.SetFloat("_Glossiness", float.Parse(tokens[1]));
                    break;
                case "illum":
                    material.SetFloat("_Mode", float.Parse(tokens[1]));
                    break;
                case "map_Kd":
                    material.mainTexture = new Texture2D(2, 2);
                    var texturePath = Path.Combine(Path.GetDirectoryName(path), tokens[1]);
                    var data = File.ReadAllBytes(texturePath);
                    var texture = new Texture2D(2, 2);
                    texture.LoadImage(data);
                    texture.Apply();
                    material.mainTexture = texture;
                    break; 
            }
        }

        return material;
    }
}
