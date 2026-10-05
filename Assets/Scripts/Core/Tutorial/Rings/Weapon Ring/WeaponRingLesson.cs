using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/*
 * Script: WeaponRingLesson
 *
 * Description:
 * Ring 2. The player takes a 3 round pistol, shoots two holograms, dry fires,
 * throws the empty gun through a panel and takes the gun waiting behind it.
 *
 * Interacts With:
 * - TutorialServices (weapon adapter events and arming)
 * - TutorialPickups (the pedestal pistol and the gun behind the panel)
 * - HologramTarget, ThrowPanel (this ring's props)
 * - TutorialHintSystem (pistolTaken, hologramsDown, pistolReturned, dryFire, throwMissed, panelBroken)
 *
 * Notes:
 * - Picking up while holding makes WeaponManager throw the held gun in the same
 *   frame. A throw in a pickup's frame is a swap and is ignored.
 * - Thrown guns delete themselves a second after landing, so whenever the player
 *   is left with no way to go on, a fresh pistol reappears on the pedestal.
 */
public class WeaponRingLesson : TutorialLesson
{
    // where the lesson is up to
    public enum Step
    {
        TakePistol,
        ShootTargets,
        DryFire,
        ThrowAtPanel,
        TakeNewGun,
        Done
    }

    [Header("Guns")]

    [Tooltip("The starter pistol")]
    [SerializeField] private WeaponStats pistol;

    [Tooltip("Where the pistol sits and where a fresh one would appear")]
    [SerializeField] private Transform pedestalPoint;

    [Tooltip("The gun waiting behind the panel")]
    [SerializeField] private WeaponStats nextGun;

    [Tooltip("Where the next gun sits")]
    [SerializeField] private Transform nextGunPoint;

    [Tooltip("real seconds after a throw before a fresh pistol reappears if the panel is still up")]
    [SerializeField] private float missedThrowReturnSeconds = 3f;


    [Header("Props")]
    [Tooltip("The two hologams that are shot first")]
    [SerializeField] private HologramTarget[] holograms;

    [Tooltip("The panel in front of the next gun")]
    [SerializeField] private ThrowPanel panel;


    [Header("Events")]

    [Tooltip("fires on every step change")]
    public UnityEvent<Step> StepChanged = new UnityEvent<Step>();

    private Step step = Step.TakePistol;
    private int hologramsDown;
    private int lastPickupFrame = -1;
    private GameObject pedestalPickup;
    private GameObject nextGunModel;
    private Coroutine returnRoutine;
    private ITutorialWeapon weapon;

    public Step CurrentStep => step;

    public override TutorialStage Stage => TutorialStage.WeaponRing;

    private void OnEnable()
    {
        setPropsListening(true);
    }

    private void OnDisable()
    {
        setPropsListening(false);
        setWeaponListening(false);
    }

    public override void BeginLesson()
    {
        base.BeginLesson();
        setWeaponListening(true);

        if (weapon != null)
        {
            weapon.SetArmingAllowed(true);
        }

        hologramsDown = 0;
        foreach (HologramTarget hologram in holograms)
        {
            if (hologram != null)
            {
                hologram.ResetTarget();
            }
        }

        if (panel != null)
        {
            //panel.ResetPanel();
        }

        placePistol();
        placeNextGun();
        setStep(Step.TakePistol);
    }

    public override void EndLesson()
    {
        base.EndLesson();
        setWeaponListening(false);
        stopReturn();
    }

    public override void ResetLesson()
    {
        base.ResetLesson();

        if (Services !=null && Services.Weapon != null)
        {
            Services.Weapon.Disarm();
        }

        stopReturn();
    }

    private void handlePickedUp(GameObject pickup)
    {
        lastPickupFrame = Time.frameCount;

        if (step == Step.TakePistol && pickup == pedestalPickup)
        {
            setStep(Step.ShootTargets);
            RaiseHint("pistolTaken");
        }
        else if (nextGunModel != null && pickup == nextGunModel)
        {
            stopReturn();
            setStep(Step.Done);
            Complete();
        }
    }

