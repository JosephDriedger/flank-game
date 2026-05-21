using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class FullScreenBackground : MonoBehaviour
{
    [SerializeField] private Camera _targetCamera;

    private SpriteRenderer _sr;

    private void Awake() => _sr = GetComponent<SpriteRenderer>();

    private void Start() => Fit();

#if UNITY_EDITOR
    private void OnValidate() => Fit();
#endif

    private void Fit()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_targetCamera == null) _targetCamera = Camera.main;
        if (_sr == null || _sr.sprite == null || _targetCamera == null) return;

        float worldHeight = _targetCamera.orthographicSize * 2f;
        float worldWidth  = worldHeight * _targetCamera.aspect;

        Vector2 spriteSize = _sr.sprite.bounds.size;
        transform.localScale = new Vector3(
            worldWidth  / spriteSize.x,
            worldHeight / spriteSize.y,
            1f);

        transform.position = new Vector3(
            _targetCamera.transform.position.x,
            _targetCamera.transform.position.y,
            transform.position.z);
    }
}
