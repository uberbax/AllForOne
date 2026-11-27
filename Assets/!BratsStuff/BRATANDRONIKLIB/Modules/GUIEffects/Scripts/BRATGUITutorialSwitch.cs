using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BRATANDRONIKLIB
{
    [DisallowMultipleComponent]
    public sealed class BRATGUITutorialSwitch : MonoBehaviour
    {
        [Tooltip("Tutorial pages in reading order. Drag entries to reorder them.")]
        public List<GameObject> pageObjects = new List<GameObject>();
        [SerializeField] private Button previous;
        [SerializeField] private Button next;
        [SerializeField] private Button skip;
        [SerializeField] private GameObject previousControl;
        [SerializeField] private GameObject nextControl;
        [SerializeField, Range(0.05f, 0.4f)] private float fadeDuration = 0.14f;

        private readonly List<CanvasGroup> pages = new List<CanvasGroup>();
        private TMP_Text forwardLabel;
        private int current;
        private Coroutine transition;

        private void OnEnable()
        {
            forwardLabel = nextControl.GetComponentInChildren<TMP_Text>(true);
            pages.Clear();
            foreach (var page in pageObjects)
            {
                if (!page) continue;
                if (!page.TryGetComponent<CanvasGroup>(out var group))
                    group = page.AddComponent<CanvasGroup>();
                if (!pages.Contains(group))
                    pages.Add(group);
            }

            current = 0;
            for (int i = 0; i < pages.Count; i++)
                Show(pages[i], i == current);

            previous.onClick.AddListener(Previous);
            next.onClick.AddListener(Next);
            skip.onClick.AddListener(Close);
            UpdateNavigation();
        }

        private void OnDisable()
        {
            previous.onClick.RemoveListener(Previous);
            next.onClick.RemoveListener(Next);
            skip.onClick.RemoveListener(Close);
            if (transition != null)
            {
                StopCoroutine(transition);
                transition = null;
            }
        }

        private static void Show(CanvasGroup page, bool visible)
        {
            page.alpha = 1f;
            page.gameObject.SetActive(visible);
        }

        private void Previous()
        {
            if (transition == null && current > 0)
                transition = StartCoroutine(SwitchTo(current - 1));
        }

        private void Next()
        {
            if (transition != null || pages.Count == 0) return;
            if (current == pages.Count - 1)
                Close();
            else
                transition = StartCoroutine(SwitchTo(current + 1));
        }

        private IEnumerator SwitchTo(int target)
        {
            previousControl.SetActive(false);
            nextControl.SetActive(false);
            yield return Fade(pages[current], 1f, 0f);
            Show(pages[current], false);
            current = target;
            var incoming = pages[current];
            incoming.alpha = 0f;
            incoming.gameObject.SetActive(true);
            yield return Fade(incoming, 0f, 1f);
            transition = null;
            UpdateNavigation();
        }

        private IEnumerator Fade(CanvasGroup page, float from, float to)
        {
            for (float time = 0f; time < fadeDuration; time += Time.unscaledDeltaTime)
            {
                page.alpha = Mathf.SmoothStep(from, to, time / fadeDuration);
                yield return null;
            }
            page.alpha = to;
        }

        public static string ForwardCaption(int pageIndex, int pageCount)
        {
            return pageIndex == pageCount - 1 ? "Finish" : "Next...";
        }

        private void UpdateNavigation()
        {
            previousControl.SetActive(current > 0);
            forwardLabel.text = ForwardCaption(current, pages.Count);
            nextControl.SetActive(pages.Count > 0);
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
