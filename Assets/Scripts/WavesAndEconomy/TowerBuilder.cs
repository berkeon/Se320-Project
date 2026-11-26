using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

// Buying and placing towers. The tower panel picks which tower to build, then the player
// clicks a build spot. A raycast from the camera only looks at the build spot layer.
public class TowerBuilder : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] LayerMask buildSpotLayer;
    [SerializeField] MoneyManager money;

    public Tower SelectedTower { get; private set; }

    // Called by the tower selection panel.
    public void SelectTower(Tower towerPrefab)
    {
        SelectedTower = towerPrefab;
    }

    void Update()
    {
        if (!LeftClickThisFrame()) return;

        // The game over screen pauses the game (timeScale 0), but Update still runs.
        // Without this check the player could keep buying towers after losing.
        if (Time.timeScale == 0f) return;

        // Clicking a UI button should not also click the build spot behind it.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (SelectedTower == null) return;

        Ray ray = cam.ScreenPointToRay(MousePosition());
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, buildSpotLayer)) return;

        BuildSpot spot = hit.collider.GetComponent<BuildSpot>();
        if (spot == null || spot.IsOccupied) return;

        if (!money.TrySpend(SelectedTower.Cost))
        {
            Debug.Log("Not enough money for " + SelectedTower.name);
            return;
        }

        Tower tower = Instantiate(SelectedTower, spot.BuildPosition, Quaternion.identity);
        spot.PlaceTower(tower);
    }

    // The project can use either the old Input Manager or the new Input System package,
    // so mouse input goes through these two helpers.
    static bool LeftClickThisFrame()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#elif ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return false;
#endif
    }

    static Vector3 MousePosition()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        return Vector3.zero;
#endif
    }
}
