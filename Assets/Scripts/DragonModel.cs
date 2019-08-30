using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonModel
{
    public DragonModel()
    {
    }

    #region Configurable variables

    /// <summary>
    /// Minimum distance starting from which dragon will try to run away from the player.
    /// </summary>
    public int MinPlayerDistance { get; set; } = 5;

    /// <summary>
    /// Dragon's speed.
    /// </summary>
    public float Speed { get; set; } = 0.3f;

    /// <summary>
    /// Radius of dragons route.
    /// </summary>
    private float Radius = 2f;
    #endregion

    #region Coordinates
    private bool _initPositionWasSet = false;
    private Vector3 _center = new Vector3(0, 0, 0);
    private float _angle;

    private Transform _transform;
    public Transform Transform
    {
        get { return _transform; }
        private set
        {
            _transform = value;
            OnTransformChanged?.Invoke(_transform);
        }
    }

    public event Action<Transform> OnTransformChanged;
    #endregion

    #region Dragon state machine
    private ActionStates _actionState = ActionStates.Idel;
    public ActionStates ActionState
    {
        get
        {
            return _actionState;
        }
        private set
        {
            if(_actionState != value)
            {
                _actionState = value;
                OnActionStateChanged?.Invoke(_actionState);
            }
        }
    }
    public event Action<ActionStates> OnActionStateChanged;
    #endregion

    #region Actions

    /// <summary>
    /// Tracks player position. If player too near, then fly/run away.
    /// </summary>
    /// <param name="position">Player(head) position</param>
    public void TrackPlayerPosition(Vector3 playerPosition)
    {
        var dist = CalculateDistance(playerPosition, Transform.position);
        Debug.Log("ASA log: distance" + dist);
        if (dist < MinPlayerDistance)
        {
            RunAway();
        }
        else
        {
            Idel();
        }
    }

    public void SetInitTransform(Transform initTransform)
    {
        if(!_initPositionWasSet)
        {
            _transform = initTransform;
            _center = initTransform.position;
            _initPositionWasSet = true;
        }
    }

    private void Idel()
    {
        ActionState = ActionStates.Idel;
    }

    /// <summary>
    /// Run via circle route. 
    /// </summary>
    private void RunAway()
    {
        ActionState = ActionStates.Fly;

        _angle += Speed * Time.deltaTime;
        var offset = new Vector3(Mathf.Cos(_angle), 0, Mathf.Sin(_angle)) * Radius;
        Transform.position = _center + offset;
        OnTransformChanged?.Invoke(_transform);
    }

    #endregion

    private float CalculateDistance(Vector3 first, Vector3 second)
    {
        return Vector3.Distance(first, second);
    }
}

public enum ActionStates
{
    Idel,
    Fly,
    Run
}
