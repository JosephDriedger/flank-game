using UnityEngine;

public sealed class PieceView : MonoBehaviour
{
    public string PieceId { get; private set; }

    [SerializeField] private GameObject _flagIndicator;

    public void Init(string pieceId)
    {
        PieceId = pieceId;
        name = pieceId;
    }

    public void SetHasFlag(bool hasFlag)
    {
        if (_flagIndicator == null)
        {
            return;
        }

        _flagIndicator.SetActive(hasFlag);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_flagIndicator == null)
        {
            Transform t = transform.Find("FlagIndicator");
            if (t != null)
            {
                _flagIndicator = t.gameObject;
            }
        }
    }
#endif
}
