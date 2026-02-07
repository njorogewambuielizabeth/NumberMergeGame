using System;
using UnityEngine;

public enum GameState
{
    Waiting,    // Menu or Pre-start
    Playing,    // Active gameplay
    GameOver,   // Board full
    Paused      // Optional
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<GameState> OnStateChanged;
    public event Action<int> OnScoreChanged;
    public event Action<int> OnBestScoreChanged;

    public GameState CurrentState { get; private set; }
    
    public int Score { get; private set; }
    public int BestScore { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Debug")]
    public bool autoStart = true;

    private void Start()
    {
        LoadBestScore();
        if (autoStart)
            StartGame();
        else
            SetState(GameState.Waiting);
    }

    public void StartGame()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
        SetState(GameState.Playing);
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        
        SetState(GameState.GameOver);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayGameOver();
        
        if (Score > BestScore)
        {
            BestScore = Score;
            SaveBestScore();
            OnBestScoreChanged?.Invoke(BestScore);
        }
    }

    public void AddScore(int amount)
    {
        if (CurrentState != GameState.Playing) return;

        Score += amount;
        OnScoreChanged?.Invoke(Score);
        
        // Optional: Check if we beat best score live
        if (Score > BestScore)
        {
            // We can update best score live or only at end. 
            // Let's update it only on Game Over generally, 
            // but maybe UI wants to know if we are currently beating it?
            // For simplicity, we just track current score here.
        }
    }

    public void RestartGame()
    {
        // Logic to reload scene or reset board is usually handled by BoardManager or a SceneController
        // We just reset data here
        StartGame();
    }

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        Debug.Log($"Game State Changed to: {newState}");
    }

    private void LoadBestScore()
    {
        BestScore = PlayerPrefs.GetInt("BestScore", 0);
        OnBestScoreChanged?.Invoke(BestScore);
    }

    private void SaveBestScore()
    {
        PlayerPrefs.SetInt("BestScore", BestScore);
        PlayerPrefs.Save();
    }
}
