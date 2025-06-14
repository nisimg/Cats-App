using _cats.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

namespace _cats.Scripts.MathGame
{
    public class MathTile : CATSMonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Tile Settings")]
        public int value;
        public TextMeshProUGUI valueText;
        public Image tileImage;
        public CanvasGroup canvasGroup;
        
        [Header("Animation Settings")]
        public float hoverScale = 1.1f;
        public float dragAlpha = 0.6f;
        public float animationDuration = 0.2f;
        
        private Vector3 originalPosition;
        private Transform originalParent;
        private Canvas canvas;
        private DropZone currentDropZone;
        private Vector3 originalScale;
        private bool isDragging = false;
        [SerializeField] private Transform tileContainer;

        void Start()
        {
            canvas = GetComponentInParent<Canvas>();
            originalScale = transform.localScale;
            UpdateDisplay();
            tileContainer= transform.parent;
        }

        public void Initialize(int tileValue)
        {
            value = tileValue;
            UpdateDisplay();
        }

        void UpdateDisplay()
        {
            if (valueText != null)
                valueText.text = value.ToString();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isDragging) return;

            isDragging = true;
            originalPosition = transform.position;
            originalParent = transform.parent;

            transform.SetParent(canvas.transform, true);
            transform.SetAsLastSibling();

            canvasGroup.alpha = dragAlpha;
            canvasGroup.blocksRaycasts = false;

            _manager.AudioManager.PlaySFX("TilePickup", 0.5f);

            InvokeEvent(CATSEventNames.OnFirstMoveDone, this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            Vector3 screenPoint = new Vector3(eventData.position.x, eventData.position.y, 0);
            Vector3 worldPoint;
            
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out worldPoint))
            {
                transform.position = worldPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            isDragging = false;
            canvasGroup.blocksRaycasts = true;

            DropZone targetDropZone = FindDropZone(eventData.position);

            if (targetDropZone != null && targetDropZone.CanAcceptTile(this))
            {
                PlaceInDropZone(targetDropZone);
            }
            else
            {
                if (currentDropZone != null)
                {
                    ReturnToTileContainer();
                }
                else
                {
                    ReturnToOriginalPosition();
                }
            }
        }

        DropZone FindDropZone(Vector2 screenPosition)
        {
            var results = new System.Collections.Generic.List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPosition;
            
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                var dropZone = result.gameObject.GetComponent<DropZone>();
                if (dropZone != null)
                    return dropZone;
            }

            return null;
        }

        void PlaceInDropZone(DropZone dropZone)
        {
            currentDropZone = dropZone;
            dropZone.PlaceTile(this);

            transform.DOMove(dropZone.transform.position, animationDuration)
                .SetEase(Ease.OutBack);
            
            canvasGroup.alpha = 1f;

            CATSManager.Instance.AudioManager.PlaySFX("TileDrop", 0.7f);

            var gameManager = FindObjectOfType<MathGameManager>();
            if (gameManager != null)
                gameManager.CheckEquationComplete();
        }

        void ReturnToOriginalPosition()
        {
            transform.SetParent(originalParent, true);

            transform.DOMove(originalPosition, animationDuration)
                .SetEase(Ease.OutBounce);
            
            canvasGroup.alpha = 1f;

            CATSManager.Instance.AudioManager.PlaySFX("TileReturn", 0.3f);
        }

       public void ReturnToTileContainer()
        {
            RemoveFromDropZone();
         
            
            
            if (tileContainer != null)
            {
                transform.SetParent(tileContainer.transform, false);
                //transform.localScale = originalScale;
                transform.DOLocalMove(Vector3.one, animationDuration)
                    .SetEase(Ease.OutBounce);
                
                canvasGroup.alpha = 1f;
                CATSManager.Instance.AudioManager.PlaySFX("TileReturn", 0.3f);
                
                Debug.Log($" Tile {value} returned to container: {tileContainer.name}");
            }
            else
            {
                Debug.LogWarning(" Tile container not found! Returning to original position.");
                ReturnToOriginalPosition();
            }
        }

        public void RemoveFromDropZone()
        {
            if (currentDropZone != null)
            {
                currentDropZone.RemoveTile();
                currentDropZone = null;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isDragging)
            {
              /*  transform.DOScale(originalScale * hoverScale, animationDuration)
                    .SetEase(Ease.OutBack);*/
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isDragging)
            {
             /*   transform.DOScale(originalScale, animationDuration)
                    .SetEase(Ease.OutBack);*/
            }
        }

        public void AnimateCorrect()
        {
            tileImage.DOColor(Color.green, 0.2f)
                .OnComplete(() => tileImage.DOColor(Color.white, 0.2f));

           // transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1f);
        }

        public void AnimateIncorrect()
        {
            tileImage.DOColor(Color.red, 0.2f)
                .OnComplete(() => tileImage.DOColor(Color.white, 0.2f));

            transform.DOShakePosition(0.5f, 10f, 20, 90f);
        }

        public void ResetToTileArea()
        {
            RemoveFromDropZone();
            ReturnToTileContainer();
        }
    }
}

namespace _cats.Scripts.MathGame
{
    [System.Serializable]
    public class MathEquation
    {
        public int firstNumber;
        public string operation;
        public int secondNumber;
        public int result;
        
        public override string ToString()
        {
            return $"{firstNumber} {operation} {secondNumber} = {result}";
        }
    }
}