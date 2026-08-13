using UnityEngine;

/// <summary>
/// Visual marker shown over a valid card effect target while dragging.
/// Edit the prefab visuals; this script only follows/shows/hides.
/// </summary>
public class CardTargetIndicator : MonoBehaviour
{
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 0.35f, 0f);

    Transform follow;

    void LateUpdate()
    {
        if (follow == null)
            return;

        transform.position = follow.position + worldOffset;
    }

    public void Show(Transform target)
    {
        follow = target;
        if (target != null)
            transform.position = target.position + worldOffset;

        gameObject.SetActive(target != null);
    }

    public void Hide()
    {
        follow = null;
        gameObject.SetActive(false);
    }
}
