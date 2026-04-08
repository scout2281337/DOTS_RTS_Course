using System;
using System.Collections.Generic;
using UnityEngine;

public class Presenter : Singleton<Presenter>
{
    public readonly List<Action> OnAbilityPress = new();
    private event Action OnEscBuffer;


    public void InvokeEscBuffer()
    {
        OnEscBuffer?.Invoke();
        Debug.Log("InvokeEscBuffer \n" +
            OnEscBuffer);
    }

    public void InvokeAbilityPress(int i)
    {
        OnAbilityPress[i]?.Invoke();
        Debug.Log("InvokeAbilityPress \n" +
            i +
            OnAbilityPress[i]);
    }
}
