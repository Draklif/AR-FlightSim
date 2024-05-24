using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] GameObject DefaultMenu;
    [SerializeField] GameObject TutorialMenu;
    [SerializeField] GameObject[] TutorialPages;

    int actualPage = 0;
    public void OpenTutorial()
    {
        DefaultMenu.SetActive(false);
        TutorialMenu.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void Load(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void NextTutorialPage()
    {
        actualPage++;
        if (actualPage < TutorialPages.Length)
        {
            TutorialPages[actualPage].SetActive(true);
            if (actualPage > 0)
            {
                TutorialPages[actualPage - 1].SetActive(false);
            }
        }
        else
        {
            actualPage--;
            CloseTutorial();
        }
    }

    public void CloseTutorial()
    {
        TutorialPages[actualPage].SetActive(false);
        actualPage = 0;
        TutorialPages[actualPage].SetActive(true);
        TutorialMenu.SetActive(false);
        DefaultMenu.SetActive(true);
    }
}
