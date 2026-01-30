using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMenuButtonScript : MonoBehaviour
{
    public Score score;
    public void ReturnMainMenu()
    {


        if (score != null && score.IsLevelWon())
        {
            int currentLevel = PlayerPrefs.GetInt("CurrentLevelToPlay", 1);
            int nextLevel = currentLevel + 1;
            int reachedLevel = PlayerPrefs.GetInt("ReachedLevel", 1);

            if (nextLevel > reachedLevel)
            {
                PlayerPrefs.SetInt("ReachedLevel", nextLevel);
            }
            PlayerPrefs.Save();
        }
        
        Time.timeScale = 1;
        SceneManager.LoadScene(0); 
    }
    public void NextLevel()
    {
        int currentLevel = PlayerPrefs.GetInt("CurrentLevelToPlay", 1);
        int nextLevel = currentLevel + 1;

        PlayerPrefs.SetInt("CurrentLevelToPlay", nextLevel);

        int reachedLevel = PlayerPrefs.GetInt("ReachedLevel", 1);
        if (nextLevel > reachedLevel)
        {
            PlayerPrefs.SetInt("ReachedLevel", nextLevel);
        }

        PlayerPrefs.Save();
        Time.timeScale = 1;
        SceneManager.LoadScene(1);
    }
    public void TryAgain()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(1);
    }
}
