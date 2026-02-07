using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public int Value { get; private set; }
    public bool isWildcard;

    private Image _bgImage;
    private Text _textComp;

    // Called manually by BoardManager after AddComponent
    public void Init(float size)
    {
        // 1. Setup RectTransform
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        
        // FORCE BOTTOM CENTER ANCHORS
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = Vector2.one * size;

        // 2. Setup Image (Background)
        _bgImage = gameObject.AddComponent<Image>();
        _bgImage.type = Image.Type.Simple;

        
        // 3. Setup Text (Number) - Force Legacy Text for reliability
        GameObject textObj = new GameObject("NumText");
        textObj.transform.SetParent(transform, false);
        
        _textComp = textObj.AddComponent<Text>();
        _textComp.alignment = TextAnchor.MiddleCenter;
        _textComp.color = Color.black; 
        _textComp.fontSize = (int)(size * 0.5f); // 50 for 100 size
        _textComp.fontStyle = FontStyle.Bold;
        
        // Guaranteed Font Loading (Arial is standard)
        _textComp.font = Font.CreateDynamicFontFromOSFont("Arial", 50);
        // Fallback
        if (_textComp.font == null) _textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }

    public void Setup(int newValue)
    {
        Value = newValue;
        if (_textComp) _textComp.text = Value.ToString();
        UpdateVisuals();
    }

    public void SetWildcard()
    {
        isWildcard = true;
        Value = 0;
        if (_textComp) _textComp.text = "?";
        UpdateVisuals();
    }

    public void UpdateValue(int newValue)
    {
        isWildcard = false;
        Value = newValue;
        if (_textComp) _textComp.text = Value.ToString();
        UpdateVisuals();
        
        // Punchier Pop Animation (Elastic Feel)
        StopAllCoroutines();
        StartCoroutine(AnimateScale());
    }

    private System.Collections.IEnumerator AnimateScale()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        
        // Overshoot for juice
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Elastic overshoot curve
            float s = 1.0f + Mathf.Sin(t * Mathf.PI) * 0.4f; 
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private void UpdateVisuals()
    {
        if (_bgImage == null) return;
        
        Color c = GetColorForValue(Value);
        if (isWildcard) c = Color.white;
        _bgImage.color = c;
        
        // Debugging the "White 2" issue
        if (Value > 0 && c == Color.white)
        {
             Debug.LogWarning($"<color=orange>[VISUAL BUG] Tile {name} (Value:{Value}) is showing WHITE. c={c}</color>");
        }
    }

    private Color GetColorForValue(int v)
    {
        // VIBRANT RGB PALETTE (No more plain white/beige)
        switch (v)
        {
            case 2: return Color.red;
            case 4: return Color.green;
            case 8: return Color.blue;
            case 16: return Color.yellow;
            case 32: return Color.cyan;
            case 64: return Color.magenta;
            case 128: return new Color(1f, 0.5f, 0f); // Orange
            case 256: return new Color(0.5f, 0f, 1f); // Purple
            case 512: return new Color(0f, 1f, 0.5f); // Lime
            case 1024: return Color.gray;
            case 2048: return Color.black; 
            default: return Color.white;
        }
    }
}
