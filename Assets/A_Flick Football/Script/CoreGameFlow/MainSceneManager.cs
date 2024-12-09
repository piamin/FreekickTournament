using UnityEngine;
using System.Collections.Generic;
using TMPro;
#if !UNITY_WEBGL
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
#endif
using System.Collections;
using System.Text.RegularExpressions;

public class MainScreen : MonoBehaviour
{
    public GameObject ProfileUI; // ������ UI
    public TMP_Text userNameMainDisplay; // ����� �̸��� ǥ���ϴ� UI Text ����Դϴ�.
    public GameObject updateMessage; // ������Ʈ �޽����� ǥ���� ������Ʈ

#if !UNITY_WEBGL
    [Space(10)]
    private FirestoreManager firestoreManager;
#endif
    private string clientVersion; // Ŭ���̾�Ʈ ���� ����

    /// <summary>
    /// ������ ���� �κ�
    /// </summary>


    [Space(10)]
    public TMP_InputField nicknameInputField; // �г��� �Է� �ʵ�
    public TMP_Text playerIDText; // �÷��̾� ID�� ��Ÿ���� �ؽ�Ʈ

    [Space(10)]
    public GameObject changeNicknameUI; // �г��� ���� UI ���� ������Ʈ

    [Space(10)]
    public GameObject warningMessage; // ��� �޽����� ��Ÿ���� ���� ������Ʈ
    public GameObject copyMessage; // ���� �޽����� ��Ÿ���� ���� ������Ʈ

    [Space(10)]
    public TMP_Text titleScreenNickname; // Ÿ��Ʋ ȭ�鿡 ǥ�õǴ� �г��� �ؽ�Ʈ
    public event System.Action OnProfileUIClosed; // ������ UI�� ���� �� �߻��ϴ� �̺�Ʈ

#if !UNITY_WEBGL
    private FirebaseFirestore db;
#endif


    void Start()
    {
        playerIDText.text = PlayerPrefs.GetString("UserUUID", "Unknown");
        nicknameInputField.text = PlayerPrefs.GetString("UserName", "Player"); // ���� �г����� �Է� �ʵ忡 ����

        clientVersion = Application.version; // Start �޼��忡�� Ŭ���̾�Ʈ ���� ��������

#if !UNITY_WEBGL
        firestoreManager = FindObjectOfType<FirestoreManager>();

        if (firestoreManager == null)
        {
            Debug.LogError("FirestoreManager is not found in the scene.");
        }
#endif

        // ����� �̸��� �ε��Ͽ� ǥ���մϴ�.
        LoadUserName();

        // �÷��̾� ������ ���� ���� ����
        SavePlayerInfo();

        // Ŭ���̾�Ʈ ���� üũ
        CheckVersionAndUpdateMessage();

    }

    // ����� �̸��� �ε��Ͽ� UI�� ǥ���մϴ�.
    void LoadUserName()
    {
        string userName = PlayerPrefs.GetString("UserName", "Player");
        userNameMainDisplay.text = userName;
    }

    // �÷��̾� ������ ���� ������ Firestore�� �����մϴ�.
    void SavePlayerInfo()
    {
#if !UNITY_WEBGL
        string playerID = PlayerPrefs.GetString("UserUUID");
        string userName = PlayerPrefs.GetString("UserName", "Player");

        if (firestoreManager != null)
        {
            firestoreManager.SaveUserInfo(playerID, userName, clientVersion);
        }
#endif
    }



    // MainScreen Ŭ�������� CheckVersionAndUpdateMessage �޼��� ����
    void CheckVersionAndUpdateMessage()
    {
#if !UNITY_WEBGL
        if (firestoreManager != null)
        {
            Debug.Log("Starting version check...");
            firestoreManager.CheckClientVersion(clientVersion, (isUpdateRequired) =>
            {
                if (isUpdateRequired)
                {
                    Debug.Log("Update required. Showing update message.");
                    updateMessage.SetActive(true); // ������Ʈ �޽��� ǥ��
                }
                else
                {
                    Debug.Log("No update required.");
                    updateMessage.SetActive(false); // ������Ʈ �޽��� �����
                }
            });
        }
        else
        {
            Debug.LogError("FirestoreManager is not available.");
        }
#endif
    }


