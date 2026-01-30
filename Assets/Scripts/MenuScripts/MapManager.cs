using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
    
    public string levelSaveKey = "ReachedLevel";
    public int totalLevels = 10;
    public string buttonPrefix = "LevelButton_";
    private string lockChildName = "Lock";

    [Header("Renk")]
    public Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color activeColor = Color.white;
    public Color completedColor = new Color(0.6f, 1f, 0.6f);

    private List<Button> levelButtons = new List<Button>();
    private List<GameObject> lockIcons = new List<GameObject>();

    private int reachedLevel;

    void Start()
    {
        reachedLevel = PlayerPrefs.GetInt(levelSaveKey, 1);
        AutoSetupLists();
        UpdateMapVisuals();
    }

    void AutoSetupLists()
    {
        levelButtons.Clear();
        lockIcons.Clear();

        for (int i = 1; i <= totalLevels; i++)
        {
            string searchName = buttonPrefix + i;
            GameObject btnObj = GameObject.Find(searchName);

            if (btnObj != null)
            {
                levelButtons.Add(btnObj.GetComponent<Button>());
                Transform lockChild = btnObj.transform.Find(lockChildName);
                if (lockChild != null) lockIcons.Add(lockChild.gameObject);
                else lockIcons.Add(null);
            }
        }
    }

    void UpdateMapVisuals()
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            int levelNumber = i + 1;
            Button btn = levelButtons[i];
            GameObject lockImg = lockIcons[i];

            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();

            btn.onClick.RemoveAllListeners();

            if (levelNumber < reachedLevel)
            {
                btn.image.color = completedColor;
                btn.interactable = true;
                if (btnText != null) btnText.text = "Level " + levelNumber.ToString();
                if (lockImg != null) lockImg.SetActive(false);

                int lvl = levelNumber;
                btn.onClick.AddListener(() => LoadLevel(lvl));
            }
            else if (levelNumber == reachedLevel)
            {
                btn.image.color = activeColor;
                btn.interactable = true;
                if (btnText != null) btnText.text ="Level "+ levelNumber.ToString();
                if (lockImg != null) lockImg.SetActive(false);

                btn.transform.localScale = Vector3.one * 1.2f;

                int lvl = levelNumber;
                btn.onClick.AddListener(() => LoadLevel(lvl));
            }
            else
            {
                btn.image.color = lockedColor;
                btn.interactable = false;
                if (btnText != null) btnText.text = "";
                if (lockImg != null) lockImg.SetActive(true);

                btn.transform.localScale = Vector3.one;
            }
        }
    }

    public void LoadLevel(int level)
    {
        PlayerPrefs.SetInt("CurrentLevelToPlay", level);
        SceneManager.LoadScene("SampleScene");
    }
}