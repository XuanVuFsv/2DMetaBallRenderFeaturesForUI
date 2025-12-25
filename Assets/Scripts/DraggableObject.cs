using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Drag Settings")]
    [Tooltip("Enable dragging")]
    public bool isDraggable = true;

    [Tooltip("Smooth drag movement")]
    public bool smoothDrag = false;

    [Tooltip("Smooth speed (only if smoothDrag is true)")]
    [Range(1f, 30f)]
    public float smoothSpeed = 10f;

    [Header("Constraints")]
    [Tooltip("Constrain dragging to horizontal axis only")]
    public bool constrainToX = false;

    [Tooltip("Constrain dragging to vertical axis only")]
    public bool constrainToY = false;

    [Tooltip("Keep within screen bounds")]
    public bool keepInScreenBounds = false;

    [Header("Events")]
    [Tooltip("Called when drag starts")]
    public UnityEngine.Events.UnityEvent onDragStart;

    [Tooltip("Called when dragging")]
    public UnityEngine.Events.UnityEvent onDragging;

    [Tooltip("Called when drag ends")]
    public UnityEngine.Events.UnityEvent onDragEnd;

    // Private variables
    private Vector3 offset;
    private Camera mainCamera;
    private Canvas parentCanvas;
    private RectTransform rectTransform;
    private bool isUI;
    private bool isDragging = false;
    private Vector3 targetPosition;

    // For 3D objects
    private float zDistance;

    private void Awake()
    {
        mainCamera = Camera.main;

        // Check if this is a UI element
        rectTransform = GetComponent<RectTransform>();
        isUI = rectTransform != null;

        if (isUI)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        targetPosition = transform.position;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isDraggable) return;

        if (isUI)
        {
            // UI drag start
            Vector3 worldPoint;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out worldPoint
            );
            offset = transform.position - worldPoint;
        }
        else
        {
            // 3D object drag start
            zDistance = mainCamera.WorldToScreenPoint(transform.position).z;
            Vector3 mouseWorldPos = GetMouseWorldPosition(Input.mousePosition);
            offset = transform.position - mouseWorldPos;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        isDragging = true;
        onDragStart?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        Vector3 newPosition;

        if (isUI)
        {
            // UI dragging
            Vector3 worldPoint;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out worldPoint
            );

            newPosition = worldPoint + offset;
        }
        else
        {
            // 3D object dragging
            Vector3 mouseWorldPos = GetMouseWorldPosition(Input.mousePosition);
            newPosition = mouseWorldPos + offset;
        }

        // Apply constraints
        if (constrainToX)
        {
            newPosition.y = transform.position.y;
            if (!isUI) newPosition.z = transform.position.z;
        }

        if (constrainToY)
        {
            newPosition.x = transform.position.x;
            if (!isUI) newPosition.z = transform.position.z;
        }

        // Keep in screen bounds
        if (keepInScreenBounds)
        {
            newPosition = ClampToScreenBounds(newPosition);
        }

        // Apply position
        if (smoothDrag)
        {
            targetPosition = newPosition;
        }
        else
        {
            transform.position = newPosition;
        }

        onDragging?.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        isDragging = false;
        onDragEnd?.Invoke();
    }

    private void Update()
    {
        // Smooth drag interpolation
        if (smoothDrag && isDragging)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime * smoothSpeed
            );
        }
    }

    private Vector3 GetMouseWorldPosition(Vector3 mouseScreenPos)
    {
        Vector3 mousePoint = mouseScreenPos;
        mousePoint.z = zDistance;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }

    private Vector3 ClampToScreenBounds(Vector3 position)
    {
        if (isUI)
        {
            // UI bounds clamping
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(mainCamera, position);

            float halfWidth = rectTransform.rect.width * rectTransform.lossyScale.x * 0.5f;
            float halfHeight = rectTransform.rect.height * rectTransform.lossyScale.y * 0.5f;

            screenPos.x = Mathf.Clamp(screenPos.x, halfWidth, Screen.width - halfWidth);
            screenPos.y = Mathf.Clamp(screenPos.y, halfHeight, Screen.height - halfHeight);

            Vector3 worldPoint;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform,
                screenPos,
                mainCamera,
                out worldPoint
            );

            return worldPoint;
        }
        else
        {
            // 3D object bounds clamping
            Vector3 screenPos = mainCamera.WorldToScreenPoint(position);
            screenPos.x = Mathf.Clamp(screenPos.x, 0, Screen.width);
            screenPos.y = Mathf.Clamp(screenPos.y, 0, Screen.height);
            screenPos.z = zDistance;

            return mainCamera.ScreenToWorldPoint(screenPos);
        }
    }

    // Public methods to control dragging
    public void EnableDrag()
    {
        isDraggable = true;
    }

    public void DisableDrag()
    {
        isDraggable = false;
        isDragging = false;
    }

    public void ToggleDrag()
    {
        isDraggable = !isDraggable;
        if (!isDraggable) isDragging = false;
    }
}