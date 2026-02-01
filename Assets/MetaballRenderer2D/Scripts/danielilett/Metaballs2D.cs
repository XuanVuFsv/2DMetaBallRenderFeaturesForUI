using UnityEngine;
using UnityEngine.UI;

public class Metaballs2D : MonoBehaviour
{
    public enum MetaballType
    {
        Sprite,     // Use CircleCollider2D
        UI          // Use RectTransform
    }

    public enum TextureSource
    {
        SolidColor,     // Use solid color only
        FromComponent,  // Auto-detect from SpriteRenderer, Image, or RawImage
        CustomTexture   // Use custom texture
    }

    [Header("Metaball Settings")]
    public MetaballType type = MetaballType.UI;

    [Tooltip("Enable/disable metaball rendering")]
    public bool renderMetaball = true;

    [Header("Texture Settings")]
    public TextureSource textureSource = TextureSource.FromComponent;

    [Tooltip("Used when textureSource = SolidColor")]
    public Color color = Color.white;

    [Tooltip("Used when textureSource = CustomTexture")]
    public Texture2D customTexture;

    [Header("Connection Settings")]
    [Tooltip("Sprite: World distance | UI: Pixel distance. Only render metaball when within this distance to another metaball.")]
    public float connectionDistance = 100f;

    [Header("UI Settings")]
    [Tooltip("Only for UI type - Use width or height for radius calculation")]
    public bool useWidth = true;

    [Tooltip("Only for UI type - Radius multiplier (0.5 = half of width/height)")]
    [Range(0.1f, 5f)]
    public float radiusMultiplier = 1f;

    // Cache components
    private CircleCollider2D circleCollider;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Camera canvasCamera;
    private SpriteRenderer spriteRenderer;
    private Image imageComponent;
    private RawImage rawImageComponent;

    // Cached texture
    private Texture2D cachedTexture;
    private bool textureCached = false;

    // Track if currently registered
    private bool isRegistered = false;

    // Track previous render state to detect changes
    private bool previousRenderState = true;

    private void Awake()
    {
        // Cache components based on type
        if (type == MetaballType.Sprite)
        {
            circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider == null)
            {
                Debug.LogError($"Metaballs2D on {gameObject.name}: Type is Sprite but no CircleCollider2D found!");
                return;
            }

            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        else if (type == MetaballType.UI)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError($"Metaballs2D on {gameObject.name}: Type is UI but no RectTransform found!");
                return;
            }

            // Find parent canvas
            parentCanvas = GetComponentInParent<Canvas>();

