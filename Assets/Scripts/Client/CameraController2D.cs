using System.Collections.Generic;
using UnityEngine;

public class CameraController2D : MonoBehaviour
{
    public enum CameraShakeDirection
    {
        None = 0,
        Horizontal,
        Vertical,
        Both,
    }
    
    public enum CameraMoveMode
    {
        None = 0,
        Target,
        TargetGroup,
    }
    
    [ReadOnly(true)] [SerializeField] private Camera camera;
    public Camera Camera => camera;
    
    [ReadOnly(true)] [SerializeField] private CameraMoveMode moveMode = CameraMoveMode.None;
    public CameraMoveMode MoveMode => moveMode;
    
    [ReadOnly(true)] [SerializeField] private Vector2 cameraSize;
    [ReadOnly(true)] [SerializeField] private Rect roomSize;
    
    [ReadOnly(true)] [SerializeField] private float moveSpeed = 7f;
    
    [ReadOnly(true)] [SerializeField] float defaultSize = 8f;

    [ReadOnly(true)] [SerializeField] private float groupPadding = 1.5f;
    [ReadOnly(true)] [SerializeField] private float minGroupOrthographicSize = 3f;
    [ReadOnly(true)] [SerializeField] private float maxGroupOrthographicSize = 20f;

    private Vector3 targetPosition = Vector3.zero;
    private Vector3 cameraPosition = Vector3.zero;
    
    private float horizontalShakePower = 0f;
    private float verticalShakePower = 0f;
    private float shakeSpeed = 7f;
    
    private float zoom = 1f;
    private float defaultOrthographicSize;
    private float orthographicSize = 1f;
    private float zoomSpeed = 7f;
    
    private float rotationAngle;
    private Vector3 defaultEulerAngles;
    private float rotationSpeed = 7f;
    
    private Transform focusedTargetObject;
    private readonly Dictionary<Transform, float> dicTargetGroup = new Dictionary<Transform, float>();
    
    private void Start()
    {
        var ratio = 1f;
        defaultOrthographicSize = defaultSize / ratio;
        camera.orthographicSize = defaultOrthographicSize;
        
        defaultEulerAngles = camera.transform.eulerAngles;
    }
    
    private void LateUpdate()
    {
        // Move
        if (moveMode == CameraMoveMode.TargetGroup)
        {
            if (TryGetTargetGroupCenter(out var centerPosition))
            {
                targetPosition = centerPosition;
            }
        }
        else if (moveMode == CameraMoveMode.Target)
        {
            if (focusedTargetObject != null)
            {
                if (dicTargetGroup.ContainsKey(focusedTargetObject))
                {
                    targetPosition = focusedTargetObject.position;
                }
            }
        }
        
        var roomSizeWidthHalf = Mathf.Max((roomSize.size.x - (cameraSize.x / zoom)) / 2f, 0);
        var roomSizeHeightHalf = Mathf.Max((roomSize.size.y - (cameraSize.y / zoom)) / 2f, 0);
        
        targetPosition = new Vector3(Mathf.Clamp(targetPosition.x, roomSize.x - roomSizeWidthHalf, roomSize.x + roomSizeWidthHalf)
            , Mathf.Clamp(targetPosition.y, roomSize.y - roomSizeHeightHalf, roomSize.y + roomSizeHeightHalf));
        
        cameraPosition = Vector3.Lerp(cameraPosition, targetPosition, moveSpeed * Time.deltaTime);
        
        // Shake
        horizontalShakePower -= horizontalShakePower / shakeSpeed;
        verticalShakePower -= verticalShakePower / shakeSpeed;
        
        var horizontalShakePosition = (horizontalShakePower >= 0.01f) ? Random.Range(-horizontalShakePower, horizontalShakePower) : 0f;
        var verticalShakePosition = (verticalShakePower >= 0.01f) ? Random.Range(-verticalShakePower, verticalShakePower) : 0f;
        
        var shakePosition = new Vector3(horizontalShakePosition, verticalShakePosition, 0f);
        
        // Rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(defaultEulerAngles.x, defaultEulerAngles.y, rotationAngle), rotationSpeed * Time.deltaTime);
        
        // Zoom
        if (moveMode == CameraMoveMode.TargetGroup)
        {
            // 이동 단계에서 경계 보정까지 끝난 targetPosition을 기준으로 타겟이 화면에 들어오는 크기를 계산
            if (TryGetTargetGroupOrthographicSize(targetPosition, out var groupOrthographicSize))
            {
                orthographicSize = groupOrthographicSize / zoom;
            }
        }
        else
        {
            orthographicSize = defaultOrthographicSize / zoom;
        }
        
        camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, orthographicSize, zoomSpeed * Time.deltaTime);
        
