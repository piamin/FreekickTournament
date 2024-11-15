using UnityEngine;
using System.Collections;

public class DemoShoot : MonoBehaviour
{
    public static DemoShoot share;

    private bool _isWallKick;

    public bool IsWallKick
    {
        get { return _isWallKick; }
        set
        {
            _isWallKick = value;
            Wall.share.IsWall = _isWallKick;
            if (_isWallKick)
            {
                Wall.share.setWall(Shoot.share._ball.transform.position);
            }
        }
    }

    [SerializeField] private int initialGKLevel = 0;

    void Awake()
    {
        share = this;
    }

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        GoalKeeperLevel.share.setLevel(initialGKLevel);
        IsWallKick = true;
    }
 

    public void Reset(bool shouldRandomNewPos)
    {
        // ShootAI reset logic must be called first, to reset new ball poosition, reset() method of other components must come after this
        if(shouldRandomNewPos)
            ShootAI.shareAI.reset();                // used this method to reset new randomised ball's position
        else
            ShootAI.shareAI.reset(ShootAI.shareAI.BallPositionX, ShootAI.shareAI.BallPositionZ);        // call like this to reset new turn with current ball position

        SlowMotion.share.reset();                   // reset the slowmotion logic

        GoalKeeperHorizontalFly.share.reset();      // reset goalkeeperhorizontalfly logic
        GoalKeeper.share.reset();                   // reset goalkeeper logic
        GoalDetermine.share.reset();                // reset goaldetermine logic so that it's ready to detect new goal

        if (Wall.share != null)                     // if there is wall in this scene
        {
            Wall.share.IsWall = IsWallKick;         // set is wall state
            if (IsWallKick)                         // if we want wall kick
                Wall.share.setWall(Shoot.share._ball.transform.position);       // set wall position with respect to ball position
        }

        CameraManager.share.reset();                // reset camera position
    }

    public void OnClick_NewTurnRandomPosition()
    {
        Reset(true);
    }

    public void OnClick_NewTurnSamePosition()
    {
        Reset(false);
    }
}
