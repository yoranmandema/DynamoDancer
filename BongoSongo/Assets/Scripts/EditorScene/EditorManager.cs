﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;

public enum Indicators {
    Button,
    Slider
}

[System.Serializable]
public struct EditorPrefab {
    public Indicators indicator;
    public GameObject prefab;
    public Sprite preview;
}

public class EditorManager : MonoBehaviour {
    public static EditorManager instance;

    public EditorPrefab currentPrefab;
    public Selector selected;
    public Level level;
    public Transform indicatorParent;
    public int beat;
    public Text beatText;
    public LevelInfo levelInfo;
    public Texture selectedTexture;

    public List<EditorPrefab> editorPrefabs = new List<EditorPrefab>();

    public float beatLength;
    public int beatsTotal;

    private TimelineDrawer timelineDrawer;

    void Awake() {
        instance = this;
    }

    void Start() {
        level = new Level {
            name = "test"
        };

        currentPrefab = editorPrefabs[0];

        timelineDrawer = FindObjectOfType<TimelineDrawer>();
        levelInfo = FindObjectOfType<LevelInfo>();
    }

    void Update() {
        if (Input.GetKey(KeyCode.LeftControl)) {
            if (Input.GetKeyDown(KeyCode.S)) {
                Save();
            }
        }

        if (Input.GetKeyDown(KeyCode.Delete)) {
            DeleteIndicator();
        }
    }

    public void LoadSong () {
        if (!level) return;

        var clip = level.song.GenerateClip();

        timelineDrawer.RedrawTimeline(clip);

        beatLength = 60f / level.bpm;

        beatsTotal = (int)Mathf.Floor(clip.length / beatLength);
    }

    public void IncreaseBeat () {
        SetBeat(beat + 1);

        if (beat >= beatsTotal - 1) SetBeat(0);
    }

    public void DecreaseBeat() {
        SetBeat(beat - 1);

        if (beat <= 0) SetBeat(beatsTotal - 1);
    }


    public void SetBeat (int num) {
        beat = num;

        beatText.text = "Beat: " + beat;

        SetVisible();
    }

    private IndicatorInfo[] GetOrderedIndicators () {
        var infos = new List<IndicatorInfo>();

        foreach (Transform child in indicatorParent) infos.Add(child.GetComponent<IndicatorInfo>());

        var ordered = infos.OrderBy(x => x.beat).ToArray();

        return ordered;
    }

    private void SetVisible () {
        var ordered = GetOrderedIndicators();

        for (int i = 0; i < ordered.Length; i++) {
            ordered[i].gameObject.SetActive(Mathf.Abs(beat - ordered[i].beat) < 8);

            var renderers = ordered[i].gameObject.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers) {
                var col = renderer.material.color;

                var alpha = (1f - Mathf.Abs(beat - ordered[i].beat) / 8f);

                renderer.material.color = new Color(col.r, col.g, col.b, alpha);
            }
        }
    }

    public void OrderIndicators () {
        var ordered = GetOrderedIndicators();

        for (int i = 0; i < ordered.Length; i++) {
            ordered[i].transform.SetSiblingIndex(i);
        }
    }

    public List<SpawnInfo> GenerateSpawnInfoList () {
        var ordered = GetOrderedIndicators();
        var list = new List<SpawnInfo>();

        for (int i = 0; i < ordered.Length; i++) {
            var spawnInfo = new SpawnInfo {
                beat = ordered[i].beat
            };

            var pos = ordered[i].transform.position / Camera.main.orthographicSize;

            spawnInfo.position.x = pos.x;
            spawnInfo.position.y = pos.y;

            list.Add(spawnInfo);
        }

        return list;
    }

    public void Unselect () {
        if (selected) {
            selected.Unselect();

            selected = null;
        }
    }

    public void CreateNewIndicator () {
        var instance = Instantiate(currentPrefab.prefab, Vector3.zero, Quaternion.identity, indicatorParent);

        instance.GetComponent<IndicatorInfo>().beat = beat;

        OrderIndicators();
    }

    public void DeleteIndicator () {
        if (selected) {
            var indicatorInfo = selected.GetComponent<IndicatorInfo>();

            level.spawnInfo.RemoveAt(indicatorInfo.spawnInfoIndex);

            Destroy(selected);
        }
    }

    public void SetName(TMPro.TMP_InputField inputField) {
        level.name = inputField.text;
    }

    public void SetBPM(TMPro.TMP_InputField inputField) {
        level.bpm = int.Parse(inputField.text);

        LoadSong();
    }

    public void Save () {
        var spawnInfoList = GenerateSpawnInfoList();

        level.spawnInfo = spawnInfoList;

        Level.Save(level);
    }

    public void Load (string file) {
        foreach (Transform child in indicatorParent) {
            Destroy(child.gameObject);
        }

        level = Level.Load(file);

        levelInfo.SetInfo();
        LoadSong();

        var i = 0;

        foreach (var spawnInfo in level.spawnInfo) {
            var prefab = editorPrefabs.Find(x => x.indicator == spawnInfo.indicator).prefab;
            var position = new Vector2 (spawnInfo.position.x, spawnInfo.position.y) * Camera.main.orthographicSize;

            var instance = Instantiate(prefab, position, Quaternion.identity, indicatorParent);
            instance.GetComponent<IndicatorInfo>().beat = spawnInfo.beat;
            instance.GetComponent<IndicatorInfo>().spawnInfoIndex = spawnInfo.beat;

            i = 0;
        }

        OrderIndicators();
    }

    public void New() {
        Level.Save(level);

        level = new Level();

        levelInfo.SetInfo();
    }

    public void Play () {
        var interScene = InterScene.Instance;

        interScene.level = level;

        SceneManager.LoadScene("GameplayScene");
    }

    public void PlayAtBeat () {
        var interEditor = InterSceneEditorInformation.Instance;

        interEditor.beat = beat;

        var interScene = InterScene.Instance;

        interScene.level = level;

        SceneManager.LoadScene("GameplayScene");
    }

    private GUIStyle guiStyle = new GUIStyle();

    public void OnGUI() {
        guiStyle.fontSize = 40;
        guiStyle.normal.textColor = Color.white;

        if (selected) {
            var bounding = selected.GetComponentInChildren<Renderer>().bounds;

            var center = Camera.main.WorldToScreenPoint(bounding.center);
            var extents = bounding.extents / Camera.main.orthographicSize * Camera.main.pixelHeight * 1.25f;

            GUI.DrawTexture(new Rect(center.x - extents.x/2, Camera.main.pixelHeight - center.y - extents.y / 2, extents.x, extents.y), selectedTexture);
        }

        var indicatorInfos = FindObjectsOfType<IndicatorInfo>();

        foreach (var indicatorInfo in indicatorInfos) {
            var bounding = indicatorInfo.gameObject.GetComponentInChildren<Renderer>().bounds;

            var center = Camera.main.WorldToScreenPoint(bounding.center);
            var extents = bounding.extents / Camera.main.orthographicSize * Camera.main.pixelHeight * 1.25f;

            GUI.Label(new Rect(center.x + extents.x/2, Camera.main.pixelHeight - center.y + extents.y / 2, extents.x, extents.y), "" + indicatorInfo.beat, guiStyle);
        }
    }
}
