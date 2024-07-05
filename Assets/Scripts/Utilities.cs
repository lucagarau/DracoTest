using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using LibHeifSharp;
using LibHeifSharp.Interop;

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
                    
                //todo  Aggiungi altri parametri come Ks, Ka, d, ecc.
            }
        }

        return material;
    }

    
    public static void ConvertHeicToPng(string inputPath, string outputPath)
    {
        try
        {
            if (!File.Exists(inputPath))
            {
                Debug.LogError("Input file not found: " + inputPath);
                return;
            }

            using (var heifContext = new HeifContext(inputPath))
            {
                var handle = heifContext.GetPrimaryImageHandle();
                var decodedImage = handle.Decode(HeifColorspace.Rgb, HeifChroma.InterleavedRgba32);

                // Convert HeifImage to Texture2D
                var texture = new Texture2D(decodedImage.Width, decodedImage.Height, TextureFormat.RGBA32, false);
                var pixelData = decodedImage.GetPlane(HeifChannel.Interleaved);
                //texture.LoadRawTextureData(pixelData);
                texture.Apply();

                // Encode Texture2D to PNG
                byte[] pngData = texture.EncodeToPNG();
                File.WriteAllBytes(outputPath, pngData);

                Debug.Log("Conversion completed: " + outputPath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error during conversion: " + ex.Message);
        }
    }
}