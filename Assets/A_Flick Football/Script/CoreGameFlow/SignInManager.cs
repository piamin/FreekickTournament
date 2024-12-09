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
    public GameObject message; // Message ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Æ®
    public GameObject checkNetworkMsg;
    public GameObject warningMessage;
    public GameObject loadingScreenPrefab;

    public GameObject newNickNameUI;
    public TMP_InputField userNameInput; // TMP_InputFieldï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½

    [Space(10)]
    public Animator backgroundAnimator;

#if !UNITY_WEBGL
    private FirestoreManager firestoreManager;
#endif

    void Start()
    {
        Application.targetFrameRate = 60;
        loadingScreenPrefab.SetActive(true);
        message.SetActive(false); // ï¿½âº»ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ Message ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Æ® ï¿½ï¿½È°ï¿½ï¿½È­

#if !UNITY_WEBGL
        firestoreManager = FindObjectOfType<FirestoreManager>(); // FirestoreManager ï¿½Î½ï¿½ï¿½Ï½ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½

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
            // ½Å±Ô »ç¿ëÀÚ: »ç¿ëÀÚ ÀÌ¸§ ÀÔ·Â UI Ç¥½Ã ¹× UUID »ý¼º
            userNameInput.text = "MVP_" + Random.Range(10001, 999999).ToString();
            userNameInput.characterLimit = 16;
            newNickNameUI.SetActive(true);
            loadingScreenPrefab.SetActive(false);

            // Message ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Æ® È°ï¿½ï¿½È­
            message.SetActive(true);

            // ï¿½ï¿½ï¿½Î¿ï¿½ UUID ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½
            if (!PlayerPrefs.HasKey("UserUUID"))
            {
                // UUIDï¿½ï¿½ 16ï¿½Ú¸ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½
                string userUUID = System.Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12);
                PlayerPrefs.SetString("UserUUID", userUUID);

                // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ UUIDï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½Õ´Ï´ï¿½.
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
                string clientVersion = Application.version; // Å¬ï¿½ï¿½ï¿½Ì¾ï¿½Æ® ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½
                Debug.Log($"Attempting to save user info: {userUUID}, {userName}");
                var saveTask = firestoreManager.SaveUserInfo(userUUID, userName, clientVersion);
                await saveTask; // ï¿½ñµ¿±ï¿½ ï¿½ï¿½ï¿½ï¿½ ï¿½Û¾ï¿½ï¿½ï¿½ ï¿½ï¿½Ù¸ï¿½
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
        PlayerPrefs.SetInt("IsNewUser", 0); // ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½Ú·ï¿½ ï¿½ï¿½ï¿½ï¿½
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
            // ï¿½ï¿½Æ®ï¿½ï¿½Å© ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ Ã³ï¿½ï¿½
            checkNetworkMsg.SetActive(true);
        }
        else
        {
            // ï¿½ï¿½Æ®ï¿½ï¿½Å© ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ Ã³ï¿½ï¿½
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
        PlayerPrefs.SetInt("IsNewUser", 0); // ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½Ú·ï¿½ ï¿½ï¿½ï¿½ï¿½
        PlayerPrefs.Save();
        SceneManager.LoadScene("0.Welcome");
    }

    public IEnumerator HideMessageAfterSeconds(GameObject message, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        message.SetActive(false);
    }
}
