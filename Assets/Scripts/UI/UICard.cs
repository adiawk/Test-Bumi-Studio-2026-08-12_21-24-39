using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICard : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] TextMeshProUGUI cardNameText;
    [SerializeField] TextMeshProUGUI cardDescriptionText;
    [SerializeField] TextMeshProUGUI cardEnergyCostText;
    [SerializeField] Image background;
    [SerializeField] Button button;
    [SerializeField] float hoverScale = 1.12f;
    [SerializeField] float hoverLift = 36f;
    [SerializeField] float playLiftThreshold = 120f;

    RuntimeCard card;
    CombatManager combatManager;
    Color defaultBackgroundColor;
    bool hasBackgroundColor;

    RectTransform rectTransform;
    Canvas rootCanvas;
    Transform handParent;
    int handSiblingIndex;
    Vector2 restAnchoredPosition;
    Vector3 restLocalScale = Vector3.one;
    bool restCached;
    bool dragging;
    LayoutElement layoutElement;
    CanvasGroup canvasGroup;

    public void SetCard(CardData cardData)
    {
        if (cardNameText != null)
            cardNameText.text = cardData != null ? cardData.CardName : string.Empty;
        if (cardDescriptionText != null)
            cardDescriptionText.text = cardData != null ? cardData.Description : string.Empty;
        if (cardEnergyCostText != null)
            cardEnergyCostText.text = cardData != null ? cardData.Cost.ToString() : string.Empty;
    }

    public void Bind(RuntimeCard runtimeCard, CombatManager combat)
    {
        card = runtimeCard;
        combatManager = combat;
        SetCard(runtimeCard != null ? runtimeCard.Data : null);

        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
            button.transition = Selectable.Transition.None;
        }

        if (background == null)
            background = GetComponent<Image>();
        CacheBackgroundColor();
        CacheRestPose();
        ApplyHoverVisual(false);
    }

    void Awake()
    {
        rectTransform = transform as RectTransform;
        if (button == null)
            button = GetComponent<Button>();
        if (background == null)
            background = GetComponent<Image>();

        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        CacheBackgroundColor();
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);

        if (dragging && combatManager != null)
            combatManager.CancelCardDrag();
    }

    void LateUpdate()
    {
        if (combatManager == null || card == null)
            return;

        if (button != null)
            button.interactable = combatManager.CanPlayCard(card);

        if (background != null && hasBackgroundColor)
        {
            bool pending = combatManager.PendingCard == card;
            background.color = pending
                ? new Color(0.42f, 0.36f, 0.16f, 1f)
                : defaultBackgroundColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dragging)
            return;

        CacheRestPose();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
            layoutElement.preferredWidth = rectTransform.rect.width;
            layoutElement.preferredHeight = rectTransform.rect.height;
        }

        ApplyHoverVisual(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dragging)
            return;

        if (layoutElement != null)
            layoutElement.ignoreLayout = false;

        ApplyHoverVisual(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (combatManager == null || card == null || !combatManager.CanPlayCard(card))
            return;

        if (!combatManager.BeginCardDrag(card))
            return;

        dragging = true;
        CacheRestPose();

        handParent = transform.parent;
        handSiblingIndex = transform.GetSiblingIndex();
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
            transform.SetParent(rootCanvas.transform, true);

        if (layoutElement != null)
            layoutElement.ignoreLayout = true;
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        transform.SetAsLastSibling();
        ApplyHoverVisual(true);
        FollowPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        FollowPointer(eventData);
        if (combatManager != null)
            combatManager.UpdateCardDragHover(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;
        IEffectTarget target = combatManager != null
            ? combatManager.FindTargetAtScreen(eventData.position)
            : null;

        bool played = combatManager != null
            && combatManager.TryConfirmCardDrag(card, target, GetDragLiftAmount(), playLiftThreshold);

        RestoreToHand();
        if (layoutElement != null)
            layoutElement.ignoreLayout = false;
        ApplyHoverVisual(false);

        if (!played && combatManager != null)
            combatManager.CancelCardDrag();
    }

    void OnClicked()
    {
        // Drag is the primary play path; click quick-plays non-enemy cards only.
        if (dragging || combatManager == null || card == null || card.Data == null)
            return;

        if (card.Data.TargetType == CardTargetType.Enemy)
            return;

        combatManager.BeginPlay(card);
    }

    void FollowPointer(PointerEventData eventData)
    {
        if (rectTransform == null)
            return;

        Canvas canvas = rootCanvas != null ? rootCanvas : GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                cam,
                out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    float GetDragLiftAmount()
    {
        if (!restCached || rectTransform == null || handParent == null)
            return 0f;

        Vector3 worldRest = handParent.TransformPoint(new Vector3(restAnchoredPosition.x, restAnchoredPosition.y, 0f));
        return rectTransform.position.y - worldRest.y;
    }

    void RestoreToHand()
    {
        if (handParent != null)
        {
            transform.SetParent(handParent, false);
            transform.SetSiblingIndex(handSiblingIndex);
        }

        if (layoutElement != null)
            layoutElement.ignoreLayout = false;
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        if (restCached && rectTransform != null)
        {
            rectTransform.anchoredPosition = restAnchoredPosition;
            rectTransform.localScale = restLocalScale;
        }
    }

    void CacheRestPose()
    {
        if (dragging)
            return;

        if (rectTransform == null)
            rectTransform = transform as RectTransform;
        if (rectTransform == null)
            return;

        restAnchoredPosition = rectTransform.anchoredPosition;
        restLocalScale = Vector3.one;
        restCached = true;
    }

    void ApplyHoverVisual(bool enabled)
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;
        if (rectTransform == null || !restCached)
            return;

        if (enabled)
        {
            rectTransform.localScale = restLocalScale * hoverScale;
            if (!dragging)
                rectTransform.anchoredPosition = restAnchoredPosition + new Vector2(0f, hoverLift);
        }
        else if (!dragging)
        {
            rectTransform.localScale = restLocalScale;
            rectTransform.anchoredPosition = restAnchoredPosition;
        }
    }

    void CacheBackgroundColor()
    {
        if (background == null || hasBackgroundColor)
            return;

        defaultBackgroundColor = background.color;
        hasBackgroundColor = true;
    }
}
