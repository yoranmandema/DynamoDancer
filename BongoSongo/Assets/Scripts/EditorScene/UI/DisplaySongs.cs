﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplaySongs : MonoBehaviour {
    public GameObject levelButton;
    public Transform contentPanel;
    public AudioClip[] songs;
    public List<string> songPaths = new List<string>();
    public GameObject panel;
    public GameObject levelPanel;
    public Button okButton;

    private List<SongButton> songButtons = new List<SongButton>();
    private SelectableButton.SelectableButtonPool selectableButtonPool = new SelectableButton.SelectableButtonPool();

    private struct SongButton {
        public AudioClip song;
        public SelectableButton button;
    }

#if UNITY_EDITOR
    void OnValidate() {
        songPaths.Clear();

        foreach (var clip in songs) {
            var path = UnityEditor.AssetDatabase.GetAssetPath(clip);
            var regex = new System.Text.RegularExpressions.Regex(@"^(Assets/Resources/)|(.mp3|.wav)$");
            path = regex.Replace(path, "");

            songPaths.Add(path);
        }
    }
#endif

    void Start() {
        GenerateButtons();

        okButton.onClick.AddListener(Ok);
    }

    void Ok () {
        var i = 0;

        foreach (var songButton in songButtons) {
            if (songButton.button.IsSelected) {
                EditorManager.instance.level = new Level();
                EditorManager.instance.level.song = new SongStandard {
                    audioClipFile = songPaths[i]
                };

                EditorManager.instance.LoadSong();
                EditorManager.instance.levelInfo.SetInfo();

                panel.SetActive(false);
                levelPanel.SetActive(false);

                break;
            }

            i++;
        }
    }

    public void GenerateButtons() {
        foreach (Transform child in contentPanel) {
            Destroy(child.gameObject);
        }

        songButtons.Clear();

        foreach (var song in songs) {
            var button = Instantiate(levelButton, contentPanel);

            button.GetComponentInChildren<Text>().text = song.name;

            var buttonComponent = button.GetComponent<SelectableButton>();

            buttonComponent.AddToPool(selectableButtonPool);

            songButtons.Add(new SongButton {
                button = buttonComponent,
                song = song
            });
        }
    }
}