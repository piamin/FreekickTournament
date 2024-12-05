using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
using TMPro;
using Holoville.HOTween;
using Unity.VisualScripting;


public class Shoot : MonoBehaviour
{
    public int maxShots = 15;  // 기본값은 15
    private int currentShots = 0;
    public int score = 0;  // 현재 스코어
    public GameObject resultPanel;  // 총 스코어를 표시할 패널
    public TextMeshProUGUI scoreText;  // 총 스코어를 표시할 텍스트
    public TextMeshProUGUI shotsText;  // 현재 남은 샷 수를 표시할 텍스트
    public TextMeshProUGUI currentScoreText;  // 현재 스코어를 표시할 텍스트
    public GameObject countdownPrefab;  // 카운트다운 애니메이션 프리팹

    public TMP_Text textNickname; // 타이틀 화면에 표시되는 닉네임 텍스트


    private bool isGameOver = false;  // 게임 종료 상태 체크

    public static Shoot share;

    public static Action EventShoot = delegate { };
    public static Action<float> EventChangeSpeedZ = delegate { };
    public static Action<float> EventChangeBallZ = delegate { };
    public static Action<float> EventChangeBallX = delegate { };
    public static Action<float> EventChangeBallLimit = delegate { };
    public static Action<Collision> EventOnCollisionEnter = delegate { };
    public static Action EventDidPrepareNewTurn = delegate { };


    public float _ballControlLimit;

    public Transform _goalKeeper;
    public Transform _ballTarget;
    protected Vector3 beginPos;
    protected bool _isShoot = false;

    public float minDistance = 1;     // 40f


    public Rigidbody _ball;
    public float factorUp = 0.012f;             // 10f
    public float factorDown = 0.003f;           // 1f
    public float factorLeftRight = 0.025f;		// 2f
    public float factorLeftRightMultiply = 0.8f;        // 2f
    public float _zVelocity = 24f;

    public AnimationCurve _curve;
    protected Camera _mainCam;

    protected float factorUpConstant = 0.017f * 960f;   // 0.015f * 960f;
    protected float factorDownConstant = 0.006f * 960f; // 0.005f * 960f;
    protected float factorLeftRightConstant = 0.0235f * 640f; // 0.03f * 640f; // 0.03f * 640f;

    public Transform _ballShadow;


    public float _speedMin = 18f;   // 20f;
    public float _speedMax = 30f;   // 36f;

    public float _distanceMinZ = 16.5f;
    public float _distanceMaxZ = 35f;

    public float _distanceMinX = -25f;
    public float _distanceMaxX = 25f;

    public bool _canShoot = false;
    public bool _canControlBall = false;

    public Transform _cachedTrans;

    public bool _enableTouch = false;
    public float screenWidth;
    public float screenHeight;

    Vector3 _prePos, _curPos;
    public float angle;
    protected ScreenOrientation orientation;

    protected Transform _ballParent;

    protected RaycastHit _hit;
    public bool _isInTutorial = false;
    public Vector3 ballVelocity;

    private float _ballPostitionZ = -22f;
    private float _ballPostitionX = 0f;

    public float BallPositionZ
    {
        get { return _ballPostitionZ; }
        set { _ballPostitionZ = value; }
    }

    public float BallPositionX
    {
        get { return _ballPostitionX; }
        set { _ballPostitionX = value; }
    }
    public TrailRenderer _effect;

    public FingerTrail fingerTrail;
    private float startTime = 0f;
    private bool shootTriggered = false;

    protected virtual void Awake()
    {
        share = this;
        _cachedTrans = transform;
        _canShoot = true;
        _ballParent = _ball.transform.parent;

        _distanceMinX = -15f;
        _distanceMaxX = 15f;
        _distanceMaxZ = 30f;

    }

