using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Metaballs2D : MonoBehaviour
{
    private new CircleCollider2D collider;
    public Color color= Color.white;
    private void Awake()
    {
        collider = GetComponent<CircleCollider2D>();
        MetaballSystem2D.Add(this);
    }

    public float GetRadius()
    {
        return collider.radius;
    }

    private void OnDestroy()
    {
        MetaballSystem2D.Remove(this);
    }
}
