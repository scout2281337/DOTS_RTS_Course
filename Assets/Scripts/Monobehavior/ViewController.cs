using UnityEngine;
using UnityEngine.InputSystem;

public class ViewController : MonoBehaviour
{
    public StyleSheetsSO defaultStyleSheet;
    public BaseUITextures baseTextures;
    public ColorSchemeSO colorScheme;

    private GameControls controls;
    private Presenter presenter;

    private void Awake()
    {
        controls = new GameControls();
        presenter = Presenter.Instance;
    }

    private void OnEnable()
    {
        controls.Player.Enable();

        controls.Player.Ability1.performed += OnAbility1Performed;
        controls.Player.Ability2.performed += OnAbility2Performed;
        controls.Player.Ability3.performed += OnAbility3Performed;
        controls.Player.Ability4.performed += OnAbility4Performed;
        controls.Player.Cancel.performed += OnCancelPerformed;
    }

    private void OnDisable()
    {
        controls.Player.Ability1.performed -= OnAbility1Performed;
        controls.Player.Ability2.performed -= OnAbility2Performed;
        controls.Player.Ability3.performed -= OnAbility3Performed;
        controls.Player.Ability4.performed -= OnAbility4Performed;
        controls.Player.Cancel.performed -= OnCancelPerformed;

        controls.Player.Disable();
    }

    private void OnAbility1Performed(InputAction.CallbackContext context)
    {
        presenter.InvokeAbilityPress(0);
    }

    private void OnAbility2Performed(InputAction.CallbackContext context)
    {
        presenter.InvokeAbilityPress(1);
    }

    private void OnAbility3Performed(InputAction.CallbackContext context)
    {
        presenter.InvokeAbilityPress(2);
    }

    private void OnAbility4Performed(InputAction.CallbackContext context)
    {
        presenter.InvokeAbilityPress(3);
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        presenter.InvokeEscBuffer();
    }
}
