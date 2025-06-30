using System;
using DG.Tweening;
using UnityEngine;

public class PageButton : CATSMonoBehaviour
{
    [SerializeField] RectTransform iconRT;
    [SerializeField] private GameObject lable;
    [SerializeField] private float iconMoveDistance;
    [SerializeField] private float animationDuration;
    private float iconNormalY;

    private void Awake()
    {
        iconNormalY = iconRT.anchoredPosition.y;
        lable.SetActive(false);
    }

    [ContextMenu("Show")]
    public void Select()
    {
        Debug.Log("Select");
        lable.SetActive(true);
        DOTween.Kill(iconRT);
        iconRT.DOAnchorPosY(iconNormalY + iconMoveDistance, animationDuration);
    }

    public void DeSelect()
    {
        Debug.Log("DeSelect", gameObject);
        lable.SetActive(false);
        DOTween.Kill(iconRT);
        iconRT.DOAnchorPosY(iconNormalY, animationDuration);
    }
}