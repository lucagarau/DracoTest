using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEBUG : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var draco = GetComponent<DracoMeshManager>();
        draco.ChangeMesh("meshes/m1.drc");
        
        var bounds = draco.GetComponent<Renderer>().bounds;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
