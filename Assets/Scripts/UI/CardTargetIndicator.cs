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
        bool selectedNewTarget = target != null && target != follow;
        follow = target;
        if (target != null)
            transform.position = target.position + worldOffset;

        gameObject.SetActive(target != null);

        if (selectedNewTarget && AudioManager.Instance != null)
            AudioManager.Instance.PlayCardTargetSelect();
    }

    public void Hide()
    {
        follow = null;
        gameObject.SetActive(false);
    }
}
