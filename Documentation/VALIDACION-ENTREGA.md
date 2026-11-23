Entrega de los puntos 14, 16, 18, 32, 33, 34, 36, 37, 39 y 42 — 23/11/2026.

Verificado con Unity 6000.6.0f1, la versión del proyecto. Se conservan paquetes, versión, imágenes y sus GUID originales. Las escenas de la compilación son MainMenu y Game.

Resultados automatizados de la versión conjunta:

- 159 pruebas EditMode aprobadas: reglas, reservas, controles, dificultad, puntuación, guardados, corrupción, errores de escritura y estadísticas.
- 50 pruebas PlayMode aprobadas: navegación con teclado/ratón, pausa/opciones, Game Over, récords, práctica, reserva, combos, guardar/continuar y estadísticas.
- 1 prueba visual aprobada, con 28 capturas a 1920×1080 y 1280×720, inspeccionadas para comprobar distribución y legibilidad de las pantallas nuevas. Las capturas se generan en Logs/VisualEvidence y no se versionan.
- Compilación Windows x64 completada correctamente: Builds/Delivery/CUBOTRIS.exe. El resultado compilado y los registros de ejecución no se versionan.

Los cambios físicos de resolución/pantalla completa deben comprobarse en el ejecutable y en los monitores del grupo. Las pruebas de Editor cubren confirmación, reversión, tiempo límite y persistencia; Unity controla por separado el tamaño de su propia vista Game.

Recorrido de revisión para el grupo:

| Punto | Acción | Resultado esperado |
|---|---|---|
| 14 | Esperar en caras 2–4 hasta los últimos cinco segundos | Aviso ámbar; si se acumula basura, aviso coral AL FIJAR |
| 16 | Opciones > Controles, cambiar una tecla y probar una duplicada | Se guarda la válida, se rechaza el conflicto; ESC cancela y Restaurar recupera los valores iniciales |
| 18 | Opciones > Pantalla, aplicar y dejar pasar diez segundos | Vuelve la configuración anterior; Confirmar conserva el cambio |
| 32 | C, otra vez C, colocar una pieza y volver a pulsar C | Una reserva por colocación; conserva forma/color, no cambia de cara |
| 33 | MODO, elegir Relajado/Normal/Desafío; consultar RÉCORDS | Cada modo tiene su ritmo y clasificación; borrar un modo conserva los demás |
| 34 | MODO > PRÁCTICA POR CARA, elegir cara y dificultad | Cara fija, reinicio directo, resultados fuera de los récords competitivos |
| 36 | Limpiar en las cuatro caras de una misma vuelta | Bonus de 400; el objetivo se reinicia para la siguiente vuelta |
| 37 | Limpiar con piezas consecutivas y luego colocar sin limpiar | Bonus 0/50/100/150…, cadena reiniciada al no limpiar |
| 39 | Pausa > Guardar y salir; cerrar/abrir y Continuar | Mismo estado, reserva y futuras piezas; se retoma en pausa |
| 42 | Terminar sin guardar nombre; abrir Estadísticas; continuar otro guardado | El resultado se cuenta aunque no tenga nombre; continuar no duplica partidas; práctica tiene grupo separado |

Hay una rama y un PR por punto, encadenados en el orden de esta tabla para que cada revisión muestre una función. La rama final codex/42-estadisticas contiene el conjunto para probarlo. No se ha realizado ningún merge. Para integrar posteriormente en main, revisar en ese orden y actualizar la base de cada PR cuando su dependencia ya esté integrada.

Los ajustes gráficos y de conexión que Unity regeneró durante la compilación fueron restaurados a su estado original. Los datos personales usados en las pruebas se aislaron en carpetas temporales; se restauraron las preferencias previas.
