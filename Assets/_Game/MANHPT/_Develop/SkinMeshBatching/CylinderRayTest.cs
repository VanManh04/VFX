using UnityEngine;

public class CylinderRayTest : MonoBehaviour
{
    public Transform              ray;
    public VAT_Utilities.Cylinder cylinder;
    public int                    segments = 16;
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        VAT_Utilities.DrawCylinder(cylinder, transform, segments);
        
        var  r     = new Ray(ray.position, ray.forward);
        
        var cylinderWorldPoint = new VAT_Utilities.Cylinder
        {
            center  = transform.TransformPoint(cylinder.center),
            radius  = cylinder.radius,
            height  = cylinder.height,
            axis    = transform.TransformDirection(cylinder.axis),
        };
        
        bool isHit = VAT_Utilities.IntersectRayCylinder(r, cylinderWorldPoint, out Vector3 intersectionPoint);
        Gizmos.color = Color.red;
        if (isHit)
        {
            Gizmos.DrawSphere(intersectionPoint, 0.1f);
            Gizmos.DrawLine(r.origin, intersectionPoint);
        }
        else
        {
            Gizmos.DrawRay(r);
        }
    }
#endif
}