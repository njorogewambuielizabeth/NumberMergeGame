using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestScoreText;
    // public TextMeshProUGUI nextBlockValueText; // For future next block preview

    [Header("Screens")]
    public GameObject mainMenuPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    private void Start()
    {
        // --- Auto-Fix: Build UI ---
        // 1. Aggressively clean up old/broken UI objects from scene
        var oldTop = GameObject.Find("TopBar");
        if (oldTop != null) DestroyImmediate(oldTop);
        
        var oldGameUI = GameObject.Find("GameUI");
        if (oldGameUI != null) DestroyImmediate(oldGameUI);

        var oldGen = GameObject.Find("TopBar_Generated");
        if (oldGen != null) DestroyImmediate(oldGen);

        // 2. Always Build new Premium UI
        BuildCustomUI();

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(40, 44, 52, 255); 
        }

        // Auto-connect UI if missing
        if (scoreText == null)
        {
            var obj = GameObject.Find("ScoreText");
            if (obj != null) scoreText = obj.GetComponent<TextMeshProUGUI>();
        }
        
        if (bestScoreText == null)
        {
            var obj = GameObject.Find("BestScoreText");
            if (obj == null) obj = GameObject.Find("HighScoreText");
            if (obj != null) bestScoreText = obj.GetComponent<TextMeshProUGUI>();
        }

        // Fix BackButton artifact
        var backBtn = GameObject.Find("BackButton");
        if (backBtn != null)
        {
             // If the user wants to remove the arrow text:
             var txt = backBtn.GetComponentInChildren<TextMeshProUGUI>();
             if (txt != null) txt.text = ""; 
             var legTxt = backBtn.GetComponentInChildren<Text>();
             if (legTxt != null) legTxt.text = "";
        }

        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScoreUI;
            GameManager.Instance.OnBestScoreChanged += UpdateBestScoreUI;
            GameManager.Instance.OnStateChanged += HandleStateChange;
            
            // Init UI
            UpdateScoreUI(GameManager.Instance.Score);
            UpdateBestScoreUI(GameManager.Instance.BestScore);
            HandleStateChange(GameManager.Instance.CurrentState);
        }

        // Start Fade Transition
        StartCoroutine(FadeIn());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        GameObject fadeObj = new GameObject("ScreenFade", typeof(RectTransform), typeof(Image));
        fadeObj.transform.SetParent(GetComponentInParent<Canvas>().transform, false);
        fadeObj.transform.SetAsLastSibling();
        
        RectTransform rt = fadeObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = fadeObj.GetComponent<Image>();
        img.color = Color.black;

        float dur = 0.5f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            img.color = new Color(0, 0, 0, 1f - (elapsed / dur));
            yield return null;
        }
        Destroy(fadeObj);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
            GameManager.Instance.OnBestScoreChanged -= UpdateBestScoreUI;
            GameManager.Instance.OnStateChanged -= HandleStateChange;
        }
    }

    private void UpdateScoreUI(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString();
    }

    private void UpdateBestScoreUI(int bestScore)
    {
        if (bestScoreText != null) bestScoreText.text = bestScore.ToString();
    }

    private void HandleStateChange(GameState state)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(state == GameState.Waiting);
        if (gameOverPanel != null) 
        {
            gameOverPanel.SetActive(state == GameState.GameOver);
            if (state == GameState.GameOver)
            {
                if (finalScoreText != null) finalScoreText.text = GameManager.Instance.Score.ToString();
            }
        }
    }

    // Called by UI Button
    public void OnPlayButtonClicked()
    {
        if (SoundManager.Instance != null) 
        {
            SoundManager.Instance.ResumeAudioContext();
            SoundManager.Instance.PlayClick();
        }
        GameManager.Instance.StartGame();
    }

    public void OnRestartButtonClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        GameManager.Instance.RestartGame();
    }

    // --- UI Generation Helpers ---

    void BuildCustomUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // 1. Setup Canvas Scaler for consistent look
        var scaler = canvas.gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        // 2. Custom Top Bar
        GameObject topBar = new GameObject("TopBar_Generated", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        topBar.transform.SetParent(canvas.transform, false);
        var topRt = topBar.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0, 1);
        topRt.anchorMax = new Vector2(1, 1);
        topRt.pivot = new Vector2(0.5f, 1);
        topRt.anchoredPosition = new Vector2(0, -50);
        topRt.sizeDelta = new Vector2(0, 250); // Larger Top Bar

        var hlg = topBar.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 40;
        hlg.padding = new RectOffset(40, 40, 20, 20);
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // Elements
        CreateCircleButton(topBar.transform, "Btn_Back", "<", false);
        
        var scorePanel = CreateScorePanel(topBar.transform, "MEDIUM MODE", new Color32(40, 40, 40, 255));
        scoreText = scorePanel.transform.Find("ValueText").GetComponent<TextMeshProUGUI>();

        var bestPanel = CreateScorePanel(topBar.transform, "ALL TIME", new Color32(40, 40, 40, 255));
        bestScoreText = bestPanel.transform.Find("ValueText").GetComponent<TextMeshProUGUI>();
        // Best Score Title Color (Teal/Green)
        bestPanel.transform.Find("TitleText").GetComponent<TextMeshProUGUI>().color = new Color32(0, 255, 180, 255);

        CreateCircleButton(topBar.transform, "Btn_Restart", "R", true);

        // 3. Main Menu Overlay (The "Play Button like Unity Hub")
        mainMenuPanel = new GameObject("MainMenu_Generated", typeof(RectTransform), typeof(Image));
        mainMenuPanel.transform.SetParent(canvas.transform, false);
        var menuRt = mainMenuPanel.GetComponent<RectTransform>();
        menuRt.anchorMin = Vector2.zero;
        menuRt.anchorMax = Vector2.one;
        menuRt.sizeDelta = Vector2.zero;
        
        mainMenuPanel.GetComponent<Image>().color = new Color32(30, 30, 30, 255); // Solid Opacity for reliability
        mainMenuPanel.transform.SetAsLastSibling();

        GameObject playBtnObj = new GameObject("PlayButton", typeof(RectTransform), typeof(Image), typeof(Button));
        playBtnObj.transform.SetParent(mainMenuPanel.transform, false);
        var playRt = playBtnObj.GetComponent<RectTransform>();
        playRt.sizeDelta = new Vector2(400, 150);
        playRt.anchoredPosition = Vector2.zero;
        
        playBtnObj.GetComponent<Image>().color = new Color32(255, 100, 100, 255);
        playBtnObj.GetComponent<Button>().onClick.AddListener(OnPlayButtonClicked);

        GameObject playTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        playTxtObj.transform.SetParent(playBtnObj.transform, false);
        var playTxt = playTxtObj.GetComponent<TextMeshProUGUI>();
        playTxt.text = "PLAY";
        playTxt.fontSize = 60;
        playTxt.alignment = TextAlignmentOptions.Center;
        playTxt.color = Color.white;
        playTxt.fontStyle = FontStyles.Bold;
        playTxt.rectTransform.anchorMin = Vector2.zero;
        playTxt.rectTransform.anchorMax = Vector2.one;

        // 4. GameOver Overlay
        gameOverPanel = new GameObject("GameOver_Generated", typeof(RectTransform), typeof(Image));
        gameOverPanel.transform.SetParent(canvas.transform, false);
        var goRt = gameOverPanel.GetComponent<RectTransform>();
        goRt.anchorMin = Vector2.zero;
        goRt.anchorMax = Vector2.one;
        goRt.sizeDelta = Vector2.zero;
        gameOverPanel.GetComponent<Image>().color = new Color32(100, 30, 30, 230);
        gameOverPanel.SetActive(false);

        GameObject goTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        goTxtObj.transform.SetParent(gameOverPanel.transform, false);
        var goTxt = goTxtObj.GetComponent<TextMeshProUGUI>();
        goTxt.text = "GAME OVER";
        goTxt.fontSize = 80;
        goTxt.alignment = TextAlignmentOptions.Center;
        goTxt.rectTransform.anchoredPosition = new Vector2(0, 100);

        GameObject scoreLabel = new GameObject("ScoreLabel", typeof(RectTransform));
        scoreLabel.transform.SetParent(gameOverPanel.transform, false);
        finalScoreText = scoreLabel.AddComponent<TextMeshProUGUI>();
        finalScoreText.text = "0";
        finalScoreText.fontSize = 120;
        finalScoreText.alignment = TextAlignmentOptions.Center;

        GameObject restartBtnObj = new GameObject("GameOverRestart", typeof(RectTransform), typeof(Image), typeof(Button));
        restartBtnObj.transform.SetParent(gameOverPanel.transform, false);
        var reRt = restartBtnObj.GetComponent<RectTransform>();
        reRt.sizeDelta = new Vector2(300, 100);
        reRt.anchoredPosition = new Vector2(0, -200);
        restartBtnObj.GetComponent<Image>().color = Color.white;
        restartBtnObj.GetComponent<Button>().onClick.AddListener(OnRestartButtonClicked);
        
        GameObject reTxtObj = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        reTxtObj.transform.SetParent(restartBtnObj.transform, false);
        var reTxt = reTxtObj.GetComponent<TextMeshProUGUI>();
        reTxt.text = "RESTART";
        reTxt.fontSize = 40;
        reTxt.color = Color.black;
        reTxt.alignment = TextAlignmentOptions.Center;
        reTxt.rectTransform.anchorMin = Vector2.zero;
        reTxt.rectTransform.anchorMax = Vector2.one;
    }

    GameObject CreateCircleButton(Transform parent, string name, string symbol, bool isRestart = false)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 120); // Larger Buttons
        // White circle background
        btnObj.GetComponent<Image>().color = Color.white; 
        
        var btn = btnObj.GetComponent<Button>();
        if (isRestart) btn.onClick.AddListener(OnRestartButtonClicked);
        
        GameObject txtObj = new GameObject("Symbol", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        var tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = symbol;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 60; // Larger Icon
        tmp.color = new Color32(255, 100, 100, 255); // Salmon Color
        tmp.rectTransform.anchorMin = Vector2.zero;
        tmp.rectTransform.anchorMax = Vector2.one;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return btnObj;
    }

    GameObject CreateScorePanel(Transform parent, string title, Color bgColor)
    {
        GameObject panel = new GameObject("ScorePanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 120); // Larger Panels
        panel.GetComponent<Image>().color = bgColor;
        
        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panel.transform, false);
        var t = titleObj.GetComponent<TextMeshProUGUI>();
        t.text = title;
        t.fontSize = 24; // Larger Title
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.fontStyle = FontStyles.Bold;
        
        GameObject valObj = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        valObj.transform.SetParent(panel.transform, false);
        var v = valObj.GetComponent<TextMeshProUGUI>();
        v.text = "0";
        v.fontSize = 40; // Larger Numbers
        v.alignment = TextAlignmentOptions.Center;
        v.color = Color.white;
        v.fontStyle = FontStyles.Bold;
        
        return panel;
    }
}
