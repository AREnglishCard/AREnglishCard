using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Analytics;
using System.Collections.Generic;
using Firebase.Auth;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance;
    FirebaseFirestore db;
    FirebaseAuth auth;

    void Awake()
    {
        // Sahne deðiþse bile bu script yok olmasýn
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Firebase'i baþlat
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
    }

    // --- SKOR KAYDETME ---
    public void SkoruKaydet(string kelime, int puan, string tur)
    {
        if (auth.CurrentUser == null) return; // Giriþ yapmamýþsa kaydetme

        string userId = auth.CurrentUser.UserId;

        // Veritabaný paketini hazýrla
        Dictionary<string, object> veri = new Dictionary<string, object>
        {
            { "hedef", kelime },
            { "puan", puan },
            { "tur", tur }, // "Kelime" veya "Cümle"
            { "tarih", FieldValue.ServerTimestamp }
        };

        // Users -> (UserID) -> Gecmis -> (RastgeleID) yoluna ekle
        db.Collection("Users").Document(userId).Collection("Gecmis").AddAsync(veri);
        Debug.Log("Skor Veritabanýna Gönderildi!");
    }

    // --- ANALÝTÝK LOGLAMA ---
    public void LogTut(string olayAdi, string deger)
    {
        FirebaseAnalytics.LogEvent(olayAdi, new Parameter("deger", deger));
        Debug.Log($"Analitik yollandý: {olayAdi} -> {deger}");
    }
}