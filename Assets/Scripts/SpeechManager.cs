using UnityEngine.Android;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SpeechManager : MonoBehaviour
{
    public static SpeechManager Instance;

    [Header("Gemini Ayarları")]
    private static string geminiApiKey = "AIzaSyBOb9ZiObkh5u-qAUogWPiwDHmaDeIoVJU";
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

            if (scoreText) scoreText.text = "Hazır (Kart Göster)";
        }
        else
        {
            if (scoreText) scoreText.text = "Mikrofon Bulunamadı!";
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
        if (scoreText) scoreText.text = "Kart: " + aktifKelime;
    }

    public void ButonKelimeOku()
    {
        if (!string.IsNullOrEmpty(aktifKelime) && TTSManager.Instance) TTSManager.Instance.SadeceKelimeyiOku();
    }

    public void ButonCumleOku()
    {
        if (!string.IsNullOrEmpty(aktifCumle) && TTSManager.Instance) TTSManager.Instance.SadeceCumleyiOku();
    }

    public void ButonMikrofonKelime()
    {
        if (string.IsNullOrEmpty(aktifKelime)) { if (scoreText) scoreText.text = "Önce kart göster!"; return; }
        puanlamaCumleIcinMi = false;
        ButonMikrofonGenel(buttonLabel);
    }

    public void ButonMikrofonCumle()
    {
        if (string.IsNullOrEmpty(aktifCumle)) { if (scoreText) scoreText.text = "Bu kartta cümle yok!"; return; }
        puanlamaCumleIcinMi = true;
        ButonMikrofonGenel(buttonLabelCumle);
    }

    void ButonMikrofonGenel(TextMeshProUGUI labelToChange)
    {
        if (!isRecording)
        {
            if (Microphone.devices.Length == 0)
            {
                scoreText.text = "Mikrofon Yok!";
                return;
            }

            isRecording = true;
            Microphone.End(currentDeviceName); // Temizle

            int kayitSuresi = puanlamaCumleIcinMi ? 8 : 4;

            // DÜZELTME: Frekansı elle vermiyoruz, cihaz ne istiyorsa onu veriyoruz (maxFreq)
            Microphone.GetDeviceCaps(currentDeviceName, out int minFreq, out int maxFreq);
            int finalFreq = (maxFreq > 0) ? maxFreq : 44100; // Cihaz ne destekliyorsa o, yoksa 44100

            try
            {
                recordingClip = Microphone.Start(currentDeviceName, false, kayitSuresi, finalFreq);

                if (scoreText) scoreText.text = "DİNLİYORUM...";
                if (scoreText) scoreText.color = Color.yellow;
                if (labelToChange) labelToChange.text = "BİTİR";
            }
            catch (Exception e)
            {
                Debug.LogError("Mikrofon Hatası: " + e.Message);
                scoreText.text = "Mic Başlatılamadı!";
                isRecording = false;
            }
        }
        else
        {
            isRecording = false;

            // Eğer Unity kaydı kendiliğinden durdurduysa (süre bittiyse) pozisyon sıfırlanmış olabilir
            int position = Microphone.GetPosition(currentDeviceName);
            Microphone.End(currentDeviceName);

            if (scoreText) scoreText.text = "Analiz...";
            if (labelToChange) labelToChange.text = puanlamaCumleIcinMi ? "CÜMLE PUANLA" : "KELİME PUANLA";

            // Eğer süre dolup durduysa position 0 dönebilir, bu durumda tüm klibi al
            if (position <= 0) position = recordingClip.samples;

            byte[] wavData = ConvertToWav(recordingClip, position);

            // Wav datası null ise ses 0 demektir (ConvertToWav içinde kontrol var)
            if (wavData == null)
            {
                scoreText.text = "MİKROFON SES ALMIYOR\n(Ses Seviyesi: 0)";
                scoreText.color = Color.red;
                return;
            }

            StartCoroutine(SendToGemini(wavData));
        }
    }

    IEnumerator SendToGemini(byte[] audioData)
    {
        string url = $"{API_URL}?key={geminiApiKey}";
        string base64Audio = Convert.ToBase64String(audioData);

        string promptText = "Transcribe the audio exactly. If it is silence or unintelligible noise, return 'SILENCE'. Do not provide explanations.";

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
                if (scoreText) scoreText.text = "İnternet/API Hatası";
                scoreText.color = Color.red;
            }
            else
            {
                string responseText = request.downloadHandler.text;
                string spokenText = ExtractTextFromJson(responseText);
                Debug.Log("GEMINI: " + spokenText);

                if (spokenText.Contains("SILENCE") || spokenText.Contains("sure"))
                {
                    if (scoreText)
                    {
                        scoreText.text = "ANLAŞILMADI\nTekrar Dene";
                        scoreText.color = Color.red;
                    }
                }
                else
                {
                    string hedef = puanlamaCumleIcinMi ? aktifCumle : aktifKelime;
                    int score = CalculateScore(hedef, spokenText);

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
        }
    }

    public class BypassCertificate : CertificateHandler { protected override bool ValidateCertificate(byte[] certificateData) { return true; } }

    // DÜZELTME: Artık frekansı klipten okuyoruz, sincap sesi olmasın diye
    byte[] ConvertToWav(AudioClip clip, int position)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            // GERÇEK FREKANSI AL
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

            // SES VAR MI KONTROLÜ
            float maxSignal = 0f;
            foreach (var sample in data)
            {
                if (Mathf.Abs(sample) > maxSignal) maxSignal = Mathf.Abs(sample);
            }

            Debug.Log($"SES SİNYAL GÜCÜ: {maxSignal}");

            // Eğer ses 0 ise boşuna gönderme, hata ver (Debug için)
            if (maxSignal < 0.0000001f)
            {
                return null;
            }

            // AUTO-BOOST
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
            return json.Substring(start, end - start).Replace("\\n", "").Trim();
        }
        catch { return "Hata"; }
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

        int n = s.Length; int m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        if (n == 0) return m == 0 ? 100 : 0;
        if (m == 0) return 0;
        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        float maxLen = Mathf.Max(n, m);
        float similarity = 1.0f - ((float)d[n, m] / maxLen);
        return Mathf.Clamp((int)(similarity * 100), 0, 100);
    }
}