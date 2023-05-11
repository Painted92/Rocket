using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreCounter : MonoBehaviour
{
    private TMP_Text _scoreText;
    private int _counterScore = 0;
    void Start()
    {
        _scoreText = GetComponent<TMP_Text>();
        _scoreText.text =$"Score:{_counterScore}";
    }

    public void AddScore()
    {
        _counterScore++;
        _scoreText.text = $"Score:{_counterScore}";
    }
}
