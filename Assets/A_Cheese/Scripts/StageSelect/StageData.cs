using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageData_New",
    menuName = "World Map Puzzle/Stage Data"
)]
public class StageData : ScriptableObject
{
    [Header("ステージ基本情報")]

    [Tooltip("ステージを識別するID。例: stage_001")]
    public string stageId;

    [Tooltip("画面に表示するステージ名")]
    public string stageName;

    [Header("ステージ画像")]

    [Tooltip("ステージ選択画面に表示する画像")]
    public Sprite stageImage;

    [Header("出題する国")]

    [Tooltip("このステージで出題するCountryId")]
    public List<string> countryIds = new List<string>();


    [Header("星評価タイム")]

    [Tooltip("この秒数以内なら星3")]
    public float threeStarTime = 30f;

    [Tooltip("この秒数以内なら星2")]
    public float twoStarTime = 60f;

    [Header("開始時の地図表示")]

    [Tooltip("ステージ開始時のMapContentの位置")]
    public Vector2 mapStartPosition = Vector2.zero;

    [Tooltip("ステージ開始時の地図拡大率")]
    public float mapStartScale = 1f;
}