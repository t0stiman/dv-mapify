using Mapify.Editor.Tools.OSM.Data;
using Mapify.Editor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

#if UNITY_EDITOR
namespace Mapify.Editor.Tools.OSM
{
    public class TrackGeneratorWindow : EditorWindow
    {
        [MenuItem("Mapify/Tools/OSM/Track Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<TrackToolsWindow>();
            window.Show();
            window.titleContent = new GUIContent("Track Generator");
        }

        public DataExtractor DataExtractor;
        public bool TryUseTagData = true;
        public bool SameLengthHandles = true;
        public float TrackHeight = 0.5f;

        // Mapify prefabs
        public Track TrackPrefab;
        public BufferStop BufferPrefab;
        public Switch LeftSwitch;
        public Switch RightSwitch;
        public Turntable Turntable;

        private bool _showPrefabs = false;
        // Ways created from extracted data.
        private Dictionary<long, TrackWay> _ways = new Dictionary<long, TrackWay>();
        // Nodes created from the extracted data.
        private Dictionary<long, TrackNode> _nodes = new Dictionary<long, TrackNode>();
        // Switches instantiated by the script.
        private Dictionary<long, Switch> _switchInstances = new Dictionary<long, Switch>();

        public bool TestMode = false;

        private void OnGUI()
        {
            if (DataExtractor == null)
            {
                EditorGUILayout.HelpBox("Data Extractor cannot be null!", MessageType.Error);
            }

            DataExtractor = (DataExtractor)EditorGUILayout.ObjectField(
                new GUIContent("Data extractor"),
                DataExtractor, typeof(DataExtractor), true);
            TryUseTagData = EditorGUILayout.Toggle(
                new GUIContent("Try to use tag data",
                "If true, will try to get age, yards and/or station info from the data and assign it."),
                TryUseTagData);
            SameLengthHandles = EditorGUILayout.Toggle(new GUIContent("Handles have same length",
                "If true, handle length will be shared between both sides, else it may differ."),
                SameLengthHandles);
            TrackHeight = EditorGUILayout.FloatField(new GUIContent("Track height",
                "How much to offset the track vertically."),
                TrackHeight);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Prefab foldout.
            _showPrefabs = EditorGUILayout.Foldout(_showPrefabs, "Prefabs");

            if (_showPrefabs)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical();
                EditorGUILayout.PropertyField(_trackPrefab, new GUIContent("Track"));
                EditorGUILayout.PropertyField(_bufferPrefab, new GUIContent("Buffer"));
                EditorGUILayout.PropertyField(_leftSwitch, new GUIContent("Left switch"));
                EditorGUILayout.PropertyField(_rightSwitch, new GUIContent("Right switch"));
                EditorGUILayout.PropertyField(_turntable, new GUIContent("Turntable"));
                EditorGUILayout.Space();

                if (GUILayout.Button(new GUIContent("Try to get default Mapify assets",
                    "This will not replace existing prefabs, set one to \"None\" to use the default prefab.")))
                {
                    _trackGenerator.TryGetDefaultAssets();
                }

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // If there's data, display a button to generate the tracks.
            if (_trackGenerator.DataExtractor != null && _trackGenerator.DataExtractor.HasData)
            {
                if (_trackGenerator.TestMode)
                {
                    EditorGUILayout.HelpBox("Test mode enabled.", MessageType.Warning);
                }
                if (GUILayout.Button("Generate Trackage"))
                {
                    _trackGenerator.GenerateTrackage();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Check the data extractor's data.", MessageType.Warning);
            }

            // If there's any children, display a button to clear them.
            // All children of the generator should've been generated by it, so it should be safe
            // to delete then.
            if (_trackGenerator.transform.childCount > 0)
            {
                if (GUILayout.Button("Clear existing tracks"))
                {
                    _trackGenerator.ClearExistingTracks();
                }
            }

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        [ContextMenu("Test Mode")]
        private void ToggleTest()
        {
            TestMode = !TestMode;
        }

        public void TryGetDefaultAssets()
        {
            TrackToolsHelper.TryGetDefaultPrefabs(ref TrackPrefab, ref BufferPrefab, ref LeftSwitch, ref RightSwitch, ref Turntable);
        }

        public void ClearExistingTracks()
        {
            // References to gameobjects, clear before children are deleted.
            _ways.Clear();
            _switchInstances.Clear();

            // Clear children.
            var children = new List<GameObject>();
            foreach (Transform child in DataExtractor.transform)
            {
                children.Add(child.gameObject);
            }

            children.ForEach(DestroyImmediate);
        }

        public bool ExtractedDataCheck()
        {
            // Check if data to work with exists...
            if (DataExtractor == null)
            {
                Debug.LogError("Data Extractor is null!");
                return false;
            }
            else if (!DataExtractor.HasData)
            {
                Debug.LogError("Data Extractor has no data!");
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool WayAndNodeDataCheck()
        {
            if (_ways.Count == 0)
            {
                Debug.LogError("There is no way data!");
                return false;
            }
            else if (_nodes.Count == 0)
            {
                Debug.LogError("There is no node data!");
                return false;
            }
            else
            {
                return true;
            }
        }

        public void GenerateWaysAndNodes()
        {
            if (!ExtractedDataCheck())
            {
                return;
            }

            Debug.Log("Generating ways and nodes...");

            _ways.Clear();
            _nodes.Clear();

            TrackWay wayRoot;
            List<long> nodeIds;
            TrackNode here;
            TrackNode prev;
            NodeVector3 nv;

            int length;

            // Turn the extracted data into a more easily usable format.
            foreach (var way in DataExtractor.WayData.Values)
            {
                // Skip any way that doesn't link at least 2 nodes, ways with no tags, and non railway ways.
                if (way.Nodes.Count() < 2 || way.Tags == null || !way.Tags.ContainsKey("railway"))
                {
                    continue;
                }

                // Reduce generated tracks to some known ones to test.
                // Test tracks, 4452674 and NH02 are part of a Testring T1, Gleis 241 joins Testring T2,
                // and 11492749 is an aditional track for T1.
                if (TestMode && !way.Name.Contains("Test") &&
                    way.Name != "4452674" &&
                    way.Name != "NH02" &&
                    way.Name != "11492749" &&
                    way.Name != "Gleis 241")
                {
                    continue;
                }

                // Create the starting way root object.
                wayRoot = new GameObject(way.Name).AddComponent<TrackWay>();
                wayRoot.transform.parent = DataExtractor.transform;
                wayRoot.transform.position = DataExtractor.NodeData[way.Nodes[0]].Position;
                wayRoot.Id = way.Id;
                wayRoot.Tags = way.Tags.Select(x => (NodeTag)x).ToArray();

                length = way.Nodes.Length;
                nv = DataExtractor.NodeData[way.Nodes[0]];

                // Prepare the way.
                nodeIds = new List<long>(length) { nv.Id };

                // Check if the node has been created already, if not do it.
                if (!_nodes.TryGetValue(nv.Id, out here))
                {
                    here = new TrackNode(nv);
                    _nodes.Add(nv.Id, here);
                }

                // Repeat the process for every node.
                for (int i = 1; i < length; i++)
                {
                    // Keep the old one on hand.
                    prev = here;
                    nv = DataExtractor.NodeData[way.Nodes[i]];
                    nodeIds.Add(nv.Id);

                    if (!_nodes.TryGetValue(nv.Id, out here))
                    {
                        here = new TrackNode(nv);
                        _nodes.Add(nv.Id, here);
                    }

                    // Try to connect to the previous one.
                    here.TryConnect(prev);
                }

                wayRoot.Nodes = nodeIds.ToArray();
                _ways.Add(way.Id, wayRoot);
            }

            CreateSegments();

            Debug.Log($"Ways: {_ways.Count} || Nodes: {_nodes.Count}");
        }

        private void CreateSegments()
        {
            int length;
            long id;
            List<long> segment;
            List<long[]> segments;

            foreach (var way in _ways.Values)
            {
                id = way.Nodes[0];
                length = way.Nodes.Length;
                segment = new List<long>() { id };
                segments = new List<long[]>();

                for (int i = 1; i < length; i++)
                {
                    id = way.Nodes[i];
                    segment.Add(id);

                    switch (_nodes[id].GetNodeType())
                    {
                        // Break the segment.
                        // Currently don't break at crosses.
                        case TrackNode.NodeType.Switch:
                        //case TrackNode.NodeType.Cross:
                        case TrackNode.NodeType.Over4:
                            segments.Add(segment.ToArray());
                            segment = new List<long>() { id };
                            continue;
                        default:
                            continue;
                    }
                }

                if (segment.Count > 1)
                {
                    segments.Add(segment.ToArray());
                }

                way.Segments = segments.ToArray();
            }
        }

        public void InstantiateTracks()
        {
            if (!ExtractedDataCheck())
            {
                return;
            }
            if (!WayAndNodeDataCheck())
            {
                return;
            }

            CalculateHandles();

            Track track;

            foreach (var way in _ways.Values)
            {
                foreach (var segment in way.Segments)
                {
                    CreateTrack(way.transform, segment, out track);

                    if (TryUseTagData)
                    {
                        AssignTrackProperties(way, ref track);
                    }
                }
            }
        }

        private void CalculateHandles()
        {
            if (!WayAndNodeDataCheck())
            {
                return;
            }

            // To create a smoother track, the nodes need to be processed in order of
            // more connections to less connections.
            TrackNode[] orderedNodes = _nodes.GetOrderedNodes();

            for (int i = 0; i < orderedNodes.Length; i++)
            {
                orderedNodes[i].CalculateHandles(SameLengthHandles);
            }
        }

        public void GenerateTrackage()
        {
            if (!ExtractedDataCheck())
            {
                return;
            }

            ClearExistingTracks();
            GenerateWaysAndNodes();
            InstantiateTracks();
        }

        private void CreateTrack(Transform parent, long[] nodeIds, out Track track)
        {
            TrackNode prev = _nodes[nodeIds[0]];
            TrackNode here = _nodes[nodeIds[1]];
            BezierPoint point;

            track = Instantiate(TrackPrefab);

            // Place the track segment in the correct spot.
            track.transform.parent = parent;
            track.name = $"[{here.Name}] TO [{_nodes[nodeIds.Last()].Name}]";
            track.transform.position = here.Position;

            BezierCurve curve = track.Curve;

            int length = nodeIds.Length;
            int f = length - 1;

            Vector3 height = Vector3.up * TrackHeight;

            if (SameLengthHandles)
            {
                curve[0].position = prev.Position + height;
                curve[0].handleStyle = BezierPoint.HandleStyle.Connected;
                curve[0].globalHandle2 = prev.GetGlobalHandle(here);
                curve[1].position = here.Position + height;
                curve[1].handleStyle = BezierPoint.HandleStyle.Connected;
                curve[1].globalHandle1 = here.GetGlobalHandle(prev);

                for (int i = 2; i < length; i++)
                {
                    prev = here;
                    here = _nodes[nodeIds[i]];

                    point = curve.AddPointAt(here.Position + height);
                    point.globalHandle1 = here.GetGlobalHandle(prev);
                }
            }
            else
            {
                curve[0].position = prev.Position + height;
                curve[0].handleStyle = BezierPoint.HandleStyle.Broken;
                curve[0].globalHandle2 = prev.GetGlobalHandle(here);
                curve[1].position = here.Position + height;
                curve[1].handleStyle = BezierPoint.HandleStyle.Broken;
                curve[1].globalHandle1 = here.GetGlobalHandle(prev);

                for (int i = 2; i < length; i++)
                {
                    prev = here;
                    here = _nodes[nodeIds[i]];

                    curve[i - 1].globalHandle2 = prev.GetGlobalHandle(here);

                    point = curve.AddPointAt(here.Position + height);
                    point.handleStyle = BezierPoint.HandleStyle.Broken;
                    point.globalHandle1 = here.GetGlobalHandle(prev);
                }
            }

            Switch s;
            BezierPoint sp;

            // Check if it starts on a switch.
            here = _nodes[nodeIds[0]];

            if (here.GetNodeType() == TrackNode.NodeType.Switch)
            {
                s = CreateOrGetSwitch(here);
                int index = here.GetIndex(_nodes[nodeIds[1]]);
                switch (index)
                {
                    case 0:
                        sp = s.GetJointPoint();
                        break;
                    case 1:
                        sp = s.GetThroughPoint();
                        break;
                    case 2:
                        sp = s.GetDivergingPoint();
                        break;
                    default:
                        throw new Exception("This cannot happen.");
                }

                curve[0].position = sp.position;

                // No idea why this needs to be done.
                if (index != 0 && here.Orientation == TrackNode.SwitchOrientation.Right)
                {
                    curve[0].globalHandle2 = sp.globalHandle2;
                }
                else
                {
                    curve[0].globalHandle2 = sp.globalHandle1;
                }
            }

            // Check if it ends on a switch.
            here = _nodes[nodeIds[f]];

            if (here.GetNodeType() == TrackNode.NodeType.Switch)
            {
                s = CreateOrGetSwitch(here);
                int index = here.GetIndex(_nodes[nodeIds[f - 1]]);
                switch (index)
                {
                    case 0:
                        sp = s.GetJointPoint();
                        break;
                    case 1:
                        sp = s.GetThroughPoint();
                        break;
                    case 2:
                        sp = s.GetDivergingPoint();
                        break;
                    default:
                        throw new Exception("This cannot happen.");
                }

                curve[f].position = sp.position;

                // Ditto.
                if (index != 0 && here.Orientation == TrackNode.SwitchOrientation.Right)
                {
                    curve[f].globalHandle1 = sp.globalHandle1;
                }
                else
                {
                    curve[f].globalHandle1 = sp.globalHandle2;
                }
            }
        }

        private Switch CreateOrGetSwitch(TrackNode node)
        {
            Switch s = null;

            // Get or create a new one switch instance.
            if (!_switchInstances.TryGetValue(node.Id, out s))
            {
                // Use the correct one...
                if (node.Orientation == TrackNode.SwitchOrientation.Left)
                {
                    s = Instantiate(LeftSwitch);
                }
                else
                {
                    s = Instantiate(RightSwitch);
                }

                _switchInstances.Add(node.Id, s);
            }

            // To position a switch, it needs to be rotated.
            s.transform.parent = DataExtractor.transform;
            s.transform.position = node.Position + Vector3.up * TrackHeight;
            s.transform.rotation = Quaternion.LookRotation(node.GetHandle(0));
            s.gameObject.name = $"SWITCH [{node.Name}]";

            return s;
        }

        private void AssignTrackProperties(TrackWay way, ref Track track)
        {
            var current = way.Tags[0];

            for (int i = 0; i < way.Tags.Length; i++)
            {
                current = way.Tags[i];
                switch (current.Key)
                {
                    case "railway":
                        switch (current.Value)
                        {
                            case "abandoned":
                            case "disused":
                                track.age = TrackAge.Old;
                                break;
                            default:
                                break;
                        }
                        continue;
                    case "service":
                        switch (current.Value)
                        {
                            case "yard":
                            case "spur":
                                //track.trackType = TrackType.Parking;
                                break;
                            default:
                                break;
                        }
                        continue;
                    case "usage":
                        switch (current.Value)
                        {
                            default:
                                break;
                        }
                        continue;
                    default:
                        continue;
                }
            }
        }
    }
}
#endif