    // Use this for initialization
    protected virtual void Start()
    {
        Application.targetFrameRate = 60;
        _mainCam = CameraManager.share._cameraMainComponent;
        resultPanel.SetActive(false);
        UpdateUI();

        textNickname.text = PlayerPrefs.GetString("UserName", "Player"); // 기존 닉네임을 입력 필드에 설정


        // 카운트다운 애니메이션 시작
        if (countdownPrefab != null)
        {
            StartCoroutine(PlayCountdownAnimation());
        }

#if UNITY_WP8 || UNITY_ANDROID
        Time.maximumDeltaTime = 0.2f;
        Time.fixedDeltaTime = 0.01f;
#else
		Time.maximumDeltaTime = 0.1f;
		Time.fixedDeltaTime = 0.01f;
#endif

        orientation = Screen.orientation;
        calculateFactors();

        _ballControlLimit = 6f;
        EventChangeBallLimit(_ballControlLimit);

        reset();
        CameraManager.share.reset();
        GoalKeeper.share.reset();

        GoalDetermine.EventFinishShoot += goalEvent;
    }

    private IEnumerator PlayCountdownAnimation()
    {
        GameObject countdownInstance = Instantiate(countdownPrefab);
        Animator animator = countdownInstance.GetComponent<Animator>();

        if (animator != null)
        {
            // 한 프레임을 기다려서 애니메이터가 초기화되도록 함
            yield return null;

            // 애니메이션의 클립 길이 가져오기
            float animationLength = animator.GetCurrentAnimatorClipInfo(0)[0].clip.length;

            yield return new WaitForSeconds(animationLength);
            Destroy(countdownInstance);
            countdownPrefab.SetActive(false);
        }
    }

    void OnDestroy()
    {
        GoalDetermine.EventFinishShoot -= goalEvent;
    }


    public virtual void goalEvent(bool isGoal, Area area)
    {
        if (isGameOver) return;  // 게임 종료 시 이벤트 무시

        _canControlBall = false;
        _canShoot = false;

        currentShots++;

        if (isGoal)
        {
            score += 1;  // 점수 증가
            Debug.Log("Goal! Current Score: " + score);
        }
        UpdateUI();

        if (currentShots >= maxShots)
        {
            isGameOver = true;  // 게임 종료 상태 설정
            ShowResult();
        }
    }

    private void UpdateUI()
    {
        // 첫 상태에서는 "KickOff" 표시
        if (currentShots == 0)
        {
            shotsText.text = "KickOff";
        }
        else
        {
            shotsText.text = "" + (maxShots - currentShots);  // 남은 샷 수 표시
        }

        currentScoreText.text = "" + score;  // 현재 스코어 표시
    }

    private void ShowResult()
    {
        StartCoroutine(ShowResultWithDelay());
    }

    private IEnumerator ShowResultWithDelay()
    {
        yield return new WaitForSeconds(2f);  // 2초 대기
        resultPanel.SetActive(true);
        scoreText.text = "" + score;

        SendGameResult();
    }

    private void SendGameResult()
    {
        // FirestoreManager 인스턴스 가져오기
        FirestoreManager firestoreManager = FindObjectOfType<FirestoreManager>();

        if (firestoreManager != null)
        {
            string playerID = PlayerPrefs.GetString("UserUUID", "Unknown");
            string nickname = PlayerPrefs.GetString("UserName", "Player");
            int startTime = PlayerPrefs.GetInt("startTime", 0); // 예: 경기 시작 시간을 기록해둔 값
            int endTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // 현재 시간을 Unix 시간으로 가져오기

            firestoreManager.SaveRankData(playerID, "GameMode", startTime, endTime, score);

            Debug.Log("Game result sent: Score = " + score + ", PlayerID = " + playerID + ", Nickname = " + nickname +
                      ", StartTime = " + startTime + ", EndTime = " + endTime);
        }
        else
        {
            Debug.LogError("FirestoreManager instance not found.");
        }
    }

    public void OnConfirmButtonClicked()
    {
        SceneManager.LoadScene("0.Welcome");
    }

