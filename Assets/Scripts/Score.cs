using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI moveText;
    [SerializeField] private TextMeshProUGUI targetScoreText;


    [SerializeField] private TextMeshProUGUI boxGoalText;
    [SerializeField] private GameObject boxGoalContainer; 
    [SerializeField] private RectTransform scoreTextRect; 

    [SerializeField] private GameObject succesful;
    [SerializeField] private GameObject gameOver;

    private int targetScore;
    private int move;
    private int currentScore;


    private int boxesToBreak;
    private bool needsToBreakBoxes;

    public bool isGameActive = true;

    public void SetLevelGoals(int target, int moves, int boxes = 0, bool mustBreakBoxes = false)
    {
        targetScore = target;
        move = moves;
        boxesToBreak = boxes;
        needsToBreakBoxes = mustBreakBoxes;

        if (boxGoalContainer != null)
        {
            boxGoalContainer.SetActive(mustBreakBoxes);
        }

        if (!mustBreakBoxes && scoreTextRect != null)
        {
            scoreTextRect.anchoredPosition = new Vector2(scoreTextRect.anchoredPosition.x, 17);
        }
        else if (scoreTextRect != null)
        {
            scoreTextRect.anchoredPosition = new Vector2(scoreTextRect.anchoredPosition.x, 30);
        }

        currentScore = 0;
        succesful.SetActive(false);
        gameOver.SetActive(false);
        isGameActive = true;
        Time.timeScale = 1;

        UpdateUI();
    }

    public void addScore(int scoreAmount, bool playSound = true)
    {
        if (!isGameActive) return;
        currentScore += scoreAmount;
        UpdateUI();

        if (playSound && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySound(SoundManager.instance.successfulSwipeSound);
        }
    }

    public void BoxBroken()
    {
        if (!isGameActive) return;
        boxesToBreak--;
        if (boxesToBreak < 0) boxesToBreak = 0;
        UpdateUI();
        
    }

    public void moved()
    {
        if (!isGameActive) return;
        move--;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = currentScore.ToString();
        if (targetScoreText != null) targetScoreText.text = targetScore.ToString();
        if (moveText != null) moveText.text = move.ToString();

        if (boxGoalText != null)
            boxGoalText.text = needsToBreakBoxes ? "X"+boxesToBreak.ToString() : "";
    }

    public void CheckGameState()
    {
        if (!isGameActive) return;

        bool scoreMet = currentScore >= targetScore;
        bool boxesMet = !needsToBreakBoxes || boxesToBreak <= 0;

        if (scoreMet && boxesMet)
        {
            isGameActive = false;
            succesful.SetActive(true);
            SoundManager.instance.PlaySound(SoundManager.instance.winSound);
            StartCoroutine(GameOverDelay());
            return;
        }
        else if (move <= 0)
        {
            isGameActive = false;
            gameOver.SetActive(true);
            SoundManager.instance.PlaySound(SoundManager.instance.loseSound);
            StartCoroutine(GameOverDelay());
        }
    }

    private System.Collections.IEnumerator GameOverDelay()
    {
        yield return new WaitForSeconds(0.5f);
        Time.timeScale = 0;
    }
    public bool IsLevelWon()
    {
        bool scoreMet = currentScore >= targetScore;
        bool boxesMet = !needsToBreakBoxes || boxesToBreak <= 0;
        return scoreMet && boxesMet;
    }
}