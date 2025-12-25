using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ScoreboardManager : MonoBehaviour
{
    [Header("UI Baðlantýlarý")]
    public GameObject satirPrefab; // Sadece Text içeren Prefab

    [Header("Listeler")]
    public Transform kelimeContent; // Sol liste kutusu
    public Transform cumleContent;  // Sað liste kutusu

    public TextMeshProUGUI durumYazisi;

    FirebaseFirestore db;
    FirebaseAuth auth;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        VerileriGetir();
    }

    void VerileriGetir()
    {
        if (auth.CurrentUser == null) return;

        string userId = auth.CurrentUser.UserId;
        db.Collection("Users").Document(userId).Collection("Gecmis")
            .OrderByDescending("tarih")
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled) return;

                // Listeleri temizle
                foreach (Transform child in kelimeContent) Destroy(child.gameObject);
                foreach (Transform child in cumleContent) Destroy(child.gameObject);

                QuerySnapshot snapshot = task.Result;
                foreach (DocumentSnapshot belge in snapshot.Documents)
                {
                    Dictionary<string, object> veri = belge.ToDictionary();
                    if (veri.ContainsKey("hedef") && veri.ContainsKey("puan"))
                    {
                        string hedef = veri["hedef"].ToString();
                        string puan = veri["puan"].ToString();
                        string tur = veri.ContainsKey("tur") ? veri["tur"].ToString() : "Kelime";

                        // Türüne göre uygun listeye ekle
                        Transform hedefKutu = (tur == "Cumle") ? cumleContent : kelimeContent;
                        GameObject yeniSatir = Instantiate(satirPrefab, hedefKutu);
                        yeniSatir.GetComponent<TextMeshProUGUI>().text = $"{hedef} : {puan}";
                    }
                }
            });
    }

    public void AnaMenuyeDon() { SceneManager.LoadScene("MainMenu"); }
}