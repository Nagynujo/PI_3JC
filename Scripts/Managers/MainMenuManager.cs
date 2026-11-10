using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Tooltip("Nome exato da cena do jogo (a que tem o Caldeirão, OrderManager etc), igual aparece em File > Build Settings")]
    public string nomeCenaDoJogo = "SampleScene";

    public void Jogar()
    {
        SceneManager.LoadScene(nomeCenaDoJogo);
    }

    public void Sair()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
    }
}
