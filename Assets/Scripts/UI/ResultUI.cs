using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TD.Game; // GameStats / GameController

public class ResultUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panelResult;
    public TMP_Text textFinalScore;
    public TMP_Text textWPM;
    public TMP_Text textAcc;
    public TMP_Text textSummary;
    public Button   buttonRestart;
    public GameController game;

    private bool shown;

    private void Awake()
    {
        if (!game) game = FindObjectOfType<GameController>();
        if (panelResult) panelResult.SetActive(false);

        if (buttonRestart)
        {
            buttonRestart.onClick.RemoveAllListeners();
            buttonRestart.onClick.AddListener(Restart);
        }

        // ここで一度だけ購読（重複防止に -= してから +=）
        if (game) { game.OnGameOver -= OnGameOverReceived; game.OnGameOver += OnGameOverReceived; }
    }

    private void OnDestroy()
    {
        if (game) game.OnGameOver -= OnGameOverReceived;
    }

    private void OnGameOverReceived(GameStats s)
    {
        if (shown) return;
        shown = true;

        if (panelResult) panelResult.SetActive(true);
        if (textFinalScore) textFinalScore.text = $"SCORE {s.Score}\nHIGH  {s.HighScore}";
        if (textWPM)        textWPM.text        = $"WPM: {s.WPMRounded}";
        if (textAcc)        textAcc.text        = $"Accuracy: {s.AccuracyPercent:0.#}%";

        // Summaryに 敵数 と Top3 を追加（Top3が未設定なら2行目は空）
        string top = (s.Top3Scores != null && s.Top3Scores.Length > 0)
            ? $"1ST {s.Top3Scores[0]}" +
            (s.Top3Scores.Length > 1 ? $"   2ND {s.Top3Scores[1]}" : "") +
            (s.Top3Scores.Length > 2 ? $"   3RD {s.Top3Scores[2]}" : "")
            : "";

        if (textSummary)
            textSummary.text =
                $"OK: {s.OkCount} / MISS: {s.MissCount} / ENE: {s.EnemiesDefeated}"
                + (top != "" ? $"\n{top}" : "");

        Time.timeScale = 0f;
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
