using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using TD.Typing;       // Tick 受信
using TD.Game;        // GameController（Score）
using TD.TypingCore;  // TypingController の State 参照用

public class ResultUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panelResult;
    public TMP_Text textFinalScore;
    public TMP_Text textWPM;
    public TMP_Text textAcc;
    public TMP_Text textSummary;
    public Button buttonRestart;

    [Header("Sources")]
    public GameController game;        // Score を持ってる
    public TypingController typing;    // WPM/Acc/OK/Miss を読み出す

    private bool shown;

    private void OnEnable()
    {
        TypingEvents.Tick += OnTick;
    }
    private void OnDisable()
    {
        TypingEvents.Tick -= OnTick;
    }

    private void Start()
    {
        if (panelResult) panelResult.SetActive(false);
        if (buttonRestart) buttonRestart.onClick.AddListener(Restart);
    }

    private void OnTick(TickEvent e)
    {
        if (shown) return;
        if (e.timeLeftMs <= 0f)
        {
            shown = true;
            ShowResult();
        }
    }

    private void ShowResult()
    {
        // スポーン停止（任意：全Wave停止）
        foreach (var sp in FindObjectsOfType<TD.TDCore.WaveSpawner>())
            sp.StopAllCoroutines();

        // 入力も緩く止めたいなら（演出は動かすため TimeScale はいじらない方が楽）
        // typing.enabled = false;

        if (panelResult) panelResult.SetActive(true);

        var s = typing != null ? typing.StateReadonly() : default;
        if (textFinalScore) textFinalScore.text = $"SCORE { (game != null ? game.Score : 0) }";
        if (textWPM)        textWPM.text        = $"WPM  { s.wpm:F1}";
        if (textAcc)        textAcc.text        = $"ACC  { (s.accuracy*100f):F0}%";
        if (textSummary)    textSummary.text    = $"OK {s.correctCount} / MISS {s.mistakes}";
    }

    private void Restart()
    {
        // シーン再読み込み（最も簡単）
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
