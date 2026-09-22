using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("TestScene");
    }
}