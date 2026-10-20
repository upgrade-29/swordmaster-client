using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

// 전투 배경. 먼 산, 중간 수풀, 땅 세 층을 서로 다른 속도로 흘려서 앞으로 걸어가는 것처럼 보이게 한다
// 각 층은 같은 간격으로 놓인 조각을 한 바퀴 길이 안에서 돌려 쓴다. 왼쪽 밖으로 나간 조각은 오른쪽 밖에서 다시 나온다
public class BattleBackgroundView : MonoBehaviour
{
    private const float FarScrollRatio = 0.2f; // 땅이 1만큼 갈 때 먼 산이 가는 거리
    private const float MidScrollRatio = 0.5f;
    private const float FarLoopWidth = 7.5f; // 조각 수 x 조각 간격. 화면 너비보다 조각 하나 이상 넓어야 빈틈이 보이지 않는다
    private const float MidLoopWidth = 7.12f;
    private const float GroundLoopWidth = 6.672f;

    [ReadOnly] [SerializeField] private Transform trnsFar;
    [ReadOnly] [SerializeField] private Transform trnsMid;
    [ReadOnly] [SerializeField] private Transform trnsGround;

    private List<Transform> listFarTile;
    private List<Transform> listMidTile;
    private List<Transform> listGroundTile;
    private List<float> listFarBaseX;
    private List<float> listMidBaseX;
    private List<float> listGroundBaseX;
    private float scrollPosition;

    private void Awake()
    {
        trnsFar = GameUtil.Bind<Transform>(transform, "Far");
        trnsMid = GameUtil.Bind<Transform>(transform, "Mid");
        trnsGround = GameUtil.Bind<Transform>(transform, "Ground/Marks");

        listFarTile = trnsFar.Cast<Transform>().ToList();
        listMidTile = trnsMid.Cast<Transform>().ToList();
        listGroundTile = trnsGround.Cast<Transform>().ToList();
        listFarBaseX = listFarTile.Select(x => x.localPosition.x).ToList();
        listMidBaseX = listMidTile.Select(x => x.localPosition.x).ToList();
        listGroundBaseX = listGroundTile.Select(x => x.localPosition.x).ToList();
    }

    private void OnDestroy()
    {
        transform.DOKill();
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

    private void ApplyScroll(float position)
    {
        ApplyLayer(listFarTile, listFarBaseX, position * FarScrollRatio, FarLoopWidth);
        ApplyLayer(listMidTile, listMidBaseX, position * MidScrollRatio, MidLoopWidth);
        ApplyLayer(listGroundTile, listGroundBaseX, position, GroundLoopWidth);
    }

    // 조각은 처음에 -loopWidth / 2 ~ loopWidth / 2 사이에 놓여 있고, 흘린 만큼 왼쪽으로 옮긴 뒤 그 범위 안으로 되돌린다
    private static void ApplyLayer(List<Transform> listTile, List<float> listBaseX, float offset, float loopWidth)
    {
        float leftEdge = -loopWidth * 0.5f;
        for (int i = 0; i < listTile.Count; i++)
        {
            Vector3 position = listTile[i].localPosition;
            position.x = leftEdge + Mathf.Repeat(listBaseX[i] - offset - leftEdge, loopWidth);
            listTile[i].localPosition = position;
        }
    }
}
