using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class BoardManager : MonoBehaviour
{
    public enum GameState
    {
        WAIT,   
        MOVE    
    }

    public GameState currentState = GameState.MOVE;

    [Header("Özel Efekt Prefablarý")]
    public GameObject bombEffectPrefab;
    public GameObject swipeEffectPrefab;

    [Header("Tahta Ayarlarý")]
    [HideInInspector] public int width;
    [HideInInspector] public int height;

    [Header("Mesafe Ayarlarý (Grid Düzeni)")]
    public float xOffset = 0.8f;
    public float yOffset = 1.1f;

    [Header("Ýpucu Ayarlarý")]
    public float hintDelay = 5f; 
    private float hintTimer;
    private bool isHintShowing = false;
    private GameObject hintCard1, hintCard2; 

    [Header("Prefablar")]
    public GameObject boxPrefab;     
    public GameObject tilePrefab;    
    public GameObject[] fruitPrefabs; 

    public Transform[] topTilePos;
    public GameObject[,] allTiles;

    [Header("Skor")]
    public Score score;
    public int scorePerTile = 10;

    [Header("Tüm Seviyelerin Listesi")]
    public List<LevelData> allLevels; 

    private LevelData currentLevel;
    void Start()
    {
        Application.targetFrameRate = 60;
        int levelToPlay = PlayerPrefs.GetInt("CurrentLevelToPlay", 1);
        int index = levelToPlay - 1;

        if (index >= 0 && index < allLevels.Count)
        {
            currentLevel = allLevels[index];
            Setup();
        }
        else
        {
            Debug.LogError("Seçilen seviye listesinde LevelData bulunamadý! Ýndeks: " + index);
        }

    }
    void Update()
    {
        if (currentState == GameState.MOVE)
        {
            hintTimer += Time.deltaTime;
            if (hintTimer >= hintDelay && !isHintShowing)
            {
                ShowHint();
            }
        }
        else
        {
            ResetHint();
        }
    }

    public void ResetHint()
    {
        hintTimer = 0f;
        if (isHintShowing)
        {
            StopHintAnimation();
            isHintShowing = false;
        }
    }
    private void Setup()
    {
        width = currentLevel.width;
        height = currentLevel.height;
        allTiles = new GameObject[width, height];
        topTilePos = new Transform[width];
        int totalBoxesOnStart = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int index = y * width + x;
                int tileType = currentLevel.boardLayout[index];

                Vector2 tempPosition = GetCenteredPosition(x, y);

                // --- ZEMÝN OLUÞTURMA ---
                GameObject backgroundTile = Instantiate(tilePrefab, tempPosition, Quaternion.identity);
                backgroundTile.transform.parent = this.transform;
                backgroundTile.name = "Tile (" + x + "," + y + ")";

                if (y == height - 1)
                {
                    topTilePos[x] = backgroundTile.transform;
                }

                if (tileType == 1)
                {
                    GenerateFruitAt(x, y, tempPosition);
                }
                else if (tileType == 2)
                {
                    GenerateObstacleAt(x, y, tempPosition);
                    totalBoxesOnStart++;
                }
                else 
                {
                    allTiles[x, y] = null;
                }
            }
        }
        if (score != null && currentLevel != null)
        {
            score.SetLevelGoals(
              currentLevel.targetScore,
              currentLevel.maxMoves,
              totalBoxesOnStart,
              currentLevel.breakAllBoxes);
        }

    }

    private void GenerateFruitAt(int x, int y, Vector2 pos)
    {
        int cardIndex = Random.Range(0, fruitPrefabs.Length);
        int maxIterations = 0;

        while (MatchesAt(x, y, fruitPrefabs[cardIndex]) && maxIterations < 100)
        {
            cardIndex = Random.Range(0, fruitPrefabs.Length);
            maxIterations++;
        }

        GameObject newCard = Instantiate(fruitPrefabs[cardIndex], pos, Quaternion.identity);
        newCard.transform.parent = this.transform;
        newCard.name = "Card (" + x + "," + y + ")";

        CardController cc = newCard.GetComponent<CardController>();
        cc.column = x;
        cc.row = y;
        cc.targetPosition = pos;

        allTiles[x, y] = newCard;
    }

    private void GenerateObstacleAt(int x, int y, Vector2 pos)
    {
        GameObject box = Instantiate(boxPrefab, pos, Quaternion.identity);
        box.transform.parent = this.transform;
        box.name = "Box (" + x + "," + y + ")";
        box.tag = "Obstacle";

        CardController cc = box.GetComponent<CardController>();
        if (cc != null)
        {
            cc.column = x;
            cc.row = y;
            cc.targetPosition = pos;
        }
        allTiles[x, y] = box;
    }

    private bool MatchesAt(int column, int row, GameObject piece)
    {
        if (column > 1)
        {
            GameObject left1 = allTiles[column - 1, row];
            GameObject left2 = allTiles[column - 2, row];
            if (left1 != null && left2 != null)
            {
                if (left1.CompareTag(piece.tag) && left2.CompareTag(piece.tag)) return true;
            }
        }
        if (row > 1)
        {
            GameObject down1 = allTiles[column, row - 1];
            GameObject down2 = allTiles[column, row - 2];
            if (down1 != null && down2 != null)
            {
                if (down1.CompareTag(piece.tag) && down2.CompareTag(piece.tag)) return true;
            }
        }
        return false;
    }
    public bool FindAllMatches()
    {
        bool foundMatch = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                GameObject current = allTiles[x, y];
                if (current == null || current.CompareTag("Obstacle")) continue;

                if (allTiles[x + 1, y] != null && allTiles[x + 2, y] != null &&
                  !allTiles[x + 1, y].CompareTag("Obstacle") && !allTiles[x + 2, y].CompareTag("Obstacle") &&
                  allTiles[x + 1, y].tag == current.tag && allTiles[x + 2, y].tag == current.tag)
                {
                    List<GameObject> matchLine = new List<GameObject> { current, allTiles[x + 1, y], allTiles[x + 2, y] };

                    for (int i = x + 3; i < width; i++)
                    {
                        if (allTiles[i, y] != null && !allTiles[i, y].CompareTag("Obstacle") && allTiles[i, y].tag == current.tag)
                            matchLine.Add(allTiles[i, y]);
                        else break;
                    }

                    if (matchLine.Count >= 4)
                        MarkAndCreateSpecialCard(matchLine, matchLine[matchLine.Count / 2]);
                    else
                        foreach (var card in matchLine) card.GetComponent<CardController>().isMatched = true;

                    foreach (var card in matchLine)
                    {
                        CardController cc = card.GetComponent<CardController>();
                        CheckObstacles(cc.column, cc.row); 
                    }
                    foundMatch = true;
                    x += matchLine.Count - 1;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                GameObject current = allTiles[x, y];
                if (current == null || current.CompareTag("Obstacle")) continue;

                if (allTiles[x, y + 1] != null && allTiles[x, y + 2] != null &&
                  !allTiles[x, y + 1].CompareTag("Obstacle") && !allTiles[x, y + 2].CompareTag("Obstacle") &&
                  allTiles[x, y + 1].tag == current.tag && allTiles[x, y + 2].tag == current.tag)
                {
                    List<GameObject> matchLine = new List<GameObject> { current, allTiles[x, y + 1], allTiles[x, y + 2] };

                    for (int i = y + 3; i < height; i++)
                    {
                        if (allTiles[x, i] != null && !allTiles[x, i].CompareTag("Obstacle") && allTiles[x, i].tag == current.tag)
                            matchLine.Add(allTiles[x, i]);
                        else break;
                    }

                    if (matchLine.Count >= 4)
                        MarkAndCreateSpecialCard(matchLine, matchLine[matchLine.Count / 2]);
                    else
                        foreach (var card in matchLine) card.GetComponent<CardController>().isMatched = true;

                    foreach (var card in matchLine)
                    {
                        CardController cc = card.GetComponent<CardController>();
                        CheckObstacles(cc.column, cc.row); 
                    }

                    foundMatch = true;
                    y += matchLine.Count - 1;
                }
            }
        }
        return foundMatch;
    }
    public void DestroyMatches()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allTiles[x, y] != null)
                {
                    CardController controller = allTiles[x, y].GetComponent<CardController>();

                    if (controller == null) continue;

                    if (controller.isMatched && !controller.isSpecial)
                    {
                        if (score != null) score.addScore(scorePerTile, true);

                        Destroy(allTiles[x, y]);
                        allTiles[x, y] = null;
                    }
                    else if (controller.isSpecial)
                    {
                        controller.isMatched = false;
                    }
                }
            }
        }
    }

    private void MarkAndCreateSpecialCard(List<GameObject> matchedCards, GameObject specialCardLocation)
    {
        CardController specialController = specialCardLocation.GetComponent<CardController>();

        if (matchedCards.Count >= 5)
        {
            specialController.MakeSpecial("BOMB"); 
        }
        else if (matchedCards.Count == 4)
        {
            specialController.MakeSpecial("STRIPED");
        }

        foreach (var card in matchedCards)
        {
            if (card != specialCardLocation)
            {
                card.GetComponent<CardController>().isMatched = true;
            }
        }
        specialController.isMatched = false;
    }
    private Coroutine fillCoroutine;

    public void StartFillProcess()
    {
        if (fillCoroutine == null)
        {
            fillCoroutine = StartCoroutine(FillProcessCoroutine());
        }
    }
    public System.Collections.IEnumerator FillProcessCoroutine()
    {
        currentState = GameState.WAIT;
        bool keepLooping = true;

        while (keepLooping)
        {
            DestroyMatches();
            yield return new WaitForSeconds(0.15f);

            for (int x = 0; x < width; x++)
            {
                CollapseColumn(x);
                
            }
            yield return new WaitForSeconds(0.2f);

            FillBoard();
            yield return new WaitForSeconds(0.35f);

            if (!FindAllMatches())
            {
                if (!IsMoveAvailable())
                {
                    yield return new WaitForSeconds(0.2f);
                    ShuffleBoard();
                    yield return new WaitForSeconds(0.4f);
                }
                keepLooping = false;
            }
        }

        if (score != null)
        {
            score.CheckGameState();

            if (score.isGameActive)
            {
                currentState = GameState.MOVE;
            }
        }

        fillCoroutine = null;
    }

    public void FillBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int index = y * width + x;

                if (currentLevel.boardLayout[index] != 0 && allTiles[x, y] == null)
                {
                    int cardIndex = Random.Range(0, fruitPrefabs.Length);
                    Vector2 spawnPos = GetCenteredPosition(x, height);

                    GameObject newCard = Instantiate(fruitPrefabs[cardIndex], spawnPos, Quaternion.identity);
                    newCard.transform.parent = this.transform;

                    allTiles[x, y] = newCard;
                    CardController cc = newCard.GetComponent<CardController>();
                    cc.column = x;
                    cc.row = y;
                    cc.targetPosition = GetCenteredPosition(x, y);
                }
            }
        }
    }


    public int CollapseColumn(int column)
    {
        int movedCount = 0;
        for (int row = 0; row < height; row++)
        {
            int index = row * width + column;
            if (currentLevel.boardLayout[index] != 0 && allTiles[column, row] == null)
            {
                for (int nextRow = row + 1; nextRow < height; nextRow++)
                {
                    if (allTiles[column, nextRow] != null)
                    {
                        allTiles[column, row] = allTiles[column, nextRow];

                        CardController controller = allTiles[column, row].GetComponent<CardController>();
                        if (controller != null)
                        {
                            controller.row = row;
                            controller.column = column;
                            controller.targetPosition = GetCenteredPosition(column, row);
                        }

                        allTiles[column, nextRow] = null;
                        movedCount++;
                        break;
                    }
                }
            }
        }
        return movedCount;
    }


    public void TryActivateSpecial(int x, int y, bool force = false)
    {
        if (!force && currentState != GameState.MOVE) return;
        
        if (allTiles[x, y] == null) return;
        
        CardController controller = allTiles[x, y].GetComponent<CardController>();
        if (controller == null || !controller.isSpecial) return;
        
        if (!force)
        {
            currentState = GameState.WAIT;
            if (score != null) score.moved();
        }
        
        if (score != null) score.addScore(scorePerTile, false);
        
        StartCoroutine(ProcessSpecialWithDelay(controller));
    }
    private System.Collections.IEnumerator ProcessSpecialWithDelay(CardController controller)
    {
        int x = controller.column;
        int y = controller.row;
        string type = controller.specialType;

        allTiles[x, y] = null;
        Destroy(controller.gameObject);

        float waitDuration = 0.2f;

        if (type == "BOMB")
            waitDuration = ProcessBombEffect(x, y);
        else if (type == "STRIPED")
            waitDuration = ProcessStripedEffect(x, y);

        yield return new WaitForSeconds(waitDuration);
        StartCoroutine(FillProcessCoroutine());
    }

    private float ProcessBombEffect(int centerX, int centerY)
    {
        Vector3 effectPos = GetCenteredPosition(centerX, centerY);
        effectPos.z = -2f;
        Instantiate(bombEffectPrefab, effectPos, Quaternion.identity);
        SoundManager.instance.PlaySound(SoundManager.instance.bombSound);

        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            for (int y = centerY - 1; y <= centerY + 1; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    GameObject target = allTiles[x, y];
                    if (target == null) continue;

                    if (target.CompareTag("Obstacle"))
                    {
                        allTiles[x, y] = null;
                        Destroy(target);

                        if (score != null) score.BoxBroken();
                    }
                    else
                    {
                        CardController cc = target.GetComponent<CardController>();
                        if (cc != null)
                        {
                            if (cc.isSpecial)
                            {
                                if (allTiles[x, y] == null) continue;
                                TryActivateSpecial(x, y, true);
                            }
                            else
                            {
                                cc.isMatched = true;
                            }
                        }
                    }
                }
            }
        }

        return 0.5f; 
    }

    private float ProcessStripedEffect(int x, int y)
    {
        int direction = Random.Range(0, 2);
        float effectDuration = 0.4f;
        Vector2 triggerPos = GetCenteredPosition(x, y);

        if (direction == 0) 
        {
            Vector3 effectPos = new Vector3(0, triggerPos.y, -2f);
            GameObject effect = Instantiate(swipeEffectPrefab, effectPos, Quaternion.identity);
            effect.transform.rotation = Quaternion.Euler(0, 0, 0);

            for (int col = 0; col < width; col++)
            {
                ProcessCellInteraction(col, y, x, y);
            }
        }
        else
        {
            Vector3 effectPos = new Vector3(triggerPos.x, 0, -2f);
            GameObject effect = Instantiate(swipeEffectPrefab, effectPos, Quaternion.identity);
            effect.transform.rotation = Quaternion.Euler(0, 0, 90);

            for (int row = 0; row < height; row++)
            {
                ProcessCellInteraction(x, row, x, y);
            }
        }

        SoundManager.instance.PlaySound(SoundManager.instance.laserSound);
        return effectDuration;
    }

    private void ProcessCellInteraction(int targetX, int targetY, int originX, int originY)
    {
        if (allTiles[targetX, targetY] == null) return;

        if (allTiles[targetX, targetY].CompareTag("Obstacle"))
        {
            Destroy(allTiles[targetX, targetY]);
            allTiles[targetX, targetY] = null;
            if (score != null) score.BoxBroken();
        }
        else
        {
            CardController cc = allTiles[targetX, targetY].GetComponent<CardController>();
            if (cc != null)
            {
                if (cc.isSpecial)
                {
                    if (targetX == originX && targetY == originY) return;

                    TryActivateSpecial(targetX, targetY, true);
                }
                else
                {
                    cc.isMatched = true;
                }
            }
        }
    }
    void CheckObstacles(int x, int y)
    {
        Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int dir in neighbors)
        {
            int checkX = x + dir.x;
            int checkY = y + dir.y;

            if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
            {
                GameObject neighbor = allTiles[checkX, checkY];
                if (neighbor != null && neighbor.CompareTag("Obstacle"))
                {
                    allTiles[checkX, checkY] = null;
                    Destroy(neighbor);
                    if (score != null)
                    {
                        score.BoxBroken();
                    }
                }
            }
        }
    }


    private bool IsMoveAvailable()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allTiles[x, y] == null || allTiles[x, y].CompareTag("Obstacle")) continue;
                int[] dx = { 1, 0 };
                int[] dy = { 0, 1 };
                for (int i = 0; i < 2; i++)
                {
                    int nextX = x + dx[i];
                    int nextY = y + dy[i];
                    if (nextX < width && nextY < height)
                    {
                        if (allTiles[nextX, nextY] == null || allTiles[nextX, nextY].CompareTag("Obstacle")) continue;

                        GameObject temp = allTiles[x, y];
                        allTiles[x, y] = allTiles[nextX, nextY];
                        allTiles[nextX, nextY] = temp;

                        bool moveWorks = CheckPotentialMatch(x, y) || CheckPotentialMatch(nextX, nextY);

                        allTiles[nextX, nextY] = allTiles[x, y];
                        allTiles[x, y] = temp;

                        if (moveWorks) return true; 
                    }
                }
            }
        }
        return false;
    }


    private bool CheckPotentialMatch(int x, int y)
    {
        string tag = allTiles[x, y].tag;
        int horizontalCount = 1;
        for (int i = x + 1; i < width && allTiles[i, y] != null && allTiles[i, y].CompareTag(tag); i++) horizontalCount++;
        for (int i = x - 1; i >= 0 && allTiles[i, y] != null && allTiles[i, y].CompareTag(tag); i--) horizontalCount++;
        if (horizontalCount >= 3) return true;

        int verticalCount = 1;
        for (int i = y + 1; i < height && allTiles[x, i] != null && allTiles[x, i].CompareTag(tag); i++) verticalCount++;
        for (int i = y - 1; i >= 0 && allTiles[x, i] != null && allTiles[x, i].CompareTag(tag); i--) verticalCount++;
        if (verticalCount >= 3) return true;

        return false;
    }

    public void ShuffleBoard()
    {
        currentState = GameState.WAIT;
        List<GameObject> fruitsOnBoard = new List<GameObject>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allTiles[x, y] != null && !allTiles[x, y].CompareTag("Obstacle"))
                {
                    fruitsOnBoard.Add(allTiles[x, y]);
                    allTiles[x, y] = null;
                }
            }
        }

        for (int i = 0; i < fruitsOnBoard.Count; i++)
        {
            GameObject temp = fruitsOnBoard[i];
            int randomIndex = Random.Range(i, fruitsOnBoard.Count);
            fruitsOnBoard[i] = fruitsOnBoard[randomIndex];
            fruitsOnBoard[randomIndex] = temp;
        }

        int fruitIndex = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (currentLevel.boardLayout[y * width + x] != 0 && allTiles[x, y] == null)
                {
                    if (fruitIndex < fruitsOnBoard.Count)
                    {
                        allTiles[x, y] = fruitsOnBoard[fruitIndex];
                        CardController cc = allTiles[x, y].GetComponent<CardController>();
                        cc.column = x;
                        cc.row = y;
                        cc.targetPosition = GetCenteredPosition(x, y);
                        fruitIndex++;
                    }
                }
            }
        }

        if (!IsMoveAvailable())
        {
            ShuffleBoard();
        }
        if (FindAllMatches())
        {
            StartCoroutine(FillProcessCoroutine());
        }
        currentState = GameState.MOVE;
    }



    public Vector2 GetCenteredPosition(int x, int y)
    {
        float totalWidth = (width - 1) * xOffset;
        float totalHeight = (height - 1) * yOffset;

        float posX = (x * xOffset) - (totalWidth / 2f);
        float posY = (y * yOffset) - (totalHeight / 2f);

        return new Vector2(posX, posY);
    }

    
    private void ShowHint()
    {
        if (isHintShowing) return;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allTiles[x, y] == null || allTiles[x, y].CompareTag("Obstacle")) continue;

                int[] dx = { 1, 0 };
                int[] dy = { 0, 1 };

                for (int i = 0; i < 2; i++)
                {
                    int nextX = x + dx[i];
                    int nextY = y + dy[i];

                    if (nextX < width && nextY < height)
                    {
                        if (allTiles[nextX, nextY] == null || allTiles[nextX, nextY].CompareTag("Obstacle")) continue;

                        SwapCardsTemporarily(x, y, nextX, nextY);

                        if (CheckPotentialMatch(x, y) || CheckPotentialMatch(nextX, nextY))
                        {
                            hintCard1 = allTiles[nextX, nextY]; 
                            hintCard2 = allTiles[x, y];
                            StartHintAnimation(hintCard1, hintCard2);
                            isHintShowing = true;
                            SwapCardsTemporarily(x, y, nextX, nextY);
                            return;
                        }
                        SwapCardsTemporarily(x, y, nextX, nextY);
                    }
                }
            }
        }
    }

    private void SwapCardsTemporarily(int x1, int y1, int x2, int y2)
    {
        GameObject temp = allTiles[x1, y1];
        allTiles[x1, y1] = allTiles[x2, y2];
        allTiles[x2, y2] = temp;
    }


    private void StartHintAnimation(GameObject c1, GameObject c2)
    {
        c1.GetComponent<CardController>().StartWobble();
        c2.GetComponent<CardController>().StartWobble();
    }
    private void StopHintAnimation()
    {
        if (hintCard1 != null) hintCard1.GetComponent<CardController>().StopWobble();
        if (hintCard2 != null) hintCard2.GetComponent<CardController>().StopWobble();
    }
    

}
/*
 * TEST ÝÇÝN BELÝRLÝ BÝR KONUMA JOKER EKLEME
    private void ForceSpecialCardForTesting(int x, int y, string type)
    {
        if (x < width && y < height && allTiles[x, y] != null)
        {
            CardController cc = allTiles[x, y].GetComponent<CardController>();
            if (cc != null)
            {
                cc.MakeSpecial(type);
                Debug.Log($"TEST: ({x},{y}) konumunda {type} joker oluþturuldu.");
            }
        }
    }

*/