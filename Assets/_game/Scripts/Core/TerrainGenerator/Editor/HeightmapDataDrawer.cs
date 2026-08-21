namespace Core.TerrainGenerator
{
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class HeightmapDataDrawer : OdinValueDrawer<TerrainData>
{
    private bool _showWorldSpaceMode = false; // Toggle режима
    private int2 _selectedChunk = new int2(-1, -1);
    // Кэшируем поля рефлексии для производительности
    private static readonly FieldInfo ActiveChunksField = typeof(TerrainData).GetField("_activeChunks", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
    private static readonly FieldInfo MaxChunksSideField = typeof(TerrainData).GetField("_maxChunksSide", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
    private static readonly FieldInfo CurrentMapMinField = typeof(TerrainData).GetField("_currentMapMin", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

    protected override void DrawPropertyLayout(GUIContent label)
    {
        // Отрисовка стандартного заголовка поля
        Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        if (label != null)
        {
            EditorGUI.LabelField(headerRect, label, EditorStyles.boldLabel);
        }

        TerrainData data = ValueEntry.SmartValue;
        if (data == null)
        {
            SirenixEditorGUI.ErrorMessageBox("HeightmapData is null");
            return;
        }

        // Извлекаем данные через рефлексию
        var activeChunks = ActiveChunksField?.GetValue(data) as Dictionary<Vector2Int, int2>;
        int maxSide = MaxChunksSideField != null ? (int)MaxChunksSideField.GetValue(data) : 0;
        Vector2Int mapMin = CurrentMapMinField != null ? (Vector2Int)CurrentMapMinField.GetValue(data) : Vector2Int.zero;

        if (maxSide <= 0 || activeChunks == null)
        {
            SirenixEditorGUI.InfoMessageBox("Данные высот не инициализированы или пустые.");
            return;
        }

        SirenixEditorGUI.BeginBox();

        // Встроенный Toggle для переключения режимов
        _showWorldSpaceMode = EditorGUILayout.ToggleLeft(
            _showWorldSpaceMode ? "Режим: Карта Мира (Позиция Чанка -> Слот Текстуры)" : "Режим: Сетка Текстуры (Слот Текстуры -> Позиция Чанка)",
            _showWorldSpaceMode,
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(5);

        // Инвертированный словарь для Быстрого поиска (Слот Текстуры -> Мировая Координата Чанка)
        Dictionary<int2, Vector2Int> textureSlotToChunk = new Dictionary<int2, Vector2Int>();
        foreach (var kvp in activeChunks)
        {
            textureSlotToChunk[kvp.Value] = kvp.Key;
        }

        // Вычисляем размер ячейки под ширину окна инспектора
        float availableWidth = EditorGUIUtility.currentViewWidth - 60f;
        float cellSize = Mathf.Clamp(availableWidth / maxSide, 28f, 50f);

        GUIStyle cellStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Max(8, (int)(cellSize * 0.28f)),
            wordWrap = true
        };

        // Рисуем сетку (Y снизу вверх для совпадения с графическими координатами)
        for (int y = maxSide - 1; y >= 0; y--)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Центрируем таблицу

            for (int x = 0; x < maxSide; x++)
            {
                bool isOccupied = false;
                string cellText = "-";

                int2 slot;
                if (!_showWorldSpaceMode)
                {
                    // РЕЖИМ 1: Сетка Текстуры
                    // Ячейка (x, y) - это слот текстуры. Показываем координату чанка в мире.
                    slot = new int2(x, y);
                    if (textureSlotToChunk.TryGetValue(slot, out Vector2Int chunkCoord))
                    {
                        isOccupied = true;
                        cellText = $"{chunkCoord.x},{chunkCoord.y}\n[{x},{y}]";
                    }
                }
                else
                {
                    // РЕЖИМ 2: Карта Мира
                    // Ячейка (x, y) - это чанк с координатой mapMin + (x, y). Показываем слот текстуры.
                    Vector2Int chunkCoord = mapMin + new Vector2Int(x, y);
                    if (activeChunks.TryGetValue(chunkCoord, out slot))
                    {
                        isOccupied = true;
                        cellText = $"{chunkCoord.x},{chunkCoord.y}\n[{slot.x},{slot.y}]";
                    }
                }

                // Задаем цвет фоновой подсветки ячейки
                Color defaultBg = GUI.backgroundColor;
                if (isOccupied)
                {
                    // Яркий/мягкий зеленый для занятых ячеек
                    GUI.backgroundColor = new Color(0.35f, 0.85f, 0.35f, 1f);
                }
                else
                {
                    // Серый полупрозрачный для пустых ячеек
                    GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                }
                if (_selectedChunk.x == slot.x && _selectedChunk.y == slot.y)
                {
                    GUI.backgroundColor *= 2f;
                }

                // Рисуем ячейку
                Rect cellRect = GUILayoutUtility.GetRect(cellSize, cellSize, GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                if (GUI.Button(cellRect, cellText, cellStyle))
                {
                    _selectedChunk = slot;
                }

                // Восстанавливаем оригинальный цвет GUI
                GUI.backgroundColor = defaultBg;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // Подвал с инфо
        EditorGUILayout.Space(4);
        GUILayout.Label($"Занято чанков: {activeChunks.Count} / {maxSide * maxSide} | mapMin: {mapMin}");

        SirenixEditorGUI.EndBox();
    }
}
#endif
}