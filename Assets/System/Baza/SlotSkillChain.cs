using UnityEngine;

internal sealed class SlotSkillChain
{
    private const int SegmentCount = 12;
    private const int SolverIterations = 12;
    private const float MaxStep = 1f / 60f;

    private readonly Vector3[] points = new Vector3[SegmentCount + 1];
    private readonly Vector3[] previous = new Vector3[SegmentCount + 1];
    private bool initialized;
    private float previousStep;

    internal void Simulate(Vector3 start, Vector3 end, float restLength, float deltaTime)
    {
        Vector3 oldStart = points[0];
        Vector3 oldEnd = points[SegmentCount];
        float resetDistance = Mathf.Max(2f, restLength * 4f);
        if (!initialized || (start - oldStart).sqrMagnitude > resetDistance * resetDistance ||
            (end - oldEnd).sqrMagnitude > resetDistance * resetDistance)
        {
            Reset(start, end, restLength);
            oldStart = start;
            oldEnd = end;
        }

        float duration = Mathf.Min(deltaTime, MaxStep * 4f);
        int stepCount = Mathf.CeilToInt(duration / MaxStep);
        if (stepCount == 0)
        {
            points[0] = start;
            points[SegmentCount] = end;
            return;
        }

        float step = duration / stepCount;
        for (int i = 1; i <= stepCount; i++)
        {
            float t = (float)i / stepCount;
            Step(Vector3.Lerp(oldStart, start, t), Vector3.Lerp(oldEnd, end, t), restLength, step);
        }
    }

    private void Reset(Vector3 start, Vector3 end, float restLength)
    {
        for (int i = 0; i <= SegmentCount; i++)
        {
            float t = (float)i / SegmentCount;
            float sag = 4f * t * (1f - t) * restLength * 0.12f;
            points[i] = Vector3.Lerp(start, end, t) + Vector3.down * sag;
            previous[i] = points[i];
        }
        initialized = true;
        previousStep = 0f;
    }

    private void Step(Vector3 start, Vector3 end, float restLength, float step)
    {
        float inertia = Mathf.Exp(-6f * step);
        if (previousStep > 0f)
            inertia *= Mathf.Min(step / previousStep, 2f);
        previousStep = step;

        Vector3 gravity = Physics.gravity * (step * step);
        for (int i = 1; i < SegmentCount; i++)
        {
            Vector3 position = points[i];
            points[i] += (position - previous[i]) * inertia + gravity;
            previous[i] = position;
        }

        points[0] = start;
        points[SegmentCount] = end;
        // The cosmetic tether pays out when a skill reaches beyond its idle length.
        float segmentLength = Mathf.Max(restLength, Vector3.Distance(start, end) * 1.015f) / SegmentCount;
        for (int iteration = 0; iteration < SolverIterations; iteration++)
        {
            bool reverse = (iteration & 1) != 0;
            for (int edge = 0; edge < SegmentCount; edge++)
            {
                int a = reverse ? SegmentCount - 1 - edge : edge;
                int b = a + 1;
                Vector3 delta = points[b] - points[a];
                float distance = delta.magnitude;
                if (distance < 0.0001f) continue;

                Vector3 correction = delta * ((distance - segmentLength) / distance);
                if (a == 0)
                    points[b] -= correction;
                else if (b == SegmentCount)
                    points[a] += correction;
                else
                {
                    points[a] += correction * 0.5f;
                    points[b] -= correction * 0.5f;
                }
            }
        }
    }

    internal void Draw(Mesh mesh, Material material, Transform frame, float pitch)
    {
        float length = 0f;
        for (int i = 0; i < SegmentCount; i++)
            length += Vector3.Distance(points[i], points[i + 1]);
        if (length < 0.001f) return;

        int count = Mathf.Clamp(Mathf.CeilToInt(length / pitch), 2, 48);
        float spacing = length / count;
        var scale = new Vector3(pitch, spacing, pitch) * 1.42f;
        int edge = 0;
        float traveled = 0f;
        float edgeLength = Vector3.Distance(points[0], points[1]);
        for (int i = 0; i < count; i++)
        {
            float distance = (i + 0.5f) * spacing;
            while (edge < SegmentCount - 1 && distance > traveled + edgeLength)
            {
                traveled += edgeLength;
                edge++;
                edgeLength = Vector3.Distance(points[edge], points[edge + 1]);
            }
            if (edgeLength < 0.0001f) continue;

            Vector3 tangent = (points[edge + 1] - points[edge]) / edgeLength;
            Vector3 center = points[edge] + tangent * (distance - traveled);
            Vector3 normal = Vector3.ProjectOnPlane(frame.forward, tangent);
            if (normal.sqrMagnitude < 0.02f)
                normal = Vector3.ProjectOnPlane(frame.right, tangent);
            Quaternion rotation = Quaternion.LookRotation(normal, tangent);
            if ((i & 1) != 0)
                rotation *= Quaternion.AngleAxis(90f, Vector3.up);

            Graphics.DrawMesh(mesh, Matrix4x4.TRS(center, rotation, scale), material, frame.gameObject.layer);
        }
    }
}