    public void OnPlayButtonPressed(string sceneName)
    {
        Application.LoadLevel(sceneName);
    }

    public void profileUI_open()
    {
        ProfileUI.SetActive(true);
        nicknameInputField.text = PlayerPrefs.GetString("UserName", "Player"); // ���� �г����� �Է� �ʵ忡 ����

    }

    public void profileUI_close()
    {
        ProfileUI.SetActive(false);
    }

    // ������ �����͸� �ε��ϴ� �޼���
    public void LoadProfileData()
    {
        // �÷��̾� ID�� PlayerPrefs���� ������
        string playerID = PlayerPrefs.GetString("UserUUID", "Unknown");
        playerIDText.text = playerID;

        // PlayerPrefs���� �г����� ������
        string userName = PlayerPrefs.GetString("UserName", "Player");
        userNameMainDisplay.text = userName;
    }

    bool isChangeNickOpen;


    // �г����� ����� �� ȣ��Ǵ� �޼���
    public void OnNicknameChange()
    {
        string userName = nicknameInputField.text;
        // �г��ӿ� ������ �ʴ� ���ڰ� ���Ե� ��� ��� �޽����� ǥ���ϰ� ���� �ð� �Ŀ� ����
        if (Regex.IsMatch(userName, @"[^a-zA-Z0-9_-]"))
        {
            warningMessage.SetActive(true);
            StartCoroutine(HideMessageAfterSeconds(warningMessage, 1f));
        }
        else
        {
            // PlayerPrefs�� �г��� ����
            PlayerPrefs.SetString("UserName", userName);
            PlayerPrefs.Save();

            nicknameInputField.text = userName;
            titleScreenNickname.text = userName;
            //changeNicknameUI.SetActive(false); // ������ UI ��Ȱ��ȭ

            // Firestore�� �г��� ����
#if !UNITY_WEBGL
            SaveNicknameToFirestore(userName);
#endif
        }
    }

#if !UNITY_WEBGL
    private async void SaveNicknameToFirestore(string userName)
    {
        string userUUID = PlayerPrefs.GetString("UserUUID");
        if (firestoreManager != null)
        {
            string clientVersion = Application.version; // Ŭ���̾�Ʈ ���� ��������
            Debug.Log($"Attempting to save updated user info: {userUUID}, {userName}");
            var saveTask = firestoreManager.SaveUserInfo(userUUID, userName, clientVersion);
            await saveTask; // �񵿱� ���� �۾��� ��ٸ�
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
#endif

    // �÷��̾� ID�� �����ϴ� �޼���
    public void OnCopyPlayerID()
    {
        string playerID = playerIDText.text;
        // �÷��̾� ID�� �ý��� Ŭ�����忡 ����
        GUIUtility.systemCopyBuffer = playerID;

        copyMessage.SetActive(true); // ���� �޽��� ǥ��
        StartCoroutine(HideMessageAfterSeconds(copyMessage, 1f)); // ���� �ð� �Ŀ� ����
    }

    // ���� �ð� �Ŀ� �޽����� ����� �ڷ�ƾ
    public IEnumerator HideMessageAfterSeconds(GameObject message, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        message.SetActive(false);
    }




    public void OnUserNameDisplayClicked()
    {
        changeNicknameUI.SetActive(true); // �г��� ���� UI Ȱ��ȭ
        nicknameInputField.text = userNameMainDisplay.text; // ���� �г����� �Է� �ʵ忡 ����
    }

    public void OnChangeNicknameConfirm()
    {
        OnNicknameChange(); // �г��� ���� �޼��� ȣ��
    }

    public void OnChangeNicknameCancel()
    {
        changeNicknameUI.SetActive(false); // �г��� ���� UI ��Ȱ��ȭ
    }
}


