// the tutorial's spaces from the inside out, the order matches TutorialManager's ring arrays
public enum TutorialStage
{
    // starting cell, time moves when you move
    TimeCell,

    // ring 1, bpm is health
    BpmRing,

    // ring 2, one gun, no reload, throw to swap
    WeaponRing,

    // ring 3, bytes and the shop
    ShopRing,

    // every boundary is gone
    Finished
}
