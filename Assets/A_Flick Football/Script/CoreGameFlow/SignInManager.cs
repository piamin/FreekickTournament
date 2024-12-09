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
    public GameObject message; // Message ������Ʈ
    public GameObject checkNetworkMsg;
    public GameObject warningMessage;
    public GameObject loadingScreenPrefab;

    public GameObject newNickNameUI;
    public TMP_InputField userNameInput; // TMP_InputField�� ����

    [Space(10)]
    public Animator backgroundAnimator;

#if !UNITY_WEBGL
    private FirestoreManager firestoreManager;
#endif

    void Start()
    {
        Application.targetFrameRate = 60;
        loadingScreenPrefab.SetActive(true);
        message.SetActive(false); // �⺻������ Message ������Ʈ ��Ȱ��ȭ

#if !UNITY_WEBGL
        firestoreManager = FindObjectOfType<FirestoreManager>(); // FirestoreManager �ν��Ͻ� ��������

        if (firestoreManager == null)
        {
            Debug.LogError("FirestoreManager is not found in the scene.");
        }
    else
        {
#endif
            StartCoroutine(InitializeSignInProcess());
#if !UNITY_WEBGL
        }
#endif
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
            // �ű� �����: ����� �̸� �Է� UI ǥ�� �� UUID ����
            userNameInput.text = "Player_" + Random.Range(10001, 999999).ToString();
            userNameInput.characterLimit = 16;
            newNickNameUI.SetActive(true);
            loadingScreenPrefab.SetActive(false);

            // Message ������Ʈ Ȱ��ȭ
            message.SetActive(true);

            // ���ο� UUID ���� �� ����
            if (!PlayerPrefs.HasKey("UserUUID"))
            {
                // UUID�� 16�ڸ��� ����
                string userUUID = System.Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12);
                PlayerPrefs.SetString("UserUUID", userUUID);

                // ������� UUID�� ����մϴ�.
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
#if !UNITY_WEBGL
            if (firestoreManager != null)
            {
                string clientVersion = Application.version; // Ŭ���̾�Ʈ ���� ��������
                Debug.Log($"Attempting to save user info: {userUUID}, {userName}");
                var saveTask = firestoreManager.SaveUserInfo(userUUID, userName, clientVersion);
                await saveTask; // �񵿱� ���� �۾��� ��ٸ�
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
#else
            ProceedToMainScene();
#endif
        }
    }

    private void ProceedToMainScene()
    {
        PlayerPrefs.SetInt("IsNewUser", 0); // ���� ����ڷ� ����
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
            // ��Ʈ��ũ ������ ���� �� ó��
            checkNetworkMsg.SetActive(true);
        }
        else
        {
            // ��Ʈ��ũ ������ ���� �� ó��
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
        PlayerPrefs.SetInt("IsNewUser", 0); // ���� ����ڷ� ����
        PlayerPrefs.Save();
        SceneManager.LoadScene("0.Welcome");
    }

    public IEnumerator HideMessageAfterSeconds(GameObject message, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        message.SetActive(false);
    }
}
