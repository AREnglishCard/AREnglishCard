using UnityEngine.Android;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class SpeechManager : MonoBehaviour
{
    public static SpeechManager Instance;

    [Header("Gemini Ayarları")]
    private static string geminiApiKey = "AIzaSyAXKCMP8rtMYJc4zpJfS2E-85Gg5W6HkfA";
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    [Header("UI Bağlantıları")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI buttonLabel;
    public TextMeshProUGUI buttonLabelCumle;

    [Header("Mikrofon Ayarı")]
    public TMP_Dropdown micDropdown;

    // --- HAFIZA ---
    private string aktifKelime = "";
    private string aktifCumle = "";

    // MOD SEÇİMİ
    private bool puanlamaCumleIcinMi = false;

    private AudioClip recordingClip;
    private string currentDeviceName = null;
    private bool isRecording = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
        StartCoroutine(MikrofonlariListele());
    }

    IEnumerator MikrofonlariListele()
    {
        yield return new WaitForSeconds(0.5f);

        if (Microphone.devices.Length > 0)
        {
            if (micDropdown != null)
            {
                micDropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < Microphone.devices.Length; i++) options.Add(Microphone.devices[i]);
                micDropdown.AddOptions(options);
                micDropdown.onValueChanged.AddListener(MikrofonDegisti);

                // İlk mikrofonu seç
                currentDeviceName = Microphone.devices[0];
            }
            else
            {
                currentDeviceName = Microphone.devices[0];
            }

            if (scoreText)
            {
                scoreText.text = "Hazır (Kart Göster)";
                scoreText.color = Color.white;
            }
        }
        else
        {
            if (scoreText)
            {
                scoreText.text = "Mikrofon Bulunamadı!";
                scoreText.color = Color.red;
            }
        }

        if (buttonLabel) buttonLabel.text = "KELİME PUANLA";
        if (buttonLabelCumle) buttonLabelCumle.text = "CÜMLE PUANLA";
    }

    public void MikrofonDegisti(int index)
    {
        if (Microphone.devices.Length > index)
        {
            currentDeviceName = Microphone.devices[index];
            Debug.Log($"Mikrofon Değişti: {currentDeviceName}");
        }
    }

    public void HedefGuncelle(string kelime, string cumle)
    {
        aktifKelime = kelime != null ? kelime.Trim() : "";
        aktifCumle = cumle != null ? cumle.Trim() : "";

        if (scoreText)
        {
            scoreText.text = "Kart: " + aktifKelime;
            scoreText.color = Color.white;
        }
    }

    public void ButonKelimeOku()
    {
        if (!string.IsNullOrEmpty(aktifKelime) && TTSManager.Instance)
            TTSManager.Instance.SadeceKelimeyiOku();
    }

    public void ButonCumleOku()
    {
        if (!string.IsNullOrEmpty(aktifCumle) && TTSManager.Instance)
            TTSManager.Instance.SadeceCumleyiOku();
    }

    public void ButonMikrofonKelime()
    {
        if (string.IsNullOrEmpty(aktifKelime))
        {
            if (scoreText)
            {
                scoreText.text = "Önce kart göster!";
                scoreText.color = Color.red;
            }
            return;
        }

        puanlamaCumleIcinMi = false;
        ButonMikrofonGenel(buttonLabel);
    }

    public void ButonMikrofonCumle()
    {
        if (string.IsNullOrEmpty(aktifCumle))
        {
            if (scoreText)
            {
                scoreText.text = "Bu kartta cümle yok!";
                scoreText.color = Color.red;
            }
            return;
        }

        puanlamaCumleIcinMi = true;
        ButonMikrofonGenel(buttonLabelCumle);
    }

    void ButonMikrofonGenel(TextMeshProUGUI labelToChange)
    {
        if (!isRecording)
        {
            // --- 1. MİKROFONU AÇMA (BAŞLATMA) ---

            if (Microphone.devices.Length == 0)
            {
                if (scoreText)
                {
                    scoreText.text = "Mikrofon Yok!";
                    scoreText.color = Color.red;
                }
                return;
            }

            if (string.IsNullOrEmpty(currentDeviceName))
                currentDeviceName = Microphone.devices[0];

            isRecording = true;
            Microphone.End(currentDeviceName); // Temizle

            int kayitSuresi = puanlamaCumleIcinMi ? 8 : 4;

            // Cihazın frekansını al
            Microphone.GetDeviceCaps(currentDeviceName, out int minFreq, out int maxFreq);
            int finalFreq = (maxFreq > 0) ? maxFreq : 44100;

            try
            {
                recordingClip = Microphone.Start(currentDeviceName, false, kayitSuresi, finalFreq);

                // [GÖRSEL] -> DİNLİYORUM MODU (State 1)
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.UpdateButtonImage(1);
                }

                if (scoreText)
                {
                    scoreText.text = "DİNLİYORUM...";
                    scoreText.color = Color.yellow;
                }

                if (labelToChange) labelToChange.text = "BİTİR";
            }
            catch (Exception e)
            {
                Debug.LogError("Mikrofon Hatası: " + e.Message);

                if (scoreText)
                {
                    scoreText.text = "Mic Başlatılamadı!";
                    scoreText.color = Color.red;
                }

                // [GÖRSEL] -> HATA OLDU, BAŞA DÖN (State 0)
                if (GameManager.Instance != null)
                    GameManager.Instance.UpdateButtonImage(0);

                isRecording = false;

                if (labelToChange)
                    labelToChange.text = puanlamaCumleIcinMi ? "CÜMLE PUANLA" : "KELİME PUANLA";
            }
        }
        else
        {
            // --- 2. KAYDI BİTİRME VE ANALİZ ---

            isRecording = false;

            int position = Microphone.GetPosition(currentDeviceName);
            Microphone.End(currentDeviceName);

            if (labelToChange)
                labelToChange.text = puanlamaCumleIcinMi ? "CÜMLE PUANLA" : "KELİME PUANLA";

            if (scoreText)
            {
                scoreText.text = "Analiz...";
                scoreText.color = Color.white;
            }

            // [GÖRSEL] -> ANALİZ MODU (State 2)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateButtonImage(2);
            }

            if (recordingClip == null)
            {
                if (scoreText)
                {
                    scoreText.text = "Kayıt alınamadı!";
                    scoreText.color = Color.red;
                }
                // Hata varsa görseli düzelt
                if (GameManager.Instance != null) GameManager.Instance.UpdateButtonImage(0);
                return;
            }

            // Süre dolup kendi durduysa pozisyon 0 dönebilir, onu düzeltiyoruz
            if (position <= 0) position = recordingClip.samples;

            byte[] wavData = ConvertToWav(recordingClip, position);

            if (wavData == null)
            {
                if (scoreText)
                {
                    scoreText.text = "MİKROFON SES ALMIYOR\n(Ses Seviyesi: 0)";
                    scoreText.color = Color.red;
                }
                // Sessizlik hatası varsa görseli düzelt (State 0)
                if (GameManager.Instance != null) GameManager.Instance.UpdateButtonImage(0);
                return;
            }

            StartCoroutine(SendToGemini(wavData));
        }
    }

    IEnumerator SendToGemini(byte[] audioData)
    {
        string url = $"{API_URL}?key={geminiApiKey}";
        string base64Audio = Convert.ToBase64String(audioData);

        string promptText =
            "Transcribe the audio exactly. If it is silence or unintelligible noise, return 'SILENCE'. Do not provide explanations.";

        string jsonBody = $@"
        {{
            ""contents"": [
                {{
                    ""parts"": [
                        {{ ""text"": ""{promptText}"" }},
                        {{
                            ""inline_data"": {{
                                ""mime_type"": ""audio/wav"",
                                ""data"": ""{base64Audio}""
                            }}
                        }}
                    ]
                }}
            ],
            ""generationConfig"": {{
                ""temperature"": 0.0,
                ""maxOutputTokens"": 60
            }}
        }}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.certificateHandler = new BypassCertificate();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Gemini Hatası: " + request.error);

                if (scoreText)
                {
                    scoreText.text = "İnternet/API Hatası";
                    scoreText.color = Color.red;
                }
            }
            else
            {
                string responseText = request.downloadHandler.text;
                string spokenText = ExtractTextFromJson(responseText);
                Debug.Log("GEMINI: " + spokenText);

                if (string.IsNullOrWhiteSpace(spokenText) || spokenText == "???" || spokenText == "Hata")
                {
                    if (scoreText)
                    {
                        scoreText.text = "ANLAŞILMADI\nTekrar Dene";
                        scoreText.color = Color.red;
                    }
                    yield break;
                }

                if (spokenText.Contains("SILENCE") || spokenText.ToLower().Contains("sure"))
                {
                    if (scoreText)
                    {
                        scoreText.text = "ANLAŞILMADI\nTekrar Dene";
                        scoreText.color = Color.red;
                    }
                    yield break;
                }

                string hedef = puanlamaCumleIcinMi ? aktifCumle : aktifKelime;
                int score = CalculateScore(hedef, spokenText);

                // ✅ KRİTİK: GameManager'a haber ver (kart diğerine geçsin)
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnSpeechScoreReceived(score);
                }

                if (DatabaseManager.Instance != null)
                {
                    string tur = puanlamaCumleIcinMi ? "Cumle" : "Kelime";
                    DatabaseManager.Instance.SkoruKaydet(hedef, score, tur);
                    DatabaseManager.Instance.LogTut("telaffuz_denemesi", score.ToString());
                }

                if (scoreText)
                {
                    if (score >= 80)
                    {
                        scoreText.text = $"HARİKA!\nDoğruluk: %{score}\nAlgılanan: {spokenText}";
                        scoreText.color = Color.green;
                    }
                    else if (score >= 50)
                    {
                        scoreText.text = $"İYİ\nDoğruluk: %{score}\nAlgılanan: {spokenText}";
                        scoreText.color = new Color(1f, 0.64f, 0f);
                    }
                    else
                    {
                        scoreText.text = $"YANLIŞ\nDoğruluk: %{score}\nAlgılanan: {spokenText}";
                        scoreText.color = Color.red;
                    }
                }
            }
        }
        if (GameManager.Instance != null) GameManager.Instance.UpdateButtonImage(0);
    }

    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) { return true; }
    }

    byte[] ConvertToWav(AudioClip clip, int position)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            int sampleRate = clip.frequency;
            int channels = clip.channels;

            stream.Write(Encoding.UTF8.GetBytes("RIFF"), 0, 4);
            stream.Write(BitConverter.GetBytes(36 + position * channels * 2), 0, 4);
            stream.Write(Encoding.UTF8.GetBytes("WAVE"), 0, 4);
            stream.Write(Encoding.UTF8.GetBytes("fmt "), 0, 4);
            stream.Write(BitConverter.GetBytes(16), 0, 4);
            stream.Write(BitConverter.GetBytes((ushort)1), 0, 2);
            stream.Write(BitConverter.GetBytes((ushort)channels), 0, 2);
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);
            stream.Write(BitConverter.GetBytes(sampleRate * channels * 2), 0, 4);
            stream.Write(BitConverter.GetBytes((ushort)(channels * 2)), 0, 2);
            stream.Write(BitConverter.GetBytes((ushort)16), 0, 2);
            stream.Write(Encoding.UTF8.GetBytes("data"), 0, 4);
            stream.Write(BitConverter.GetBytes(position * channels * 2), 0, 4);

            float[] data = new float[position * channels];
            clip.GetData(data, 0);

            float maxSignal = 0f;
            foreach (var sample in data)
                if (Mathf.Abs(sample) > maxSignal) maxSignal = Mathf.Abs(sample);

            Debug.Log($"SES SİNYAL GÜCÜ: {maxSignal}");

            if (maxSignal < 0.0000001f)
                return null;

            float boost = 0.98f / maxSignal;

            foreach (var sample in data)
            {
                short intSample = (short)(Mathf.Clamp(sample * boost, -1f, 1f) * 32767f);
                stream.Write(BitConverter.GetBytes(intSample), 0, 2);
            }

            return stream.ToArray();
        }
    }

    string ExtractTextFromJson(string json)
    {
        try
        {
            string marker = "\"text\": \"";
            int start = json.IndexOf(marker);
            if (start == -1) return "???";
            start += marker.Length;
            int end = json.IndexOf("\"", start);
            if (end == -1) return "???";
            return json.Substring(start, end - start).Replace("\\n", "").Trim();
        }
        catch
        {
            return "Hata";
        }
    }

    string Temizle(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        char[] arr = text.ToCharArray();
        StringBuilder sb = new StringBuilder();
        foreach (char c in arr)
        {
            if (char.IsLetter(c) || char.IsWhiteSpace(c))
                sb.Append(c);
        }
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ").Trim().ToLower();
    }

    int CalculateScore(string target, string received)
    {
        string s = Temizle(target);
        string t = Temizle(received);

        if (string.IsNullOrEmpty(t)) return 0;
        if (s == t) return 100;

        int n = s.Length;
        int m = t.Length;

        if (n == 0) return m == 0 ? 100 : 0;
        if (m == 0) return 0;

        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(
                    Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        float maxLen = Mathf.Max(n, m);
        float similarity = 1.0f - ((float)d[n, m] / maxLen);
        return Mathf.Clamp((int)(similarity * 100), 0, 100);
    }
}