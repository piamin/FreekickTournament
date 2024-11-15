using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks; // 추가
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

    // 서버 시간을 가져오는 메서드 추가
    public void GetServerTimestamp(Action<int> onTimestampReceived)
    {
        DocumentReference docRef = db.Collection("server_time").Document("current_time");

        // 서버 시간 설정 요청
        docRef.SetAsync(new Dictionary<string, object> { { "timestamp", FieldValue.ServerTimestamp } })
            .ContinueWithOnMainThread(setTask =>
            {
                if (setTask.IsCompleted)
                {
                    // 서버 시간 가져오기
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

        // 고유한 문서 ID 생성
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

        // 고유한 문서 ID 생성
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

                    // 현재 클라이언트 버전이 최신 버전인지 체크
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
