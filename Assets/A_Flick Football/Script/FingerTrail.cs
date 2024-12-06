using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    private bool IsClockwise(List<Vector3> points)
    {
        float sum = 0;
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[(i + 1) % points.Count];
            sum += (p2.x - p1.x) * (p2.y + p1.y);
        }
        return sum > 0;
    }

    List<Vector3> CalculateVectors(List<Vector3> points)
    {
        List<Vector3> vectors = new List<Vector3>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 vector = points[i + 1] - points[i];
            if (vector.magnitude > 0)
            {
                vectors.Add(vector.normalized);
            }
        }
        return vectors;
    }

    float CalculateVectorsMagnitudeSum(List<Vector3> points)
    {
        float sum = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 vector = points[i + 1] - points[i];
            sum += vector.magnitude;
        }
        return sum;
    }

    List<float> CalculateAngles(List<Vector3> vectors)
    {
        List<float> angles = new List<float>();
        for (int i = 0; i < vectors.Count - 1; i++)
        {
            Vector3 v1 = vectors[i];
            Vector3 v2 = vectors[i + 1];
            float cosTheta = Vector3.Dot(v1, v2);
            float angle = Mathf.Acos(Mathf.Clamp(cosTheta, -1.0f, 1.0f));

            // Determine the sign of the angle using the cross product
            float crossProduct = Vector3.Cross(v1, v2).z;
            if (crossProduct < 0)
            {
                angle = -angle;
            }

            angles.Add(angle);
        }
        return angles;
    }

    float CalculateVariance(List<float> angles)
    {
        float mean = 0;
        foreach (float angle in angles)
        {
            mean += angle;
        }
        mean /= angles.Count;

        float variance = 0;
        foreach (float angle in angles)
        {
            variance += Mathf.Pow(angle - mean, 2);
        }
        variance /= angles.Count;

        return variance;
    }

    float CalculateAngleSum(List<float> angles)
    {
        float sum = 0;
        foreach (float angle in angles)
        {
            sum += angle;
        }
        return sum;
    }    

    float CalculateStartDirection()
    {
        float sum = 0f;
        int count = 0;

        var lastPos = touchPositions[0].Value;
        var lastAngle = 0f;
        for (int i = 1; i < touchPositions.Count; i++)
        {
            Vector3 vector = touchPositions[i].Value - lastPos;
            if (vector.magnitude > 8f)
            {
                var angle = Vector3.SignedAngle(vector, Vector3.up, Vector3.forward);

                if (count > 0 && Mathf.Abs(angle - lastAngle) > 10f) break;
                if (count > 4) break;

                lastPos = touchPositions[i].Value;
                lastAngle = angle;

                sum += angle;
                count++;
            }
        }
        return sum / count;
    }

    void HandleShoot()
    {
        if (touchPositions.Count < 3 || touchTime < 0.1f)
            return;

        Vector3 firstTouch = touchPositions[0].Value;
        Vector3 lastTouch = touchPositions[touchPositions.Count - 1].Value;

        Vector3 leftMost = touchPositions[0].Value;
        Vector3 rightMost = touchPositions[0].Value;
        Vector3 topMost = touchPositions[0].Value;
        Vector3 bottomMost = touchPositions[0].Value;

        foreach(var touch in touchPositions)
        {
            if (touch.Value.x < leftMost.x)
                leftMost = touch.Value;
            if (touch.Value.x > rightMost.x)
                rightMost = touch.Value;
            if (touch.Value.y > topMost.y)
                topMost = touch.Value;
            if (touch.Value.y < bottomMost.y)
                bottomMost = touch.Value;
        }

        var points = touchPositions.ConvertAll(x => x.Value);

        List<Vector3> vectors = CalculateVectors(points);
        List<float> angles = CalculateAngles(vectors);
        float startXdir = CalculateStartDirection();
        startXdir = Mathf.Clamp(startXdir, -50f, 50f);
        // float variance = CalculateVariance(angles);
        float angleSum = CalculateAngleSum(angles) / Mathf.PI * 180f;

        float magnitudeSum = CalculateVectorsMagnitudeSum(points);

        bool isClockwise = IsClockwise(points);
        Debug.Log("IsClockwise: " + isClockwise);
        // Debug.Log("GetAngleVariance: " + variance);
        Debug.Log("GetAngleSum: " + angleSum);

        var xAngle = startXdir / 2f;
        Debug.Log("xAngle: " + xAngle);
        // var xAngle = 0f;
        var yAngle = 23f * (touchTime - 0.1f) / 0.9f + 13f;

        // var yAngle = 45f;

        var addForceFactor = Mathf.Clamp(Mathf.Abs(angleSum), 0f, 240f) / 240f;
        Debug.Log("addForceFactor: " + addForceFactor);

        var power = 12f * (1.0f - (touchTime - 0.1f) / 0.9f) + 28f;
        var forcePower = power * addForceFactor;
        forcePower *= Mathf.Clamp((rightMost.y - leftMost.y) / (Screen.width * 0.4f), 1f, 1.2f);
        power *= Mathf.Clamp((topMost.y - bottomMost.y) / (Screen.height * 0.4f), 0.9f, 1.1f);

        var direction = Quaternion.Euler(yAngle, xAngle, 0);
        var speed = direction * Vector3.forward * power;
        
        var addForce = (isClockwise) ? Vector3.right * forcePower : Vector3.left * forcePower;

        canTouch = false;

        shootScript.ShootBall(speed, addForce, 1.5f);
        // shootScript.ShootBall(speed, addForce, 0.75f);
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

            Vector3 mousePos = Input.mousePosition;
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