using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private float pageWidth;
    int currentPageIndex = -1;
    [SerializeField] private RectTransform pagePerent;
    [SerializeField] private RectTransform ScrollebleRect;
    public Action<int> pageChanged;
    [SerializeField] private float pageSwepDur = 0.5f;

    // Public properties
    public int CurrentPageIndex => currentPageIndex;
    public int TotalPages => pagePerent.childCount;
    
    // Animation state
    private bool isTransitioning = false;

    private IEnumerator Start()
    {
        yield return null;
        pageWidth = pagePerent.rect.width;

        for (int i = 0; i < pagePerent.childCount; i++)
        {
            RectTransform page = pagePerent.GetChild(i) as RectTransform;
            page.anchoredPosition = Vector2.right * i * pageWidth;
        }

        ShowPage(2);
    }

    public void ShowPage(int pageIndex)
    {
        if(pageIndex == currentPageIndex || isTransitioning) return;
        if(pageIndex < 0 || pageIndex >= TotalPages) return;
        
        isTransitioning = true;
        
        var targetPerentAnchoredPosition = Vector2.left * pageWidth * pageIndex;
        DOTween.Kill(ScrollebleRect);
        ScrollebleRect.DOAnchorPos(targetPerentAnchoredPosition, pageSwepDur)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => {
                isTransitioning = false;
            });
            
        if (pagePerent.GetChild(pageIndex).TryGetComponent(out UIPage uiPage))
        {
            uiPage.Show();
        }
        
        pageChanged?.Invoke(pageIndex);
        staticPageChanged?.Invoke();
        currentPageIndex = pageIndex;
    }
    
    public void NextPage()
    {
        if (currentPageIndex < TotalPages - 1 && !isTransitioning)
        {
            ShowPage(currentPageIndex + 1);
        }
    }
    
    public void PreviousPage()
    {
        if (currentPageIndex > 0 && !isTransitioning)
        {
            ShowPage(currentPageIndex - 1);
        }
    }
    
    public bool CanGoNext()
    {
        return currentPageIndex < TotalPages - 1 && !isTransitioning;
    }
    
    public bool CanGoPrevious()
    {
        return currentPageIndex > 0 && !isTransitioning;
    }

    public static Action staticPageChanged { get; set; }
}