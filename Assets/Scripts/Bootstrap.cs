using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureManagersExist()
    {
        Debug.Log("Bootstrap: Checking for essential Managers...");

        // 1. Ensure UIManager
        if (Object.FindFirstObjectByType<UIManager>() == null)
        {
            Debug.Log("Bootstrap: Creating missing UIManager...");
            GameObject uiGo = new GameObject("UIManager");
            uiGo.AddComponent<UIManager>();
        }

        // 2. Ensure GameManager (if it assumes it's a singleton)
        if (Object.FindFirstObjectByType<GameManager>() == null)
        {
             Debug.Log("Bootstrap: Creating missing GameManager...");
             GameObject gmGo = new GameObject("GameManager");
             gmGo.AddComponent<GameManager>();
        }

        // 3. Ensure SoundManager
        if (Object.FindFirstObjectByType<SoundManager>() == null)
        {
            Debug.Log("Bootstrap: Creating missing SoundManager...");
            GameObject smGo = new GameObject("SoundManager");
            smGo.AddComponent<SoundManager>();
        }
    }
}
