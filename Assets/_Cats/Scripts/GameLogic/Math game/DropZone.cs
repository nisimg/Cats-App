using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace _cats.Scripts.MathGame
{
    public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Drop Zone Settings")]
        public EquationPosition position;
        public Image backgroundImage;
        public Text placeholderText;
        public Color normalColor = Color.white;
        public Color highlightColor = Color.yellow;
        public Color filledColor = Color.green;
        
        [Header("Animation Settings")]
        public float scaleMultiplier = 1.1f;
        public float animationDuration = 0.3f;
        
        private MathTile currentTile;
        private Vector3 originalScale;
        private bool isHighlighted = false;

        void Start()
        {
            originalScale = transform.localScale;
            backgroundImage.color = normalColor;
            
            if (placeholderText != null)
                placeholderText.text = "?";
        }

        public bool CanAcceptTile(MathTile tile)
        {
            return currentTile == null || currentTile == tile;
        }

        public void PlaceTile(MathTile tile)
        {
            if (currentTile != null && currentTile != tile)
            {
                currentTile.ResetToTileArea();
            }

            currentTile = tile;
            tile.transform.SetParent(transform, false);
            
            UpdateVisualState();
            
            if (placeholderText != null)
                placeholderText.gameObject.SetActive(false);

            transform.DOScale(originalScale * 1.2f, 0.1f)
                .OnComplete(() => transform.DOScale(originalScale, 0.2f));
        }

        public void RemoveTile()
        {
           
            currentTile = null;
            UpdateVisualState();
            
            // Show placeholder
            if (placeholderText != null)
                placeholderText.gameObject.SetActive(true);
        }

        void UpdateVisualState()
        {
            if (currentTile != null)
            {
                backgroundImage.DOColor(filledColor, animationDuration);
            }
            else
            {
                backgroundImage.DOColor(isHighlighted ? highlightColor : normalColor, animationDuration);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            var tile = eventData.pointerDrag?.GetComponent<MathTile>();
            if (tile != null && CanAcceptTile(tile))
            {
                PlaceTile(tile);
            }

            if (CanAcceptTile(tile) == false)
            {

                currentTile.ReturnToTileContainer();
                RemoveTile();
                PlaceTile(tile);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (currentTile == null)
            {
                isHighlighted = true;
                UpdateVisualState();
                
                // Scale up animation
                transform.DOScale(originalScale * scaleMultiplier, animationDuration)
                    .SetEase(Ease.OutBack);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (currentTile == null)
            {
                isHighlighted = false;
                UpdateVisualState();
                
                transform.DOScale(originalScale, animationDuration)
                    .SetEase(Ease.OutBack);
            }
        }

        public int GetCurrentValue()
        {
            return currentTile != null ? currentTile.value : -1;
        }

        public bool IsEmpty()
        {
            return currentTile == null;
        }

        public void AnimateCorrect()
        {
            backgroundImage.DOColor(Color.green, 0.2f)
                .OnComplete(() => backgroundImage.DOColor(filledColor, 0.2f));
            
            transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1f);
            
            if (currentTile != null)
                currentTile.AnimateCorrect();
        }

        public void AnimateIncorrect()
        {
            backgroundImage.DOColor(Color.red, 0.2f)
                .OnComplete(() => backgroundImage.DOColor(normalColor, 0.2f));
            
            transform.DOShakePosition(0.5f, 10f, 20, 90f);
            
            if (currentTile != null)
                currentTile.AnimateIncorrect();
        }

        public void ClearZone()
        {
            if (currentTile != null)
            {
                currentTile.ResetToTileArea();
                RemoveTile();
            }
        }
    }

    public enum EquationPosition
    {
        FirstNumber,
        SecondNumber,
        Result
    }
}