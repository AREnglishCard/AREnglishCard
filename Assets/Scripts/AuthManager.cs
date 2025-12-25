using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    [Header("GÝRÝÞ EKRANI (Ana Sahne)")]
    public InputField girisEmailInput;
    public InputField girisPasswordInput;
    public TextMeshProUGUI bildirimText;

    [Header("KAYIT PANELÝ (Pop-up)")]
    public GameObject kayitPaneli;
    public InputField kayitEmailInput;
    public InputField kayitPasswordInput;

    private FirebaseAuth auth;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        if (kayitPaneli != null)
            kayitPaneli.SetActive(false);
    }

    public void KayitPaneliniAc()
    {
        kayitPaneli.SetActive(true);
        bildirimText.text = ""; 
    }

    public void KayitPaneliniKapat()
    {
        kayitPaneli.SetActive(false);
    }

    public void KayitOl()
    {
        string email = kayitEmailInput.text;
        string password = kayitPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            bildirimText.text = "Kayýt için boþ alan býrakmayýn!";
            return;
        }

        StartCoroutine(KayitIslemi(email, password));
    }

    private IEnumerator KayitIslemi(string email, string password)
    {
        bildirimText.text = "Kayýt yapýlýyor...";
        var islem = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => islem.IsCompleted);

        if (islem.Exception != null)
        {
            bildirimText.text = "Kayýt Hatasý: " + islem.Exception.GetBaseException().Message;
        }
        else
        {
            bildirimText.text = "Kayýt Baþarýlý! Þimdi giriþ yapabilirsin.";
            KayitPaneliniKapat(); 
            girisEmailInput.text = email;
        }
    }

    public void GirisYap()
    {
        string email = girisEmailInput.text;
        string password = girisPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            bildirimText.text = "Giriþ bilgileri eksik!";
            return;
        }

        StartCoroutine(GirisIslemi(email, password));
    }

    private IEnumerator GirisIslemi(string email, string password)
    {
        bildirimText.text = "Giriþ yapýlýyor...";
        var islem = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => islem.IsCompleted);

        if (islem.Exception != null)
        {
            bildirimText.text = "Giriþ Baþarýsýz: " + islem.Exception.GetBaseException().Message;
        }
        else
        {
            bildirimText.text = "Giriþ Baþarýlý! Yönlendiriliyor...";
            yield return new WaitForSeconds(1.0f);
            SceneManager.LoadScene("MainMenu");
        }
    }
}