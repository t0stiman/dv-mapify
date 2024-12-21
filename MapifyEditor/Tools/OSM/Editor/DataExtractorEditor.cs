using Mapify.Editor.Tools.OSM.Data;
using Mapify.Editor.Utils;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Mapify.Editor.Tools.OSM.Editor
{
    [CustomEditor(typeof(DataExtractor))]
    public class DataExtractorEditor : UnityEditor.Editor
    {
        private DataExtractor _dataExtractor;

        private SerializedProperty _file;
        private SerializedProperty _latitude;
        private SerializedProperty _longitude;
        private SerializedProperty _filterNodes;
        private SerializedProperty _alwaysDraw;
        private SerializedProperty _drawTrackStartAndEnd;
        private SerializedProperty _drawAll;

        private bool _filterFoldout = false;

        private void OnEnable()
        {
            _dataExtractor = (DataExtractor)target;

            _file = serializedObject.FindProperty(nameof(DataExtractor.OsmFile));
            _latitude = serializedObject.FindProperty(nameof(DataExtractor.Latitude));
            _longitude = serializedObject.FindProperty(nameof(DataExtractor.Longitude));
            _filterNodes = serializedObject.FindProperty(nameof(DataExtractor.FilterNodesNotInWays));
            _alwaysDraw = serializedObject.FindProperty(nameof(DataExtractor.AlwaysDraw));
            _drawTrackStartAndEnd = serializedObject.FindProperty(nameof(DataExtractor.DrawTrackStartAndEnd));
            _drawAll = serializedObject.FindProperty(nameof(DataExtractor.DrawEveryNode));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool hasData = _dataExtractor.HasData;

            _file.stringValue = EditorGUILayout.TextField(
                new GUIContent("OSM File", "The location of the file with OSM data"),
                _file.stringValue);
            _latitude.doubleValue = MathHelper.ClampD(EditorGUILayout.DoubleField(
                new GUIContent("Latitude", "Latitude at the centre"),
                _latitude.doubleValue), -90.0, 90.0);
            _longitude.doubleValue = MathHelper.ClampD(EditorGUILayout.DoubleField(
                new GUIContent("Longitude", "Longitude at the centre"),
                _longitude.doubleValue), -180.0, 180.0);

            EditorHelper.Separator();

            // Filter selector
            _dataExtractor.CurrentFilter = (DataExtractor.Filter)EditorGUILayout.EnumPopup(
                new GUIContent("Data filter",
                "Which filter to use when extracting data.\nCustom Code uses a filter set by code.\nCustom Inspector creates a filter from a dropdown."),
                _dataExtractor.CurrentFilter);

            if (_dataExtractor.CurrentFilter == DataExtractor.Filter.CustomInspector)
            {
                _filterFoldout = EditorGUILayout.Foldout(_filterFoldout, "Filter");
            }

            // Other settings used when data is generated.
            // Only allow filtering by ways if not everything is included.
            GUI.enabled = _dataExtractor.CurrentFilter != DataExtractor.Filter.All;

            _filterNodes.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Filter nodes not in ways",
                "If true, any node not part of a way will be filtered out. This may have a large performance impact while generating data."),
                _filterNodes.boolValue);

            GUI.enabled = true;
            EditorGUILayout.Space();

            // Tell the user if there's anything already generated.
            if (!hasData)
            {
                EditorGUILayout.HelpBox("No data!", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Data exists!", MessageType.Info);
            }

            EditorHelper.BeginHorizontalCentre();
            GUI.backgroundColor = hasData ? EditorHelper.Warning : EditorHelper.Accept;
            if (GUILayout.Button(new GUIContent("Generate data"), GUILayout.MaxWidth(EditorGUIUtility.labelWidth)))
            {
                _dataExtractor.GenerateData();
            }
            EditorHelper.EndHorizontalCentre();

            EditorHelper.BeginHorizontalCentre();
            GUI.enabled = hasData;
            GUI.backgroundColor = EditorHelper.Cancel;
            if (GUILayout.Button(new GUIContent("Clear Data"), GUILayout.MaxWidth(EditorGUIUtility.labelWidth)))
            {
                _dataExtractor.ClearData();
            }
            EditorHelper.EndHorizontalCentre();

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            GUI.backgroundColor = Color.white;

            // Drawing options.
            _alwaysDraw.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Always draw", "If false, will only draw when the GameObject is selected."),
                _alwaysDraw.boolValue);
            _drawTrackStartAndEnd.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Draw track start and end", "Draw a closed circle at the start of a track and an open one at the end."),
                _drawTrackStartAndEnd.boolValue);
            _drawAll.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Draw every node", "If true, will draw every node, if false only the ends and special features."),
                _drawAll.boolValue);
            _dataExtractor.ColouringMode = (DataExtractor.ColourMode)EditorGUILayout.EnumPopup(
                new GUIContent("Draw mode", "Which way the nodes and ways will be coloured."),
                _dataExtractor.ColouringMode);

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }
    }
}
#endif
