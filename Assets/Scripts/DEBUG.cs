using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEBUG : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var renderer = GetComponent<Renderer>();
        var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
        var uvs = mesh.uv;
        
        // Stampa le coordinate uv di ogni vertic
            
        Debug.Log("Coordinate uv di: " + gameObject.name);
        for (int i = 0; i < uvs.Length; i++)
        {
            Debug.Log("Vertice " + i + ": " + uvs[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
    
            
            
        
    }
}
