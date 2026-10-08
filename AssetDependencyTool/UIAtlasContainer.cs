using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.U2D;

// 빌드 전 에디터 스크립트: 프로젝트의 모든 SpriteAtlas를 순회하며
// "스프라이트 이름 -> 어느 아틀라스 경로에 들어있는지"를 인덱싱합니다.
// Prebuild_UIAtlas가 이 인덱스를 이용해 각 UI 프리팹이 실제로 어떤 아틀라스에 의존하는지 추적합니다.
public class UIAtlasContainer
{
    private Dictionary<string, string> atlasContainer = new Dictionary<string, string>();

    public void Initialize(string path, BuildTarget buildTarget)
    {
        SpriteAtlasUtility.PackAllAtlases(buildTarget, false);

        var atlasPaths = AssetDatabase.FindAssets("t:spriteatlas", new string[] { path })
            .Select(v => AssetDatabase.GUIDToAssetPath(v)).ToList();

        foreach (var atlasPath in atlasPaths)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            atlas.SetIncludeInBuild(false);

            // Tight Packing이 켜져 있으면 개별 스프라이트 이름 조회가 어긋날 수 있어 강제로 끕니다.
            var packingSetting = atlas.GetPackingSettings();
            bool dirty = false;

            if (packingSetting.enableTightPacking)
            {
                packingSetting.enableTightPacking = false;
                atlas.SetPackingSettings(packingSetting);

                dirty = true;
            }

            if (dirty)
            {
                EditorUtility.SetDirty(atlas);
                AssetDatabase.SaveAssetIfDirty(atlas);
                AssetDatabase.Refresh();
            }

            atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);

            Sprite[] sprites = new Sprite[atlas.spriteCount];
            atlas.GetSprites(sprites);

            foreach (var spr in sprites)
            {
                if (atlasContainer.ContainsKey(spr.name)
                    && atlasContainer[spr.name] != atlasPath)
                {
                    Debugger.Error($"Duplicate sprite name. {spr.name}, atlasA: {atlasContainer[spr.name]}, atlasB: {atlas.name}");
                    continue;
                }

                // 아틀라스에 패킹된 스프라이트 인스턴스 이름에는 "(Clone)"이 붙어서 실제 원본 이름으로 정규화합니다.
                string removeText = "(Clone)";
                int removeAt = spr.name.IndexOf(removeText);
                string spriteName = spr.name.Remove(removeAt, removeText.Length);

                atlasContainer.Add(spriteName, atlasPath);
            }
        }
    }

    public string GetAtlasBySprite(string name)
    {
        string atlasPath;
        atlasContainer.TryGetValue(name, out atlasPath);

        return atlasPath;
    }
}
