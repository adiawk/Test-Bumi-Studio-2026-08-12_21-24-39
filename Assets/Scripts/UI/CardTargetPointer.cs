using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Slay-the-Spire style curved targeting pointer (UI).
/// Edit segment/arrow sprites on the prefab; this script only lays them out.
/// </summary>
public class CardTargetPointer : MonoBehaviour
{
    [SerializeField] RectTransform segmentTemplate;
    [SerializeField] RectTransform arrowHead;
    [SerializeField] int segmentCount = 16;
    [SerializeField] float curveStrength = 0.35f;
    [SerializeField] float segmentScaleStart = 1f;
    [SerializeField] float segmentScaleEnd = 0.55f;
    [SerializeField] bool hideTemplate = true;

    readonly List<RectTransform> segments = new List<RectTransform>();
    RectTransform rectTransform;
    Canvas canvas;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        EnsureSegments();
        if (hideTemplate && segmentTemplate != null)
            segmentTemplate.gameObject.SetActive(false);
        Hide();
    }

    public void Show(Vector2 fromCanvasLocal, Vector2 toCanvasLocal)
    {
        EnsureSegments();
        gameObject.SetActive(true);
        Layout(fromCanvasLocal, toCanvasLocal);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Layout(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 8f)
        {
            for (int i = 0; i < segments.Count; i++)
                segments[i].gameObject.SetActive(false);
            if (arrowHead != null)
                arrowHead.gameObject.SetActive(false);
            return;
        }

        // Control point bends the arc upward relative to the chord.
        Vector2 mid = (from + to) * 0.5f;
        Vector2 perpendicular = new Vector2(-delta.y, delta.x).normalized;
        if (perpendicular.y < 0f)
            perpendicular = -perpendicular;
        Vector2 control = mid + perpendicular * (distance * curveStrength);

        int count = segments.Count;
        for (int i = 0; i < count; i++)
        {
            float t = (i + 1f) / (count + 1f);
            Vector2 pos = EvaluateQuadratic(from, control, to, t);
            Vector2 tangent = EvaluateQuadraticDerivative(from, control, to, t).normalized;
            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            float scale = Mathf.Lerp(segmentScaleStart, segmentScaleEnd, t);

            RectTransform segment = segments[i];
            segment.gameObject.SetActive(true);
            segment.anchoredPosition = pos;
            segment.localRotation = Quaternion.Euler(0f, 0f, angle);
            segment.localScale = new Vector3(scale, scale, 1f);
        }

        if (arrowHead != null)
        {
            Vector2 tipTangent = EvaluateQuadraticDerivative(from, control, to, 1f).normalized;
            float tipAngle = Mathf.Atan2(tipTangent.y, tipTangent.x) * Mathf.Rad2Deg;
            arrowHead.gameObject.SetActive(true);
            arrowHead.anchoredPosition = to;
            arrowHead.localRotation = Quaternion.Euler(0f, 0f, tipAngle);
            arrowHead.SetAsLastSibling();
        }
    }

    void EnsureSegments()
    {
        if (segmentTemplate == null)
            return;

        while (segments.Count < segmentCount)
        {
            RectTransform clone = Instantiate(segmentTemplate, transform);
            clone.name = "Segment_" + segments.Count;
            clone.gameObject.SetActive(true);
            segments.Add(clone);
        }

        for (int i = segmentCount; i < segments.Count; i++)
            segments[i].gameObject.SetActive(false);
    }

    static Vector2 EvaluateQuadratic(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return (u * u * a) + (2f * u * t * b) + (t * t * c);
    }

    static Vector2 EvaluateQuadraticDerivative(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        return (2f * (1f - t) * (b - a)) + (2f * t * (c - b));
    }
}
