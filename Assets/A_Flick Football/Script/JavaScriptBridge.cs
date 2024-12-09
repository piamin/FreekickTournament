using UnityEngine;
using System.Runtime.InteropServices;

class JavaScriptBridge : MonoBehaviour
{
    [DllImport("__Internal")]
    public static extern void SaveScoreToServer(int score);
}