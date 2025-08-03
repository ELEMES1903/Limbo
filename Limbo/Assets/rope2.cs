using UnityEngine;

public class RopeSimulation : MonoBehaviour
{
    [Header("Rope Settings")]
    public int nodeCount = 20;
    public float segmentLength = 0.5f;
    public int iterations = 10;
    public bool isAnchored = true;

    [Header("Physics Settings")]
    public float mass = 1.0f; // Higher = slower response to forces
    public float gravityScale = 1.0f; // Multiplier for gravity effect
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float damping = 0.98f; // Preserves some motion, dampens oscillation
    public float nodeDrag = 0.0f; // Extra drag per frame

    [Header("Constraint Settings")]
    [Range(0f, 1f)]
    public float tensionStrength = 1.0f; // 1.0 = fully enforces distance

    [Header("Debug Visualization")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.green;
    public float gizmoNodeRadius = 0.05f;

    private Vector3[] nodes;
    private Vector3[] prevNodes;

    void Start()
    {
        InitializeRope();
    }

    void InitializeRope()
    {
        nodes = new Vector3[nodeCount];
        prevNodes = new Vector3[nodeCount];

        Vector3 startPos = transform.position;

        for (int i = 0; i < nodeCount; i++)
        {
            nodes[i] = startPos + Vector3.down * segmentLength * i;
            prevNodes[i] = nodes[i];
        }
    }

    void Update()
    {
        SimulateRope();
    }

    void SimulateRope()
    {
        float deltaTime = Time.deltaTime;

        for (int i = 1; i < nodeCount; i++) // Skip anchored node
        {
            Vector3 velocity = nodes[i] - prevNodes[i];

            // Clamp tiny movement
            if (velocity.sqrMagnitude < 0.0001f)
                velocity = Vector3.zero;

            // Apply drag and damping
            velocity *= damping;
            velocity *= (1f - nodeDrag * deltaTime); // drag per second

            // Verlet integration
            prevNodes[i] = nodes[i];
            nodes[i] += velocity;

            // Gravity force
            Vector3 force = gravity * gravityScale * deltaTime * deltaTime / Mathf.Max(mass, 0.01f);
            nodes[i] += force;
        }

        for (int i = 0; i < iterations; i++)
        {
            ApplyConstraints();
        }
    }

    void ApplyConstraints()
    {
        if (isAnchored)
            nodes[0] = transform.position;

        for (int i = 0; i < nodeCount - 1; i++)
        {
            Vector3 dir = nodes[i + 1] - nodes[i];
            float dist = dir.magnitude;
            float error = dist - segmentLength;
            Vector3 correction = dir.normalized * (error * 0.5f * tensionStrength);

            if (i != 0 || !isAnchored)
                nodes[i] += correction;

            nodes[i + 1] -= correction;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || nodes == null || nodes.Length != nodeCount)
            return;

        Gizmos.color = gizmoColor;
        for (int i = 0; i < nodeCount - 1; i++)
        {
            Gizmos.DrawLine(nodes[i], nodes[i + 1]);
            Gizmos.DrawSphere(nodes[i], gizmoNodeRadius);
        }
        Gizmos.DrawSphere(nodes[nodeCount - 1], gizmoNodeRadius);
    }
}
