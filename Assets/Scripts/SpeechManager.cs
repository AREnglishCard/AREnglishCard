using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Text;
using System.IO;
using TMPro;
using UnityEngine.UI;

public class SpeechManager : MonoBehaviour
{
    public static SpeechManager Instance;

    [Header("Gemini Ayarlarý")]
    public string geminiApiKey = "BURAYA_API_KEY_YAZIN";

    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

    [Header("UI Baðlantýlarý")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI buttonLabel;
    public TextMeshProUGUI buttonLabelCumle;

    // --- HAFIZA ---
    private string aktifKelime = "";
    private string aktifCumle = "";

    // MOD SEÇÝMÝ
    private bool puanlamaCumleIcinMi = false;

    private AudioClip recordingClip;
    private string deviceName;
    private bool isRecording = false;

    void Awake() { if (Instance == null) Instance = this; }

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            deviceName = Microphone.devices[0];
            if (scoreText) scoreText.text = "Hazýr (Kart Göster)";
        }
        else
        {
            if (scoreText) scoreText.text = "Mikrofon Yok!";
        }

        if (buttonLabel) buttonLabel.text = "KELÝME PUANLA";
        if (buttonLabelCumle) buttonLabelCumle.text = "CÜMLE PUANLA";
    }

    public void HedefGuncelle(string kelime, string cumle)
    {
        // Gelen verileri temizle ve kaydet
        aktifKelime = kelime != null ? kelime.Trim() : "";
        aktifCumle = cumle != null ? cumle.Trim() : "";

        if (scoreText) scoreText.text = "Kart: " + aktifKelime;
        Debug.Log($"Kart: {aktifKelime} | Cümle: {aktifCumle}");
    }

    // --- BUTON FONKSÝYONLARI ---
    public void ButonKelimeOku() { if (!string.IsNullOrEmpty(aktifKelime) && TTSManager.Instance) TTSManager.Instance.SadeceKelimeyiOku(); }
    public void ButonCumleOku() { if (!string.IsNullOrEmpty(aktifCumle) && TTSManager.Instance) TTSManager.Instance.SadeceCumleyiOku(); }

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
            if (string.IsNullOrEmpty(deviceName)) return;
            isRecording = true;
            recordingClip = Microphone.Start(deviceName, false, 5, 44100);

            if (scoreText) scoreText.text = "Dinliyorum...";
            if (scoreText) scoreText.color = Color.yellow;
            if (labelToChange) labelToChange.text = "BÝTÝR";
        }
        else
        {
            isRecording = false;
            int position = Microphone.GetPosition(deviceName);
            Microphone.End(deviceName);

            if (scoreText) scoreText.text = "Gönderiliyor...";
            if (labelToChange) labelToChange.text = puanlamaCumleIcinMi ? "CÜMLE PUANLA" : "KELÝME PUANLA";

            byte[] wavData = ConvertToWav(recordingClip, position);
            StartCoroutine(SendToGemini(wavData));
        }
    }

    IEnumerator SendToGemini(byte[] audioData)
    {
        string url = $"{API_URL}?key={geminiApiKey}";
        string base64Audio = Convert.ToBase64String(audioData);

        string promptText = puanlamaCumleIcinMi
            ? "Listen to this audio. Transcribe exactly the sentence spoken. Do not auto-correct."
            : "Listen to this audio. Transcribe exactly the single word spoken. Do not auto-correct.";

        string jsonBody = $@"{{""contents"":[{{""parts"":[{{""text"":""{promptText}""}},{{""inline_data"":{{""mime_type"":""audio/wav"",""data"":""{base64Audio}""}}}}]}}]}}";

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
                string hataMesaji = $"HATA KODU: {request.responseCode}\n{request.error}";
                if (scoreText) scoreText.text = hataMesaji;
                scoreText.color = Color.red;
                Debug.LogError("GEMINI HATASI: " + request.downloadHandler.text);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                string spokenText = ExtractTextFromJson(responseText);

                string hedef = puanlamaCumleIcinMi ? aktifCumle : aktifKelime;
                int score = CalculateScore(hedef, spokenText);

                // -----------------------------------------------------------
                //  VERÝTABANI VE ANALÝTÝK KAYDI BURADA YAPILIYOR
                // -----------------------------------------------------------
                if (DatabaseManager.Instance != null)
                {
                    string tur = puanlamaCumleIcinMi ? "Cumle" : "Kelime";

                    // 1. Veritabanýna Skoru Kaydet (Firestore)
                    DatabaseManager.Instance.SkoruKaydet(hedef, score, tur);

                    // 2. Analitik Log Tut (Firebase Analytics)
                    DatabaseManager.Instance.LogTut("telaffuz_denemesi", score.ToString());
                }
                // -----------------------------------------------------------

                if (scoreText)
                {
                    if (score >= 75) { scoreText.text = $"HARÝKA! ({score})\n{spokenText}"; scoreText.color = Color.green; }
                    else if (score >= 50) { scoreText.text = $"ÝYÝ ({score})\n{spokenText}"; scoreText.color = new Color(1f, 0.64f, 0f); }
                    else { scoreText.text = $"OLMADI ({score})\nAlgýlanan: {spokenText}"; scoreText.color = Color.red; }
                }
            }
        }
    }

    public class BypassCertificate : CertificateHandler { protected override bool ValidateCertificate(byte[] certificateData) { return true; } }

    byte[] ConvertToWav(AudioClip clip, int position)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            stream.Write(Encoding.UTF8.GetBytes("RIFF"), 0, 4); stream.Write(BitConverter.GetBytes(36 + position * 2), 0, 4); stream.Write(Encoding.UTF8.GetBytes("WAVE"), 0, 4); stream.Write(Encoding.UTF8.GetBytes("fmt "), 0, 4); stream.Write(BitConverter.GetBytes(16), 0, 4); stream.Write(BitConverter.GetBytes((ushort)1), 0, 2); stream.Write(BitConverter.GetBytes((ushort)1), 0, 2); stream.Write(BitConverter.GetBytes(44100), 0, 4); stream.Write(BitConverter.GetBytes(44100 * 2), 0, 4); stream.Write(BitConverter.GetBytes((ushort)2), 0, 2); stream.Write(BitConverter.GetBytes((ushort)16), 0, 2); stream.Write(Encoding.UTF8.GetBytes("data"), 0, 4); stream.Write(BitConverter.GetBytes(position * 2), 0, 4);
            float[] data = new float[position]; clip.GetData(data, 0);
            foreach (var sample in data) { short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f); stream.Write(BitConverter.GetBytes(intSample), 0, 2); }
            return stream.ToArray();
        }
    }

    string ExtractTextFromJson(string json)
    {
        try
        {
            string marker = "\"text\": \""; int start = json.IndexOf(marker);
            if (start == -1) return "???";
            start += marker.Length; int end = json.IndexOf("\"", start);
            return json.Substring(start, end - start).Replace("\\n", "").Trim();
        }
        catch { return "JSON Hatasý"; }
    }

    int CalculateScore(string target, string received)
    {
        string s = target.ToLower().Trim().Replace(".", "").Replace("!", "").Replace("?", "").Replace(",", "");
        string t = received.ToLower().Trim().Replace(".", "").Replace("!", "").Replace("?", "").Replace(",", "");
        if (s == t) return 100;
        int n = s.Length; int m = t.Length; int[,] d = new int[n + 1, m + 1];
        if (n == 0) return m == 0 ? 100 : 0; if (m == 0) return 0;
        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }
        for (int i = 1; i <= n; i++) { for (int j = 1; j <= m; j++) { int cost = (t[j - 1] == s[i - 1]) ? 0 : 1; d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost); } }
        float maxLen = Mathf.Max(n, m); float similarity = 1.0f - ((float)d[n, m] / maxLen);
        return Mathf.Clamp((int)(similarity * 100), 0, 100);
    }
}