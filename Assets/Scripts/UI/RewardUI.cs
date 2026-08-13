using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Card reward placeholders. Selection logic comes in a later phase.
/// </summary>
public class RewardUI : MonoBehaviour
{
    [SerializeField] Button cardChoice1Button;
    [SerializeField] Button cardChoice2Button;
    [SerializeField] Button cardChoice3Button;
    [SerializeField] Button continueButton;
    readonly CardData[] offered = new CardData[3];

    void Start()
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        OfferCards();
        BindChoice(cardChoice1Button, 0);
        BindChoice(cardChoice2Button, 1);
        BindChoice(cardChoice3Button, 2);
    }

void OfferCards()
    {
        List<CardData> pool = new List<CardData>();
        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        IReadOnlyList<CardData> source = run != null ? run.RewardPool : null;
        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                    pool.Add(source[i]);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            offered[i] = pool.Count == 0 ? null : pool[Random.Range(0, pool.Count)];
            if (offered[i] != null && pool.Count > 1)
                pool.Remove(offered[i]);
        }
    }

void BindChoice(Button button, int index)
    {
        if (button == null)
            return;

        bool hasCard = offered[index] != null;
        button.interactable = hasCard;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null && hasCard)
            label.text = offered[index].CardName + "\n" + offered[index].Cost + " energy\n" + offered[index].Description;

        int captured = index;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => Choose(captured));
    }

void Choose(int index)
    {
        if (offered[index] == null || GameManager.Instance == null)
            return;

        GameManager.Instance.NotifyRewardChosen(offered[index]);
    }




    void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    void SetChoiceInteractable(bool interactable)
    {
        if (cardChoice1Button != null) cardChoice1Button.interactable = interactable;
        if (cardChoice2Button != null) cardChoice2Button.interactable = interactable;
        if (cardChoice3Button != null) cardChoice3Button.interactable = interactable;
    }

    void OnContinueClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[RewardUI] GameManager is missing.");
            return;
        }

        GameManager.Instance.NotifyRewardChosen(null);
    }
}
