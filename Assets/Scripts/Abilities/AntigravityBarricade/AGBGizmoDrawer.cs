using UnityEngine;

public class AGBGizmoDrawer : MonoBehaviour
{
    public float Range;

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = new Color(0, 0.7f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}
