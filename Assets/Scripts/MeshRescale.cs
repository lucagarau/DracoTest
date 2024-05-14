using UnityEngine;

public class MeshRescale : MonoBehaviour
{
/*
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, desiredsize);
    }

    private void Start()
    {
        Resize(ObjectToRescale, desiredsize);

    }

    private void OnGUI()
    {
        if (GUILayout.Button("Resize"))
        {
            Resize(ObjectToRescale, desiredsize);
        }
    }*/
    
    public void Resize(GameObject toRescale, Vector3 desiredsize)
    {
        // rescale the object so that it fits the desired size
        var c = toRescale.GetComponent<BoxCollider>();
        if (c == null)
        {
            Debug.LogError("No collider found on the object to rescale");
            return;
        }
        BoxCollider destinationObject = c as BoxCollider;
        if (destinationObject == null)
        {
            Debug.LogError("The collider is not a box collider");
            return;
        }
        
        // Create a box collider to represent the desired size
        BoxCollider desiredSizeCollider = gameObject.AddComponent<BoxCollider>();
        desiredSizeCollider.size = desiredsize;
        desiredSizeCollider.center = Vector3.zero;
        
        var destinationCollider = destinationObject.GetComponent<BoxCollider>(); // Oggetto di riferimento (destinazione)
        
        float scaleX = ScaleUtils.CalculateScaleFactor(destinationCollider, desiredSizeCollider, Axis.X);
        float scaleY = ScaleUtils.CalculateScaleFactor(destinationCollider, desiredSizeCollider, Axis.Y);
        float scaleZ = ScaleUtils.CalculateScaleFactor(destinationCollider, desiredSizeCollider, Axis.Z);

        // Apply the scaling factors
        toRescale.transform.localScale = new Vector3(
            toRescale.transform.localScale.x * scaleX,
            toRescale.transform.localScale.y * scaleY,
            toRescale.transform.localScale.z * scaleZ
        );

        

    }
}
public class ScaleUtils
{
    public static float CalculateScaleFactor(BoxCollider fromCollider, BoxCollider toCollider, Axis axis)
    {
        // Get the topmost and bottommost points of the fromCollider
        Vector3 topMostFrom = Vector3.zero;
        Vector3 bottomMostFrom = Vector3.zero;
        switch (axis)
        {
            case Axis.X:
                topMostFrom = new Vector3(fromCollider.bounds.max.x, fromCollider.bounds.center.y, fromCollider.bounds.center.z);
                bottomMostFrom = new Vector3(fromCollider.bounds.min.x, fromCollider.bounds.center.y, fromCollider.bounds.center.z);
                break;
            case Axis.Y:
                topMostFrom = new Vector3(fromCollider.bounds.center.x, fromCollider.bounds.max.y, fromCollider.bounds.center.z);
                bottomMostFrom = new Vector3(fromCollider.bounds.center.x, fromCollider.bounds.min.y, fromCollider.bounds.center.z);
                break;
            case Axis.Z:
                topMostFrom = new Vector3(fromCollider.bounds.center.x, fromCollider.bounds.center.y, fromCollider.bounds.max.z);
                bottomMostFrom = new Vector3(fromCollider.bounds.center.x, fromCollider.bounds.center.y, fromCollider.bounds.min.z);
                break;
        }

        // Get the topmost and bottommost points of the toCollider
        Vector3 topMostTo = Vector3.zero;
        Vector3 bottomMostTo = Vector3.zero;
        switch (axis)
        {
            case Axis.X:
                topMostTo = new Vector3(toCollider.bounds.max.x, toCollider.bounds.center.y, toCollider.bounds.center.z);
                bottomMostTo = new Vector3(toCollider.bounds.min.x, toCollider.bounds.center.y, toCollider.bounds.center.z);
                break;
            case Axis.Y:
                topMostTo = new Vector3(toCollider.bounds.center.x, toCollider.bounds.max.y, toCollider.bounds.center.z);
                bottomMostTo = new Vector3(toCollider.bounds.center.x, toCollider.bounds.min.y, toCollider.bounds.center.z);
                break;
            case Axis.Z:
                topMostTo = new Vector3(toCollider.bounds.center.x, toCollider.bounds.center.y, toCollider.bounds.max.z);
                bottomMostTo = new Vector3(toCollider.bounds.center.x, toCollider.bounds.center.y, toCollider.bounds.min.z);
                break;
        }

        // Calculate the distance between the topmost and bottommost points of both colliders
        float distanceFrom = Vector3.Distance(topMostFrom, bottomMostFrom);
        float distanceTo = Vector3.Distance(topMostTo, bottomMostTo);

        // Calculate the scaling factor
        float scaleFactor = distanceTo / distanceFrom;

        return scaleFactor;
    }
}

