using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Layout root stays in the hand. Visual child handles hover/drag motion.
/// </summary>
public class UICard : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerMoveHandler
{
    [Header("Content")]
    [SerializeField] RectTransform visual;
    [SerializeField] TextMeshProUGUI cardNameText;
    [SerializeField] TextMeshProUGUI cardDescriptionText;
    [SerializeField] TextMeshProUGUI cardEnergyCostText;
    [SerializeField] Image background;
    [SerializeField] Button button;

    [Header("Hover")]
    [SerializeField] float hoverScale = 1.14f;
    [SerializeField] float hoverLift = 42f;
    [SerializeField] float hoverTiltDegrees = 6f;

    [Header("Drag")]
    [SerializeField] float dragScale = 1.18f;
    [SerializeField] float dragTiltPerVelocity = 0.035f;
    [SerializeField] float maxDragTilt = 14f;
    [SerializeField] float playLiftThreshold = 100f;
    [SerializeField] float dragLiftLimit = 100f;
    [SerializeField] float dragFollowXFactor = 0.2f;
    [SerializeField] float pointerOriginOffsetY = 90f;

    [Header("Spring")]
    [SerializeField] float positionSpring = 18f;
    [SerializeField] float scaleSpring = 16f;
    [SerializeField] float tiltSpring = 14f;
    [SerializeField] float settleEpsilon = 0.6f;

    RuntimeCard card;
    CombatManager combatManager;
    Color defaultBackgroundColor;
    bool hasBackgroundColor;

    RectTransform rectTransform;
    Canvas rootCanvas;
    Camera eventCamera;
    Transform visualHomeParent;
    Canvas visualCanvas;
    int visualDefaultSortOrder;

    Vector2 visualRestLocalPos;
    Vector3 visualRestScale = Vector3.one;
    Vector2 targetVisualPos;
    Vector3 targetVisualScale = Vector3.one;
    float targetTiltZ;
    Vector2 lastPointerCanvasPos;
    Vector2 pointerVelocity;

    bool hovered;
    bool dragging;
    bool returning;
    bool exactFollow;
    bool pastDragLimit;

    LayoutElement layoutElement;
    CanvasGroup canvasGroup;

    public Button Button
    {
        get
        {
            if (button == null)
                button = GetComponent<Button>();
            return button;
        }
    }

    public void SetCard(CardData cardData)
    {
        if (cardNameText != null)
            cardNameText.text = cardData != null ? cardData.CardName : string.Empty;
        if (cardDescriptionText != null)
            cardDescriptionText.text = cardData != null ? cardData.Description : string.Empty;
        if (cardEnergyCostText != null)
            cardEnergyCostText.text = cardData != null ? cardData.Cost.ToString() : string.Empty;
    }

    public void BindOffer(string displayName, string description, string energyCost, string extraLine = null)
    {
        card = null;
        combatManager = null;

        if (cardNameText != null)
            cardNameText.text = displayName ?? string.Empty;

        string body = description ?? string.Empty;
        if (!string.IsNullOrEmpty(extraLine))
            body = string.IsNullOrEmpty(body) ? extraLine : body + "\n" + extraLine;
        if (cardDescriptionText != null)
            cardDescriptionText.text = body;

        if (cardEnergyCostText != null)
            cardEnergyCostText.text = energyCost ?? string.Empty;
    }

    public void Bind(RuntimeCard runtimeCard, CombatManager combat)
    {
        card = runtimeCard;
        combatManager = combat;
        if (cardNameText != null)
            cardNameText.text = runtimeCard != null ? runtimeCard.Name : string.Empty;
        if (cardDescriptionText != null)
            cardDescriptionText.text = runtimeCard != null ? runtimeCard.Description : string.Empty;
        if (cardEnergyCostText != null)
            cardEnergyCostText.text = runtimeCard != null ? runtimeCard.Cost.ToString() : string.Empty;

        EnsureVisual();
        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
            button.transition = Selectable.Transition.None;
        }

        if (background == null && visual != null)
            background = visual.GetComponentInChildren<Image>(true);

