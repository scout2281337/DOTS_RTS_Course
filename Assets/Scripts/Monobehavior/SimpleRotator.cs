using UnityEngine;

public class SimpleRotator : MonoBehaviour
{
    [SerializeField] private Vector3 rotationSpeed = new(0f, 90f, 0f);
    [SerializeField] private Space rotationSpace = Space.Self;
    [SerializeField] private bool useUnscaledTime;

    private void Update()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        transform.Rotate(rotationSpeed * deltaTime, rotationSpace);
    }
}