using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int columns = 5;
    public int rows = 8;
    public float tileSize = 100f; 
    public float spacing = 10f;
    public RectTransform boardHolder;

    [Header("Game Settings")]
    public float dropSpeed = 3000f; // Fast drop
    [Range(0f, 1f)] public float wildcardChance = 0.05f;

    private Tile[,] grid;
    private Tile pendingTile;
    private Tile nextTile;
    private RectTransform nextTileContainer;
    private bool isBusy = false;
    private int maxSpawnPower = 1;

    void Start()
    {
        // 1. CLEANUP
        string[] tags = { "BoardContainer", "BoardHolder", "NextPreviewContainer", "Tile_Gen", "Tile_Preview" };
        foreach (string tag in tags) {
            var g = GameObject.Find(tag);
            while (g != null) { DestroyImmediate(g); g = GameObject.Find(tag); }
        }

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();

        // 2. INITIALIZE BOARD CONTAINER
        GameObject boardObj = new GameObject("BoardContainer", typeof(RectTransform));
        if (mainCanvas != null) boardObj.transform.SetParent(mainCanvas.transform, false);
        
        boardHolder = boardObj.GetComponent<RectTransform>();
        float width = columns * tileSize + (columns - 1) * spacing + 40;
        float height = rows * tileSize + (rows - 1) * spacing + 40;
        boardHolder.sizeDelta = new Vector2(width, height);
        boardHolder.anchorMin = new Vector2(0.5f, 0f);
        boardHolder.anchorMax = new Vector2(0.5f, 0f);
        boardHolder.pivot = new Vector2(0.5f, 0f);
        boardHolder.anchoredPosition = Vector2.zero; // Strictly at bottom

        grid = new Tile[columns, rows];
        CreateBackgroundGrid();

        // 3. INITIALIZE PREVIEW CONTAINER
        GameObject nextObj = new GameObject("NextPreviewContainer", typeof(RectTransform));
        if (mainCanvas != null) nextObj.transform.SetParent(mainCanvas.transform, false);
        nextTileContainer = nextObj.GetComponent<RectTransform>();
        nextTileContainer.anchorMin = new Vector2(0.5f, 1f);
        nextTileContainer.anchorMax = new Vector2(0.5f, 1f);
        nextTileContainer.pivot = new Vector2(0.5f, 1f);
        nextTileContainer.anchoredPosition = new Vector2(0, -120); 
        nextTileContainer.sizeDelta = new Vector2(tileSize, tileSize);

        GameObject labelObj = new GameObject("NextLabel"); 
        labelObj.transform.SetParent(nextTileContainer, false);
        Text l = labelObj.AddComponent<Text>(); 
        l.text = "NEXT";
        l.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        l.alignment = TextAnchor.MiddleCenter;
        l.color = Color.white;
        l.fontSize = 40;
        l.fontStyle = FontStyle.Bold;

        RectTransform lRt = labelObj.GetComponent<RectTransform>();
        if (lRt == null) lRt = labelObj.AddComponent<RectTransform>();
        lRt.sizeDelta = new Vector2(300, 100);
        lRt.anchoredPosition = new Vector2(0, 100); 

        // 4. SETUP ENVIRONMENT
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color32(40, 44, 52, 255);
        }

        // 5. START GAME LOGIC (Containers are now guaranteed to exist)
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentState == GameState.Playing) OnGameStateChanged(GameState.Playing);
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Playing)
        {
            ClearBoard();
            SpawnNewTile();
        }
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;
        if (isBusy || pendingTile == null) return;
        HandleInput();
    }

    void SpawnNewTile()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        UpdateDifficulty();

        // 1. Ensure we have a next tile to pull from
        if (nextTile == null) 
        {
            nextTile = CreateNewTileInstance("InitialNextTile");
        }
        else
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySpawn();
        }

        // 2. Promotion: The one in the box becomes the one in hand
        pendingTile = nextTile;
        pendingTile.name = $"PendingTile_{pendingTile.Value}";
        pendingTile.transform.SetParent(boardHolder, false);
        
        RectTransform rt = pendingTile.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, GetRowY(rows - 1) + (tileSize * 0.5f)); 

        // 3. Generation: Create the *new* next one
        nextTile = CreateNewTileInstance("NextTilePreview");
        PositionNextTilePreview();
        
        Debug.Log($"<color=yellow>[DIAGNOSTIC] Spawned: Holding={pendingTile.Value} (ID:{pendingTile.gameObject.GetInstanceID()}), Preview Box={nextTile.Value} (ID:{nextTile.gameObject.GetInstanceID()})</color>");
    }

    private Tile CreateNewTileInstance(string name)
    {
        int power = Random.Range(1, maxSpawnPower + 1); 
        int value = Mathf.RoundToInt(Mathf.Pow(2, power));
        GameObject go = new GameObject(name);
        
        // Parent to the specialized preview container initially
        if (nextTileContainer != null) go.transform.SetParent(nextTileContainer, false);
        else go.transform.SetParent(this.transform.parent, false);
        
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;

        Tile t = go.AddComponent<Tile>();
        t.Init(tileSize);
        if (Random.value < wildcardChance) t.SetWildcard();
        else t.Setup(value);
        return t;
    }

    private void PositionNextTilePreview()
    {
        if (nextTile == null || nextTileContainer == null) return;
        nextTile.transform.SetParent(nextTileContainer, false);
        RectTransform rt = nextTile.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one; // Ensure scale is reset
    }

    // --- Dropping ---

    private void HandleInput()
    {
        if (UnityEngine.InputSystem.Pointer.current == null) return;
        var pointer = UnityEngine.InputSystem.Pointer.current;
        Vector2 screenPos = pointer.position.ReadValue();
        bool wasPressed = pointer.press.wasPressedThisFrame;

        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = Camera.main;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(boardHolder, screenPos, cam, out Vector2 localPos);
        
        if (pendingTile == null || isBusy) return;

        int col = GetColumnFromLocal(localPos.x);
        col = Mathf.Clamp(col, 0, columns - 1);

        RectTransform tileRect = pendingTile.GetComponent<RectTransform>();
        if (tileRect != null)
        {
            Vector2 pos = tileRect.anchoredPosition;
            pos.x = GetColumnX(col);
            tileRect.anchoredPosition = pos;
        }
        
        if (wasPressed) StartCoroutine(DropTile(col));
    }


    IEnumerator DropTile(int column)
    {
        isBusy = true;
        RectTransform pendingRT = pendingTile.GetComponent<RectTransform>();
        pendingRT.SetAsLastSibling();

        int targetRow = GetLowestEmptyRow(column);
        if (targetRow < 0)
        {
             if (IsColumnFull(column)) GameManager.Instance.GameOver();
             Destroy(pendingTile.gameObject);
             pendingTile = null;
             isBusy = false;
             yield break;
        }

        Vector2 startPos = pendingRT.anchoredPosition;
        Vector2 targetPos = new Vector2(GetColumnX(column), GetRowY(targetRow));

        // SMOOTH DROP ANIMATION (Juice!)
        float dur = 0.2f; 
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            pendingRT.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / dur);
            yield return null;
        }
        pendingRT.anchoredPosition = targetPos;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayDrop();

        Tile droppedTile = pendingTile;
        grid[column, targetRow] = droppedTile;
        pendingTile = null;

        yield return MergeRoutine(column, targetRow);

        if (IsBoardFull()) GameManager.Instance.GameOver();
        else SpawnNewTile();

        isBusy = false;
    }

    IEnumerator MergeRoutine(int col, int row)
    {
        if (row <= 0) yield break;

        Tile current = grid[col, row];
        Tile below = grid[col, row - 1];

        if (current == null || below == null) yield break;

        bool shouldMerge = false;
        int newValue = 0;

        if (current.isWildcard) { shouldMerge = true; newValue = below.Value * 2; }
        else if (below.isWildcard) { shouldMerge = true; newValue = current.Value * 2; }
        else if (current.Value == below.Value) { shouldMerge = true; newValue = current.Value * 2; }

        if (shouldMerge)
        {
            below.UpdateValue(newValue);
            Destroy(current.gameObject);
            grid[col, row] = null;
            GameManager.Instance.AddScore(newValue);
            
            if (SoundManager.Instance != null) SoundManager.Instance.PlayMerge();

            yield return new WaitForSeconds(0.1f);
            yield return MergeRoutine(col, row - 1);
        }
    }

    // --- Helpers ---

    float GetColumnX(int col)
    {
        float totalWidth = columns * tileSize + (columns - 1) * spacing;
        float startX = -totalWidth / 2f + tileSize / 2f;
        return startX + col * (tileSize + spacing);
    }
    
    float GetRowY(int row)
    {
        float startY = tileSize / 2f + 20; // Correct: Offset from bottom pivot
        return startY + row * (tileSize + spacing);
    }

    int GetColumnFromLocal(float localX)
    {
        float totalWidth = columns * tileSize + (columns - 1) * spacing;
        float startX = -totalWidth / 2f; 
        int col = Mathf.RoundToInt((localX - startX) / (tileSize + spacing));
        return col; 
    }

    void ClearBoard()
    {
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                grid[x, y] = null;

        if (boardHolder != null)
            foreach (Transform child in boardHolder) Destroy(child.gameObject);
        
        if (nextTileContainer != null)
            foreach (Transform child in nextTileContainer)
                if (child.name.Contains("Tile")) Destroy(child.gameObject);
            
        pendingTile = null;
        nextTile = null;
    }

    int GetLowestEmptyRow(int col)
    {
        for (int r = 0; r < rows; r++)
            if (grid[col, r] == null) return r;
        return -1;
    }
    
    bool IsColumnFull(int col) { return grid[col, rows - 1] != null; }
    bool IsBoardFull() { return false; }
    
    void UpdateDifficulty()
    {
        if (GameManager.Instance.Score > 5000) maxSpawnPower = 4;
        else maxSpawnPower = 2;
    }

    void CreateBackgroundGrid()
    {
        if (boardHolder == null) return;
        GameObject bgGroup = new GameObject("BackgroundGrid", typeof(RectTransform));
        bgGroup.transform.SetParent(boardHolder, false);
        bgGroup.transform.SetAsFirstSibling();
        
        RectTransform rt = bgGroup.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                GameObject slot = new GameObject($"Slot_{x}_{y}", typeof(Image));
                slot.transform.SetParent(bgGroup.transform, false);
                RectTransform slotRT = slot.GetComponent<RectTransform>();
                slotRT.anchorMin = new Vector2(0.5f, 0f);
                slotRT.anchorMax = new Vector2(0.5f, 0f);
                slotRT.pivot = new Vector2(0.5f, 0f);
                slotRT.sizeDelta = Vector2.one * tileSize;
                slotRT.anchoredPosition = new Vector2(GetColumnX(x), GetRowY(y));
                slot.GetComponent<Image>().color = new Color32(60, 60, 60, 255);
            }
        }
    }
}
