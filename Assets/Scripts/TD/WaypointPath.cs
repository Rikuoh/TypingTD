using UnityEngine;

namespace TD.TDCore
{
    public class WaypointPath : MonoBehaviour
    {
        public Transform[] waypoints;

        public Vector3 GetPoint(int index)
        {
            index = Mathf.Clamp(index, 0, waypoints.Length - 1);
            return waypoints[index].position;
        }
        public int Count => waypoints != null ? waypoints.Length : 0;
    }
}