    public void calculateFactors()
    {
        screenHeight = Screen.height;
        screenWidth = Screen.width;

        minDistance = (100 * screenHeight) / 960f;
        factorUp = factorUpConstant / screenHeight;
        factorDown = factorDownConstant / screenHeight;
        factorLeftRight = factorLeftRightConstant / screenWidth;

        if (fingerTrail)
            factorLeftRight *= 2f;

        Debug.Log("Orientation : " + orientation + "\t Screen height = " + screenHeight
            + "\t Screen width = " + screenWidth + "\t factorUp = " + factorUp + "\t factorDown = " + factorDown
            + "\t factorLeftRight = " + factorLeftRight + "\t minDistance = " + minDistance);
    }

    protected void LateUpdate()
    {
        if (screenHeight != Screen.height)
        {
            orientation = Screen.orientation;
            calculateFactors();
            CameraManager.share.reset();
        }
    }
    void FixedUpdate()
    {
        ballVelocity = _ball.velocity;
    }

    protected virtual void Update()
    {
        if (isGameOver) return;  // 게임 종료 시 입력 무시

        if (fingerTrail)
        {
            if (shootTriggered && _canShoot)
            {
                startTime += Time.deltaTime;
                fingerTrail.RemoveEventUntil(startTime);
                bool remain = fingerTrail.IsEventExists();
                if (remain)
                {
                    mouseMove(fingerTrail.GetMousePosition());
                }
                else
                {
                    mouseEnd();
                }
                if (_isShoot)
                {
                    Vector3 speed = _ballParent.InverseTransformDirection(_ball.velocity);
                    speed.z = _zVelocity;
                    _ball.velocity = _ballParent.TransformDirection(speed);
                }
            }
        }
        else if (_canShoot)
        {
            // neu banh chua vao luoi hoac trung thu mon, khung thanh thi banh duoc phep bay voi van toc dang co
            if (_enableTouch && !_isInTutorial)
            {
                if (Input.GetMouseButtonDown(0))
                {           // touch phase began
                    mouseBegin(Input.mousePosition);
                }
                else if (Input.GetMouseButton(0))
                {
                    mouseMove(Input.mousePosition);
                }
                else if (Input.GetMouseButtonUp(0))
                {   // touch ended
                    mouseEnd();
                }
            }
            if (_isShoot)
            {
                Vector3 speed = _ballParent.InverseTransformDirection(_ball.velocity);
                speed.z = _zVelocity;
                _ball.velocity = _ballParent.TransformDirection(speed);
            }
        }

        Vector3 pos = _ball.transform.position;
        pos.y = 0.015f;
        _ballShadow.position = pos;
    }


    public void mouseBegin(Vector3 pos)
    {
        _prePos = _curPos = pos;
        beginPos = _curPos;
    }

    public void mouseEnd()
    {
        if (_isShoot == true)
        {       // neu da sut' roi` thi ko cho dieu khien banh nua, tranh' truong` hop nguoi choi tao ra nhung cu sut ko the~ do~ noi~
                //_canControlBall = false;
        }
    }

    public void mouseMove(Vector3 pos)
    {
        if (_curPos != pos)
        {
            _prePos = _curPos;
            _curPos = pos;

            Vector3 distance = _curPos - beginPos;

            if (_isShoot == false)
            {
                if (distance.y > 0 && distance.magnitude >= minDistance)
                {
                    if (Physics.Raycast(_mainCam.ScreenPointToRay(_curPos), out _hit, 100f) && !_hit.transform.tag.Equals("Ball"))
                    {
                        _isShoot = true;

                        Vector3 point1 = _hit.point;        // contact point
                        point1.y = 0;
                        point1 = _ball.transform.InverseTransformPoint(point1);     // dua point1 ve he truc toa do cua ball, coi ball la goc toa do cho de~
                        point1 -= Vector3.zero;         // vector tao boi point va goc' toa do

                        Vector3 diff = point1;
                        diff.Normalize();               // normalized rat' quan trong khi tinh' goc

                        float angle = 90 - Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
                        float x = _zVelocity * Mathf.Tan(angle * Mathf.Deg2Rad);

                        _ball.velocity = _ballParent.TransformDirection(new Vector3(x, distance.y * factorUp, _zVelocity));
                        _ball.angularVelocity = new Vector3(0, x, 0f);

                        if (EventShoot != null)
                        {
                            EventShoot();
                        }
                    }
                }
            }
            else
            {
                if (_canControlBall == true)
                {
                    if (_cachedTrans.position.z < -_ballControlLimit)
                    {
                        distance = _curPos - _prePos;

                        Vector3 speed = _ballParent.InverseTransformDirection(_ball.velocity);
                        speed.y += distance.y * ((distance.y > 0) ? factorUp : factorDown);
                        speed.x += distance.x * factorLeftRight * factorLeftRightMultiply;
                        _ball.velocity = _ballParent.TransformDirection(speed);

                        speed = _ball.angularVelocity;
                        speed.y += distance.x * factorLeftRight;
                        _ball.angularVelocity = speed;
                    }
                    else
                    {
                        _canControlBall = false;
                    }
                }
            }
        }
    }

