using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Esta carpeta Editor excluye la herramienta de la aplicacion compilada.
public static class CubotrisSetup
{
    private const string MenuPath = "Tools/CUBOTRIS/Configurar Tetris";
    private const string UndoName = "Configurar Tetris";
    private const string PrefabPath = "Assets/Prefabs/Bloque.prefab";
    private const string BlockSpritePath = "Assets/Art/Tetrominos/Tetromino_block1_1.png";
    private const string BlockSpriteName = "Tetromino_block1_1_0";
    private const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    private const string MaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    [MenuItem(MenuPath)]
    public static void Configure()
    {
        if (!CanConfigure())
            return;

        int undoGroup = -1;
        try
        {
            Scene scene = SceneManager.GetActiveScene();
            // Incluye objetos inactivos y cambios sin guardar, solo en la escena activa.
            GameObject[] objects = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => item.gameObject).ToArray();

            GameObject boardObject = FindNamed(objects, "Tablero");
            Board[] boards = objects.SelectMany(item => item.GetComponents<Board>()).ToArray();
            if (boards.Length > 1 || (boards.Length == 1 && boardObject != null && boards[0].gameObject != boardObject))
                throw new InvalidOperationException("Hay varios tableros posibles. Conserva un solo Board antes de configurar; no se elimino ninguno.");
            if (boardObject == null && boards.Length == 1)
                boardObject = boards[0].gameObject;

            GameObject backgroundObject = FindNamed(objects, "FondoTablero");
            GameObject cameraObject = FindCamera(objects);
            CheckCanBeRoot(boardObject);
            CheckCanBeRoot(backgroundObject);
            CheckCanBeRoot(cameraObject);
            if (boardObject != null && boardObject.GetComponents<Spawner>().Length > 1)
                throw new InvalidOperationException("Tablero contiene varios Spawner. No se modificaron esos componentes.");

            // Revisar las dependencias antes de tocar la escena o crear un prefab.
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
                throw new InvalidOperationException("No se encontro el material Sprite-Unlit-Default en " + MaterialPath);

            SpriteRenderer existingBackground = backgroundObject != null ? backgroundObject.GetComponent<SpriteRenderer>() : null;
            Sprite backgroundSprite = existingBackground != null ? existingBackground.sprite : null;
            if (backgroundSprite == null)
                backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            if (backgroundSprite == null)
                throw new InvalidOperationException("No se encontro el sprite Square del paquete 2D Sprite.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Sprite blockSprite = null;
            if (prefab != null)
                ValidateBlockPrefab(prefab);
            else
            {
                if (System.IO.File.Exists(PrefabPath))
                    throw new InvalidOperationException("Ya existe un archivo en " + PrefabPath + " que no se pudo cargar. No se sobrescribio.");
                blockSprite = AssetDatabase.LoadAllAssetsAtPath(BlockSpritePath).OfType<Sprite>()
                    .FirstOrDefault(sprite => sprite.name == BlockSpriteName);
                if (blockSprite == null || blockSprite.rect.width != 32 || blockSprite.rect.height != 32)
                    throw new InvalidOperationException("No se encontro el recorte individual de 32 x 32 " + BlockSpriteName + " en " + BlockSpritePath);
                if (Vector2.Distance(blockSprite.pivot, blockSprite.rect.size * 0.5f) > 0.01f)
                    throw new InvalidOperationException("El bloque individual necesita Pivot Center en Sprite Editor; no se cambio su importacion.");
            }

            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            if (prefab == null)
                prefab = CreateBlockPrefab(scene, blockSprite, material);
            SpriteRenderer blockRenderer = prefab.GetComponent<SpriteRenderer>();

            if (boardObject == null)
                boardObject = CreateObject("Tablero", scene);
            SetRootTransform(boardObject, Vector3.zero, Vector3.one);
            Undo.RecordObject(boardObject, UndoName);
            boardObject.name = "Tablero";
            boardObject.SetActive(true);
            RecordPrefabChanges(boardObject);

            Board board = GetOrAdd<Board>(boardObject);
            // RequireComponent agrega Spawner al agregar Board; GetOrAdd nunca lo duplica.
            Spawner spawner = GetOrAdd<Spawner>(boardObject);
            Undo.RecordObjects(new UnityEngine.Object[] { board, spawner }, UndoName);
            board.enabled = true;
            spawner.enabled = true;
            RecordPrefabChanges(board);
            RecordPrefabChanges(spawner);
            SerializedObject serializedBoard = new SerializedObject(board);
            serializedBoard.FindProperty("blockPrefab").objectReferenceValue = blockRenderer;
            serializedBoard.ApplyModifiedProperties();

            if (cameraObject == null)
                cameraObject = CreateObject("Main Camera", scene);
            SetRootTransform(cameraObject, new Vector3(5f, 10f, -10f), Vector3.one);
            Undo.RecordObject(cameraObject, UndoName);
            cameraObject.tag = "MainCamera";
            cameraObject.SetActive(true);
            RecordPrefabChanges(cameraObject);
            Camera camera = GetOrAdd<Camera>(cameraObject);
            Undo.RecordObject(camera, UndoName);
            camera.enabled = true;
            camera.orthographic = true;
            camera.orthographicSize = 11f;
            // Incluir la capa de las piezas incluso si la camara tenia una mascara personalizada.
            camera.cullingMask |= 1 << prefab.layer;
            RecordPrefabChanges(camera);

            if (backgroundObject == null)
                backgroundObject = CreateObject("FondoTablero", scene);
            SpriteRenderer background = GetOrAdd<SpriteRenderer>(backgroundObject);
            Undo.RecordObject(background, UndoName);
            background.sprite = backgroundSprite;
            background.enabled = true;
            background.drawMode = SpriteDrawMode.Simple;
            if (existingBackground == null)
                background.color = new Color(0.08f, 0.09f, 0.13f, 1f);
            if (background.sharedMaterial == null || existingBackground == null)
                background.sharedMaterial = material;
            background.sortingLayerID = blockRenderer.sortingLayerID;
            background.sortingOrder = Mathf.Max(short.MinValue, blockRenderer.sortingOrder - 1);
            RecordPrefabChanges(background);

            // Escalar segun el sprite, sin alterar las texturas ni sus importadores.
            Vector2 size = backgroundSprite.rect.size / backgroundSprite.pixelsPerUnit;
            Vector3 scale = new Vector3(Board.Width / size.x, Board.Height / size.y, 1f);
            Vector2 pivot = backgroundSprite.pivot / backgroundSprite.pixelsPerUnit;
            SetRootTransform(backgroundObject, new Vector3(pivot.x * scale.x, pivot.y * scale.y, 1f), scale);
            Undo.RecordObject(backgroundObject, UndoName);
            backgroundObject.SetActive(true);
            backgroundObject.layer = prefab.layer;
            RecordPrefabChanges(backgroundObject);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = boardObject;
            SceneView.RepaintAll();
            Debug.Log("CUBOTRIS configurado en la escena actual. Guarda con Ctrl+S y pulsa Play.", boardObject);
        }
        catch (Exception exception)
        {
            // Los cambios de escena se revierten juntos si falla el montaje.
            // Un prefab nuevo es un asset independiente y se conserva para reutilizarlo.
            if (undoGroup >= 0)
                Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogError("No se pudo configurar CUBOTRIS: " + exception.Message);
        }
    }

