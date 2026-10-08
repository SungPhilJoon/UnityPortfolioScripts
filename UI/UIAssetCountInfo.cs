using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAssetCountInfo : UIBehaviour
{
    [Serializable]
    public struct asset
    {
        public int assetIndex;
        public bool initActive;
    }

    private static asset[] defaultAssetIndexs = new asset[]
    {
        new asset() {assetIndex = 99101, initActive = true},
        new asset() {assetIndex = 99102, initActive = true},
        //new UIAssetCountInfo.asset() {assetIndex = 99105, initActive = false},
        //new UIAssetCountInfo.asset() {assetIndex = 99111, initActive = false},
        //new UIAssetCountInfo.asset() {assetIndex = 99112, initActive = false}
    };

    [SerializeField] private Transform createAssetInfoTransform = null;
    [SerializeField] private asset[] assets = defaultAssetIndexs;
    //[SerializeField] private int[] plusButtonIndexs = null;

    private UIMiniAssetInfo assetInfo = null;


    private void OnEnable()
    {
        SetAssetInfo();
    }

    private void OnDisable()
    {
        Release();
    }

    //만약 assetInfo 가 null 인데 참조를 했다면 당신의 타이밍을 고민해보아야 함
    public void RefreshAssets(int[] assetIndices, bool appendDefault = false)
    {
        assetInfo.Release();

        if (appendDefault)
        {
            setDefault();
        }

        foreach (var assetIndex in assetIndices)
        {
            assetInfo.ShowAssetCount(assetIndex, true);
        }
    }
    public void RefreshAssets(int assetIndex, bool appendDefault = false)
    {
        assetInfo.Release();

        if(appendDefault)
        {
            this.setDefault();
        }

        assetInfo.ShowAssetCount(assetIndex, true);
    }
    public void ResetAssets(bool appendDefault = false)
    {
        assetInfo.Release();

        if (appendDefault)
        {
            this.setDefault();
        }

        foreach (var asset in assets)
        {
            assetInfo.ShowAssetCount(asset.assetIndex, true);
        }
    }
    public void SetAssetInfo()
    {
        if (assetInfo != null)
        {
            return;
        }
        
        assetInfo = UIManager.Instance.CreateUIItem<UIMiniAssetInfo>(E_UIChildType.AssetInfo, createAssetInfoTransform);
        UIUtil.Stretch(assetInfo.rectTransform);

        for (int i = 0; i < assets.Length; i++)
        {
            assetInfo.ShowAssetCount(assets[i].assetIndex, assets[i].initActive);
        }
    }

    private void Release()
    {
        assetInfo.Release();
        assetInfo.DestroyGameObject();

        assetInfo = null;
    }

    private void setDefault()
    {
        for (int i = 0; i < defaultAssetIndexs.Length; i++)
        {
            //var assetData = TableManager.Instance.ItemInfoData.Find(defaultAssetIndexs[i].assetIndex);
            assetInfo.ShowAssetCount(defaultAssetIndexs[i].assetIndex, defaultAssetIndexs[i].initActive);
        }

        //for (int i = 0; i < defaultAssetButtonIndexs.Length; i++)
        //{
        //    var assetData = TableManager.Instance.ItemInfoData.Find(defaultAssetButtonIndexs[i]);
        //    SetPlusButton(assetData);
        //}

    }
#if UNITY_EDITOR
    private void Reset()
    {
        createAssetInfoTransform = GetComponent<Transform>();
    }
#endif
}
