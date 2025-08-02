using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class VerletRope : MonoBehaviour
{
    [Header("Rope Settings")]
    public Transform startPoint;
    public Transform endPoint;
    public int segmentCount = 35;
    public float segmentLength = 0.25f;
    public float ropeWidth = 0.05f;

    [Header("Physics")]
    public float weight = 1f;
    public float damping = 0.99f;
    public int solverIterations = 8;

    [Header("Collision")]
    public LayerMask collisionMask;
    public float collisionRadius = 0.05f;
    public float friction = 0.01f;

    [Header("Gizmos")]
    public bool showGizmos = true;

    private LineRenderer lineRenderer;

    private List<Vector3> positions = new List<Vector3>();
    private List<Vector3> prevPositions = new List<Vector3>();

    void OnValidate()
    {
        segmentCount = Mathf.Max(2, segmentCount);
        segmentLength = Mathf.Max(0.01f, segmentLength);
        collisionRadius = Mathf.Max(0f, collisionRadius);
        weight = Mathf.Max(0f, weight);
        damping = Mathf.Clamp01(damping);
    }

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segmentCount;
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;
        lineRenderer.useWorldSpace = true;

        positions.Clear();
        prevPositions.Clear();

        Vector3 dir = (endPoint.position - startPoint.position).normalized;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 pos = startPoint.position + dir * segmentLength * i;
            positions.Add(pos);
            prevPositions.Add(pos);
        }
    }

    void Update()
    {
        if (positions.Count != segmentCount)
            Initialize();

        Simulate(Time.deltaTime);
        ApplyConstraints();
        ResolveCollisions();
        UpdateLineRenderer();
    }

    void Simulate(float deltaTime)
    {
        for (int i = 1; i < positions.Count - 1; i++)
        {
            Vector3 current = positions[i];
            Vector3 prev = prevPositions[i];
            Vector3 velocity = (current - prev) * damping;

            Vector3 next = current + velocity + Vector3.down * weight * deltaTime * deltaTime;

            prevPositions[i] = current;
            positions[i] = next;
        }
    }

    void ApplyConstraints()
    {
        positions[0] = startPoint.position;
        positions[positions.Count - 1] = endPoint.position;

        for (int it = 0; it < solverIterations; it++)
        {
            for (int i = 0; i < positions.Count - 1; i++)
            {
                Vector3 p1 = positions[i];
                Vector3 p2 = positions[i + 1];
                float dist = (p1 - p2).magnitude;
                float error = dist - segmentLength;
                Vector3 dir = (p2 - p1).normalized;

                Vector3 correction = dir * error * 0.5f;

                if (i != 0)
                    positions[i] += correction;
                if (i + 1 != positions.Count - 1)
                    positions[i + 1] -= correction;
            }
        }
    }

    void ResolveCollisions()
    {
        for (int i = 1; i < positions.Count - 1; i++)
        {
            Vector3 current = positions[i];
            Collider[] hits = Physics.OverlapSphere(current, collisionRadius, collisionMask);

            foreach (var hit in hits)
            {
                if (hit.attachedRigidbody != null && hit.attachedRigidbody.isKinematic == false)
                    continue;

                Vector3 closest = hit.ClosestPoint(current);
                Vector3 toPoint = current - closest;
                float distance = toPoint.magnitude;

                if (distance < collisionRadius && distance > 0.0001f)
                {
                    Vector3 resolveDir = toPoint.normalized;
                    float resolveDist = collisionRadius - distance;

                    positions[i] += resolveDir * resolveDist;

                    // Friction response
                    Vector3 velocity = positions[i] - prevPositions[i];
                    Vector3 tangent = Vector3.ProjectOnPlane(velocity, resolveDir);
                    Vector3 frictionVec = -tangent * friction;
                    positions[i] += frictionVec;
                }
            }
        }
    }

    void UpdateLineRenderer()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            lineRenderer.SetPosition(i, positions[i]);
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || positions == null || positions.Count < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < positions.Count; i++)
        {
            Gizmos.DrawSphere(positions[i], collisionRadius * 0.5f);
        }
    }
}
