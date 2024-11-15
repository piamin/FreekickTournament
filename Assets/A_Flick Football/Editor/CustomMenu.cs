using UnityEngine;
using UnityEditor;
using System.IO;
using System;

#if UNITY_EDITOR

public class CustomEditorMenuTest : MonoBehaviour
{
    #region  Delete Preferences
    [MenuItem("Piamin/Delete Preferences", false, 4)]
    public static void DeleteAllPreferences()
    {
        PlayerPrefs.DeleteAll();
    }
    #endregion

    #region CaptureScreenshot
    [MenuItem("Piamin/Capture Screenshot/1X")]
    private static void Capture1XScreenshot()
    {
        CaptureScreenshot(1);
    }

    [MenuItem("Piamin/Capture Screenshot/2X")]
    private static void Capture2XScreenshot()
    {
        CaptureScreenshot(2);
    }

    [MenuItem("Piamin/Capture Screenshot/3X")]
    private static void Capture3XScreenshot()
    {
        CaptureScreenshot(3);
    }

    public static void CaptureScreenshot(int supersize)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string imgName = "IMG-" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00") + "-" + DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") + DateTime.Now.Second.ToString("00") + ".png";
        string fullPath = Path.Combine(desktopPath, imgName);

        ScreenCapture.CaptureScreenshot(fullPath, supersize);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }
    #endregion
}

#endif