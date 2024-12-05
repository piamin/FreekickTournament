using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FingerTrail : MonoBehaviour
{
    private TrailRenderer trail;
    // private Material trailMaterial;
    private Camera mainCamera;
    public float trailWidthInPixels = 10f; // 원하는 트레일의 너비를 픽셀 단위로 설
    
    public Shoot shootScript;

    private int screenWidth;
    private int screenHeight;

    private float touchTime;

    const float distance = 10f;

    private bool isTouching = false;
    private bool canTouch = false;

    private List<KeyValuePair<float, Vector3>> touchPositions = new List<KeyValuePair<float, Vector3>>();

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        // trailMaterial = new Material(trail.material);
        // trail.material = trailMaterial;
        mainCamera = Camera.main;
        UpdateTrailWidth();
        screenWidth = Screen.width;
        screenHeight = Screen.height;
    }

    public void Reset()
    {
        canTouch = true;
        isTouching = false;
        touchTime = 0f;
        touchPositions.Clear();
    }

    void HandleShoot()
    {
        canTouch = false;
        shootScript.ShootBall();
    }

    public void RemoveEventUntil(float time)
    {
        for (int i = 0; i < touchPositions.Count; i++)
        {
            if (touchPositions[i].Key > time)
            {
                touchPositions.RemoveRange(0, i);
                break;
            }
        }
    }

    public bool IsEventExists()
    {
        return touchPositions.Count > 0;
    }

    public Vector3 GetMousePosition()
    {
        if (touchPositions.Count == 0)
            return Vector3.zero;
        return touchPositions[0].Value;
    }

    void Update()
    {
        // 화면 크기 변경 감지
        if (screenWidth != Screen.width || screenHeight != Screen.height)
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            UpdateTrailWidth();
        }

        if (!canTouch)
            return;

        if (isTouching && Input.GetMouseButtonUp(0))
        {
            isTouching = false;
            trail.enabled = false;
            HandleShoot();
        }

        if (isTouching && Input.GetMouseButton(0))
        {
            touchTime += Time.deltaTime;

            Vector2 mousePos = Input.mousePosition;
            touchPositions.Add(new KeyValuePair<float, Vector3>(touchTime, mousePos));

            Vector3 mouseWPosition = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distance));
            trail.transform.position = mouseWPosition;

            if (touchTime > 1f)
            {
                isTouching = false;
                trail.enabled = false;
                HandleShoot();
            }
        }

        if (!isTouching && Input.GetMouseButtonDown(0))
        {
            isTouching = true;

            touchTime = 0f;
            touchPositions.Add(new KeyValuePair<float, Vector3>(touchTime, Input.mousePosition));

            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distance));
            trail.transform.position = mousePosition;
            StartCoroutine(ClearTrail());
        }
    }

    void UpdateTrailWidth()
    {
        // 화면의 높이를 기준으로 트레일의 너비를 조정
        float screenHeight = Screen.height;

        // 카메라의 뷰포트 크기 계산
        float frustumHeight = 2.0f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        // 픽셀 단위로 트레일의 너비를 설정
        float pixelsPerUnit = screenHeight / frustumHeight;
        trail.widthMultiplier = trailWidthInPixels / pixelsPerUnit;
    }

    void OnValidate()
    {
        // 에디터에서 값이 변경될 때마다 트레일의 너비를 업데이트
        if (trail != null && mainCamera != null)
        {
            UpdateTrailWidth();
        }
    }

    IEnumerator ClearTrail()
    {
        yield return new WaitForEndOfFrame();
        trail.Clear();
        trail.enabled = true;
    }
}