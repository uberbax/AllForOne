using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIStartScreen : MonoBehaviour
{
    public static bool BlocksSceneInput =>
        UnityEngine.Object.FindAnyObjectByType<GUIStartScreen>(FindObjectsInactive.Exclude) != null;

    public bool startScreenOff;
    public Button start;
    public Button wish;
    public Button quit;
    public List<UnoLoc> forseTranslate = new List<UnoLoc>();

    private void Awake()
    {
        EventManager.SUB("PARSE_ENDED", OnParseEnded);
        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null) canvas.enabled = !startScreenOff;
    }

    private void Start()
    {
        start?.onClick.AddListener(StartGame);
        if (wish != null)
        {
            wish.interactable = false;
            wish.onClick.AddListener(OpenStorePage);
        }
        quit?.onClick.AddListener(Application.Quit);
        if (startScreenOff && start != null)
            FunctionTimer.Create(() => start.onClick.Invoke(), 0.01f);
        if (ConfigLoader.parseEnded)
            ApplyStaticConfig();
    }

    private void OnDestroy()
    {
        EventManager.UNSUB("PARSE_ENDED", OnParseEnded);
        start?.onClick.RemoveListener(StartGame);
        wish?.onClick.RemoveListener(OpenStorePage);
        quit?.onClick.RemoveListener(Application.Quit);
    }

    private void OnParseEnded(ArgPass _)
    {
        ApplyStaticConfig();
    }

    private void ApplyStaticConfig()
    {
        if (wish != null)
            wish.interactable = IsValidStoreUrl(MainCycle_WhoHeroes.SteamUrl());
    }

    private static bool IsValidStoreUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }

    private void OpenStorePage()
    {
        var url = MainCycle_WhoHeroes.SteamUrl();
        if (IsValidStoreUrl(url))
            Application.OpenURL(url);
    }

    private void StartGame()
    {
        gameObject.SetActive(false);
        EventManager.INV("game_start", new ArgPass());
    }
}