            // Get canvas camera
            if (parentCanvas != null)
            {
                if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvasCamera = null; // Overlay doesn't use camera
                }
                else
                {
                    canvasCamera = parentCanvas.worldCamera;
                }
            }

            imageComponent = GetComponent<Image>();
            rawImageComponent = GetComponent<RawImage>();
        }

        previousRenderState = renderMetaball;
    }

    private void OnEnable()
    {
        // Register when enabled (if renderMetaball is true)
        if (renderMetaball && !isRegistered)
        {
            MetaballSystem2D.Add(this);
            isRegistered = true;
        }
    }

    private void Update()
    {
        // Check if renderMetaball state changed
        if (renderMetaball != previousRenderState)
        {
            if (renderMetaball)
            {
                // Enable rendering
                if (!isRegistered)
                {
                    MetaballSystem2D.Add(this);
                    isRegistered = true;
                }
            }
            else
            {
                // Disable rendering
                if (isRegistered)
                {
                    MetaballSystem2D.Remove(this);
                    isRegistered = false;
                }
            }

            previousRenderState = renderMetaball;
        }
    }

    private void OnDisable()
    {
        // Unregister when disabled
        if (isRegistered)
        {
            MetaballSystem2D.Remove(this);
            isRegistered = false;
        }
    }

    private void OnDestroy()
    {
        // Unregister when destroyed
        if (isRegistered)
        {
            MetaballSystem2D.Remove(this);
            isRegistered = false;
        }
    }

    public bool IsRenderingEnabled()
    {
        return renderMetaball;
    }

    public void EnableRendering()
    {
        renderMetaball = true;
    }

    public void DisableRendering()
    {
        renderMetaball = false;
    }

    public void ToggleRendering()
    {
        renderMetaball = !renderMetaball;
    }

    public float GetRadius()
    {
        switch (type)
        {
            case MetaballType.Sprite:
                return GetSpriteRadius();

            case MetaballType.UI:
                return GetUIRadius();

            default:
                return 1f;
        }
    }

    private float GetSpriteRadius()
    {
        if (circleCollider == null)
            return 1f;

        // Calculate actual radius with transform scale
        float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        return circleCollider.radius * maxScale;
    }

    private float GetUIRadius()
    {
        if (rectTransform == null)
            return 1f;

        // Get actual size from RectTransform
        Vector2 size = rectTransform.rect.size;
        float dimension = useWidth ? size.x : size.y;

        // Calculate base radius
        float baseRadius = dimension * radiusMultiplier * 0.5f;

        // Apply scale (lossyScale.x because canvas usually scales uniformly)
        float finalRadius = baseRadius * rectTransform.lossyScale.x;

        return finalRadius;
    }

    public Vector2 GetWorldPosition()
    {
        switch (type)
        {
            case MetaballType.Sprite:
                return GetSpritePosition();

            case MetaballType.UI:
                return GetUIPosition();

            default:
                return transform.position;
        }
    }

    private Vector2 GetSpritePosition()
    {
        if (circleCollider == null)
            return transform.position;

        // Include collider offset
        return (Vector2)transform.position + circleCollider.offset;
    }

    private Vector2 GetUIPosition()
    {
        if (rectTransform == null)
            return transform.position;

        // For UI, position is already world position of RectTransform
        return rectTransform.position;
    }

    public Color GetColor()
    {
        // If using FromComponent, get color from component
        if (textureSource == TextureSource.FromComponent)
        {
            return GetColorFromComponent();
        }

        // Otherwise use the set color
        return color;
    }

    private Color GetColorFromComponent()
    {
        // Get color from component
        if (type == MetaballType.Sprite && spriteRenderer != null)
        {
            return spriteRenderer.color;
        }

        if (type == MetaballType.UI)
        {
            if (imageComponent != null)
            {
                return imageComponent.color;
            }

            if (rawImageComponent != null)
            {
                return rawImageComponent.color;
            }
        }

        // Fallback to white if no component found
        return Color.white;
    }

    public Texture2D GetTexture()
    {
        // Cache texture to avoid repeated lookups
        if (textureCached)
            return cachedTexture;

        switch (textureSource)
        {
            case TextureSource.SolidColor:
                cachedTexture = null;
                break;

            case TextureSource.FromComponent:
                cachedTexture = GetTextureFromComponent();
                break;

            case TextureSource.CustomTexture:
                cachedTexture = customTexture;
                break;
        }

        textureCached = true;

        return cachedTexture;
    }

    private Texture2D GetTextureFromComponent()
    {
        // Priority order: SpriteRenderer -> Image -> RawImage

        // Try SpriteRenderer (for Sprite type)
        if (type == MetaballType.Sprite && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite.texture;
        }

        // Try Image (for UI type)
        if (type == MetaballType.UI && imageComponent != null && imageComponent.sprite != null)
        {
            return imageComponent.sprite.texture;
        }

        // Try RawImage (for UI type)
        if (type == MetaballType.UI && rawImageComponent != null && rawImageComponent.texture != null)
        {
            // RawImage.texture is a Texture, need to cast to Texture2D
            Texture2D tex2D = rawImageComponent.texture as Texture2D;

            if (tex2D == null)
            {
                Debug.LogWarning($"RawImage texture on {gameObject.name} is not a Texture2D. Type: {rawImageComponent.texture.GetType()}");
            }

            return tex2D;
        }

        return null;
    }

    public bool HasTexture()
    {
        return GetTexture() != null;
    }

    public float GetConnectionDistance()
    {
        return connectionDistance;
    }

    public MetaballType GetMetaballType()
    {
        return type;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    // Public method to manually refresh registration
    public void RefreshRegistration()
    {
        if (gameObject.activeInHierarchy && enabled && renderMetaball)
        {
            if (!isRegistered)
            {
                MetaballSystem2D.Add(this);
                isRegistered = true;
            }
        }
        else
        {
            if (isRegistered)
            {
                MetaballSystem2D.Remove(this);
                isRegistered = false;
            }
        }
    }

    // Clear texture cache when settings change
    private void OnValidate()
    {
        textureCached = false;

        // Update registration when renderMetaball changes in inspector
        if (Application.isPlaying)
        {
            RefreshRegistration();
        }

        if (type == MetaballType.Sprite && GetComponent<CircleCollider2D>() == null)
        {
            Debug.LogWarning($"Metaballs2D on {gameObject.name}: Type is Sprite but no CircleCollider2D found. Consider adding one or changing type to UI.");
        }

        if (type == MetaballType.UI && GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning($"Metaballs2D on {gameObject.name}: Type is UI but no RectTransform found. This object is not a UI element.");
        }

        // Check for UI components in edit mode
        if (type == MetaballType.UI && textureSource == TextureSource.FromComponent)
        {
            Image img = GetComponent<Image>();
            RawImage rawImg = GetComponent<RawImage>();

            if (img == null && rawImg == null)
            {
                Debug.LogWarning($"Metaballs2D on {gameObject.name}: TextureSource is FromComponent but no Image or RawImage component found.");
            }
            else if (img != null && img.sprite == null)
            {
                Debug.LogWarning($"Metaballs2D on {gameObject.name}: Image component found but sprite is null.");
            }
            else if (rawImg != null && rawImg.texture == null)
            {
                Debug.LogWarning($"Metaballs2D on {gameObject.name}: RawImage component found but texture is null.");
            }
        }
    }
}