public enum Axis
{
    X,
    Y,
    Z
}
public class MovementUtils 
{
    
    /* AGILE NOTES
 
 * Position: Restituisce la posizione del pivot dell'oggetto. NOTA: Questo non è detto che sia sempre al centro dell'oggetto, dipende da come è stato costruito il modello
 * [Collider]
 * Extents: E' sempre la metà della dimensione del bounding box.
 * Center: Rappresenta il
 * Size: E' l'altezza del bounding box. Corrisponde a 2*bounds.extents
 * Max: E' la somma bounds.center + bounds.extents. E' il punto in alto a destra (topRight)
 * Min: E' la differenza bounds.center - bounds.extents. E' il punto in basso a sinistra (bottomLeft)
 *
 * bounds.center = transform.position +  BoxCollider.position !! Attenzione: bounds.center != da collider.center!!!!!!!!!!!!!!
 * bounds.center.y == center.y * transform.localScale.y + transform.position.y;
 */

    public enum Direction
    {
        Above,
        Below,
        Left,
        Right,
        Front,
        Back
    }

    public static Vector3 GetPositionBTouchesA(BoxCollider A, BoxCollider B, Direction direction)
    {
        switch (direction)
        {
            case Direction.Above:
                return GetPositionBTouchesA_Above(A, B);
            case Direction.Below:
                return GetPositionBTouchesA_Below(A, B);
            case Direction.Left:
                return GetPositionBTouchesA_Left(A, B);
            case Direction.Right:
                return GetPositionBTouchesA_Right(A, B);
            case Direction.Front:
                return GetPositionBTouchesA_InFronOf(A, B);
            case Direction.Back:
                return GetPositionBTouchesA_BackOf(A, B);
            default:
                throw new System.NotImplementedException();
        }
    }

    private static Vector3 GetPositionBTouchesA_Above(BoxCollider A, BoxCollider B)
    {
        var bBounds = B.bounds;
        var newY = bBounds.max.y - A.center.y * A.gameObject.transform.localScale.y + A.bounds.extents.y;
        var newPosition = new Vector3(bBounds.center.x,newY,bBounds.center.z);
        return newPosition;
    }

    private static Vector3 GetPositionBTouchesA_Below(BoxCollider A, BoxCollider B)
    {
        var bounds = B.bounds;
        var newY = bounds.min.y - A.center.y * A.gameObject.transform.localScale.y - A.bounds.extents.y;
        var newPosition = new Vector3(bounds.center.x,newY,bounds.center.z);
        return newPosition;
    }

    private static Vector3 GetPositionBTouchesA_InFronOf(BoxCollider A, BoxCollider B)
    {
        var bBounds = B.bounds;
        var newZ = bBounds.max.z - A.center.z * A.gameObject.transform.localScale.z + A.bounds.extents.z;
        var newPosition = new Vector3(bBounds.center.x,bBounds.center.y,newZ);
        return newPosition;
    }

    private static Vector3 GetPositionBTouchesA_BackOf(BoxCollider A, BoxCollider B)
    {
        var bBounds = B.bounds;

        var newZ = bBounds.min.z - A.center.z * A.gameObject.transform.localScale.z - A.bounds.extents.z;
        var newPosition = new Vector3(bBounds.center.x,bBounds.center.y,newZ);
        return newPosition;
    }

    private static Vector3 GetPositionBTouchesA_Left(BoxCollider A, BoxCollider B)
    {
        var bBounds = B.bounds;
        var newX = bBounds.min.x - A.center.x * A.gameObject.transform.localScale.x -A.bounds.extents.x;
        var newPosition = new Vector3(newX,bBounds.center.y,bBounds.center.z);
        return newPosition;
    }

    private static Vector3 GetPositionBTouchesA_Right(BoxCollider A, BoxCollider B)
    {
        var bBounds = B.bounds;
        var newX = bBounds.max.x - A.center.x * A.gameObject.transform.localScale.x +A.bounds.extents.x;
        var newPosition = new Vector3(newX,bBounds.center.y,bBounds.center.z);
        return newPosition;
    }
}