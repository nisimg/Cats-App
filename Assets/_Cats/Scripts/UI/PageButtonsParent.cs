using System;
using _cats.Scripts.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PageButtonsParent : CATSMonoBehaviour
{
    [SerializeField] Transform pageButtonParent;
    [SerializeField] private UIManager uiManager;
    [SerializeField] Vector2 minMaxWidth;
    [SerializeField] float animationDuration = 1f;
    int currentPage = -1;

    private void Awake()
    {
        for (int i = 0; i < pageButtonParent.childCount; i++)
        {
            int _i = i;
            pageButtonParent.GetChild(i).GetComponent<Button>().onClick.AddListener((() => uiManager.ShowPage(_i)));
        }

        AddListener(CATSEventNames.pageChanged,(OnPageChanged));
    }

    private void OnPageChanged(object obj)
    {
        OnPageChanged((int) obj);
    }

    private void OnPageChanged(int pageIndex)
    {
        if (currentPage == pageIndex) return;

        UpdatePageButtonState(pageIndex);

        for (int i = 0; i < pageButtonParent.childCount; i++)
        {
            var element = pageButtonParent.GetChild(i).GetComponent<LayoutElement>();
            DOTween.Kill(element);
            var from = minMaxWidth.x;
            var to = minMaxWidth.y;
            if (pageIndex != i)
            {
                from = minMaxWidth.y;
                to = minMaxWidth.x;
            }

            float currentValue = from;
            DOTween.To(() => currentValue, x =>
            {
                currentValue = x;
                UpadtePageButtonWidth(element, x);
            }, to, animationDuration).SetEase(Ease.OutExpo);
        }

        currentPage = pageIndex;
    }

    private void UpdatePageButtonState(int pageIndex)
    {
        if (currentPage >= 0)
            pageButtonParent.GetChild(currentPage).GetComponent<PageButton>().DeSelect();

        pageButtonParent.GetChild(pageIndex).GetComponent<PageButton>().Select();
    }

    private void UpadtePageButtonWidth(LayoutElement element, float f)
    {
        element.preferredWidth = f;
    }
}