    [MenuItem(MenuPath, true)]
    private static bool CanConfigure()
    {
        Scene scene = SceneManager.GetActiveScene();
        return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling
            && PrefabStageUtility.GetCurrentPrefabStage() == null
            && scene.IsValid() && scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene);
    }

    private static GameObject FindNamed(GameObject[] objects, string name)
    {
        GameObject[] matches = objects.Where(item => item.name == name).ToArray();
        if (matches.Length > 1)
            throw new InvalidOperationException("Hay varios objetos llamados " + name + ". No se elimino ni eligio ninguno automaticamente.");
        return matches.FirstOrDefault();
    }

    private static GameObject FindCamera(GameObject[] objects)
    {
        GameObject[] tagged = objects.Where(item => item.CompareTag("MainCamera")).ToArray();
        if (tagged.Length > 1)
            throw new InvalidOperationException("Hay varias MainCamera en la escena activa. No se modificaron.");
        if (tagged.Length == 1)
            return tagged[0];
        GameObject named = FindNamed(objects, "Main Camera");
        if (named != null)
            return named;
        GameObject[] cameras = objects.Where(item => item.GetComponent<Camera>() != null).ToArray();
        if (cameras.Length > 1)
            throw new InvalidOperationException("Asigna la etiqueta MainCamera a la camara del juego para identificarla.");
        return cameras.FirstOrDefault();
    }

    private static void ValidateBlockPrefab(GameObject prefab)
    {
        SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null || !renderer.enabled)
            throw new InvalidOperationException("Bloque.prefab necesita un SpriteRenderer habilitado con Sprite en su raiz. Se conservo el prefab existente.");
        if (prefab.GetComponentInChildren<Rigidbody2D>(true) != null || prefab.GetComponentInChildren<Collider2D>(true) != null)
            throw new InvalidOperationException("Bloque.prefab contiene fisicas 2D. Retiralas antes de configurar; se conservo el prefab existente.");
    }

    private static GameObject CreateBlockPrefab(Scene scene, Sprite sprite, Material material)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // Objeto temporal propio: nunca guardar ni destruir un objeto del usuario.
        GameObject temporary = new GameObject("Bloque");
        SceneManager.MoveGameObjectToScene(temporary, scene);
        try
        {
            SpriteRenderer renderer = temporary.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 1;
            temporary.transform.localScale = new Vector3(
                sprite.pixelsPerUnit / sprite.rect.width, sprite.pixelsPerUnit / sprite.rect.height, 1f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporary, PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("No se pudo guardar " + PrefabPath);
            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(temporary);
        }
    }

    private static GameObject CreateObject(string name, Scene scene)
    {
        GameObject item = new GameObject(name);
        SceneManager.MoveGameObjectToScene(item, scene);
        Undo.RegisterCreatedObjectUndo(item, UndoName);
        return item;
    }

    private static T GetOrAdd<T>(GameObject item) where T : Component
    {
        T component = item.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(item);
    }

    private static void CheckCanBeRoot(GameObject item)
    {
        if (item != null && item.transform.parent != null && PrefabUtility.IsPartOfPrefabInstance(item)
            && PrefabUtility.GetOutermostPrefabInstanceRoot(item) != item)
            throw new InvalidOperationException(item.name + " esta dentro de un prefab anidado. No se altero su estructura.");
    }

    private static void SetRootTransform(GameObject item, Vector3 position, Vector3 scale)
    {
        if (item.transform.parent != null)
            Undo.SetTransformParent(item.transform, null, UndoName);
        Undo.RecordObject(item.transform, UndoName);
        item.transform.SetPositionAndRotation(position, Quaternion.identity);
        item.transform.localScale = scale;
        RecordPrefabChanges(item.transform);
    }

    private static void RecordPrefabChanges(UnityEngine.Object item)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(item))
            PrefabUtility.RecordPrefabInstancePropertyModifications(item);
    }
}
