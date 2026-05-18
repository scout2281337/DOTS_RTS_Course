using UnityEngine;

public class WeaponAudioPresenter : MonoBehaviour
{
    [SerializeField] private AudioCueSO defaultShotCue;
    [SerializeField] private AudioCueSO explosiveShotCue;
    [SerializeField] private AudioCueSO dispersiveShotCue;

    private void Start()
    {
        EventMediator.Instance.OnBulletShot += OnBulletShot;
    }

    private void OnDestroy()
    {
        if (EventMediator.Instance != null)
            EventMediator.Instance.OnBulletShot -= OnBulletShot;
    }

    private void OnBulletShot(BulletShotEvent evt)
    {
        AudioCueSO cue = evt.WeaponType switch
        {
            WeaponType.Explosive => explosiveShotCue,
            WeaponType.Dispersive => dispersiveShotCue,
            _ => defaultShotCue
        };

        GameAudioManager.Instance.Play3D(cue, evt.Start);
    }
}