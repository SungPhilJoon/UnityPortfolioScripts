using System;
using System.Collections.Generic;
using UnityEngine;

// Prebuild_UIAtlas가 만든 "UI 프리팹 -> 의존 아틀라스 목록" 매핑의 직렬화 포맷입니다.
// 빌드 전에는 WriteFile()로 JSON 파일에 기록하고, 런타임에는 ReadFile()로 같은 파일을
// 다시 읽어 들여 딕셔너리로 재구성합니다 — 빌드 타임에 계산한 의존성 정보를 런타임에도
// 그대로 재사용하는 구조입니다.
[Serializable]
public class UIAtlasLink
{
    public string path;
    public List<string> atlas = new List<string>();

    public void Add(string atlasPath)
    {
        if (!atlas.Contains(atlasPath))
        {
            atlas.Add(atlasPath);
        }
    }
}

[Serializable]
public class UIAtlasData
{
    public static readonly string FilePath = "Assets/Prefabs/UI/AtlasList.json";

    public List<UIAtlasLink> atlasData = new List<UIAtlasLink>();

#if UNITY_EDITOR
    public static bool WriteFile(UIAtlasData atlasData)
    {
        try
        {
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(atlasData, Newtonsoft.Json.Formatting.Indented);

            System.IO.File.WriteAllText(FilePath, jsonData);
        }
        catch (Exception e)
        {
            Debugger.Error(e.Message);
            return false;
        }
        return true;
    }
#endif

    // 런타임에서 이 파일을 읽는 쪽 (예: 아틀라스 프리로드 시 어떤 UI가 어떤 아틀라스를
    // 필요로 하는지 조회) — 빌드 타임 산출물을 그대로 소비합니다.
    public static Dictionary<string, UIAtlasLink> ReadFile()
    {
        using (var textRef = ResourceManager.Instance.LoadResource(FilePath))
        {
            var textAsset = textRef.GetResource<TextAsset>();
            if (textAsset == null)
            {
                Debugger.Error($"Read AtlasData load failed. {FilePath}");

                return null;
            }

            var readData = Newtonsoft.Json.JsonConvert.DeserializeObject<UIAtlasData>(textAsset.text);
            if (readData == null)
            {
                Debugger.Error($"Read AtlasData deserializeObject failed. {FilePath}");
                return null;
            }

            var atlasDictionary = new Dictionary<string, UIAtlasLink>();

            foreach (var data in readData.atlasData)
            {
                atlasDictionary.Add(data.path, data);
            }

            return atlasDictionary;
        }
    }
}
