
using UnityEngine;

public class positionManipulator : MonoBehaviour
{
    public GameObject target;
    
    // Update is called once per frame
    void LateUpdate()
    {
        this.transform.rotation = target.transform.rotation;
        
    }
}
