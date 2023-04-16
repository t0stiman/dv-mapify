﻿using System.Collections.Generic;
using System.Linq;
using Mapify.Editor.Utils;
using Mapify.Editor.Validators;
using UnityEditor;
using UnityEngine;

namespace Mapify.Editor
{
    public class MapValidationGui : EditorWindow
    {
        private const string WINDOW_TITLE = "Map Validation Result";
        private const string ERROR_COLOR = "maroon";
        private const string WARNING_COLOR = "orange";

        private static MapValidationGui window;

        private readonly Vector2 scrollPosition = Vector2.zero;
        private Result[] errors;
        private Result[] warnings;

        [MenuItem("Mapify/Validate Map")]
        private static void OpenAndValidate(MenuCommand menuCommand)
        {
            OpenAndValidate();
        }

        public static bool OpenAndValidate()
        {
            bool wasOpen = HasOpenInstances<MapValidationGui>();
            if (wasOpen) window.Close();
            List<Result> results = MapValidator.Validate().ToList();
            if (results.Count == 0)
                return true;
            window = GetWindow<MapValidationGui>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.errors = results.Where(r => r.type == Result.ResultType.ERROR).ToArray();
            window.warnings = results.Where(r => r.type == Result.ResultType.WARNING).ToArray();
            window.Show();
            return window.errors.Length == 0;
        }

        public void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) {
                richText = true
            };

            if (errors.Length != 0)
            {
                GUILayout.Label($"<color={ERROR_COLOR}>Your map has errors!</color>", style);
                DrawResults(errors, ERROR_COLOR, style);
            }

            if (warnings.Length != 0)
            {
                GUILayout.Label($"<color={WARNING_COLOR}>Your map has warnings!</color>", style);
                DrawResults(warnings, WARNING_COLOR, style);
            }

            if (GUILayout.Button("Close"))
                Close();
        }

        private void DrawResults(IEnumerable<Result> results, string color, GUIStyle style)
        {
            GUILayout.BeginScrollView(scrollPosition);

            foreach (Result result in results)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"• <color={color}>{result.message}</color>", style);
                if (result.context != null && GUILayout.Button("View Object"))
                    Selection.activeObject = result.context;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }
    }
}
