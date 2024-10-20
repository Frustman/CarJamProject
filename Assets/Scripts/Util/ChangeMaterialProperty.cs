using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEngine;

public class ChangeMaterialProperty : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private UnityEngine.Color baseColor;

    private MaterialPropertyBlock propertyBlock;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private Renderer mRenderer;

    private Material mMaterial;

    void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        mRenderer = GetComponent<Renderer>();
        ApplyChange();
    }

    public void SetColor(UnityEngine.Color color)
    {
        baseColor = color;
        ApplyChange();
    }

    public void ApplyChange()
    {
        propertyBlock.SetColor(BaseColor, baseColor);
        mRenderer.SetPropertyBlock(propertyBlock);
    }
}