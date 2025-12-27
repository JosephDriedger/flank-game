using UnityEngine;

[CreateAssetMenu(menuName = "FlankGame/Board Map Config", fileName = "BoardMapConfig")]
public sealed class BoardMapConfig : ScriptableObject
{
    [TextArea(8, 30)]
    public string boardMap;

    // Keep true unless you know you want even-r
    public bool oddR = true;
}
