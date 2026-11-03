using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SonidoCaida : MonoBehaviour
{
    [SerializeField] private Board tablero;
    [SerializeField] private AudioClip sonidoCaida;
    [SerializeField, Range(0f, 1f)] private float volumen = 0.8f;

    
    [SerializeField, Range(0f, 0.3f)] private float variacionTono = 0.1f;

    private AudioSource fuente;

    private void Awake()
    {
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;

        if (tablero == null)
            tablero = GetComponent<Board>();
    }

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
      
        fuente.PlayOneShot(sonidoCaida, volumen);
    }
}
