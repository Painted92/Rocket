using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void LoadGameScene(int numberScene)
    {
        SceneManager.LoadSceneAsync(numberScene);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
