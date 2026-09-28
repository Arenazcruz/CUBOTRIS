# CUBOTRIS: Tetris básico en una sola escena

## Archivos y responsabilidades

- `Assets/Scripts/Tetris/Board.cs`: cuadrícula de 10 columnas y 20 filas, colisiones, fijación de bloques, limpieza de líneas y Game Over.
- `Assets/Scripts/Tetris/Piece.cs`: cuatro bloques de la pieza activa, teclado, movimiento, rotación, caída automática y Hard Drop.
- `Assets/Scripts/Tetris/TetrominoData.cs`: celdas iniciales, colores y rotación de I, J, L, O, S, T y Z. No se añade como componente.
- `Assets/Scripts/Tetris/Spawner.cs`: selecciona una de las siete piezas al azar y comprueba que pueda aparecer.

`MenuPrincipal.cs` se conserva, pero no se utiliza. El Tetris no carga otras escenas.

## Assets revisados

`Assets/Art/Tetrominos` contiene 52 PNG de 120 × 120 píxeles: 38 imágenes de piezas completas y 14 imágenes de bloques individuales (`Tetromino_block1_1` a `Tetromino_block1_7` y `Tetromino_block2_1` a `Tetromino_block2_7`).

Los bloques individuales ya tienen un recorte de 32 × 32 en sus metadatos de importación, con Sprite Mode Multiple y 100 Pixels Per Unit. Las imágenes completas pueden reservarse para una futura UI. Para el juego se utiliza un bloque individual claro, coloreado por código según el tetrominó.

## Configuración en Unity

Realiza el montaje fuera de Play Mode para conservar los cambios.

### 1. Utilizar una sola escena

1. Abre `Assets/Scenes/SampleScene.unity` y trabaja directamente en ella. No necesitas crear `Juego` ni una escena de menú.
2. Si habías montado un menú, desactiva su Canvas y su controlador. No borres `MenuPrincipal.cs`.
3. Mantén Main Camera. La luz Global Light 2D puede permanecer.
4. Para generar una aplicación, deja solamente esta escena habilitada en la Scene List de File → Build Profiles.

### 2. Preparar el sprite del bloque

1. Selecciona `Assets/Art/Tetrominos/Tetromino_block1_1.png`.
2. Mantén Texture Type **Sprite (2D and UI)** y Sprite Mode **Multiple**. No utilices toda la imagen de 120 × 120 como un bloque, porque contiene margen transparente.
3. Cambia **Pixels Per Unit** de 100 a **32**. Así, el recorte de 32 × 32 ocupará exactamente una unidad de Unity.
4. Configura Filter Mode **Point (no filter)** y Compression **None**. Pulsa **Apply**.
5. En Sprite Editor, comprueba el recorte existente: X = 44, Y = 44, Width = 32, Height = 32 y Pivot **Center**. Conserva el recorte; no es necesario volver a dividir la imagen. Pulsa Apply si cambiaste algo.
6. Expande la flecha del PNG en Project. Utiliza el sub-sprite `Tetromino_block1_1_0`.

Estos cambios de importación los realiza Unity desde su interfaz. Los scripts no modifican las texturas ni sus metadatos.

### 3. Crear el prefab Bloque

1. Crea un GameObject vacío llamado **Bloque** y añade **Sprite Renderer**.
2. Arrastra `Tetromino_block1_1_0` al campo **Sprite**.
3. Configura Transform: Position `(0, 0, 0)`, Rotation `(0, 0, 0)`, Scale `(1, 1, 1)`.
4. Configura Sprite Renderer: Color blanco, Draw Mode **Simple**, Sorting Layer **Default**, Order in Layer **1**.
5. Para que se vea sin depender de luces, asigna el material de URP **Sprite-Unlit-Default** mediante el selector del campo Material.
6. No añadas Rigidbody2D, Collider2D ni scripts al bloque. La cuadrícula controla todas las colisiones.
7. Crea `Assets/Prefabs` desde Project. Arrastra Bloque desde Hierarchy hasta esa carpeta para crear **Bloque.prefab**.
8. Elimina únicamente la instancia de montaje de Bloque de Hierarchy. Conserva el prefab en Project.

### 4. Crear el tablero

1. Crea un GameObject vacío llamado **Tablero**.
2. Deja Position `(0, 0, 0)`, Rotation `(0, 0, 0)` y Scale `(1, 1, 1)`.
3. Añade el componente **Board**. Unity añadirá **Spawner** automáticamente por la dependencia declarada en el script.
4. Arrastra **Bloque.prefab** desde Project al campo **Block Prefab** de Board. El campo toma el componente SpriteRenderer del prefab.
5. Mantén habilitados Tablero, Board y Spawner. No añadas Piece a mano: Spawner lo crea durante la partida.

Esta es la única referencia que necesitas conectar para el código del juego. No hace falta Tilemap, Canvas, EventSystem ni Input Actions adicional. El teclado se lee mediante el Input System ya instalado.

### 5. Ajustar la cámara y mostrar el área del tablero

1. Selecciona Main Camera.
2. Configura Projection **Orthographic**, Size **11**, Position `(5, 10, -10)` y Rotation `(0, 0, 0)`.
3. En Game, utiliza una proporción **16:9** para ver el tablero completo con margen.
4. Para distinguir el área jugable, crea **GameObject → 2D Object → Sprites → Square**, llámalo **FondoTablero** y déjalo en la raíz de la escena.
5. Configura FondoTablero: Position `(5, 10, 0)`, Scale `(10, 20, 1)`, color gris oscuro, material **Sprite-Unlit-Default** y Order in Layer **-10**. El Square estándar mide una unidad antes de escalarlo.
6. Usa un color de fondo de cámara distinto al gris del tablero para ver sus límites.
7. Guarda la escena.

