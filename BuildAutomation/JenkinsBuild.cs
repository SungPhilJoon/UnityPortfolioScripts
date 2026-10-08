using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;

#if UNITY_EDITOR_OSX
using UnityEditor.iOS.Xcode;
#endif

namespace Builder
{
    // Jenkins가 커맨드라인 인자만 넘기면 호출하는 Editor 전용 진입점 모음입니다.
    // 실제 파일은 Android/iOS 빌드, 에셋번들 빌드·업로드, 패치 정보 업로드까지 담당하는
    // 더 많은 메서드가 있는데, 포트폴리오에는 대표 흐름(APK 빌드, iOS 빌드+PBXProject 후처리,
    // 에셋번들 빌드/업로드 한 쌍)만 남기고 생략했습니다 — 나머지는 인자만 다른 동일 패턴입니다.
    public static class JenkinsBuild
    {
        private static List<IBuildWorker> workers = new List<IBuildWorker>();

        private static readonly string INSPECTION_KEYWORD = "_inspection";

        // Jenkins 파이프라인이 "-appBuildPath value" 형태로 넘기는 인자를 파싱합니다.
        public static string GetArgs(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && args.Length > i + 1)
                {
                    return args[i + 1];
                }
            }

            return string.Empty;
        }

        public static void AndroidApkAppBuild()
        {
            workers.Clear();

            BuildTarget buildTarget = BuildTarget.Android;

            string appPath = GetArgs("-appBuildPath");
            string buildVersion = GetArgs("-buildVersion");
            string versionCode = GetArgs("-buildVersionCode");
            string[] symbols = getDefineSymbol();
            string packageName = getAppPackageName();

            if (string.IsNullOrEmpty(appPath))
            {
                Debug.LogError($"AppBuildPath is Null");

                return;
            }

            if (!int.TryParse(versionCode, out int buildVersionCode))
            {
                if (string.IsNullOrEmpty(versionCode))
                {
                    buildVersionCode = PlayerSettings.Android.bundleVersionCode;
                }
                else
                {
                    Debug.LogError($"{versionCode} is UnValid");

                    return;
                }
            }

            if (string.IsNullOrEmpty(buildVersion))
            {
                buildVersion = PlayerSettings.bundleVersion;
            }

            var group = buildTargetToGroup(buildTarget);
            if (group == null)
            {
                return;
            }

            var apkBuilder = new BuildAppAndroidAPK(buildVersionCode);

            apkBuilder.Set(
                buildVersion,
                appPath,
                "gauntlet",
                group.Value,
                buildTarget,
                symbols,
                packageName);

            workers.Add(apkBuilder);

            doWork();
        }

        public static void iOSAppBuild()
        {
            workers.Clear();

            BuildTarget buildTarget = BuildTarget.iOS;

            string appPath = GetArgs("-appBuildPath");
            string buildVersion = GetArgs("-buildVersion");
            string[] symbols = getDefineSymbol();
            string packageName = getAppPackageName();

            if (string.IsNullOrEmpty(appPath))
            {
                Debug.LogError($"AppBuildPath is Null");

                return;
            }

            var group = buildTargetToGroup(buildTarget);
            if (group == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(buildVersion))
            {
                buildVersion = PlayerSettings.bundleVersion;
            }

            var build = new BuildApp();

            build.Set(
                buildVersion,
                appPath,
                "gauntlet",
                group.Value,
                buildTarget,
                symbols,
                packageName);

            workers.Add(build);

            doWork();
        }

        // Android AAB, 에셋번들 빌드/업로드(정식·점검), 패치 정보 업로드도 같은 IBuildWorker 패턴을
        // 따르는 메서드가 각각 있습니다 — 대상(Android/iOS)과 인자 조합만 다르고 구조는 동일해서 생략했습니다.

        public static void AndroidBundleBuild()
        {
            bundleBuild(BuildTarget.Android, false);
        }

        public static void AndroidBundleUpload()
        {
            bundleUpload(BuildTarget.Android, false);
        }

        // iOS 빌드가 끝난 직후 Unity가 호출하는 후처리 훅입니다. Xcode 프로젝트 파일(PBXProject)을
        // 직접 열어서 Capability 추가, 빌드 옵션, 브리지 코드 삽입까지 코드로 자동화합니다.
        // 이후 단계(Fastlane으로 Xcode 빌드 → TestFlight 업로드)는 Unity 바깥의 CI 스크립트가 이어받습니다.
#if UNITY_IOS
        [PostProcessBuildAttribute(1)]
