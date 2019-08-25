using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonModel
{
    public DragonModel(Vector3 initPosition)
    {
        Position = initPosition;
    }

    #region Configurable variables

    /// <summary>
    /// Minimum distance starting from which dragon will try to run away from the player.
    /// </summary>
    public int MinPlayerDistance { get; set; } = 10;

    /// <summary>
    /// Dragon's speed.
    /// </summary>
    public int Speed { get; set; } = 1;
    #endregion

    #region Coordinates
    private Vector3 _position;
    public Vector3 Position
    {
        get { return _position; }
        private set
        {
            _position = value;
            OnPositionChanged?.Invoke(_position);
        }
    }

    public event Action<Vector3> OnPositionChanged;
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
    /// <param name="position">TODO change to unity position?</param>
    public void TrackPlayerPosition(Vector3 playerPosition)
    {
        if (CalculateDistance(playerPosition, Position) < MinPlayerDistance)
        {
            RunAway();
        }
        else
        {
            Idel();
        }
    }

    private void Idel()
    {
        ActionState = ActionStates.Idel;
    }

    private void RunAway()
    {
        ActionState = ActionStates.Fly;
        var newPosition = new Vector3(_position.x + 2 * Speed * Time.deltaTime, _position.y, _position.z + 2 * Speed * Time.deltaTime);
        Position = newPosition;
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
