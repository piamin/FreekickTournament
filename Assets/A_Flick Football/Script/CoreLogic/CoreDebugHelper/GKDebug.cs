using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using Random = UnityEngine.Random;

public class GKDebug : MonoBehaviour {

    public Text _labelPredictFactor;
    public Slider _sliderPredictFactor;

    public Text _labelLevelGK;
    public Slider _sliderLevelGK;

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        GoalKeeperLevel.EventChangeLevel += OnChangeGKLevel;
        if (_sliderPredictFactor)
        {
            _sliderPredictFactor.value = (1f - 1f) / (20f - 1f);
        }
        if (_sliderLevelGK)
        {
            _sliderLevelGK.value = 2f / GoalKeeperLevel.share.getMaxLevel();
        }
    }

    void OnDestroy()
    {
        GoalKeeperLevel.EventChangeLevel -= OnChangeGKLevel;
    }

    void OnChangeGKLevel(LevelGK levelData, int level)
    {
        _sliderLevelGK.value = level * 1f / GoalKeeperLevel.share.getMaxLevel();
        _labelLevelGK.text = "" + level;
    }

    public void onValueChange_PredictFactor(float val)
    {
        GoalKeeper.share._predictFactor = (int)Mathf.Lerp(1, 20, val);
        _labelPredictFactor.text = "" + GoalKeeper.share._predictFactor;
    }

    public void onValueChanged_LevelGK(float val)
    {
        if (GoalKeeperLevel.share != null)
        {
            int level = (int)Mathf.Lerp(0, GoalKeeperLevel.share.getMaxLevel(), val);
            GoalKeeperLevel.share.setLevel(level);
            _labelLevelGK.text = "" + level;
        }
    }
}
