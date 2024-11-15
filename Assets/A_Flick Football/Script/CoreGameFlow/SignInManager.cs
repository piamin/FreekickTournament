using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections;
//using DG.Tweening;
using TMPro;
using System.Threading.Tasks;

public class SignInManager : MonoBehaviour
{
    public GameObject message; // Message 오브젝트
    public GameObject checkNetworkMsg;
    public GameObject warningMessage;
    public GameObject loadingScreenPrefab;

    public GameObject newNickNameUI;
    public TMP_InputField userNameInput; // TMP_InputField로 변경

    [Space(10)]
    public Animator backgroundAnimator;

    private FirestoreManager firestoreManager;

    void Start()
    {
        Application.targetFrameRate = 60;
        loadingScreenPrefab.SetActive(true);
        message.SetActive(false); // 기본적으로 Message 오브젝트 비활성화
        firestoreManager = FindObjectOfType<FirestoreManager>(); // FirestoreManager 인스턴스 가져오기

        if (firestoreManager == null)
        {
            Debug.LogError("FirestoreManager is not found in the scene.");
        }
        else
        {
            StartCoroutine(InitializeSignInProcess());
        }
    }

    IEnumerator InitializeSignInProcess()
    {
        yield return new WaitForSeconds(0.5f);
        CheckNetworkConnection();
        if (!IsNetworkAvailable())
        {
            checkNetworkMsg.SetActive(true);
            loadingScreenPrefab.SetActive(false);
        }
        else
        {
            CheckUserRecord();
        }
    }

    void CheckUserRecord()
    {
        if (PlayerPrefs.HasKey("IsNewUser") && PlayerPrefs.GetInt("IsNewUser") == 0)
        {
            SceneManager.LoadScene("0.Welcome");
        }
        else
        {
            // 신규 사용자: 사용자 이름 입력 UI 표시 및 UUID 생성
            userNameInput.text = "Player_" + Random.Range(10001, 999999).ToString();
            userNameInput.characterLimit = 16;
            newNickNameUI.SetActive(true);
            loadingScreenPrefab.SetActive(false);

            // Message 오브젝트 활성화
            message.SetActive(true);

            // 새로운 UUID 생성 및 저장
            if (!PlayerPrefs.HasKey("UserUUID"))
            {
                // UUID는 16자리로 생성
                string userUUID = System.Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12);
                PlayerPrefs.SetString("UserUUID", userUUID);

                // 사용자의 UUID를 기록합니다.
                Debug.Log("New User UUID: " + userUUID);
            }

            PlayerPrefs.Save();
        }
    }

    public async void OnNameSubmit()
    {
        string userName = userNameInput.text;
        if (Regex.IsMatch(userName, @"[^a-zA-Z0-9_-]"))
        {
            //warningMessage.SetActive(true);
            StartCoroutine(HideMessageAfterSeconds(warningMessage, 1f));
        }
        else
        {
            string userUUID = PlayerPrefs.GetString("UserUUID");
            PlayerPrefs.SetString("UserName", userName);
            PlayerPrefs.Save();

            if (firestoreManager != null)
            {
                string clientVersion = Application.version; // 클라이언트 버전 가져오기
                Debug.Log($"Attempting to save user info: {userUUID}, {userName}");
                var saveTask = firestoreManager.SaveUserInfo(userUUID, userName, clientVersion);
                await saveTask; // 비동기 저장 작업을 기다림
                if (saveTask.IsCompleted)
                {
                    Debug.Log("User info saved to Firestore.");
                    ProceedToMainScene();
                }
                else
                {
                    Debug.LogError("Error saving user info to Firestore: " + saveTask.Exception);
                    warningMessage.SetActive(true);
                    StartCoroutine(HideMessageAfterSeconds(warningMessage, 2f));
                }
            }
            else
            {
                Debug.LogError("FirestoreManager instance is not found.");
                ProceedToMainScene();
            }
        }
    }

    private void ProceedToMainScene()
    {
        PlayerPrefs.SetInt("IsNewUser", 0); // 기존 사용자로 설정
        PlayerPrefs.Save();

        loadingScreenPrefab.SetActive(true);

        if (backgroundAnimator != null)
        {
            backgroundAnimator.SetTrigger("Move");
        }

        StartCoroutine(StartTutorialAfterDelay(0.5f));
    }

    private void CheckNetworkConnection()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            // 네트워크 연결이 없을 때 처리
            checkNetworkMsg.SetActive(true);
        }
        else
        {
            // 네트워크 연결이 있을 때 처리
            checkNetworkMsg.SetActive(false);
        }
    }

    private bool IsNetworkAvailable()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    public void OnNetworkCheckFail()
    {
        Application.Quit();
    }

    IEnumerator StartTutorialAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        TutorialStage_start();
    }

    public void TutorialStage_start()
    {
        PlayerPrefs.SetInt("IsNewUser", 0); // 기존 사용자로 설정
        PlayerPrefs.Save();
        SceneManager.LoadScene("0.Welcome");
    }

    public IEnumerator HideMessageAfterSeconds(GameObject message, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        message.SetActive(false);
    }
}
