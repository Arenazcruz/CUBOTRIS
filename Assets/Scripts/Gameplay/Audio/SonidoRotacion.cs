using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cubotris.Gameplay
{
    // Reproduce un sonido corto cada vez que la pieza activa gira.
    // No modifica la lógica del juego: solo observa el GameController.
    [RequireComponent(typeof(AudioSource))]
    public sealed class SonidoRotacion : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private AudioClip sonidoRotar;
        [SerializeField, Range(0f, 1f)] private float volumen = 0.6f;
        [SerializeField, Range(0f, 0.2f)] private float variacionTono = 0.05f;

        private AudioSource fuente;
        private TetrisGame juegoAnterior;
        private int rotacionAnterior;
        private int piezasAnteriores;
        private bool teniaPieza;

        private void Awake()
        {
            fuente = GetComponent<AudioSource>();
            fuente.playOnAwake = false;
            if (controller == null)
                controller = FindFirstObjectByType<GameController>();
            if (sonidoRotar == null)
                sonidoRotar = Resources.Load<AudioClip>("Audio/rotar");
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
            if (FindFirstObjectByType<SonidoRotacion>() != null) return;
            if (FindFirstObjectByType<GameController>() == null) return;
            new GameObject("SonidoRotacion").AddComponent<SonidoRotacion>();
        }

        private void LateUpdate()
        {
            if (controller == null || controller.Model == null) return;
            var juego = controller.Model;

            // Si es otra partida (reinicio) solo guardamos el estado inicial.
            if (juego != juegoAnterior)
            {
                juegoAnterior = juego;
                Guardar(juego);
                return;
            }

            // Mismo número de piezas fijadas = sigue la misma pieza.
            // Si cambió su rotación, el jugador la giró.
            bool mismaPieza = juego.HasActivePiece && teniaPieza
                && juego.PiecesLocked == piezasAnteriores;
            if (mismaPieza && juego.Active.Rotation != rotacionAnterior)
                Reproducir();

            Guardar(juego);
        }

        private void Guardar(TetrisGame juego)
        {
            teniaPieza = juego.HasActivePiece;
            piezasAnteriores = juego.PiecesLocked;
            rotacionAnterior = juego.HasActivePiece ? juego.Active.Rotation : 0;
        }

        private void Reproducir()
        {
            if (sonidoRotar == null) return;
            fuente.pitch = 1f + Random.Range(-variacionTono, variacionTono);
            fuente.PlayOneShot(sonidoRotar, volumen);
        }
    }
}
