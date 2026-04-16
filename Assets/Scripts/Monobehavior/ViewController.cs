using UnityEngine;
using UnityEngine.InputSystem;

public class ViewController : Singleton<ViewController>
{
    public StyleSheetsSO DefaultStyleSheet;
    public BaseUITextures BaseTextures;
    public ColorSchemeSO ColorScheme;

    private GameControls _controls;
    private Presenter _presenter;

    protected override void Awake()
    {
        base.Awake();

        _controls = new GameControls();
        _presenter = Presenter.Instance;
    }

    private void OnEnable()
    {
        _controls.Player.Enable();

        _controls.Player.Ability1.performed += OnAbility1Performed;
        _controls.Player.Ability2.performed += OnAbility2Performed;
        _controls.Player.Ability3.performed += OnAbility3Performed;
        _controls.Player.Ability4.performed += OnAbility4Performed;
        _controls.Player.Cancel.performed += OnCancelPerformed;
    }

    private void OnDisable()
    {
        _controls.Player.Ability1.performed -= OnAbility1Performed;
        _controls.Player.Ability2.performed -= OnAbility2Performed;
        _controls.Player.Ability3.performed -= OnAbility3Performed;
        _controls.Player.Ability4.performed -= OnAbility4Performed;
        _controls.Player.Cancel.performed -= OnCancelPerformed;

        _controls.Player.Disable();
    }

    private void OnAbility1Performed(InputAction.CallbackContext context)
    {
        _presenter.InvokeAbilityPress(0);
    }

    private void OnAbility2Performed(InputAction.CallbackContext context)
    {
        _presenter.InvokeAbilityPress(1);
    }

    private void OnAbility3Performed(InputAction.CallbackContext context)
    {
        _presenter.InvokeAbilityPress(2);
    }

    private void OnAbility4Performed(InputAction.CallbackContext context)
    {
        _presenter.InvokeAbilityPress(3);
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        _presenter.InvokeEscBuffer();
    }
}
