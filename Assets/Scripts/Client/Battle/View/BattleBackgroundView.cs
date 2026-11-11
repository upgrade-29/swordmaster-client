using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

// 전투 배경. 먼 배경, 중간, 땅 세 층을 서로 다른 속도로 흘려서 앞으로 걸어가는 것처럼 보이게 한다
// 각 층은 같은 이미지 조각을 이어 놓고 한 바퀴 길이 안에서 돌려 쓴다. 왼쪽 밖으로 나간 조각은 오른쪽 밖에서 다시 나온다
public class BattleBackgroundView : MonoBehaviour
{
    private const float FarScrollRatio = 0.2f; // 땅이 1만큼 갈 때 먼 배경이 가는 거리
    private const float MidScrollRatio = 0.5f;
    private const float LayerLoopWidth = 22.5f; // 조각 수(2) x 조각 너비(11.25). 화면 너비보다 조각 하나 이상 넓어야 빈틈이 보이지 않는다

    [ReadOnly] [SerializeField] private Transform trnsFar;
    [ReadOnly] [SerializeField] private Transform trnsMid;
    [ReadOnly] [SerializeField] private Transform trnsGround;

    private List<SpriteRenderer> listFarTile;
    private List<SpriteRenderer> listMidTile;
    private List<SpriteRenderer> listGroundTile;
    private List<float> listFarBaseX;
    private List<float> listMidBaseX;
    private List<float> listGroundBaseX;
    private float scrollPosition;

    private void Awake()
    {
        trnsFar = GameUtil.Bind<Transform>(transform, "Far");
        trnsMid = GameUtil.Bind<Transform>(transform, "Mid");
        trnsGround = GameUtil.Bind<Transform>(transform, "Ground");

        listFarTile = trnsFar.Cast<Transform>().Select(x => x.GetComponent<SpriteRenderer>()).ToList();
        listMidTile = trnsMid.Cast<Transform>().Select(x => x.GetComponent<SpriteRenderer>()).ToList();
        listGroundTile = trnsGround.Cast<Transform>().Select(x => x.GetComponent<SpriteRenderer>()).ToList();
        listFarBaseX = listFarTile.Select(x => x.transform.localPosition.x).ToList();
        listMidBaseX = listMidTile.Select(x => x.transform.localPosition.x).ToList();
        listGroundBaseX = listGroundTile.Select(x => x.transform.localPosition.x).ToList();
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    /// <summary>
    /// 스테이지 이미지 code({imageCode}_BG_FAR/MID/GROUND)로 미리 읽은 배경 이미지를 층마다 넣는다. 이미지가 없는 층은 그대로 둔다.
    /// </summary>
    public void SetStage(string imageCode)
    {
        SetLayerSprite(listFarTile, imageCode, GameConstants.AddressableAddresses.StageBackgroundFar);
        SetLayerSprite(listMidTile, imageCode, GameConstants.AddressableAddresses.StageBackgroundMid);
        SetLayerSprite(listGroundTile, imageCode, GameConstants.AddressableAddresses.StageBackgroundGround);
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

    private static void SetLayerSprite(List<SpriteRenderer> listTile, string imageCode, string layerName)
    {
        Sprite sprite = ResourceManager.Instance.GetSprite($"{imageCode}{GameConstants.AddressableAddresses.StageBackgroundInfix}{layerName}");
        if (sprite == null)
        {
            return;
        }

        foreach (SpriteRenderer tile in listTile)
        {
            tile.sprite = sprite;
        }
    }

    private void ApplyScroll(float position)
    {
        ApplyLayer(listFarTile, listFarBaseX, position * FarScrollRatio);
        ApplyLayer(listMidTile, listMidBaseX, position * MidScrollRatio);
        ApplyLayer(listGroundTile, listGroundBaseX, position);
    }

    // 조각은 처음에 -LayerLoopWidth / 2 ~ LayerLoopWidth / 2 사이에 놓여 있고, 흘린 만큼 왼쪽으로 옮긴 뒤 그 범위 안으로 되돌린다
    private static void ApplyLayer(List<SpriteRenderer> listTile, List<float> listBaseX, float offset)
    {
        float leftEdge = -LayerLoopWidth * 0.5f;
        for (int i = 0; i < listTile.Count; i++)
        {
            Transform trnsTile = listTile[i].transform;
            Vector3 position = trnsTile.localPosition;
            position.x = leftEdge + Mathf.Repeat(listBaseX[i] - offset - leftEdge, LayerLoopWidth);
            trnsTile.localPosition = position;
        }
    }
}
