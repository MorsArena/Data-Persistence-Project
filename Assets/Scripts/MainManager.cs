using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainManager : MonoBehaviour
{
    public static MainManager instance;

    public Brick BrickPrefab;
    public int LineCount = 6;
    public Rigidbody Ball;

    public Text Curr_Score;
    public Text Best_Score;
    public GameObject GameOverText;

    private GameObject entryTemplate;
    private GameObject entryContainer;

    private TMP_InputField nameInput;
    public string playerName;

    private int maxScore = 0;
    private string maxPlayer;

    private List<HighscoreEntry> highscoreEntries = new List<HighscoreEntry>();
    private List<Transform> highscoreTransforms = new List<Transform>();

    private bool m_Started = false;
    private int m_Points;

    private HighscoreEntryList highscores;

    private bool m_GameOver = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(instance);

            LoadHighscores();

            nameInput = GameObject.Find("Name").GetComponent<TMP_InputField>();
            nameInput.onEndEdit.AddListener(GetPlayerName);

            SpawnBricks();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void GetPlayerName(string name)
    {
        playerName = name;
    }

    public void Play()
    {
        if (highscores != null && highscores.highscoreEntries != null)
        {
            maxScore = highscores.highscoreEntries[0].score;
            maxPlayer = highscores.highscoreEntries[0].name;
        }

        LoadLevel(1);
    }

    public void HighScoreMenu()
    {
        LoadLevel(2);
    }

    private void LoadLevel(int index)
    {
        StartCoroutine(LoadAsyncScene(index));
    }

    IEnumerator LoadAsyncScene(int index)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(index);


        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (index == 1)
        {
            MainLoad();
        }
        else if (index == 2)
        {
            HighscoreLoad();
        }

        Debug.Log("Scene Loaded");
    }

    void MainLoad()
    {
        GameOverText = GameObject.Find("GameoverText");
        GameOverText.SetActive(false);

        m_GameOver = false;
        m_Started = false;

        Curr_Score = GameObject.Find("ScoreText").GetComponent<Text>();
        Best_Score = GameObject.Find("ScoreText (1)").GetComponent<Text>();

        Best_Score.text = "Best Score - " + maxPlayer + " : " + maxScore;

        Ball = GameObject.Find("Ball").GetComponent<Rigidbody>();

        SpawnBricks();
    }

    void HighscoreLoad()
    {
        entryContainer = GameObject.Find("Container");
        entryTemplate = GameObject.Find("RankTemplate");

        entryTemplate.gameObject.SetActive(false);

        if (highscores != null && highscores.highscoreEntries != null)
        {
            highscoreTransforms = new List<Transform>();
            highscores = SortHighscores(highscores);
            foreach (HighscoreEntry entry in highscores.highscoreEntries)
            {
                CreateHighscoreEntryTransform(entry, entryContainer, highscoreTransforms);
            }
        }    
    }

    public void Exit()
    {
        SaveHighscores();
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    private void SaveHighscores()
    {
        if (highscores != null && highscoreEntries != null)
        {
            for (int i = 0; i < highscores.highscoreEntries.Count; i++)
            {
                HighscoreEntryListSerializable score = new HighscoreEntryListSerializable { score = highscores.highscoreEntries[i].score, name = highscores.highscoreEntries[i].name, listCount = highscores.highscoreEntries.Count };
                string json = JsonUtility.ToJson(score);
                File.WriteAllText(Application.persistentDataPath + "/highscores" + i + ".json", json);
            }
        }
    }

    private void LoadHighscores()
    {
        int listCount;

        string path0 = Application.persistentDataPath + "/highscores0" + ".json";
        if (File.Exists(path0))
        {
            string json = File.ReadAllText(path0);
            HighscoreEntryListSerializable firstScore = JsonUtility.FromJson<HighscoreEntryListSerializable>(json);
            listCount = firstScore.listCount;
            highscoreEntries.Add(new HighscoreEntry { score = firstScore.score, name = firstScore.name });

            for (int j = 1; j < listCount; j++)
            {
                string path = Application.persistentDataPath + "/highscores" + j + ".json";

                if (File.Exists(path))
                {
                    string json2 = File.ReadAllText(path);
                    HighscoreEntryListSerializable score = JsonUtility.FromJson<HighscoreEntryListSerializable>(json2);
                    highscoreEntries.Add(new HighscoreEntry { score = score.score, name = score.name });
                }
            }

            highscores = new HighscoreEntryList { highscoreEntries = highscoreEntries };
        }
    }

    void SpawnBricks()
    {
        const float step = 0.6f;
        int perLine = Mathf.FloorToInt(4.0f / step);
        
        int[] pointCountArray = new [] {1,1,2,2,5,5};
        for (int i = 0; i < LineCount; ++i)
        {
            for (int x = 0; x < perLine; ++x)
            {
                Vector3 position = new Vector3(-1.5f + step * x, 2.5f + i * 0.3f, 0);
                var brick = Instantiate(BrickPrefab, position, Quaternion.identity);
                brick.PointValue = pointCountArray[i];
                brick.onDestroyed.AddListener(AddPoint);
            }
        }
    }

    private void Update()
    {
        if (!m_Started && SceneManager.GetActiveScene().buildIndex == 1)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_Started = true;
                float randomDirection = UnityEngine.Random.Range(-1.0f, 1.0f);
                Vector3 forceDir = new Vector3(randomDirection, 1, 0);
                forceDir.Normalize();

                Ball.transform.SetParent(null);
                Ball.AddForce(forceDir * 2.0f, ForceMode.VelocityChange);
            }
        }
        else if (m_GameOver)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                GameOverText.SetActive(false);
                LoadLevel(1);
            }
        }
    }

    void AddPoint(int point)
    {
        m_Points += point;
        Curr_Score.text = $"Score : {m_Points}";
    }

    public void GameOver()
    {
        m_GameOver = true;
        GameOverText.SetActive(true);

        if (highscores == null || highscores.highscoreEntries == null)
        {
            HighscoreEntry newScore = new HighscoreEntry { score = m_Points, name = playerName };
            highscoreEntries.Add(newScore);
            highscores = new HighscoreEntryList{ highscoreEntries = highscoreEntries };
        }
        else
        {
            highscores.highscoreEntries.Add(AddHighscoreEntry(m_Points, playerName));
            highscores = SortHighscores(highscores);
        }

        if (highscores.highscoreEntries[0].score > maxScore)
        {
            maxScore = highscores.highscoreEntries[0].score;
            maxPlayer = highscores.highscoreEntries[0].name;
            Best_Score.text = "Best Score - " + maxPlayer + " : " + maxScore;
        }

        m_Points = 0;
    }

    private class HighscoreEntry
    {
        public int score;
        public string name;
    }

    private class HighscoreEntryList
    {
        public List<HighscoreEntry> highscoreEntries;
    }

    [Serializable]
    public class HighscoreEntryListSerializable
    {
        public int score;
        public string name;
        public int listCount;
    }

    private void CreateHighscoreEntryTransform(HighscoreEntry highscoreEntry, GameObject container, List<Transform> transformList)
    {
        float templateHeight = 30f;

        Transform entryTransform = Instantiate( entryTemplate.transform, container.transform);
        RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>();
        entryRectTransform.anchoredPosition = new Vector2(0, -templateHeight * transformList.Count);
        entryTransform.gameObject.SetActive(true);

        int rank = transformList.Count + 1;
        string rankString;

        switch (rank)
        {
            case 1: rankString = "1st"; break;
            case 2: rankString = "2nd"; break;
            case 3: rankString = "3rd"; break;
            default: rankString = rank + "th"; break;
        }

        entryTransform.Find("RankTxt").GetComponent<TextMeshProUGUI>().text = rankString;
        entryTransform.Find("ScoreTxt").GetComponent<TextMeshProUGUI>().text = highscoreEntry.score.ToString();
        entryTransform.Find("NameTxt").GetComponent<TextMeshProUGUI>().text = highscoreEntry.name;

        transformList.Add(entryTransform);
    }

    private HighscoreEntry AddHighscoreEntry(int score, string name)
    {
        HighscoreEntry highscoreEntry = new HighscoreEntry { score = score, name = name };
        
        return highscoreEntry;
    }

    private HighscoreEntryList SortHighscores(HighscoreEntryList highscoreList)
    {
        for (int i = 0; i < highscoreList.highscoreEntries.Count; i++)
        {
            for (int j = i + 1; j < highscoreList.highscoreEntries.Count; j++)
            {
                if (highscoreList.highscoreEntries[j].score > highscoreList.highscoreEntries[i].score)
                {
                    HighscoreEntry tmp = highscoreList.highscoreEntries[i];
                    highscoreList.highscoreEntries[i] = highscoreEntries[j];
                    highscoreList.highscoreEntries[j] = tmp;
                }
            }
        }

        if (highscoreList.highscoreEntries.Count > 10)
        {
            highscoreList.highscoreEntries.RemoveAt(10);
        }

        return highscoreList;
    }
}
