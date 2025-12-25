using UnityEngine;
using UnityEngine.SceneManagement; 

public class MenuDonus : MonoBehaviour
{
    public string menuSahneAdi = "MainMenu";

    public void MenuyeDon()
    {
        if (SpeechManager.Instance != null) Destroy(SpeechManager.Instance.gameObject);

        SceneManager.LoadScene(menuSahneAdi);
    }
}