using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    // Arrastrar aqui el Panel de opciones desde el Inspector.
    [SerializeField] private GameObject panelOpciones;

    private void Start()
    {
        // El menu comienza con las opciones cerradas.
        Volver();
    }

    public void Jugar()
    {
        // La escena Juego debe estar incluida en la lista de escenas del build.
        SceneManager.LoadScene("Juego");
    }

    public void Opciones()
    {
        // Alternar entre mostrar y ocultar el panel.
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(!panelOpciones.activeSelf);
        }
    }

    public void Volver()
    {
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(false);
        }
    }

    public void Salir()
    {
#if UNITY_EDITOR
        // Application.Quit no cierra el Editor; mostramos un mensaje de prueba.
        Debug.Log("Saliendo de CUBOTRIS");
#endif
        Application.Quit();
    }
}
