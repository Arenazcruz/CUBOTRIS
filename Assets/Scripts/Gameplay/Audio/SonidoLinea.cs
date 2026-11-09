using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cubotris.Gameplay
{
    // Reproduce un sonido cuando el jugador completa una o más líneas.
    // Mientras más líneas borre de una vez, más agudo suena.
    // No modifica la lógica del juego: solo observa el contador de líneas.
    [RequireComponent(typeof(AudioSource))]
    public sealed class SonidoLinea : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private AudioClip sonidoLinea;
        [SerializeField, Range(0f, 1f)] private float volumen = 0.8f;
        [SerializeField, Range(0f, 0.3f)] private float subidaTonoPorLinea = 0.08f;

        private AudioSource fuente;
        private TetrisGame juegoAnterior;
        private int lineasAnteriores;

        private void Awake()
        {
            fuente = GetComponent<AudioSource>();
            fuente.playOnAwake = false;
            if (controller == null)
                controller = FindFirstObjectByType<GameController>();
            if (sonidoLinea == null)
                sonidoLinea = Resources.Load<AudioClip>("Audio/linea");
        }

        // Se activa solo: cuando se carga una escena que tiene el juego,
        // crea un objeto con este componente. No hace falta editar la escena.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Registrar()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
            AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (FindFirstObjectByType<SonidoLinea>() != null) return;
            if (FindFirstObjectByType<GameController>() == null) return;
            new GameObject("SonidoLinea").AddComponent<SonidoLinea>();
        }

        private void LateUpdate()
        {
            if (controller == null || controller.Model == null) return;
            var juego = controller.Model;

            // Partida nueva (reinicio): empezamos a contar desde cero.
            if (juego != juegoAnterior)
            {
                juegoAnterior = juego;
                lineasAnteriores = juego.LinesCleared;
                return;
            }

            int nuevas = juego.LinesCleared - lineasAnteriores;
            if (nuevas > 0)
                Reproducir(nuevas);

            lineasAnteriores = juego.LinesCleared;
        }

        private void Reproducir(int lineas)
        {
            if (sonidoLinea == null) return;
            fuente.pitch = 1f + (Mathf.Clamp(lineas, 1, 4) - 1) * subidaTonoPorLinea;
            fuente.PlayOneShot(sonidoLinea, volumen);
        }
    }
}
