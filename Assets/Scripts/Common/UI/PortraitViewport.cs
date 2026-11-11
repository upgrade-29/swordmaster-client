using UnityEngine;
using UnityEngine.UI;

// 카메라와 UI가 같은 1080x1920 영역을 사용한다. 남는 공간은 입력을 막는 검은 여백으로 덮는다.
// 기존 씬 캔버스는 이 루트 아래에 두므로 GameObject.Find와 자식 바인딩 경로를 유지한다.
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
public sealed class PortraitViewport : MonoBehaviour
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

    [SerializeField] private Camera sceneCamera;
    [SerializeField] private RectTransform content;

    private Canvas rootCanvas;
    private RectTransform[] bars;
    private Vector2 previousScreenSize;
    private Rect originalCameraRect;

    private void Awake()
    {
        rootCanvas = GetComponent<Canvas>();
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = ReferenceResolution;
        content.localScale = Vector3.one;

        // 여백으로 튀어나간 UI와 터치 영역을 함께 자른다. 팝업과 동전 효과에도 적용된다.
        if (content.GetComponent<RectMask2D>() == null)
        {
            content.gameObject.AddComponent<RectMask2D>();
        }

        bars = new[] { CreateBar("LeftBar"), CreateBar("RightBar"), CreateBar("BottomBar"), CreateBar("TopBar") };
        if (sceneCamera != null)
        {
            originalCameraRect = sceneCamera.rect;
        }
    }

    private void OnEnable()
    {
        previousScreenSize = Vector2.zero;
        UpdateViewport();
    }

    private void LateUpdate()
    {
        UpdateViewport();
    }

    private void OnDisable()
    {
        if (sceneCamera != null)
        {
            sceneCamera.rect = originalCameraRect;
        }
    }

    // CanvasScaler와 같은 디스플레이 크기를 사용한다. 회전이나 창 크기 변경도 다음 프레임에 반영한다.
    private void UpdateViewport()
    {
        Vector2 screenSize = rootCanvas.renderingDisplaySize;
        if (screenSize.x <= 0f || screenSize.y <= 0f || screenSize == previousScreenSize)
        {
            return;
        }

        previousScreenSize = screenSize;
        Rect viewport = CalculateViewport(screenSize);
        if (sceneCamera != null)
        {
            // orthographicSize는 건드리지 않아 전투의 확대/복귀 연출과 충돌하지 않는다.
            sceneCamera.rect = viewport;
        }

        SetBar(bars[0], Vector2.zero, new Vector2(viewport.xMin, 1f));
        SetBar(bars[1], new Vector2(viewport.xMax, 0f), Vector2.one);
        SetBar(bars[2], new Vector2(viewport.xMin, 0f), new Vector2(viewport.xMax, viewport.yMin));
        SetBar(bars[3], new Vector2(viewport.xMin, viewport.yMax), new Vector2(viewport.xMax, 1f));
    }

    public static Rect CalculateViewport(Vector2 screenSize)
    {
        if (screenSize.x <= 0f || screenSize.y <= 0f)
        {
            return new Rect(0f, 0f, 1f, 1f);
        }

        float scale = Mathf.Min(screenSize.x / ReferenceResolution.x, screenSize.y / ReferenceResolution.y);
        Vector2 size = new Vector2(ReferenceResolution.x * scale / screenSize.x,
            ReferenceResolution.y * scale / screenSize.y);
        return new Rect((Vector2.one - size) * 0.5f, size);
    }

    private RectTransform CreateBar(string barName)
    {
        GameObject bar = new GameObject(barName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.layer = gameObject.layer;
        RectTransform rect = (RectTransform)bar.transform;
        rect.SetParent(transform, false);
        Image image = bar.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;
        return rect;
    }

    private static void SetBar(RectTransform bar, Vector2 min, Vector2 max)
    {
        bar.anchorMin = min;
        bar.anchorMax = max;
        bar.offsetMin = bar.offsetMax = Vector2.zero;
        bar.gameObject.SetActive(max.x > min.x && max.y > min.y);
    }
}