    public void ShootBall()
    {
        startTime = 0f;
        shootTriggered = true;
        mouseBegin(fingerTrail.GetMousePosition());

        Debug.Log("ShootBall");
    }

    protected void OnCollisionEnter(Collision other)
    {
        string tag = other.gameObject.tag;
        if (tag.Equals("Player") || tag.Equals("Obstacle") || tag.Equals("Net") || tag.Equals("Wall"))
        {   // banh trung thu mon hoac khung thanh hoac da vao luoi roi thi ko cho banh bay voi van toc nua, luc nay de~ cho physics engine tinh' toan' quy~ dao bay
            Debug.Log($"Collision with {tag}");

            _canShoot = false;

            if (tag.Equals("Net"))
            {
                _ball.velocity /= 3f;
            }
        }

        EventOnCollisionEnter(other);
    }

    private void enableEffect()
    {
        //		_effect.enabled = true;
        _effect.time = 1;
    }

    public virtual void reset()
    {
        reset(-Random.Range(_distanceMinX, _distanceMaxX), -Random.Range(_distanceMinZ, _distanceMaxZ));

    }

    public virtual void reset(float x, float z)
    {
        Debug.Log(string.Format("<color=#c3ff55>Reset Ball Pos, x = {0}, z = {1}</color>", x, z));

        _effect.time = 0;
        //		_effect.enabled = false;
        Invoke("enableEffect", 0.1f);

        BallPositionX = x;
        EventChangeBallX(x);
        BallPositionZ = z;
        EventChangeBallZ(z);


        _canControlBall = true;
        _isShoot = false;

        if (fingerTrail != null)
        {
            fingerTrail.Reset();
            shootTriggered = false;
            _canShoot = true;
        }
        else
        {
            _canShoot = true;
        }

        // reset ball
        _ball.velocity = Vector3.zero;
        _ball.angularVelocity = Vector3.zero;
        _ball.transform.localEulerAngles = Vector3.zero;

        Vector3 pos = new Vector3(BallPositionX, 0f, BallPositionZ);
        Vector3 diff = -pos;
        diff.Normalize();
        float angleRadian = Mathf.Atan2(diff.z, diff.x);        // tinh' goc' lech
        float angle = 90 - angleRadian * Mathf.Rad2Deg;

        _ball.transform.parent.localEulerAngles = new Vector3(0, angle, 0);     // set parent cua ball xoay 1 do theo truc y = goc lech

        _ball.transform.position = new Vector3(BallPositionX, 0.16f, BallPositionZ);

        pos = _ballTarget.position;
        pos.x = 0;
        _ballTarget.position = pos;

        float val = (Mathf.Abs(_ball.transform.localPosition.z) - _distanceMinZ) / (_distanceMaxZ - _distanceMinZ);
        _zVelocity = Mathf.Lerp(_speedMin, _speedMax, val);

        EventChangeSpeedZ(_zVelocity);

        EventDidPrepareNewTurn();
    }

    public void enableTouch()
    {
        _enableTouch = true;
    }

    public void disableTouch()
    {
        StartCoroutine(_disableTouch());
    }

    private IEnumerator _disableTouch()
    {
        yield return new WaitForEndOfFrame();
        _enableTouch = false;
    }
}
