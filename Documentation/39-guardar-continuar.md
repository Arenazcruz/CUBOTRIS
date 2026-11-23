PAUSA > GUARDAR Y SALIR abre una confirmación. Existe una ranura local; se avisa cuando reemplaza otra partida. Si falla la escritura, la partida continúa pausada y el archivo anterior se conserva. CONTINUAR en el menú muestra el resumen y carga en pausa; REANUDAR devuelve los controles tras soltar las teclas.

Se guardan los cuatro tableros y colores, cara actual, pieza y orientación, gravedad parcial, reserva, bolsa de siete piezas y sus generadores aleatorios, próxima pieza/color, tiempos activos, basura pendiente, dificultad/práctica, puntuación, rachas y objetivo de vuelta. La animación de cambio de cara se presenta ya terminada al continuar; el estado del tablero se conserva.

El archivo versionado `continue.json` se almacena en `Application.persistentDataPath`, mediante archivo temporal y reemplazo con copia `.bak`. Un archivo inválido se rechaza sin borrarlo ni sobreescribirlo. No se recuperan automáticamente partidas terminadas desde la copia. Reiniciar, abandonar desde pausa o perder invalida el checkpoint correspondiente. Empezar otra partida no elimina un guardado anterior: solo guardarla reemplaza la ranura.

La identidad de partida se conserva al continuar, evitando registrar el mismo resultado varias veces. Los generadores usan estado explícito para conservar también los sorteos futuros; siguen usando bolsa de siete piezas sin repetir colores consecutivos. No se introduce guardado automático: para conservar avances nuevos hay que volver a GUARDAR Y SALIR.

Comprobación: jugar varias caras, reservar, pausar durante una transición y guardar; cerrar/abrir el juego, CONTINUAR y comparar mini tableros, reserva, score y tiempo. Reanudar, guardar de nuevo, comprobar que mantiene racha y basura. Perder y comprobar que ese checkpoint deja de estar disponible.
