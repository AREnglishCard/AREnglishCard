using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void FruitsSahnesiniAc()
    {
        SceneManager.LoadScene("FruitsScene");
    }

    public void AnimalsSahnesiniAc()
    {
        SceneManager.LoadScene("AnimalsScene");
    }

    public void VehiclesSahnesiniAc()
    {
        SceneManager.LoadScene("VehiclesScene");
    }

    public void LobiyeGit()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    public void AcProfil()
    {
        SceneManager.LoadScene("ScoreboardScene");
    }
}