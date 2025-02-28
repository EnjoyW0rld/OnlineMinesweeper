using UnityEngine;

public class MouseControlls : MonoBehaviour
{
    [SerializeField] private int scrollSpeed = 2;
    [SerializeField] private float _clickThreshold = .13f;
    [SerializeField] private int _dragSpeed = 2;
    private bool _isHolding;
    private Vector3 _clickPos;
    private Vector3 originalCamPos;
    private float _clickTime;

    public UnityEngine.Events.UnityEvent OnSingleClick;
    private void Awake()
    {
        if(OnSingleClick == null) OnSingleClick = new UnityEngine.Events.UnityEvent();
    }

    private void Update()
    {
        float scroll = Input.mouseScrollDelta.y;
        Camera.main.orthographicSize -= scroll * Time.deltaTime * scrollSpeed;
        if (Input.GetMouseButtonDown(0))
        {
            originalCamPos = Camera.main.transform.position;
            _isHolding = true;
            _clickPos = Input.mousePosition;
            _clickTime = 0;
        }
        // Camera drag logic
        if (_isHolding)
        {
            Vector3 delta = Camera.main.ScreenToViewportPoint(_clickPos - Input.mousePosition) * _dragSpeed;
            Vector3 camPos = originalCamPos;

            camPos.x = camPos.x + delta.x;
            camPos.y = camPos.y + delta.y;
            Camera.main.transform.position = camPos;
            _clickTime += Time.deltaTime;
        }
        if (Input.GetMouseButtonUp(0))
        {
            _isHolding = false;
            if (_clickTime < _clickThreshold) OnSingleClick?.Invoke();
        }
    }
}
