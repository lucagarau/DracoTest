using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class sliderScript : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Slider slider;
    [SerializeField] private DracoMeshManager obj;
    
    void Start()
    {
        slider.onValueChanged.AddListener((v) => {obj.resizeObject(v);
            Debug.Log($"Resize con v {v.ToString()}");}
        );
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