        CacheBackgroundColor();
        ResetVisualHome();
        hovered = false;
        dragging = false;
        returning = false;
        exactFollow = false;
        pastDragLimit = false;
        targetVisualPos = visualRestLocalPos;
        targetVisualScale = visualRestScale;
        targetTiltZ = 0f;
        ApplyVisualImmediate();
        SetVisualSortBoost(false);
    }

    void Awake()
    {
        rectTransform = transform as RectTransform;
        EnsureVisual();

        if (button == null)
            button = GetComponent<Button>();

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

        if (combatManager != null)
            combatManager.HideTargetPointer();
    }

    void Update()
    {
        if (visual == null)
            return;

        if (!dragging && !hovered && !returning)
            return;

        float dt = Time.unscaledDeltaTime;

        if (exactFollow)
            visual.anchoredPosition = targetVisualPos;
        else
        {
            visual.anchoredPosition = Vector2.Lerp(
                visual.anchoredPosition,
                targetVisualPos,
                1f - Mathf.Exp(-positionSpring * dt));
        }

        visual.localScale = Vector3.Lerp(
            visual.localScale,
            targetVisualScale,
            1f - Mathf.Exp(-scaleSpring * dt));

        float currentZ = NormalizeAngle(visual.localEulerAngles.z);
        float nextZ = Mathf.Lerp(currentZ, targetTiltZ, 1f - Mathf.Exp(-tiltSpring * dt));
        visual.localRotation = Quaternion.Euler(0f, 0f, nextZ);

        if (returning && !dragging && !hovered)
        {
            if (Vector2.Distance(visual.anchoredPosition, targetVisualPos) <= settleEpsilon
                && Mathf.Abs(NormalizeAngle(visual.localEulerAngles.z) - targetTiltZ) < 0.35f
                && (visual.localScale - targetVisualScale).sqrMagnitude < 0.0004f)
            {
                returning = false;
                ApplyVisualImmediate();
                SetVisualSortBoost(false);
            }
        }
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

        EnsureVisual();
        hovered = true;
        returning = false;
        exactFollow = false;
        CacheEventCamera(eventData);
        UpdateHoverTargets(eventData);
        SetVisualSortBoost(true);
        PlayCardSfxHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dragging)
            return;

        hovered = false;
        returning = true;
        exactFollow = false;
        targetVisualPos = visualRestLocalPos;
        targetVisualScale = visualRestScale;
        targetTiltZ = 0f;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!hovered || dragging)
            return;

        UpdateHoverTargets(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (combatManager == null || card == null || !combatManager.CanPlayCard(card))
            return;

        if (!combatManager.BeginCardDrag(card))
            return;

        EnsureVisual();
        dragging = true;
        PlayCardSfxGrab();
        hovered = false;
        returning = false;
        exactFollow = true;
        pastDragLimit = false;
        CacheEventCamera(eventData);
        rootCanvas = GetComponentInParent<Canvas>();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        // Detach visual only — root slot stays in HorizontalLayoutGroup.
        if (rootCanvas != null)
            visual.SetParent(rootCanvas.transform, true);

        visual.SetAsLastSibling();
        SetVisualSortBoost(true);

        if (TryGetCanvasLocalPoint(eventData.position, out Vector2 pointerLocal))
        {
            lastPointerCanvasPos = pointerLocal;
            pointerVelocity = Vector2.zero;
            Vector2 clamped = ClampDragPosition(pointerLocal, out pastDragLimit);
            targetVisualPos = clamped;
            visual.anchoredPosition = clamped;
            UpdateTargetPointer(pointerLocal, pastDragLimit);
        }

        targetVisualScale = visualRestScale * dragScale;
        targetTiltZ = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || visual == null)
            return;

        if (!TryGetCanvasLocalPoint(eventData.position, out Vector2 pointerLocal))
            return;

        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        pointerVelocity = (pointerLocal - lastPointerCanvasPos) / dt;
        lastPointerCanvasPos = pointerLocal;

        Vector2 clamped = ClampDragPosition(pointerLocal, out pastDragLimit);
        targetVisualPos = clamped;
        visual.anchoredPosition = clamped;
        targetVisualScale = visualRestScale * dragScale;
        targetTiltZ = pastDragLimit
            ? 0f
            : Mathf.Clamp(-pointerVelocity.x * dragTiltPerVelocity, -maxDragTilt, maxDragTilt);

        UpdateTargetPointer(pointerLocal, pastDragLimit);

        if (combatManager != null)
            combatManager.UpdateCardDragHover(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;
        exactFollow = false;

        IEffectTarget target = combatManager != null
            ? combatManager.FindTargetAtScreen(eventData.position)
            : null;

        float liftForPlay = pastDragLimit
            ? Mathf.Max(GetDragLiftAmount(), playLiftThreshold)
            : GetDragLiftAmount();

        bool played = combatManager != null
            && combatManager.TryConfirmCardDrag(card, target, liftForPlay, playLiftThreshold);

        pastDragLimit = false;
        if (combatManager != null)
            combatManager.HideTargetPointer();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        if (played)
        {
            PlayCardSfxPlay();
            // Hide immediately; hand refresh will destroy the slot shortly after.
            if (visual != null)
            {
                if (visualHomeParent != null)
                    visual.SetParent(visualHomeParent, false);
                visual.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
            return;
        }

        PlayCardSfxCancel();
        BeginReturnVisualHome();
        if (combatManager != null)
            combatManager.CancelCardDrag();
    }

    void OnClicked()
    {
        if (dragging || combatManager == null || card == null || card.Data == null)
            return;

        if (card.Data.TargetType == CardTargetType.Enemy)
            return;

        combatManager.BeginPlay(card);
    }

    void UpdateHoverTargets(PointerEventData eventData)
    {
        targetVisualPos = visualRestLocalPos + new Vector2(0f, hoverLift);
        targetVisualScale = visualRestScale * hoverScale;

        if (TryGetPointerInVisualParent(eventData.position, out Vector2 localPointer))
        {
            Vector2 delta = localPointer - targetVisualPos;
            targetTiltZ = Mathf.Clamp(-delta.x / 80f * hoverTiltDegrees, -hoverTiltDegrees, hoverTiltDegrees);
        }
        else
        {
            targetTiltZ = 0f;
        }
    }

    void BeginReturnVisualHome()
    {
        EnsureVisual();
        if (visualHomeParent == null)
            visualHomeParent = transform;

        if (combatManager != null)
            combatManager.HideTargetPointer();

        // Keep world pose, then spring back to local rest under the slot.
        visual.SetParent(visualHomeParent, true);
        visual.SetAsLastSibling();
        returning = true;
        hovered = false;
        exactFollow = false;
        pastDragLimit = false;
        targetVisualPos = visualRestLocalPos;
        targetVisualScale = visualRestScale;
        targetTiltZ = 0f;
    }

    Vector2 ClampDragPosition(Vector2 pointerCanvasLocal, out bool overLimit)
    {
        Vector2 slotCanvas = GetSlotCanvasPosition();
        float maxY = slotCanvas.y + dragLiftLimit;
        overLimit = pointerCanvasLocal.y > maxY;

        if (!overLimit)
            return pointerCanvasLocal;

        // Lock near the play line so the card does not cover battlefield targets.
        float x = Mathf.Lerp(slotCanvas.x, pointerCanvasLocal.x, dragFollowXFactor);
        return new Vector2(x, maxY);
    }

    void UpdateTargetPointer(Vector2 pointerCanvasLocal, bool overLimit)
    {
        if (combatManager == null || rootCanvas == null)
            return;

        bool needsPointer = overLimit
            && card != null
            && card.Data != null
            && card.Data.TargetType == CardTargetType.Enemy;

        if (!needsPointer)
        {
            combatManager.HideTargetPointer();
            return;
        }

        Vector2 from = visual != null
            ? visual.anchoredPosition + new Vector2(0f, pointerOriginOffsetY)
            : GetSlotCanvasPosition() + new Vector2(0f, pointerOriginOffsetY);

        combatManager.SetTargetPointer(rootCanvas.transform, from, pointerCanvasLocal, true);
    }

    Vector2 GetSlotCanvasPosition()
    {
        Canvas canvas = rootCanvas != null ? rootCanvas : GetComponentInParent<Canvas>();
        if (canvas == null || rectTransform == null)
            return Vector2.zero;

        return canvas.transform.InverseTransformPoint(rectTransform.position);
    }

    void ResetVisualHome()
    {
        EnsureVisual();
        if (visual == null)
            return;

        if (visual.parent != transform)
            visual.SetParent(transform, false);

        visualHomeParent = transform;
        visual.anchorMin = new Vector2(0.5f, 0.5f);
        visual.anchorMax = new Vector2(0.5f, 0.5f);
        visual.pivot = new Vector2(0.5f, 0.5f);
        visual.sizeDelta = rectTransform != null ? rectTransform.rect.size : visual.sizeDelta;
        visualRestLocalPos = Vector2.zero;
        visualRestScale = Vector3.one;
        visual.anchoredPosition = visualRestLocalPos;
        visual.localScale = visualRestScale;
        visual.localRotation = Quaternion.identity;
    }

    void EnsureVisual()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (visual != null)
        {
            EnsureVisualCanvas();
            return;
        }

        Transform existing = transform.Find("Visual");
        if (existing != null)
        {
            visual = existing as RectTransform;
            EnsureVisualCanvas();
            return;
        }

        GameObject visualGo = new GameObject("Visual", typeof(RectTransform));
        visual = visualGo.GetComponent<RectTransform>();
        visual.SetParent(transform, false);
        visual.anchorMin = new Vector2(0.5f, 0.5f);
        visual.anchorMax = new Vector2(0.5f, 0.5f);
        visual.pivot = new Vector2(0.5f, 0.5f);
        visual.anchoredPosition = Vector2.zero;
        visual.localScale = Vector3.one;
        if (rectTransform != null)
            visual.sizeDelta = rectTransform.rect.size;

        // Move current visual children under the container (skip utilities).
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == visual)
                continue;
            child.SetParent(visual, true);
        }

        visualHomeParent = transform;
        EnsureVisualCanvas();
    }

    void EnsureVisualCanvas()
    {
        if (visual == null)
            return;

        visualCanvas = visual.GetComponent<Canvas>();
        if (visualCanvas == null)
            visualCanvas = visual.gameObject.AddComponent<Canvas>();

        visualDefaultSortOrder = visualCanvas.sortingOrder;
        if (visual.GetComponent<GraphicRaycaster>() == null)
            visual.gameObject.AddComponent<GraphicRaycaster>();
    }

    void SetVisualSortBoost(bool boosted)
    {
        if (visualCanvas == null)
            EnsureVisualCanvas();
        if (visualCanvas == null)
            return;

        visualCanvas.overrideSorting = boosted;
        visualCanvas.sortingOrder = boosted ? visualDefaultSortOrder + 100 : visualDefaultSortOrder;
    }

    void ApplyVisualImmediate()
    {
        if (visual == null)
            return;

        visual.anchoredPosition = targetVisualPos;
        visual.localScale = targetVisualScale;
        visual.localRotation = Quaternion.Euler(0f, 0f, targetTiltZ);
    }

    bool TryGetCanvasLocalPoint(Vector2 screenPosition, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        Canvas canvas = rootCanvas != null ? rootCanvas : GetComponentInParent<Canvas>();
        if (canvas == null)
            return false;

        rootCanvas = canvas;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPosition,
            cam,
            out localPoint);
    }

    bool TryGetPointerInVisualParent(Vector2 screenPosition, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (visual == null || visual.parent is not RectTransform parentRect)
            return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? eventCamera : null;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPosition,
            cam,
            out localPoint);
    }

    void CacheEventCamera(PointerEventData eventData)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : canvas.worldCamera;
        else
            eventCamera = null;
    }

    float GetDragLiftAmount()
    {
        if (visual == null || rectTransform == null)
            return 0f;

        Canvas canvas = rootCanvas != null ? rootCanvas : GetComponentInParent<Canvas>();
        if (canvas == null)
            return visual.anchoredPosition.y;

        Vector3 slotCanvas = canvas.transform.InverseTransformPoint(rectTransform.position);
        return visual.anchoredPosition.y - slotCanvas.y;
    }

    static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    void CacheBackgroundColor()
    {
        if (background == null || hasBackgroundColor)
            return;

        defaultBackgroundColor = background.color;
        hasBackgroundColor = true;
    }

    static void PlayCardSfxHover()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCardHover();
    }

    static void PlayCardSfxGrab()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCardGrab();
    }

    static void PlayCardSfxPlay()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCardPlay();
    }

    static void PlayCardSfxCancel()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCardCancel();
    }
}
