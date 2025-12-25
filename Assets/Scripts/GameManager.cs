using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    public Text roleText;
    public Text wordText;
    public Text timerText;

    [Header("Bu Sahneye Ait Kelimeler")]
    // Burayý hangi sahnedeyse ona göre doldur (Örn: FruitsScene ise sadece meyveler)
    public string[] wordPool = { "apple", "banana", "orange" };

    private string currentTargetWord;
    private float startTime;
    private bool isGameActive = false;

    void Start()
    {
        // Online moddaysak (Lobi üzerinden gelindiyse)
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            int randomIndex = Random.Range(0, wordPool.Length);
            photonView.RPC("RPC_SetupRound", RpcTarget.All, wordPool[randomIndex]);
        }
        // Offline moddaysak (Direkt kategori butonuyla gelindiyse)
        else if (!PhotonNetwork.IsConnected)
        {
            isGameActive = true;
            currentTargetWord = wordPool[Random.Range(0, wordPool.Length)];
            wordText.text = "KELÝME: " + currentTargetWord.ToUpper();
            roleText.text = "TEK OYUNCULU MOD";
        }
    }

    [PunRPC]
    void RPC_SetupRound(string word)
    {
        currentTargetWord = word;
        isGameActive = true;
        startTime = (float)PhotonNetwork.Time;

        if (PhotonNetwork.IsMasterClient)
        {
            roleText.text = "ROLÜN: ANLATICI";
            wordText.text = "KELÝMEN: " + currentTargetWord.ToUpper();
        }
        else
        {
            roleText.text = "ROLÜN: TAHMÝNCÝ";
            wordText.text = "ARKADAÞINI DÝNLE...";
        }
    }

    public void CheckGuess(string detectedCardName)
    {
        if (isGameActive && detectedCardName.ToLower() == currentTargetWord.ToLower())
        {
            if (PhotonNetwork.IsConnected)
                photonView.RPC("RPC_WinGame", RpcTarget.All);
            else
                RPC_WinGame();
        }
    }

    [PunRPC]
    void RPC_WinGame()
    {
        isGameActive = false;
        roleText.text = "TEBRÝKLER!";
        wordText.text = "DOÐRU: " + currentTargetWord.ToUpper();
    }
}