using UnityEngine;

// A place where the player can build one tower.
// Build spots are on their own layer, so the click raycast ignores everything else.
[RequireComponent(typeof(Collider))]
public class BuildSpot : MonoBehaviour
{
    Tower tower;

    public bool IsOccupied => tower != null;

    // The top of the spot, where the tower should stand.
    public Vector3 BuildPosition
    {
        get
        {
            Bounds bounds = GetComponent<Collider>().bounds;
            return new Vector3(transform.position.x, bounds.max.y, transform.position.z);
        }
    }

    public void PlaceTower(Tower newTower)
    {
        tower = newTower;
    }
}
