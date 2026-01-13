using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("UI Elemanlarý")]
    public Text infoText;
    public Text timerText;
    public Text scoreText;

    [Header("Buton Görsel Ayarlarý")]
    public Image butonImage;        // Inspector'dan butonun üzerindeki Image bileþenini buraya sürükle
    public Sprite imgKelimeDinle;   // 1. Durum (Varsayýlan)
    public Sprite imgDinliyorum;    // 2. Durum (Mikrofon açýk)
    public Sprite imgAnaliz;        // 3. Durum (Gemini Bekleniyor)

    [Header("Oyun Ayarlarý")]
    public string[] wordPool = { "cat", "dog", "bird", "car", "apple", "banana" };
    public float gameDuration = 60f;
    public string mainMenuSceneName = "MainMenu";

    // Private Deðiþkenler
    private float currentTimer;
    private bool isGameRunning = false;
    private int myScore = 0;
    private int opponentScore = 0;

    // Tur kontrol
    private string currentTargetWord = "";
    private bool hasShownCard = false;
    private bool hasSpoken = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        currentTimer = gameDuration;
        myScore = 0;
        opponentScore = 0;
        UpdateScoreUI();

        if (!PhotonNetwork.IsConnected)
        {
            SetInfo("TEK OYUNCULU MOD", Color.white);
            StartGameLogic();
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
                    photonView.RPC(nameof(RPC_StartGame), RpcTarget.All);
                else
                    SetInfo("OYUNCU BEKLENÝYOR", Color.white);
            }
            else
            {
                SetInfo("KURUCU BEKLENÝYOR...", Color.white);
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
            photonView.RPC(nameof(RPC_StartGame), RpcTarget.All);
    }

    [PunRPC]
    public void RPC_StartGame()
    {
        SetInfo("RAKÝP GELDÝ! BAÞLIYORUZ!", Color.green);
        StartCoroutine(StartCountdown());
    }

    IEnumerator StartCountdown()
    {
        yield return new WaitForSeconds(1f);
        SetInfo("3...", Color.white);
        yield return new WaitForSeconds(1f);
        SetInfo("2...", Color.white);
        yield return new WaitForSeconds(1f);
        SetInfo("1...", Color.white);
        yield return new WaitForSeconds(1f);

        StartGameLogic();
    }

    void StartGameLogic()
    {
        isGameRunning = true;
        currentTimer = gameDuration;

        // skor reset
        myScore = 0;
        opponentScore = 0;
        UpdateScoreUI();

        NextTurn();
    }

    void Update()
    {
        if (!isGameRunning) return;

        currentTimer -= Time.deltaTime;
        if (timerText) timerText.text = Mathf.CeilToInt(Mathf.Max(0f, currentTimer)).ToString();

        // Multiplayer: sadece master bitirir
        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.IsMasterClient && currentTimer <= 0f)
                photonView.RPC(nameof(RPC_FinishGame), RpcTarget.All);
        }
        else
        {
            if (currentTimer <= 0f)
                EndGameLocal();
        }
    }

    [PunRPC]
    public void RPC_FinishGame()
    {
        EndGameLocal();
    }

    void EndGameLocal()
    {
        if (!isGameRunning) return;

        isGameRunning = false;
        if (timerText) timerText.text = "0";

        string resultMessage;

        if (!PhotonNetwork.IsConnected)
        {
            resultMessage = "OYUN BÝTTÝ!\nSkorun: " + myScore;
            SetInfo(resultMessage, Color.white);
        }
        else
        {
            if (myScore > opponentScore)
                SetInfo("KAZANDIN!\nTEBRÝKLER", Color.green);
            else if (myScore < opponentScore)
                SetInfo("KAYBETTÝN...", Color.red);
            else
                SetInfo("BERABERE!", Color.yellow);
        }

        StartCoroutine(ReturnToMenuRoutine());
    }

    IEnumerator ReturnToMenuRoutine()
    {
        yield return new WaitForSeconds(5f);

        if (PhotonNetwork.IsConnected)
        {
            SetInfo("MENÜYE DÖNÜLÜYOR...", Color.white);
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ===================== TUR / HEDEF =====================

    void NextTurn()
    {
        if (!isGameRunning) return;

        hasShownCard = false;
        hasSpoken = false;

        if (wordPool == null || wordPool.Length == 0)
        {
            SetInfo("Kelime havuzu boþ!", Color.red);
            return;
        }

        string yeniKelime = currentTargetWord;

        // Ayný kelimeyi üst üste getirmemeye çalýþ
        if (wordPool.Length == 1)
        {
            yeniKelime = wordPool[0];
        }
        else
        {
            int safety = 0;
            while (yeniKelime == currentTargetWord && safety < 25)
            {
                yeniKelime = wordPool[Random.Range(0, wordPool.Length)];
                safety++;
            }
        }

        currentTargetWord = (yeniKelime ?? "").Trim();

        SetInfo("HEDEF: " + currentTargetWord.ToUpper(), Color.blue);

        if (SpeechManager.Instance != null)
            SpeechManager.Instance.HedefGuncelle(currentTargetWord, "This is a " + currentTargetWord);
    }

    // ===================== DIÞTAN GELEN EVENTLER =====================

    // Kart algýlandý (Vuforia vs)
    public void OnCardDetected(string cardName)
    {
        if (!isGameRunning) return;
        if (hasShownCard) return;

        if (string.IsNullOrWhiteSpace(cardName)) return;

        string a = cardName.Trim().ToLower();
        string b = (currentTargetWord ?? "").Trim().ToLower();

        if (a == b && !string.IsNullOrEmpty(b))
        {
            hasShownCard = true;
            AddScore(10);

            SetInfo($"DOÐRU KART!\nÞÝMDÝ OKU: {currentTargetWord.ToUpper()}", Color.yellow);
        }
    }

    // SpeechManager skor döndürdü
    public void OnSpeechScoreReceived(int score)
    {
        if (!isGameRunning) return;

        // Kart gösterilmediyse konuþma turu bitirmesin
        if (!hasShownCard)
        {
            SetInfo("ÖNCE KARTI GÖSTER!", Color.red);
            return;
        }

        // Zaten konuþulduysa tekrar tetikleme (double click / iki coroutine vb)
        if (hasSpoken) return;

        hasSpoken = true;

        int safeScore = Mathf.Clamp(score, 0, 100);
        AddScore(safeScore);

        StartCoroutine(WaitAndNextTurn(safeScore));
    }

    IEnumerator WaitAndNextTurn(int lastScore)
    {
        SetInfo($"HARÝKA! (+{lastScore})", Color.green);
        yield return new WaitForSeconds(1.5f);
        NextTurn();
    }

    // ===================== SKOR =====================

    public void AddScore(int amount)
    {
        if (!isGameRunning) return;

        myScore += amount;
        UpdateScoreUI();

        if (PhotonNetwork.IsConnected && photonView != null)
            photonView.RPC(nameof(RPC_UpdateOpponentScore), RpcTarget.Others, myScore);
    }

    [PunRPC]
    public void RPC_UpdateOpponentScore(int remoteScore)
    {
        opponentScore = remoteScore;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (!scoreText) return;

        if (PhotonNetwork.IsConnected)
            scoreText.text = $"BEN: {myScore} - RAKÝP: {opponentScore}";
        else
            scoreText.text = $"SKOR: {myScore}";
    }

    // ===================== UI YARDIMCI =====================

    void SetInfo(string msg, Color c)
    {
        if (infoText)
        {
            infoText.text = msg;
            //infoText.color = c;
        }
        else
        {
            Debug.Log(msg);
        }
    }

    public void UpdateButtonImage(int state)
    {
        if (butonImage == null) return;

        switch (state)
        {
            case 0:
                if (imgKelimeDinle) butonImage.sprite = imgKelimeDinle;
                break;
            case 1:
                if (imgDinliyorum) butonImage.sprite = imgDinliyorum;
                break;
            case 2:
                if (imgAnaliz) butonImage.sprite = imgAnaliz;
                break;
            default:
                if (imgKelimeDinle) butonImage.sprite = imgKelimeDinle;
                break;
        }
    }
}