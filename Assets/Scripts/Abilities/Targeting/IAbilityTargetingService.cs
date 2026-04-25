public interface IAbilityTargetingService
{
    bool IsTargeting { get; }
    UnitClass ActiveUnitClass { get; }

    bool StartTargeting(UnitClass unitClass);
    void CancelTargeting();
}