El tablero ocupa X de 0 a 10 e Y de 0 a 20. Cada bloque tiene su centro en una celda, por ejemplo `(0.5, 0.5)` en la esquina inferior izquierda. Board dibuja una cuadrícula auxiliar cuando están activados los Gizmos; no es un gráfico permanente de la aplicación.

Jerarquía inicial sugerida:

```text
SampleScene
├── Main Camera
├── Global Light 2D
├── FondoTablero                  [SpriteRenderer]
└── Tablero                       [Board, Spawner]
```

Durante Play aparecerán la pieza activa y los bloques fijados como hijos de Tablero.

## Controles y comportamiento

| Acción | Teclas |
|---|---|
| Izquierda | Flecha izquierda / A |
| Derecha | Flecha derecha / D |
| Caída rápida mientras se mantiene pulsada | Flecha abajo / S |
| Rotación de 90° en sentido horario | Flecha arriba / W |
| Caída instantánea y fijación | Espacio |

- Una pulsación horizontal mueve una celda. Mantener la tecla repite el movimiento tras una breve pausa.
- Caída normal: una celda cada 0.8 segundos. Caída rápida: una celda cada 0.05 segundos.
- Cada rotación prueba la posición actual y pequeños desplazamientos horizontales de hasta dos celdas. Si no cabe, se rechaza. No es una implementación del sistema avanzado SRS.
- La pieza O conserva su forma al rotar. La I usa un centro de rotación entre celdas.
- El tablero tiene 20 filas totales, todas visibles. No hay filas ocultas; las piezas aparecen dentro de las dos filas superiores.
- Cuando falla un intento de bajar por gravedad o caída rápida, la pieza se fija. Hard Drop la fija inmediatamente. No se añade un temporizador especial de bloqueo.
- La selección es aleatoria independiente; puede repetirse una pieza.
- Si la siguiente pieza no cabe al aparecer, se registra exactamente `GAME OVER` y se detienen el movimiento y la generación de piezas.
- Para reiniciar esta primera versión, sal de Play Mode y vuelve a entrar.

## Pruebas manuales

1. **Inicio:** pulsa Play y haz clic en Game para darle foco. Debe aparecer una pieza de cuatro bloques cerca de la parte superior.
2. **Caída automática:** no pulses teclas durante varios segundos; la pieza debe bajar regularmente.
3. **Movimiento:** prueba flechas y A/D, tanto pulsaciones breves como mantenidas. Comprueba que la pieza se detenga en ambos lados del fondo del tablero.
4. **Rotación:** prueba flecha arriba y W en el centro y junto a las paredes. Cuatro rotaciones en una zona libre devuelven la orientación original. O debe permanecer igual.
5. **Caída rápida:** mantén flecha abajo o S y luego suelta; la velocidad debe aumentar y después volver a la normal.
6. **Hard Drop:** pulsa Espacio una vez. La pieza debe caer hasta el suelo o una pila y aparecer otra. La nueva no debe caer instantáneamente por esa misma pulsación.
7. **Colisiones:** apila varias piezas; ninguna debe atravesar el suelo, los bordes ni los bloques fijados. Prueba una rotación que no tenga espacio: debe conservar su posición y forma.
8. **Línea completa:** llena una fila de diez celdas. Debe desaparecer al fijar la pieza que la completa; todos los bloques superiores deben bajar una fila.
9. **Varias líneas:** deja un hueco vertical y complétalo con una I vertical para cerrar varias filas a la vez. Todas deben desaparecer y los bloques superiores deben bajar el número correspondiente de filas.
10. **Game Over:** apila piezas en las columnas centrales sin limpiar líneas hasta impedir que aparezca la siguiente. Console debe mostrar `GAME OVER` una sola vez. Las teclas ya no deben mover ni generar piezas.

Si no aparecen bloques, revisa Console y la referencia Block Prefab. Si aparecen muy pequeños, comprueba Pixels Per Unit = 32 y la escala del prefab = 1. Si el teclado no responde, da foco a Game y confirma que Active Input Handling utiliza Input System Package (New), como en la configuración original del proyecto.

## Comprobación de código

Se compiló el código con las bibliotecas instaladas de Unity 6000.6.0f1 y Unity.InputSystem: **cero errores**. El compilador aislado mostró dos advertencias CS0649 por las referencias que se asignan en el Inspector: `Board.blockPrefab` y el campo ya existente `MenuPrincipal.panelOpciones`.

Además, se ejecutaron **129 comprobaciones satisfactorias en Play Mode de Unity**, en un proyecto temporal separado y sin interfaz gráfica. Incluyeron las siete formas, cuatro rotaciones, bordes, suelo, bloques ocupados, Hard Drop, nueva pieza, limpieza de una a cuatro líneas consecutivas y líneas separadas, desplazamiento visual de bloques, controles de teclado simulados, gravedad, prevención de doble Hard Drop en un fotograma y Game Over.

Las pruebas no sustituyen la revisión visual de la escena que se monte manualmente. No se editó ningún archivo `.unity` ni `MenuPrincipal.cs`. No se escribieron manualmente archivos en Library, Temp o Logs del proyecto original. Unity generó automáticamente `Assets/Scripts/Tetris.meta` y los cuatro `.cs.meta` al importar los scripts.
