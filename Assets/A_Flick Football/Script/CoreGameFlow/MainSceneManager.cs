using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections;
using System.Text.RegularExpressions;

public class MainScreen : MonoBehaviour
{
    public GameObject ProfileUI; // 프로필 UI
    public TMP_Text userNameMainDisplay; // 사용자 이름을 표시하는 UI Text 요소입니다.
    public GameObject updateMessage; // 업데이트 메시지를 표시할 오브젝트

    [Space(10)]
    private FirestoreManager firestoreManager;
    private string clientVersion; // 클라이언트 빌드 버전

    /// <summary>
    /// 프로필 관련 부분
    /// </summary>


    [Space(10)]
    public TMP_InputField nicknameInputField; // 닉네임 입력 필드
    public TMP_Text playerIDText; // 플레이어 ID를 나타내는 텍스트

    [Space(10)]
    public GameObject changeNicknameUI; // 닉네임 변경 UI 게임 오브젝트

    [Space(10)]
    public GameObject warningMessage; // 경고 메시지를 나타내는 게임 오브젝트
    public GameObject copyMessage; // 복사 메시지를 나타내는 게임 오브젝트

    [Space(10)]
    public TMP_Text titleScreenNickname; // 타이틀 화면에 표시되는 닉네임 텍스트
    public event System.Action OnProfileUIClosed; // 프로필 UI가 닫힐 때 발생하는 이벤트



    private FirebaseFirestore db;


    void Start()
    {
        playerIDText.text = PlayerPrefs.GetString("UserUUID", "Unknown");
        nicknameInputField.text = PlayerPrefs.GetString("UserName", "Player"); // 기존 닉네임을 입력 필드에 설정

        clientVersion = Application.version; // Start 메서드에서 클라이언트 버전 가져오기
        firestoreManager = FindObjectOfType<FirestoreManager>();

        if (firestoreManager == null)
        {
            Debug.LogError("FirestoreManager is not found in the scene.");
        }

        // 사용자 이름을 로드하여 표시합니다.
        LoadUserName();

        // 플레이어 정보와 빌드 버전 저장
        SavePlayerInfo();

        // 클라이언트 버전 체크
        CheckVersionAndUpdateMessage();

    }

    // 사용자 이름을 로드하여 UI에 표시합니다.
    void LoadUserName()
    {
        string userName = PlayerPrefs.GetString("UserName", "Player");
        userNameMainDisplay.text = userName;
    }

    // 플레이어 정보와 빌드 버전을 Firestore에 저장합니다.
    void SavePlayerInfo()
    {
        string playerID = PlayerPrefs.GetString("UserUUID");
        string userName = PlayerPrefs.GetString("UserName", "Player");

        if (firestoreManager != null)
        {
            firestoreManager.SaveUserInfo(playerID, userName, clientVersion);
        }
    }



    // MainScreen 클래스에서 CheckVersionAndUpdateMessage 메서드 수정
    void CheckVersionAndUpdateMessage()
    {
        if (firestoreManager != null)
        {
            Debug.Log("Starting version check...");
            firestoreManager.CheckClientVersion(clientVersion, (isUpdateRequired) =>
            {
                if (isUpdateRequired)
                {
                    Debug.Log("Update required. Showing update message.");
                    updateMessage.SetActive(true); // 업데이트 메시지 표시
                }
                else
                {
                    Debug.Log("No update required.");
                    updateMessage.SetActive(false); // 업데이트 메시지 숨기기
                }
            });
        }
        else
        {
            Debug.LogError("FirestoreManager is not available.");
        }
    }


    public void OnPlayButtonPressed(string sceneName)
    {
        Application.LoadLevel(sceneName);
    }

    public void profileUI_open()
    {
        ProfileUI.SetActive(true);
        nicknameInputField.text = PlayerPrefs.GetString("UserName", "Player"); // 기존 닉네임을 입력 필드에 설정

    }

    public void profileUI_close()
    {
        ProfileUI.SetActive(false);
    }











    // 프로필 데이터를 로드하는 메서드
    public void LoadProfileData()
    {
        // 플레이어 ID를 PlayerPrefs에서 가져옴
        string playerID = PlayerPrefs.GetString("UserUUID", "Unknown");
        playerIDText.text = playerID;

        // PlayerPrefs에서 닉네임을 가져옴
        string userName = PlayerPrefs.GetString("UserName", "Player");
        userNameMainDisplay.text = userName;
    }

    bool isChangeNickOpen;


    // 닉네임이 변경될 때 호출되는 메서드
    public void OnNicknameChange()
    {
        string userName = nicknameInputField.text;
        // 닉네임에 허용되지 않는 문자가 포함된 경우 경고 메시지를 표시하고 일정 시간 후에 숨김
        if (Regex.IsMatch(userName, @"[^a-zA-Z0-9_-]"))
        {
            warningMessage.SetActive(true);
            StartCoroutine(HideMessageAfterSeconds(warningMessage, 1f));
        }
        else
        {
            // PlayerPrefs에 닉네임 저장
            PlayerPrefs.SetString("UserName", userName);
            PlayerPrefs.Save();

            nicknameInputField.text = userName;
            titleScreenNickname.text = userName;
            //changeNicknameUI.SetActive(false); // 프로필 UI 비활성화

            // Firestore에 닉네임 저장
            SaveNicknameToFirestore(userName);
        }
    }

    private async void SaveNicknameToFirestore(string userName)
    {
        string userUUID = PlayerPrefs.GetString("UserUUID");
        if (firestoreManager != null)
        {
            string clientVersion = Application.version; // 클라이언트 버전 가져오기
            Debug.Log($"Attempting to save updated user info: {userUUID}, {userName}");
            var saveTask = firestoreManager.SaveUserInfo(userUUID, userName, clientVersion);
            await saveTask; // 비동기 저장 작업을 기다림
            if (saveTask.IsCompleted)
            {
                Debug.Log("Updated user info saved to Firestore.");
            }
            else
            {
                Debug.LogError("Error saving updated user info to Firestore: " + saveTask.Exception);
                warningMessage.SetActive(true);
                StartCoroutine(HideMessageAfterSeconds(warningMessage, 2f));
            }
        }
        else
        {
            Debug.LogError("FirestoreManager instance is not found.");
        }
    }


    // 플레이어 ID를 복사하는 메서드
    public void OnCopyPlayerID()
    {
        string playerID = playerIDText.text;
        // 플레이어 ID를 시스템 클립보드에 복사
        GUIUtility.systemCopyBuffer = playerID;

        copyMessage.SetActive(true); // 복사 메시지 표시
        StartCoroutine(HideMessageAfterSeconds(copyMessage, 1f)); // 일정 시간 후에 숨김
    }

    // 일정 시간 후에 메시지를 숨기는 코루틴
    public IEnumerator HideMessageAfterSeconds(GameObject message, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        message.SetActive(false);
    }




    public void OnUserNameDisplayClicked()
    {
        changeNicknameUI.SetActive(true); // 닉네임 변경 UI 활성화
        nicknameInputField.text = userNameMainDisplay.text; // 현재 닉네임을 입력 필드에 설정
    }

    public void OnChangeNicknameConfirm()
    {
        OnNicknameChange(); // 닉네임 변경 메서드 호출
    }

    public void OnChangeNicknameCancel()
    {
        changeNicknameUI.SetActive(false); // 닉네임 변경 UI 비활성화
    }
}


