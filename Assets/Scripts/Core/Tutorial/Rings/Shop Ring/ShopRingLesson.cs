using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

/*
 * Script: ShopRingLesson
 *
 * Description:
 * Ring 3. Clearing three holograms pays the Byte grant and opens the shop popup,
 * the same way it opens between waves. The player buys one thing and closes it
 * with Q. Closing without buying opens the popup again.
 *
 * Interacts With:
 * - HologramTarget (the mini round)
 * - TutorialShopConfig (the grant and the stock)
 * - TutorialServices (the shop adapter)
 * - TutorialHintSystem (roundCleared, purchased, closedWithoutBuying)
 *
 * Notes:
 * - The shop adapter runs its own copy of the between-wave shop, with the same
 *   freeze, Q to close and pause behavior, so tutorial buys never touch upgrade tiers.
 */

public class ShopRingLesson : TutorialLesson
{
    public enum Step
    {
        ClearRound,
        BuyOne,
        CloseShop,
        Done
    }

    [Header("Round")]
    [Tooltip("the holograms that make up the mini round")]
    [SerializeField] private HologramTarget[] targets;

    [Header("Shop")]
    [Tooltip("the Byte grant and what the tutorial shop sells")]
    [SerializeField] private TutorialShopConfig stock;

    [Tooltip("the player has to buy something before closing counts")]
    [SerializeField] private bool isPurchaseRequired = true;

    [Header("Events")]
    [Tooltip("fires on every step change")]
    public UnityEvent<Step> StepChanged = new UnityEvent<Step>();

    [Tooltip("fires when the player closes the shop without buying")]
    public UnityEvent ClosedWithoutBuying = new UnityEvent();

    private Step step = Step.ClearRound;
    private int targetsDown;
    private bool hasPurchased;
    private ITutorialShop shop;

    public Step CurrentStep => step;

    public override TutorialStage Stage => TutorialStage.ShopRing;

    private void OnEnable()
    {
        setTargetsListening(true);
    }

    private void OnDisable()
    {
        setTargetsListening(false);
        setShopListening(false);
    }

    public override void BeginLesson()
    {
        base.BeginLesson();
        setShopListening(true);

        if (shop!= null)
        {
            shop.SetStock(stock);
        }

        targetsDown = 0;
        hasPurchased = false;
        foreach (HologramTarget target in targets)
        {
            if (targets != null)
            {
                target.ResetTarget();
            }
        }

        setStep(Step.ClearRound);
    }

    public override void EndLesson()
    {
        base.EndLesson();
        setShopListening(false);
    }

    public override void ResetLesson()
    {
        base.ResetLesson();

        if (shop!= null)
        {
            shop.ClearTutorialState();
        }
    }

    private void handleTargetKilled(HologramTarget target)
    {
        if (!IsRunning || step != Step.ClearRound)
        {
            return;
        }

        targetsDown++;
        if (targetsDown < targets.Length)
        {
            return;
        }

        if (shop != null)
        {
            if (stock != null)
            {
                shop.GrantBytes(stock.ByteGrant);
            }

            shop.OpenShop();
        }

        setStep(Step.BuyOne);
        RaiseHint("roundCleared");
    }

    private void handlePurchased(string itemId)
    {
        hasPurchased = true;
        setStep(Step.CloseShop);
        RaiseHint("purchased");
    }

    private void handleShopClosed()
    {
        if (step == Step.ClearRound || step == Step.Done)
        {
            return;
        }

        if (isPurchaseRequired && !hasPurchased)
        {
            ClosedWithoutBuying.Invoke();
            RaiseHint("closedWithoutBuying");
            shop.OpenShop();
            return;
        }

        setStep(Step.Done);
        Complete();
    }

    private void setTargetsListening(bool isListening)
    {
        foreach (HologramTarget target in targets)
        {
            if (target == null)
            {
                continue;
            }

            target.Killed.RemoveListener(handleTargetKilled);
            if (isListening)
            {
                target.Killed.AddListener(handleTargetKilled);
            }
        }
    }

    private void setShopListening(bool isListening)
    {
        if (shop != null)
        {
            shop.Closed -= handleShopClosed;
            shop.Purchased -= handlePurchased;
            shop = null;
        }

        if (isListening && Services != null && Services.Shop != null)
        {
            shop = Services.Shop;
            shop.Closed += handleShopClosed;
            shop.Purchased += handlePurchased;
        }
    }

    private void setStep(Step next)
    {
        step = next;
        StepChanged.Invoke(step);
    }
}
