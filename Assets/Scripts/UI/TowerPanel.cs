using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The tower selection panel at the bottom of the screen.
// Clicking a button tells the tower builder which tower to place next.
public class TowerPanel : MonoBehaviour
{
    [Serializable]
    public class TowerOption
    {
        public string displayName;
        public Tower towerPrefab;
        public Button button;
    }

    [SerializeField] TowerBuilder builder;
    [SerializeField] TowerOption[] options;
    [SerializeField] Color normalColor = new Color(0.18f, 0.22f, 0.30f);
    [SerializeField] Color selectedColor = new Color(0.20f, 0.55f, 0.30f);

    void Start()
    {
        foreach (TowerOption option in options)
        {
            TMP_Text label = option.button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = option.displayName + "\n$" + option.towerPrefab.Cost;

            TowerOption clicked = option;
            option.button.onClick.AddListener(() => Select(clicked));
        }
        RefreshColors();
    }

    void Select(TowerOption option)
    {
        builder.SelectTower(option.towerPrefab);
        RefreshColors();
    }

    // The selected tower's button turns green.
    void RefreshColors()
    {
        foreach (TowerOption option in options)
        {
            Image image = option.button.GetComponent<Image>();
            if (image != null)
                image.color = builder.SelectedTower == option.towerPrefab ? selectedColor : normalColor;
        }
    }
}
