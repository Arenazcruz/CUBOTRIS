using UnityEngine;

// Reproduce un golpe cada vez que una pieza se fija en el tablero.
// Va en el mismo objeto que el Board (Tablero).
[RequireComponent(typeof(AudioSource))]
public class SonidoCaida : MonoBehaviour
{
    [SerializeField] private Board tablero;
    [SerializeField] private AudioClip sonidoCaida;
    [SerializeField, Range(0f, 1f)] private float volumen = 0.8f;

    // Variar un poco el tono evita que el sonido se escuche "robotico" al repetirse.
    [SerializeField, Range(0f, 0.3f)] private float variacionTono = 0.1f;

    private AudioSource fuente;

    private void Awake()
    {
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;

        if (tablero == null)
            tablero = GetComponent<Board>();
    }

    // Suscribirse en OnEnable y desuscribirse en OnDisable evita
    // llamadas a un objeto destruido si se recarga la escena.
    private void OnEnable()
    {
        if (tablero != null)
            tablero.PiezaFijada += ReproducirCaida;
    }

    private void OnDisable()
    {
        if (tablero != null)
            tablero.PiezaFijada -= ReproducirCaida;
    }

    private void ReproducirCaida()
    {
        if (sonidoCaida == null)
            return;

        fuente.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        // PlayOneShot permite que dos golpes seguidos se superpongan sin cortarse.
        fuente.PlayOneShot(sonidoCaida, volumen);
    }
}
