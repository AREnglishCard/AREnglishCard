using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement; // SAHNE YÖNETÝMÝ ÝÇÝN GEREKLÝ

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("UI Elemanlarý")]
    public Text infoText;
    public Text timerText;
    public Text scoreText;

    [Header("Oyun Ayarlarý")]
    public string[] wordPool = { "cat", "dog", "bird", "car", "apple", "banana" };
    public float gameDuration = 60f;
    public string mainMenuSceneName = "MainMenu"; 

    // Private Deðiþkenler
    private float currentTimer;
    private bool isGameRunning = false;
    private int myScore = 0;
    private int opponentScore = 0;

    // Döngü Kontrolü
    private string currentTargetWord = "";
    private bool hasShownCard = false;
    private bool hasSpoken = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
   

    void Start()
    {
        currentTimer = gameDuration;
        UpdateScoreUI();

        if (!PhotonNetwork.IsConnected)
        {
            infoText.text = "TEK OYUNCULU MOD";
            StartGameLogic();
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
                    photonView.RPC("RPC_StartGame", RpcTarget.All);
                else
                    infoText.text = "OYUNCU BEKLENÝYOR";
            }
            else
            {
                infoText.text = "KURUCU BEKLENÝYOR...";
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            photonView.RPC("RPC_StartGame", RpcTarget.All);
        }
    }

    [PunRPC]
    public void RPC_StartGame()
    {
        infoText.text = "RAKÝP GELDÝ! BAÞLIYORUZ!";
        infoText.color = Color.green;
        StartCoroutine(StartCountdown());
    }

    IEnumerator StartCountdown()
    {
        yield return new WaitForSeconds(1f);
        infoText.text = "3...";
        yield return new WaitForSeconds(1f);
        infoText.text = "2...";
        yield return new WaitForSeconds(1f);
        infoText.text = "1...";
        yield return new WaitForSeconds(1f);

        StartGameLogic();
    }

    void StartGameLogic()
    {
        isGameRunning = true;
        currentTimer = gameDuration;
        myScore = 0;
        opponentScore = 0;
        NextTurn();
    }

    void NextTurn()
    {
        if (!isGameRunning) return;

        hasShownCard = false;
        hasSpoken = false;

        if (wordPool.Length > 0)
        {
            string yeniKelime = "";
            if (wordPool.Length > 1)
            {
                do { yeniKelime = wordPool[Random.Range(0, wordPool.Length)]; }
                while (yeniKelime == currentTargetWord);
            }
            else { yeniKelime = wordPool[0]; }

            currentTargetWord = yeniKelime;

            infoText.text = "HEDEF: " + currentTargetWord.ToUpper();
            infoText.color = Color.blue;

            if (SpeechManager.Instance != null)
                SpeechManager.Instance.HedefGuncelle(currentTargetWord, "This is a " + currentTargetWord);
        }
    }

    void Update()
    {
        if (isGameRunning)
        {
            currentTimer -= Time.deltaTime;
            timerText.text = Mathf.CeilToInt(currentTimer).ToString();

            // Sadece MasterClient bitirme emrini verir
            if (PhotonNetwork.IsMasterClient && currentTimer <= 0)
            {
                photonView.RPC("RPC_FinishGame", RpcTarget.All);
            }

            // Single Player ise direkt bitir
            if (!PhotonNetwork.IsConnected && currentTimer <= 0)
            {
                EndGameLocal();
            }
        }
    }

    [PunRPC]
    public void RPC_FinishGame()
    {
        EndGameLocal();
    }

    void EndGameLocal()
    {
        isGameRunning = false;
        timerText.text = "0";
        string resultMessage = "";

        if (!PhotonNetwork.IsConnected)
        {
            resultMessage = "OYUN BÝTTÝ!\nSkorun: " + myScore;
            infoText.color = Color.white;
        }
        else
        {
            if (myScore > opponentScore)
            {
                resultMessage = "KAZANDIN!\nTEBRÝKLER";
                infoText.color = Color.green;
            }
            else if (myScore < opponentScore)
            {
                resultMessage = "KAYBETTÝN...";
                infoText.color = Color.red;
            }
            else
            {
                resultMessage = "BERABERE!";
                infoText.color = Color.yellow;
            }
        }

        infoText.text = resultMessage;

        // --- BURADA MENÜYE DÖNME SÜRECÝNÝ BAÞLATIYORUZ ---
        StartCoroutine(ReturnToMenuRoutine());
    }

    // --- MENÜYE DÖNÜÞ KODLARI ---
    IEnumerator ReturnToMenuRoutine()
    {
        // 1. Oyuncular sonucu görsün diye 5 saniye bekle
        yield return new WaitForSeconds(5f);

        if (PhotonNetwork.IsConnected)
        {
            infoText.text = "MENÜYE DÖNÜLÜYOR...";
            PhotonNetwork.LeaveRoom(); // Odadan çýk (Bu OnLeftRoom'u tetikler)
        }
        else
        {
            // Offline ise direkt sahne deðiþtir
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    // Odadan çýkýþ tamamlanýnca otomatik çalýþýr
    public override void OnLeftRoom()
    {
        // Ana Menü sahnesine geç
        SceneManager.LoadScene(mainMenuSceneName);
    }
    // ----------------------------------------------------

    public void OnCardDetected(string cardName)
    {
        if (isGameRunning && !hasShownCard && cardName.Trim().ToLower() == currentTargetWord.Trim().ToLower())
        {
            hasShownCard = true;
            AddScore(10);
            infoText.text = $"DOÐRU KART!\nÞÝMDÝ OKU: {currentTargetWord.ToUpper()}";
            infoText.color = Color.yellow;
        }
    }

    public void OnSpeechScoreReceived(int score)
    {
        if (isGameRunning && !hasSpoken)
        {
            if (!hasShownCard) { infoText.text = "ÖNCE KARTI GÖSTER!"; return; }

            hasSpoken = true;
            AddScore(score);
            StartCoroutine(WaitAndNextTurn(score));
        }
    }

    IEnumerator WaitAndNextTurn(int lastScore)
    {
        infoText.text = $"HARÝKA! (+{lastScore})";
        infoText.color = Color.green;
        yield return new WaitForSeconds(1.5f);
        NextTurn();
    }

    public void AddScore(int amount)
    {
        if (!isGameRunning) return;
        myScore += amount;
        UpdateScoreUI();

        if (PhotonNetwork.IsConnected)
            photonView.RPC("RPC_UpdateOpponentScore", RpcTarget.Others, myScore);
    }

    [PunRPC]
    public void RPC_UpdateOpponentScore(int remoteScore)
    {
        opponentScore = remoteScore;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (PhotonNetwork.IsConnected)
            scoreText.text = $"BEN: {myScore} - RAKÝP: {opponentScore}";
        else
            scoreText.text = $"SKOR: {myScore}";
    }
}