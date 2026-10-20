using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

// 전투 배경. 스테이지 배경 이미지 01~03을 화면 너비 간격으로 이어 놓고 땅과 같은 속도로 흘려서 앞으로 걸어가는 것처럼 보이게 한다
// 조각은 한 바퀴 길이 안에서 돌려 쓴다. 왼쪽 밖으로 나간 조각은 오른쪽 밖에서 다시 나오므로 01 → 02 → 03 → 01 순서로 계속 이어진다
public class BattleBackgroundView : MonoBehaviour
{
    private const float PanelLoopWidth = 16.875f; // 조각 수(3) x 조각 간격(화면 너비 5.625). 화면 너비보다 조각 하나 이상 넓어야 빈틈이 보이지 않는다

    [ReadOnly] [SerializeField] private Transform trnsPanels;

    private List<SpriteRenderer> listPanel;
    private List<float> listPanelBaseX;
    private float scrollPosition;

    private void Awake()
    {
        trnsPanels = GameUtil.Bind<Transform>(transform, "Panels");

        listPanel = trnsPanels.Cast<Transform>().Select(x => x.GetComponent<SpriteRenderer>()).ToList();
        listPanelBaseX = listPanel.Select(x => x.transform.localPosition.x).ToList();
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    /// <summary>
    /// 스테이지 이미지 code({imageCode}_BG_01~03)로 미리 읽은 배경 이미지를 조각에 차례로 넣는다. 이미지가 없는 조각은 그대로 둔다.
    /// </summary>
    public void SetStage(string imageCode)
    {
        for (int i = 0; i < listPanel.Count; i++)
        {
            Sprite sprite = ResourceManager.Instance.GetSprite($"{imageCode}{GameConstants.AddressableAddresses.StageBackgroundInfix}{i + 1:D2}");
            if (sprite != null)
            {
                listPanel[i].sprite = sprite;
            }
        }
    }

    /// <summary>
    /// 땅을 distance만큼 duration(1배속 기준) 동안 흘린다. 다음 적이 땅에 붙어 다가오는 연출과 같은 곡선을 쓴다.
    /// </summary>
    public void Scroll(float distance, float duration, Ease ease, float speed)
    {
        transform.DOKill();
        float from = scrollPosition;
        scrollPosition += distance;

        Tween tween = DOVirtual.Float(from, scrollPosition, duration, ApplyScroll)
            .SetEase(ease)
            .SetTarget(transform);
        tween.timeScale = speed;
    }

    // 조각은 처음에 -loopWidth / 2 ~ loopWidth / 2 사이에 놓여 있고, 흘린 만큼 왼쪽으로 옮긴 뒤 그 범위 안으로 되돌린다
    private void ApplyScroll(float position)
    {
        float leftEdge = -PanelLoopWidth * 0.5f;
        for (int i = 0; i < listPanel.Count; i++)
        {
            Transform trnsPanel = listPanel[i].transform;
            Vector3 panelPosition = trnsPanel.localPosition;
            panelPosition.x = leftEdge + Mathf.Repeat(listPanelBaseX[i] - position - leftEdge, PanelLoopWidth);
            trnsPanel.localPosition = panelPosition;
        }
    }
}