        var resultPosition = cameraPosition + shakePosition;
        transform.position = new Vector3(resultPosition.x, resultPosition.y, transform.position.z);
    }
    
    private bool TryGetTargetGroupCenter(out Vector3 centerPosition)
    {
        centerPosition = Vector3.zero;

        var position = Vector3.zero;
        float totalFocusAmount = 0f;
        foreach (var targetObject in dicTargetGroup)
        {
            if (targetObject.Key == null)
            {
                continue;
            }

            position += new Vector3(targetObject.Key.position.x, targetObject.Key.position.y, 0f) * targetObject.Value;
            totalFocusAmount += targetObject.Value;
        }

        if (totalFocusAmount <= 0f)
        {
            return false;
        }

        centerPosition = position / totalFocusAmount;
        return true;
    }

    // focusAmount가 가장 큰 타겟은 여백까지 포함해 화면 안에 완전히 들어오고,
    // 그보다 작은 타겟은 (focusAmount / 최대 focusAmount) 비율만큼만 화면 크기에 반영된다.
    private bool TryGetTargetGroupOrthographicSize(Vector3 centerPosition, out float groupOrthographicSize)
    {
        groupOrthographicSize = 0f;

        float maxFocusAmount = 0f;
        foreach (var targetObject in dicTargetGroup)
        {
            if (targetObject.Key == null)
            {
                continue;
            }

            maxFocusAmount = Mathf.Max(maxFocusAmount, targetObject.Value);
        }

        if (maxFocusAmount <= 0f)
        {
            return false;
        }

        var aspect = camera.aspect;
        foreach (var targetObject in dicTargetGroup)
        {
            if (targetObject.Key == null)
            {
                continue;
            }

            var offset = targetObject.Key.position - centerPosition;

            // orthographicSize는 화면 세로 길이의 절반이므로, 가로 거리는 aspect로 나눠서 세로 기준으로 환산
            var requiredSize = Mathf.Max(Mathf.Abs(offset.y), Mathf.Abs(offset.x) / aspect) + groupPadding;
            var focusRatio = targetObject.Value / maxFocusAmount;

            groupOrthographicSize = Mathf.Max(groupOrthographicSize, requiredSize * focusRatio);
        }

        groupOrthographicSize = Mathf.Clamp(groupOrthographicSize, minGroupOrthographicSize, maxGroupOrthographicSize);
        return true;
    }

    public void SetCameraMoveMode(CameraMoveMode mode)
    {
        if (moveMode == mode)
        {
            return;
        }
        
        moveMode = mode;
    }
    
    public void AddTarget(Transform target, float focusAmount)
    {
        if (target == null)
        {
            return;
        }
        
        dicTargetGroup[target] = focusAmount;
    }
    
    public void RemoveTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }
        
        dicTargetGroup.Remove(target);
    }

    public void FocusTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }
        
        dicTargetGroup.TryAdd(target, 1f);
        
        focusedTargetObject = target;
    }
    
    public void SetMoveSpeed(float speed)
    {
        this.moveSpeed = speed;
    }
    
    public void Shake(float power, CameraShakeDirection direction = CameraShakeDirection.Both)
    {
        switch (direction)
        {
            case CameraShakeDirection.Horizontal:
                horizontalShakePower = power;
                break;
            case CameraShakeDirection.Vertical:
                verticalShakePower = power;
                break;
            case CameraShakeDirection.Both:
                horizontalShakePower = power;
                verticalShakePower = power;
                break;
        }
    }
    
    public void Zoom(float size, float speed)
    {
        zoom = size;
        zoomSpeed = speed;
    }
    
    public void Rotate(float angle, float speed)
    {
        rotationAngle = angle;
        rotationSpeed = speed;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, cameraSize);
        
        Gizmos.color = Color.orange;
        Gizmos.DrawWireCube(transform.position, cameraSize * (camera.orthographicSize / defaultOrthographicSize));
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(roomSize.center - (roomSize.size / 2f), roomSize.size);
    }
}