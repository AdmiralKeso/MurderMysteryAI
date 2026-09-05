using UnityEngine;
using UnityEngine.UI;

// Minimal stand-in for a native dropdown: click to cycle through a fixed set
// of options. Used for the scenario/resolution/screen-type pickers in the
// in-game menu — avoids hand-building Unity's more involved Dropdown widget
// hierarchy (Template/Viewport/Scrollbar) purely through an editor script.
public class CycleSelector : MonoBehaviour
{
    [SerializeField] private Text valueText;
    [SerializeField] private Button button;

    private string[] labels;
    private string[] values;
    private int index;

    public int CurrentIndex => index;
    public string CurrentValue => values != null && values.Length > 0 ? values[index] : null;

    public void Setup(string[] displayLabels, string[] optionValues = null)
    {
        labels = displayLabels;
        values = optionValues ?? displayLabels;
        index = 0;
        button.onClick.AddListener(Cycle);
        Refresh();
    }

    public void SetIndex(int newIndex)
    {
        if (labels == null || labels.Length == 0)
        {
            return;
        }

        index = ((newIndex % labels.Length) + labels.Length) % labels.Length;
        Refresh();
    }

    private void Cycle()
    {
        SetIndex(index + 1);
    }

    private void Refresh()
    {
        valueText.text = labels[index];
    }
}
