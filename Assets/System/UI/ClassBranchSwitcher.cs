using UnityEngine;
using UnityEngine.UI;

/// <summary>Switches the displayed class tree without changing the player's class.</summary>
public sealed class ClassBranchSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject[] branches = new GameObject[0];
    [SerializeField] private Button[] previousButtons = new Button[0];
    [SerializeField] private Button[] nextButtons = new Button[0];
    private int currentIndex;
    private bool initialized;

    public int CurrentIndex => currentIndex;

    private void OnEnable()
    {
        foreach (var button in previousButtons)
            if (button != null) { button.onClick.RemoveListener(Previous); button.onClick.AddListener(Previous); }
        foreach (var button in nextButtons)
            if (button != null) { button.onClick.RemoveListener(Next); button.onClick.AddListener(Next); }
        if (!initialized)
        {
            for (int i = 0; i < branches.Length; i++)
                if (branches[i] != null && branches[i].activeSelf) { currentIndex = i; break; }
            initialized = true;
        }
        ShowBranch(currentIndex);
    }

    private void OnDisable()
    {
        foreach (var button in previousButtons) if (button != null) button.onClick.RemoveListener(Previous);
        foreach (var button in nextButtons) if (button != null) button.onClick.RemoveListener(Next);
    }

    public void Previous() => ShowBranch(currentIndex - 1);
    public void Next() => ShowBranch(currentIndex + 1);

    public void ShowBranch(int index)
    {
        if (branches.Length == 0) return;
        currentIndex = Mathf.Clamp(index, 0, branches.Length - 1);

        for (int i = 0; i < branches.Length; i++)
        {
            //if (branches[i] != null) branches[i].SetActive(i == currentIndex);
            if (branches[i] != null)
            {
                branches[i].transform.localScale = (i == currentIndex ? Vector3.one : Vector3.zero);
            }
        }
        
        foreach (var button in previousButtons) if (button != null) button.interactable = currentIndex > 0;
        foreach (var button in nextButtons) if (button != null) button.interactable = currentIndex < branches.Length - 1;
    }
}
