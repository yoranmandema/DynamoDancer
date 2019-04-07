﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager instance;

    public int score;
    public float buttonSize;

    void Awake() {
        if (instance == null) {
            instance = this;
        }
        else {
            Debug.LogError("Too many game managers in the scene!");
        }
    }

    public void AddScore(int add) {
        score += add;

        InterScene.instance.score = score;
    }

    void Update() {

    }
}
