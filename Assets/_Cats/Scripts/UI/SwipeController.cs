using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private float maxSwipeTime = 1f;
    [SerializeField] private bool enableSwipe = true;
    
    [Header("References")]
    [SerializeField] private UIManager uiManager;
    
    private Vector2 startPosition;
    private float startTime;
    private bool isDragging = false;

    private void Start()
    {
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableSwipe) return;
        
        startPosition = eventData.position;
        startTime = Time.time;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Simple drag detection - no visual feedback during drag
        if (!enableSwipe || !isDragging) return;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enableSwipe || !isDragging) return;
        
        isDragging = false;
        
        Vector2 endPosition = eventData.position;
        float swipeTime = Time.time - startTime;
        Vector2 swipeVector = endPosition - startPosition;
        float swipeDistance = swipeVector.magnitude;
        
        // Check if it's a valid swipe
        if (swipeTime <= maxSwipeTime && swipeDistance >= minSwipeDistance)
        {
            float horizontalDistance = Mathf.Abs(swipeVector.x);
            float verticalDistance = Mathf.Abs(swipeVector.y);
            
            // Must be primarily horizontal
            if (horizontalDistance > verticalDistance)
            {
                if (swipeVector.x > 0)
                {
                    // Swipe right - previous page
                    uiManager.PreviousPage();
                }
                else
                {
                    // Swipe left - next page
                    uiManager.NextPage();
                }
            }
        }
    }
    
    private void Update()
    {
        if (!enableSwipe) return;
        
        // Keyboard shortcuts for testing
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            uiManager.PreviousPage();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            uiManager.NextPage();
        }
    }
    
    public void EnableSwipe()
    {
        enableSwipe = true;
    }
    
    public void DisableSwipe()
    {
        enableSwipe = false;
    }
}