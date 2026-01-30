using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Match3/Level Data")]
public class LevelData : ScriptableObject
{
    public int width = 7;
    public int height = 9;
    public int maxMoves = 20;
    public int targetScore = 1000;
    public bool breakAllBoxes;

    // Harita verisi: 0 = Boþ, 1 = Normal Meyve, 2 = Kutu (Engel)
    [HideInInspector]
    public int[] boardLayout;

    public void InitializeLayout()
    {
        if (boardLayout == null || boardLayout.Length != width * height)
        {
            boardLayout = new int[width * height];
            for (int i = 0; i < boardLayout.Length; i++)
            {
                boardLayout[i] = 1;
            }
        }
    }
}