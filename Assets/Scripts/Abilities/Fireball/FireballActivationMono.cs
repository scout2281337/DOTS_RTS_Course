using UnityEngine;

// Legacy adapter kept for backwards compatibility with existing scene/UI bindings.
public class FireballActivationMono : MonoBehaviour
{
    public bool AbilityUseMode = false;

    private void Update()
    {
        if (!AbilityUseMode)
            return;

        if (AbilityTargetingServiceMono.Instance != null && AbilityTargetingServiceMono.Instance.StartTargeting(UnitClass.Arsonist))
        {
            AbilityUseMode = false;
        }
    }
}
