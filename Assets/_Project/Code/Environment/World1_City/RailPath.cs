using UnityEngine;
using System.Collections.Generic;

namespace RunawayHeroes
{
    /// <summary>
    /// Defines the path for grindable rails, allowing characters to follow curves and complex rail shapes.
    /// </summary>
    public class RailPath : MonoBehaviour
    {
        [Header("Path Settings")]
        [Tooltip("Control points defining the rail path. If empty, the rail will be a straight line in the transform's forward direction")]
        [SerializeField] private List<Transform> pathPoints = new List<Transform>();
        
        [Tooltip("Whether the path forms a closed loop")]
        [SerializeField] private bool isLooping = false;
        
        [Tooltip("Display the path in the scene view")]
        [SerializeField] private bool showGizmos = true;
        
        [Tooltip("Color of the path in the scene view")]
        [SerializeField] private Color gizmoColor = Color.cyan;
        
        [Header("Debug")]
        [SerializeField] private bool showDirectionVectors = false;
        [SerializeField] private float directionVectorLength = 1f;

        // Cached path data
        private List<Vector3> calculatedPath = new List<Vector3>();
        private List<Vector3> calculatedDirections = new List<Vector3>();
        private float pathLength = 0f;
        private float[] segmentLengths;

        /// <summary>
        /// Initialize the path calculations
        /// </summary>
        private void Start()
        {
            CalculatePath();
        }

        /// <summary>
        /// Calculate the path points and directions
        /// </summary>
        public void CalculatePath()
        {
            calculatedPath.Clear();
            calculatedDirections.Clear();
            
            // If no path points, use transform forward
            if (pathPoints == null || pathPoints.Count == 0)
            {
                calculatedPath.Add(transform.position);
                calculatedPath.Add(transform.position + transform.forward * 10f);
                
                calculatedDirections.Add(transform.forward);
                calculatedDirections.Add(transform.forward);
                
                segmentLengths = new float[1];
                segmentLengths[0] = 10f;
                pathLength = 10f;
                return;
            }
            
            // If only one point, add transform position as first point
            if (pathPoints.Count == 1)
            {
                calculatedPath.Add(transform.position);
                calculatedPath.Add(pathPoints[0].position);
                
                Vector3 dir = (pathPoints[0].position - transform.position).normalized;
                calculatedDirections.Add(dir);
                calculatedDirections.Add(dir);
                
                segmentLengths = new float[1];
                segmentLengths[0] = Vector3.Distance(transform.position, pathPoints[0].position);
                pathLength = segmentLengths[0];
                return;
            }
            
            // Calculate path points and directions
            segmentLengths = new float[pathPoints.Count - 1 + (isLooping ? 1 : 0)];
            pathLength = 0f;
            
            for (int i = 0; i < pathPoints.Count; i++)
            {
                calculatedPath.Add(pathPoints[i].position);
                
                if (i < pathPoints.Count - 1)
                {
                    Vector3 dir = (pathPoints[i + 1].position - pathPoints[i].position).normalized;
                    calculatedDirections.Add(dir);
                    
                    float distance = Vector3.Distance(pathPoints[i].position, pathPoints[i + 1].position);
                    segmentLengths[i] = distance;
                    pathLength += distance;
                }
                else if (isLooping)
                {
                    Vector3 dir = (pathPoints[0].position - pathPoints[i].position).normalized;
                    calculatedDirections.Add(dir);
                    
                    float distance = Vector3.Distance(pathPoints[i].position, pathPoints[0].position);
                    segmentLengths[i] = distance;
                    pathLength += distance;
                }
                else
                {
                    // For the last point, use the same direction as the previous segment
                    calculatedDirections.Add(calculatedDirections[calculatedDirections.Count - 1]);
                }
            }
            
            // If looping, add first point again to close the loop
            if (isLooping)
            {
                calculatedPath.Add(pathPoints[0].position);
                calculatedDirections.Add(calculatedDirections[0]);
            }
        }

