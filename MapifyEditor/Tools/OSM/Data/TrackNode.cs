using Mapify.Editor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mapify.Editor.Tools.OSM.Data
{
    [Serializable]
    public class TrackNode
    {
        // Type of node, based on number of connections.
        public enum NodeType
        {
            Empty       = 0,
            End         = 1,
            Connected   = 2,
            Switch      = 3,
            Cross       = 4,
            Over4       = 5
        }

        public long Id;
        public string Name;
        public Vector3 Position;
        public NodeTag[] Tags;
        public List<TrackNode> Connected;

        private List<TrackNodeHandle> _handles;

        public TrackNode(long id, string name, Vector3 position, NodeTag[] tags)
        {
            Id = id;
            Name = name;
            Position = position;
            Tags = tags;
            Connected = new List<TrackNode>();

            _handles = new List<TrackNodeHandle>();
        }

        public TrackNode(NodeVector3 node) : this(node.Id, node.Name, node.Position, node.Tags) { }
        // Empty constructor for serialization.
        public TrackNode() : this(-1, "", Vector3.zero, new NodeTag[0]) { }

        public NodeType GetNodeType()
        {
            switch (Connected.Count)
            {
                case 0:
                    return NodeType.Empty;
                case 1:
                    return NodeType.End;
                case 2:
                    return NodeType.Connected;
                case 3:
                    return NodeType.Switch;
                case 4:
                    return NodeType.Cross;
                default:
                    return NodeType.Over4;
            }
        }

        public bool IsSwitch()
        {
            return GetNodeType() == NodeType.Switch;
        }

        public Vector3 GetHandle(int index)
        {
            if (index < 0)
            {
                return Vector3.zero;
            }

            return _handles[index].FullHandle;
        }

        public Vector3 GetHandle(TrackNode node)
        {
            int index = GetIndex(node);
            return GetHandle(index);
        }

        public Vector3 GetGlobalHandle(int index)
        {
            if (index < -1)
            {
                return Vector3.zero;
            }

            return Position + GetHandle(index);
        }

        public Vector3 GetGlobalHandle(TrackNode node)
        {
            return GetGlobalHandle(GetIndex(node));
        }

        /// <summary>
        /// Returns true if @node is the node that connects to this node on the track before the junction.
        /// </summary>
        public bool IsBeforeTrackNode(TrackNode node)
        {
            var index = GetIndex(node);
            if (index == -1)
            {
                throw new ArgumentException($"Node {node.Id} is not connected to this node {Id} at all!");
            }
            return index == 0;
        }

        /// <summary>
        /// Get the index of a connected node to this node. Returns -1 if @node is not connected to this node.
        /// </summary>
        public int GetIndex(TrackNode node)
        {
            for (int i = 0; i < _handles.Count; i++)
            {
                if (Connected[i].Id == node.Id)
                {
                    return i;
                }
            }

            return -1;
        }

        public void TryConnect(TrackNode node)
        {
            // Connect if it hasn't been connected before.
            if (!Connected.Any(x => x.Id == node.Id))
            {
                Connected.Add(node);
                // Do the same for the connected node, so the connection is both ways.
                node.TryConnect(this);
            }
        }

        public NodeType CalculateHandles(bool sameLength)
        {
            NodeType nodeType = GetNodeType();
            _handles.Clear();

            switch (nodeType)
            {
                case NodeType.Empty:
                    Debug.LogWarning($"Node {Id}:{Position} is empty.");
                    break;
                case NodeType.End:
                    // Do not point directly to the other node if it has a handle, instead
                    // try to smooth it. If there's no handle it will be a straight line.
                    Vector3 target = Connected[0].Position + Connected[0].GetHandle(this) * 1.5f;
                    Vector3 dif = Connected[0].Position - Position;
                    _handles.Add(new TrackNodeHandle((target - Position).normalized, dif.magnitude * MathHelper.OneThird));
                    break;
                case NodeType.Connected:
                {
                    // Smooth.
                    var handles = MathHelper.GetSizedSmoothHandles(Connected[0].Position, Position, Connected[1].Position);
                    _handles.Add(handles.Next);
                    _handles.Add(handles.Prev);
                    break;
                }
                case NodeType.Switch:
                {
                    // These handles are a special case, as such they are just the directions to the
                    // connected points instead of proper handles. The actual handles are calculated
                    // using these and the switch instance's positions.
                    _handles.Add(new TrackNodeHandle((Connected[0].Position - Position).normalized, 1));
                    _handles.Add(new TrackNodeHandle((Connected[1].Position - Position).normalized, 1));
                    _handles.Add(new TrackNodeHandle((Connected[2].Position - Position).normalized, 1));

                    // Check which node pair is the straightest line.
                    float dot01 = Vector3.Dot(_handles[0].Direction, _handles[1].Direction);
                    float dot12 = Vector3.Dot(_handles[1].Direction, _handles[2].Direction);
                    float dot20 = Vector3.Dot(_handles[2].Direction, _handles[0].Direction);

                    if (dot12 < dot01 && dot12 < dot20)
                    {
                        // 1 and 2 are the straightest, so 0 is the diverging track.
                        SwitchSwap(Connected[1], Connected[2], Connected[0]);
                    }
                    else if (dot20 < dot01 && dot20 < dot12)
                    {
                        // 2 and 0 are the straightest, so 1 is the diverging track.
                        SwitchSwap(Connected[2], Connected[0], Connected[1]);
                    }
                    // No need to switch if 0 and 1 are the straightest pair (2 is diverging).

                    // If the diverging exit is closer to the join point instead of the through exit,
                    // swap them around.
                    // Good: 2 1  1 2   Bad:  1  1
                    //        \|  |/          |  |
                    //         |  |          /|  |\
                    //         0  0         2 0  0 2
                    if (Vector3.Dot(_handles[2].Direction, _handles[0].Direction) > 0)
                    {
                        SwitchSwap(Connected[1], Connected[0], Connected[2]);
                    }

                    // Smooth
                    var handles01 = MathHelper.GetSizedSmoothHandles(Connected[0].Position, Position, Connected[1].Position);
                    _handles[0] = handles01.Next;
                    _handles[1] = handles01.Prev;

                    var handles02 = MathHelper.GetSizedSmoothHandles(Connected[0].Position, Position, Connected[2].Position);
                    _handles[2] = handles02.Prev;

                    break;
                }
                case NodeType.Cross:
                    Debug.LogWarning($"Node {Id}:{Position} is a cross, not implemented yet.");
                    _handles.Add(new TrackNodeHandle((Connected[0].Position - Position).normalized, 1));
                    _handles.Add(new TrackNodeHandle((Connected[1].Position - Position).normalized, 1));
                    _handles.Add(new TrackNodeHandle((Connected[2].Position - Position).normalized, 1));
                    _handles.Add(new TrackNodeHandle((Connected[3].Position - Position).normalized, 1));
                    break;
                case NodeType.Over4:
                    Debug.LogWarning($"Node {Id}:{Position} has more than 4 connections.");

                    foreach (var trackNode in Connected)
                    {
                        _handles.Add(new TrackNodeHandle((trackNode.Position - Position).normalized, 1));
                    }
                    break;
                default:
                    break;
            }

            return nodeType;
        }

        private void SwitchSwap(TrackNode into, TrackNode through, TrackNode diverging)
        {
            Connected[0] = into;
            Connected[1] = through;
            Connected[2] = diverging;

            _handles.Clear();
            _handles.Add(new TrackNodeHandle((Connected[0].Position - Position).normalized, 1));
            _handles.Add(new TrackNodeHandle((Connected[1].Position - Position).normalized, 1));
            _handles.Add(new TrackNodeHandle((Connected[2].Position - Position).normalized, 1));
        }

        public static Vector3 Flatten(Vector3 handle)
        {
            float length = handle.magnitude;
            handle.y = 0;
            return handle.normalized * length;
        }
    }
}
