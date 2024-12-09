#if !UNITY_WEBGL

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks; // �߰�
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Text.RegularExpressions;
using TMPro;

public class FirestoreManager : MonoBehaviour
{
    FirebaseFirestore db;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                Debug.Log("Firebase Firestore initialized successfully.");
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {task.Result}");
            }
        });
    }

    // ���� �ð��� �������� �޼��� �߰�
    public void GetServerTimestamp(Action<int> onTimestampReceived)
    {
        DocumentReference docRef = db.Collection("server_time").Document("current_time");

        // ���� �ð� ���� ��û
        docRef.SetAsync(new Dictionary<string, object> { { "timestamp", FieldValue.ServerTimestamp } })
            .ContinueWithOnMainThread(setTask =>
            {
                if (setTask.IsCompleted)
                {
                    // ���� �ð� ��������
                    docRef.GetSnapshotAsync().ContinueWithOnMainThread(snapshotTask =>
                    {
                        if (snapshotTask.IsCompleted)
                        {
                            DocumentSnapshot snapshot = snapshotTask.Result;
                            if (snapshot.TryGetValue("timestamp", out Timestamp serverTimestamp))
                            {
                                DateTime serverTime = serverTimestamp.ToDateTime();
                                int unixTime = (int)(serverTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                onTimestampReceived(unixTime);
                            }
                        }
                    });
                }
            });
    }

    public Task SaveUserInfo(string userID, string userName, string clientVersion)
    {
        Dictionary<string, object> userInfo = new Dictionary<string, object>
        {
            { "userID", userID },
            { "userName", userName },
            { "clientVersion", clientVersion }
        };

        return db.Collection("Userinfo").Document(userID).SetAsync(userInfo).ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                Debug.Log("User info and client version saved successfully.");
            }
            else
            {
                Debug.LogError("Error saving user info: " + task.Exception);
            }
        });
    }

    //Ranking
    public void SaveRankData(string userID, string databaseName, int startTime, int endTime, int score)
    {
        Dictionary<string, object> rankData = new Dictionary<string, object>
        {
            { "playerID", userID },
            { "startGame", startTime },
            { "endGame", endTime },
            { "score", score },
            { "mode", databaseName }
        };

        // ������ ���� ID ����
        string documentID = Guid.NewGuid().ToString();

        db.Collection("RankData").Document(documentID).SetAsync(rankData).ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                Debug.Log("Game data saved successfully.");
            }
            else
            {
                Debug.LogError("Error saving game data: " + task.Exception);
            }
        });
    }
    //Ranking


    public void SaveGameData(string userID, string databaseName, int startTime, int endTime, int score)
    {
        Dictionary<string, object> gameData = new Dictionary<string, object>
        {
            { "playerID", userID },
            { "startGame", startTime },
            { "endGame", endTime },
            { "score", score }
        };

        // ������ ���� ID ����
        string documentID = Guid.NewGuid().ToString();

        db.Collection(databaseName).Document(documentID).SetAsync(gameData).ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                Debug.Log("Game data saved successfully.");
            }
            else
            {
                Debug.LogError("Error saving game data: " + task.Exception);
            }
        });
    }

    public void CheckClientVersion(string currentVersion, Action<bool> callback)
    {
        Debug.Log("Checking client version...");
        DocumentReference docRef = db.Collection("ClientVer").Document("ClientVer");
        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    Debug.Log("Document exists, reading data...");
                    Dictionary<string, object> versionData = snapshot.ToDictionary();
                    string latestAndroidVersion = versionData["android"].ToString();
                    string latestIOSVersion = versionData["iOS"].ToString();

                    Debug.Log($"Latest Android Version: {latestAndroidVersion}, Latest iOS Version: {latestIOSVersion}");

                    bool isUpdateRequired = false;

                    // ���� Ŭ���̾�Ʈ ������ �ֽ� �������� üũ
#if UNITY_ANDROID
                    isUpdateRequired = string.Compare(currentVersion, latestAndroidVersion) < 0;
#elif UNITY_IOS
                isUpdateRequired = string.Compare(currentVersion, latestIOSVersion) < 0;
#endif

                    callback(isUpdateRequired);
                }
                else
                {
                    Debug.LogError("ClientVer document does not exist.");
                    callback(false);
                }
            }
            else
            {
                Debug.LogError("Error getting document: " + task.Exception);
                callback(false);
            }
        });
    }


}

#endif
