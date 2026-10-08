using TMPro;
using UnityEngine.UI;

// 별 등급 UI(획득 개수만큼 별을 채우고, 한계 돌파 시 "왕별+숫자" 표기로 전환)를
// 계산과 트윈 재생까지 포함해서 하나의 정적 바인더로 모아둔 예시.
public static class UIIconStarBinder
{
    public static int limit = 10;

    public static void Binding(int current, int max, Image[] images, Image maxGrade, TMP_Text level)
    {
        for (int i = 0; i < images.Length; i++)
        {
            images[i].SetActive(false);
        }

        maxGrade.SetActive(false);

        // limit(10)을 넘어가면 별 대신 "왕별 + 초과분 숫자" 표기로 전환하는 특수 규칙
        bool isGradeStar = current > limit;
        if (isGradeStar)
        {
            BindingGradeStar(current, maxGrade, level);
        }
        else
        {
            BindingStars(current, max, images);
        }

        maxGrade.gameObject.SetActive(isGradeStar);
        level.gameObject.SetActive(isGradeStar);
    }

    public static void BindingGradeStar(int current, Image maxGrade, TMP_Text level)
    {
        CommonData starImageData = TableManager.Instance.CommonData.Find("starsGradeImage");

        maxGrade.gameObject.SetActive(true);
        maxGrade.sprite = UIIconLoader.LoadCommonSprite(starImageData.GetData(2).GetString());
        level.text = (current - limit).ToString();
    }

    public static void BindingStars(int current, int max, Image[] images)
    {
        CommonData starImageData = TableManager.Instance.CommonData.Find("starsGradeImage");
        CommonData emptyStarImageData = TableManager.Instance.CommonData.Find("starsGradeOffImage");

        int begin = (current - 1) % images.Length;
        int beginStep = (current - 1) / images.Length;
        int end = (max - 1) % images.Length;
        int endStep = (max - 1) / images.Length;

        if (beginStep != endStep)
        {
            end = images.Length;
        }

        for (int i = 0; i < images.Length; ++i)
        {
            if (i <= end)
            {
                images[i].gameObject.SetActive(true);

                if (i > begin)
                {
                    images[i].sprite = UIIconLoader.LoadCommonSprite(emptyStarImageData.GetData(beginStep).GetString());
                }
                else
                {
                    images[i].sprite = UIIconLoader.LoadCommonSprite(starImageData.GetData(beginStep).GetString());
                }
            }
            else
            {
                images[i].gameObject.SetActive(false);
            }
        }
    }

    public static void PlayGradeStarTween(int current, Image gradeImage, Jun_TweenRuntime gradeStarTween, TMP_Text levelText)
    {
        StopTween(gradeStarTween);

        BindingGradeStar(current, gradeImage, levelText);

        gradeStarTween.Play();
    }

    public static void StopTween(Jun_TweenRuntime tween)
    {
        if (tween.isPlaying)
        {
            tween.Rewind();
            tween.Stop();
        }
    }
}
