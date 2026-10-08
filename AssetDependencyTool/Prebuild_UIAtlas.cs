using System.Linq;
using UnityEditor;
using UnityEngine;

// 모든 UI 프리팹을 순회하면서, 프리팹 안의 각 Image가 참조하는 스프라이트가
// 어느 아틀라스에서 왔는지(UIAtlasContainer)를 조회해 "UI 프리팹 -> 의존 아틀라스 목록"
// 데이터를 만들어 파일로 저장합니다. 에셋번들 빌드 시 이 데이터로 필요한 아틀라스만
// 함께 번들링하기 위한 사전 작업입니다.
public static class Prebuild_UIAtlas
{
    [MenuItem("Tools/Tmp/TestAtlas Write")]
    public static void Execute()
    {
        Execute(BuildTarget.Android);
    }

    public static bool Execute(BuildTarget buildTarget)
    {
        UIAtlasContainer container = new UIAtlasContainer();
        container.Initialize("Assets/Prefabs/UI/Atlas", buildTarget);

        var uiList = AssetDatabase.FindAssets("", new[] { "Assets/Prefabs/UI" })
            .Select(v => AssetDatabase.GUIDToAssetPath(v)).ToList();

        UIAtlasData exportData = new UIAtlasData();

        foreach (var uiPath in uiList)
        {
            var uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(uiPath);
            if (uiPrefab == null)
            {
                continue;
            }

            UIAtlasLink data = new UIAtlasLink();
            data.path = uiPath;

            var images = uiPrefab.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var image in images)
            {
                if (image.sprite == null)
                {
                    continue;
                }

                // Resources 폴더나 별도 스프라이트 링크 테이블로 관리되는 것은 아틀라스 추적 대상에서 제외합니다.
                if (AssetDatabase.GetAssetPath(image.sprite).ToLower().IndexOf("resources") != -1
                    || AssetDatabase.GetAssetPath(image.sprite).ToLower().IndexOf("uispriteslink") != -1)
                {
                    continue;
                }

                var useAtlasPath = container.GetAtlasBySprite(image.sprite.name);
                if (useAtlasPath == null)
                {
                    Debugger.Error($"BuildAtlas Error. Invalid sprite. ui: {uiPath}, sprite: {EditorPath.GetHierachyPath(image.gameObject)}");
                    continue;
                }

                data.Add(useAtlasPath);
            }

            exportData.atlasData.Add(data);
        }

        if (UIAtlasData.WriteFile(exportData))
        {
            AssetDatabase.SaveAssets();

            return true;
        }

        return false;
    }
}
