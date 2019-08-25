using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public enum DragonAnimationTriggers
{
    Idel,
    Fly
}

public class DragonAnimation : MonoBehaviour
{
    private Animator _animator;
    private DragonModel _dragon;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _dragon = GetComponent<DragonAI>().Dragon;

        _dragon.OnActionStateChanged += OnActionStateChanged;
    }

    private void OnActionStateChanged(ActionStates state)
    {
        switch(state)
        {
            case ActionStates.Idel:
                if (!_animator.GetBool(DragonAnimationTriggers.Idel.ToString()))
                {
                    _animator.SetTrigger(DragonAnimationTriggers.Idel.ToString());
                }
                break;
            case ActionStates.Fly:
                if(!_animator.GetBool(DragonAnimationTriggers.Fly.ToString()))
                {
                    _animator.SetTrigger(DragonAnimationTriggers.Fly.ToString());
                }
                break;
        }
    }

}