#endif
        public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
        {
#if UNITY_EDITOR_OSX

            if (buildTarget != BuildTarget.iOS)
            {
                return;
            }

            string projectPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromFile(projectPath);

            string schemeTarget = pbxProject.GetUnityMainTargetGuid();
            string frameworkTarget = pbxProject.GetUnityFrameworkTargetGuid();

            // Capabilities 추가
            pbxProject.AddCapability(schemeTarget, PBXCapabilityType.SignInWithApple);
            pbxProject.AddCapability(schemeTarget, PBXCapabilityType.PushNotifications);
            pbxProject.AddCapability(schemeTarget, PBXCapabilityType.InAppPurchase);

            // iOS 버전, Bitcode 및 Swift 설정
            pbxProject.SetBuildProperty(schemeTarget, "IPHONEOS_DEPLOYMENT_TARGET", "13.0");
            pbxProject.SetBuildProperty(schemeTarget, "ENABLE_BITCODE", "NO");
            pbxProject.SetBuildProperty(schemeTarget, "SWIFT_VERSION", "5.0");
            pbxProject.SetBuildProperty(frameworkTarget, "VALID_ARCHS", "arm64");
            pbxProject.SetBuildProperty(frameworkTarget, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");

            // 런타임 경로 설정
            pbxProject.AddBuildProperty(schemeTarget, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks");
            pbxProject.AddBuildProperty(frameworkTarget, "OTHER_LDFLAGS", "-ObjC");

            // Apple 로그인 세션 처리를 위한 Swift/Objective-C 브리지 코드를 빌드 산출물에 직접 삽입합니다
            // (실제 Swift/ObjC 소스 문자열은 생략 — GetSwiftCode()/GetObjCCode()가 반환합니다)
            string swiftFilePath = Path.Combine(path, "AppleSessionHelper.swift");
            File.WriteAllText(swiftFilePath, GetSwiftCode());
            pbxProject.AddFile(swiftFilePath, "AppleSessionHelper.swift", PBXSourceTree.Source);
            pbxProject.AddFileToBuild(frameworkTarget, pbxProject.FindFileGuidByProjectPath("AppleSessionHelper.swift"));

            pbxProject.WriteToFile(projectPath);

            // Info.plist 수정
            string plistPath = Path.Combine(path, "Info.plist");
            PlistDocument plistDoc = new PlistDocument();
            plistDoc.ReadFromFile(plistPath);

            if (plistDoc == null)
            {
                Debug.LogError($"Plist is Empty : {plistPath}");
                return;
            }

            plistDoc.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plistDoc.root.SetString("GADApplicationIdentifier", AdMob.APPID);

            plistDoc.WriteToFile(plistPath);

#endif
        }

        private static string GetSwiftCode()
        {
            // Apple ID 세션 확인/초기화용 Swift 코드 (생략)
            return string.Empty;
        }

        private static void bundleBuild(BuildTarget buildTarget, bool isInspection)
        {
            string bundlePath = GetArgs("-bundleBuildPath");
            string bundleVersion = GetArgs("-bundleVersion");

            if (string.IsNullOrEmpty(bundlePath))
            {
                Debug.LogError($"BundleBuildPath is Null");

                return;
            }

            if (string.IsNullOrEmpty(bundleVersion))
            {
                Debug.LogError("BundleVersion is Null");

                return;
            }

            if (isInspection)
            {
                bundleVersion += INSPECTION_KEYWORD;
            }

            var group = buildTargetToGroup(buildTarget);
            if (group != null)
            {
                IBuildWorker builder = new BuildBundle(bundleVersion, bundlePath, group.Value, buildTarget);

                workers.Add(builder);
            }

            doWork();
        }

        private static void bundleUpload(BuildTarget buildTarget, bool isInspection)
        {
            string bundlePath = GetArgs("-bundleBuildPath");
            string bundleVersion = GetArgs("-bundleVersion");
            string bucket = GetArgs("-bucketName");
            string regionSystemName = GetArgs("-regionSystemName");

            var region = Amazon.RegionEndpoint.GetBySystemName(regionSystemName);

            if (string.IsNullOrEmpty(bundlePath) || string.IsNullOrEmpty(bundleVersion))
            {
                Debug.LogError("BundleBuildPath or BundleVersion is Null");

                return;
            }

            if (isInspection)
            {
                bundleVersion += INSPECTION_KEYWORD;
            }

            var group = buildTargetToGroup(buildTarget);
            if (group != null)
            {
                IBuildWorker builder = new BuildUpload(buildTarget, bundleVersion,
                    new BuildSetting.Build() { bucket = bucket, region = region }, bundlePath);

                workers.Add(builder);
            }

            repeatWork();
        }

        // 빌드류(APK/AAB/번들 빌드)는 한 번만 수행하면 되므로 doWork(),
        // 업로드류(번들 업로드, 패치 정보 업로드)는 완료될 때까지 반복 폴링해야 해서 repeatWork()를 씁니다.
        private static void doWork()
        {
            foreach (var worker in workers)
            {
                var workProcess = worker.Work().GetEnumerator();
                while (workProcess.MoveNext())
                {
                    if (workProcess.Current == E_BuildWorkerResult.Work)
                    {
                        break;
                    }
                    else if (workProcess.Current == E_BuildWorkerResult.Fail)
                    {
                        Debug.Log("Build Fail");

                        break;
                    }
                }
            }
        }

        private static void repeatWork()
        {
            foreach (var worker in workers)
            {
                var workProcess = worker.Work().GetEnumerator();
                while (workProcess.MoveNext())
                {
                    if (workProcess.Current == E_BuildWorkerResult.Work)
                    {
                        continue;
                    }
                    else if (workProcess.Current == E_BuildWorkerResult.Fail)
                    {
                        Debug.Log("Build Fail");

                        break;
                    }
                }
            }
        }

        private static BuildTargetGroup? buildTargetToGroup(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android: return BuildTargetGroup.Android;
                case BuildTarget.iOS: return BuildTargetGroup.iOS;
            }

            return null;
        }

        private static string[] getDefineSymbol()
        {
            return GetArgs("-appDefineSymbols").Split('|');
        }

        private static string getAppPackageName()
        {
            string overrideName = GetArgs("-packageName");

            return overrideName;
        }
    }
}