        /// <summary>
        /// Get the closest point on the path to the given position
        /// </summary>
        public Vector3 GetClosestPoint(Vector3 position, out float distanceAlongPath, out int segmentIndex)
        {
            float minDistance = float.MaxValue;
            Vector3 closestPoint = Vector3.zero;
            distanceAlongPath = 0f;
            segmentIndex = 0;
            
            float currentDistance = 0f;
            
            // Check each segment
            for (int i = 0; i < calculatedPath.Count - 1; i++)
            {
                Vector3 pointOnSegment = GetClosestPointOnSegment(position, i, out float segmentDistance);
                float distanceToPoint = Vector3.Distance(position, pointOnSegment);
                
                if (distanceToPoint < minDistance)
                {
                    minDistance = distanceToPoint;
                    closestPoint = pointOnSegment;
                    segmentIndex = i;
                    distanceAlongPath = currentDistance + segmentDistance;
                }
                
                currentDistance += segmentLengths[i];
            }
            
            return closestPoint;
        }

        /// <summary>
        /// Get the closest point on a specific segment
        /// </summary>
        private Vector3 GetClosestPointOnSegment(Vector3 position, int segmentIndex, out float distanceAlongSegment)
        {
            Vector3 startPoint = calculatedPath[segmentIndex];
            Vector3 endPoint = calculatedPath[segmentIndex + 1];
            Vector3 segmentDirection = (endPoint - startPoint).normalized;
            float segmentLength = segmentLengths[segmentIndex];
            
            Vector3 toPosition = position - startPoint;
            float dotProduct = Vector3.Dot(toPosition, segmentDirection);
            
            // Clamp to segment
            dotProduct = Mathf.Clamp(dotProduct, 0f, segmentLength);
            distanceAlongSegment = dotProduct;
            
            return startPoint + segmentDirection * dotProduct;
        }

        /// <summary>
        /// Get the position along the path at the specified distance
        /// </summary>
        public Vector3 GetPositionAtDistance(float distance)
        {
            // If path is empty or distance is invalid
            if (calculatedPath.Count < 2 || distance < 0f || distance > pathLength)
            {
                return transform.position;
            }
            
            float currentDistance = 0f;
            
            // Find segment that contains the distance
            for (int i = 0; i < segmentLengths.Length; i++)
            {
                if (currentDistance + segmentLengths[i] >= distance)
                {
                    // Found segment, interpolate position
                    float segmentDistance = distance - currentDistance;
                    float ratio = segmentDistance / segmentLengths[i];
                    return Vector3.Lerp(calculatedPath[i], calculatedPath[i + 1], ratio);
                }
                
                currentDistance += segmentLengths[i];
            }
            
            // Should not reach here
            return calculatedPath[calculatedPath.Count - 1];
        }

        /// <summary>
        /// Get the direction at the specified position along the path
        /// </summary>
        public Vector3 GetDirectionAtPoint(Vector3 position)
        {
            float distanceAlongPath;
            int segmentIndex;
            GetClosestPoint(position, out distanceAlongPath, out segmentIndex);
            
            // If only one segment or at the end of a non-looping path
            if (calculatedDirections.Count <= 1 || 
                (segmentIndex >= calculatedDirections.Count - 1 && !isLooping))
            {
                return calculatedDirections[Mathf.Max(0, calculatedDirections.Count - 1)];
            }
            
            // Get the direction at the segment
            return calculatedDirections[segmentIndex];
        }

        /// <summary>
        /// Get the total length of the path
        /// </summary>
        public float GetPathLength()
        {
            return pathLength;
        }

        /// <summary>
        /// Draw the path in the scene view
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;
                
            if (calculatedPath.Count == 0 || (Application.isPlaying == false))
                CalculatePath();
                
            Gizmos.color = gizmoColor;
            
            // Draw path
            for (int i = 0; i < calculatedPath.Count - 1; i++)
            {
                Gizmos.DrawLine(calculatedPath[i], calculatedPath[i + 1]);
            }
            
            // Draw points
            for (int i = 0; i < calculatedPath.Count; i++)
            {
                Gizmos.DrawSphere(calculatedPath[i], 0.1f);
            }
            
            // Draw direction vectors
            if (showDirectionVectors && calculatedDirections.Count > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < calculatedPath.Count; i++)
                {
                    if (i < calculatedDirections.Count)
                    {
                        Gizmos.DrawRay(calculatedPath[i], calculatedDirections[i] * directionVectorLength);
                    }
                }
            }
        }
    }
}