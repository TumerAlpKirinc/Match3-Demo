using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

public class CardController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Sprite bomb;
    public Sprite striped;
    public bool isSpecial = false;
    public string specialType = "";
    [Header("Hareket Ayarlarý")]
    public float moveSpeed = 20f;

    public int column;
    public int row;

    private BoardManager board;
    private GameObject otherCard;
    private Vector2 firstTouchPosition;
    private Vector2 finalTouchPosition;

    public Vector2 targetPosition;
    public bool isMatched = false;


    private Coroutine wobbleCoroutine;
    void Start()
    {
        board = FindFirstObjectByType<BoardManager>();
        targetPosition = board.GetCenteredPosition(column, row);
    }

    void Update()
    {
        if ((Vector2)transform.position != targetPosition)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (board.score != null && !board.score.isGameActive) return;

       
        if (board.currentState != BoardManager.GameState.MOVE) return;
        board.ResetHint();
        firstTouchPosition = Camera.main.ScreenToWorldPoint(eventData.pressPosition);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (board.currentState != BoardManager.GameState.MOVE)
            return;

        finalTouchPosition = Camera.main.ScreenToWorldPoint(eventData.position);
        float dragDistance = Vector2.Distance(firstTouchPosition, finalTouchPosition);

        if (dragDistance < 0.1f)
        {
            if (isSpecial) board.TryActivateSpecial(column, row);
        }
        else
        {
            CalculateAngle();
        }
    }

    void CalculateAngle()
    {
        if (Vector3.Distance(firstTouchPosition, finalTouchPosition) < 0.5f) return;
        float swipeAngle = Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y, finalTouchPosition.x - firstTouchPosition.x) * 180 / Mathf.PI;
        MovePieces(swipeAngle);
    }

    void MovePieces(float swipeAngle)
    {

        board.currentState = BoardManager.GameState.WAIT;
        int oldColumn = column;
        int oldRow = row;

        // Yön Tayini
        if (swipeAngle > -45 && swipeAngle <= 45 && column < board.width - 1)
        {
            otherCard = board.allTiles[column + 1, row];
            CheckAndSwap(oldColumn, oldRow, column + 1, row);
        }
        else if (swipeAngle > 45 && swipeAngle <= 135 && row < board.height - 1)
        {
            otherCard = board.allTiles[column, row + 1];
            CheckAndSwap(oldColumn, oldRow, column, row + 1);
        }
        else if ((swipeAngle > 135 || swipeAngle <= -135) && column > 0)
        {
            otherCard = board.allTiles[column - 1, row];
            CheckAndSwap(oldColumn, oldRow, column - 1, row);
        }
        else if (swipeAngle < -45 && swipeAngle >= -135 && row > 0)
        {
            otherCard = board.allTiles[column, row - 1];
            CheckAndSwap(oldColumn, oldRow, column, row - 1);
        }
        else
        {
            board.currentState = BoardManager.GameState.MOVE;
        }
    }

    private void CheckAndSwap(int c1, int r1, int c2, int r2)
    {
        if (otherCard == null)
        {
            board.currentState = BoardManager.GameState.MOVE;
            return;
        }

        SwapPositions(c1, r1, c2, r2);
        StartCoroutine(CheckMatchAndSwapBack());
    }

    private void SwapPositions(int c1, int r1, int c2, int r2)
    {
        GameObject temp = board.allTiles[c1, r1];
        board.allTiles[c1, r1] = board.allTiles[c2, r2];
        board.allTiles[c2, r2] = temp;

        UpdateCardData(board.allTiles[c1, r1], c1, r1);
        UpdateCardData(board.allTiles[c2, r2], c2, r2);
    }

    private void UpdateCardData(GameObject card, int c, int r)
    {
        if (card == null) return;

        CardController cc = card.GetComponent<CardController>();
        if (cc != null) 
        {
            cc.column = c;
            cc.row = r;
            cc.targetPosition = board.GetCenteredPosition(c, r);
        }
    }

    private IEnumerator CheckMatchAndSwapBack()
    {
        yield return new WaitForSeconds(.2f);

        if (otherCard != null)
        {
            CardController otherCC = otherCard.GetComponent<CardController>();

            if (this.isSpecial || (otherCC != null && otherCC.isSpecial))
            {
                CheckAndActivateSpecial();
                otherCard = null;
                yield break;
            }

            if (board.FindAllMatches())
            {
                if (board.score != null) board.score.moved();
                board.StartFillProcess();
            }
            else
            {
                if (SoundManager.instance != null && SoundManager.instance.failSwipeSound != null)
                {
                    SoundManager.instance.PlaySound(SoundManager.instance.failSwipeSound);
                }
                int c1 = column;
                int r1 = row;
                int c2 = otherCC.column;
                int r2 = otherCC.row;
                SwapPositions(c1, r1, c2, r2);

                yield return new WaitForSeconds(0.25f);
                board.currentState = BoardManager.GameState.MOVE;
            }
        }
        otherCard = null;
    }

    public void MakeSpecial(string type)
    {
        isSpecial = true;
        specialType = type;
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            
            sr.sprite = (specialType == "BOMB") ? bomb : striped;
            
        }
    }

    private void CheckAndActivateSpecial()
    {


        if (board.score != null) board.score.moved();
        
        if (this.isSpecial)
        {
            board.currentState = BoardManager.GameState.MOVE;
            board.TryActivateSpecial(column, row);
            return;
        }

        if (otherCard != null)
        {
            CardController otherCC = otherCard.GetComponent<CardController>();
            if (otherCC != null && otherCC.isSpecial)
            {
                board.currentState = BoardManager.GameState.MOVE;
                board.TryActivateSpecial(otherCC.column, otherCC.row);
            }
        }
    }



    public void StartWobble()
    {
        if (wobbleCoroutine == null)
        {
            wobbleCoroutine = StartCoroutine(WobbleAnimation());
        }
    }

    public void StopWobble()
    {
        if (wobbleCoroutine != null)
        {
            StopCoroutine(wobbleCoroutine);
            wobbleCoroutine = null;
        }
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private System.Collections.IEnumerator WobbleAnimation()
    {
        Vector3 originalScale = Vector3.one;
        float timer = 0;

        while (true)
        {
            timer += Time.deltaTime * 5f;
            float zRot = Mathf.Sin(timer) * 10f;
            transform.rotation = Quaternion.Euler(0, 0, zRot);
            float scaleOffset = Mathf.Sin(timer) * 0.1f;
            transform.localScale = originalScale + new Vector3(scaleOffset, scaleOffset, 0);
            yield return null;
        }
    }
}