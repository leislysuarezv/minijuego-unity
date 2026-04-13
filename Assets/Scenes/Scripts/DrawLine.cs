using System.Collections.Generic;
using UnityEngine;

public class DrawLine : MonoBehaviour
{
    private LineRenderer line;
    private readonly List<Vector3> points = new List<Vector3>();

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 0;

        // 🔥 GROSOR DE LA LÍNEA
        line.startWidth = 0.2f;
        line.endWidth = 0.2f;
    }

    void OnEnable()
    {
        CursorInputRouter.Instance.Held += HandlePaintingInput;
    }

    void OnDisable()
    {
        if (!CursorInputRouter.HasInstance)
        {
            return;
        }

        CursorInputRouter.Instance.Held -= HandlePaintingInput;
    }

    void HandlePaintingInput(Vector3 _)
    {
        if (ScoreManager.CurrentPhase != ScoreManager.GamePhase.Painting)
        {
            return;
        }

        if (line == null)
        {
            line = GetComponent<LineRenderer>();
        }

        Vector3 playerPosition = transform.position;

        if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], playerPosition) > 0.1f)
        {
            points.Add(playerPosition);
            line.positionCount = points.Count;
            line.SetPositions(points.ToArray());
        }
    }
}