    private void handleDryFired()
    {
        if (step == Step.DryFire)
        {
            setStep(Step.ThrowAtPanel);
            RaiseHint("dryFire");
            return;
        }

        placePistolIfNeeded();
    }

    private void handleThrown(GameObject thrownModel)
    {
        if (Time.frameCount == lastPickupFrame)
        {
            return;
        }

        if (step == Step.DryFire || step == Step.ThrowAtPanel)
        {
            setStep(Step.ThrowAtPanel);
            stopReturn();
            returnRoutine = StartCoroutine(ReturnAfterMiss());
            return;
        }

        placePistolIfNeeded();
    }

    private void handleHologramKilled(HologramTarget hologram)
    {
        if (!IsRunning || step != Step.ShootTargets)
        {
            return;
        }

        hologramsDown++;
        if (hologramsDown < holograms.Length)
        {
            return;
        }

        if (panel != null)
        {
            panel.ArmPanel();
        }

        setStep(Step.DryFire);
        RaiseHint("hologramsDown");
    }

    private void handlePanelBroken()
    {
        if (!IsRunning)
        {
            return;
        }

        stopReturn();

        TutorialPickups.SetPickup(nextGunModel, nextGun, -1, true);
        setStep(Step.TakeNewGun);
        RaiseHint("panelBroken");
    }

    private void placePistolIfNeeded()
    {
        bool isOutOfRounds = weapon != null && (!weapon.IsHoldingWeapon || weapon.CurrentAmmo <= 0);
        if (step==Step.ShootTargets&&isOutOfRounds&&pedestalPickup==null)
        {
            placePistol();
            RaiseHint("pistolReturned");
        }
    }

    private IEnumerator ReturnAfterMiss()
    {
        yield return new WaitForSecondsRealtime(missedThrowReturnSeconds);
        returnRoutine = null;

        bool isUnarmed = weapon != null && !weapon.IsHoldingWeapon;

        if (IsRunning && step == Step.ThrowAtPanel && isUnarmed && pedestalPickup == null)
        {
            placePistol();
            RaiseHint("throwMissed");
        }
    }

    private void placePistol()
    {
        if (pedestalPickup != null)
        {
            Destroy(pedestalPickup);
        }

        pedestalPickup = TutorialPickups.Spawn(pistol, pedestalPoint, -1);
    }

    private void placeNextGun()
    {
        if (nextGunModel!= null)
        {
            Destroy(nextGunModel);
        }

        nextGunModel=TutorialPickups.Spawn(nextGun, nextGunPoint, -1);
        TutorialPickups.SetPickup(nextGunModel, nextGun, -1, false);
    }

    private void setPropsListening(bool isListening)
    {
        foreach(HologramTarget hologram in holograms)
        {
            if (hologram == null)
            {
                continue;
            }

            hologram.Killed.RemoveListener(handleHologramKilled);
            if (isListening)
            {
                hologram.Killed.AddListener(handleHologramKilled);
            }
        }

        if (panel != null)
        {
            panel.Broken.RemoveListener(handlePanelBroken);
            if (isListening)
            {
                panel.Broken.AddListener(handlePanelBroken);
            }
        }
    }

    private void setWeaponListening(bool isListening)
    {
        if (weapon != null)
        {
            weapon.DryFired -= handleDryFired;
            weapon.Thrown -= handleThrown;
            weapon.PickedUp -= handlePickedUp;
            weapon = null;
        }

        if (isListening && Services != null && Services.Weapon != null)
        {
            weapon = Services.Weapon;
            weapon.DryFired += handleDryFired;
            weapon.Thrown += handleThrown;
            weapon.PickedUp += handlePickedUp;
        }
    }

    private void stopReturn()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }
    }

    private void setStep(Step next)
    {
        step = next;
        StepChanged.Invoke(next);
    }
}   
