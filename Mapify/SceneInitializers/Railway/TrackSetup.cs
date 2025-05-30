﻿using System.Collections.Generic;
using System.Linq;
using DV;
using Mapify.Editor;
using Mapify.Editor.Utils;
using Mapify.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mapify.SceneInitializers.Railway
{
    [SceneSetupPriority(int.MinValue)]
    public class TrackSetup : SceneSetup
    {
        private const string IN_JUNCTION_NAME = "in_junction";

        public override void Run()
        {
            var allTracks = Object.FindObjectsOfType<Track>();
            var nonSwitchTracks = allTracks.Where(t => !t.IsSwitch).ToArray();

            Mapify.LogDebug(() => "Creating RailTracks");
            CreateRailTracks(nonSwitchTracks, false);

            Mapify.LogDebug(() => "Creating Junctions");
            CreateJunctions();

            nonSwitchTracks.SetActive(true);

            Mapify.LogDebug(() => "Connecting tracks");
            ConnectTracks(allTracks);

            AlignAllTrackEnds();
            TestConnections();
        }

        // copied from B99.3 RailManager class
        private void AlignAllTrackEnds()
        {
            foreach (RailTrack railTrack in Object.FindObjectsOfType<RailTrack>())
            {
                if (!railTrack.dontChange)
                    railTrack.TryAlignHandles();
            }
            Debug.Log((object) "Aligned all track ends");
        }

        // copied from B99.3 RailManager class
        private void TestConnections()
        {
            var objectsOfType = Object.FindObjectsOfType<RailTrack>();
            var flag = false;
            foreach (var track1 in objectsOfType)
            {
                if ((bool) (Object) track1.inJunction && !track1.inJunction.HasBranch(new Junction.Branch(track1, true)))
                {
                    Debug.LogError($"Junction '{track1.inJunction.name}' doesn't have track '{track1.name}' assigned", track1.inJunction);
                    flag = true;
                }
                if ((bool) (Object) track1.outJunction && !track1.outJunction.HasBranch(new Junction.Branch(track1, false)))
                {
                    Debug.LogError($"Junction '{track1.outJunction.name}' doesn't have track '{track1.name}' assigned", track1.outJunction);
                    flag = true;
                }
                if (track1.inIsConnected && !(bool) (Object) track1.inJunction)
                {
                    var track2 = track1.inBranch.track;
                    var branch = track1.inBranch.first ? track2.inBranch : track2.outBranch;
                    if (!(branch.track == track1) || !branch.first)
                    {
                        Debug.LogError($"Track '{track2.name}'s IN is not connected to track {track1.name}. Manually set or reconnect branches", track2);
                        flag = true;
                    }
                }
                if (track1.outIsConnected && !(bool) (Object) track1.outJunction)
                {
                    var track3 = track1.outBranch.track;
                    var branch = track1.outBranch.first ? track3.inBranch : track3.outBranch;
                    if (!(branch.track == track1) || branch.first)
                    {
                        Debug.LogError($"Track '{track3.name}'s OUT is not connected to track {track1.name}. Manually set or reconnect branches", track3);
                        flag = true;
                    }
                }
            }
            if (!flag)
                Debug.Log("Checked all connections, no errors were found");
            else
                Debug.LogError("Problems found when checking connections, see errors above");
        }

        private static List<RailTrack> CreateRailTracks(IEnumerable<Track> tracks, bool setActive)
        {
            return tracks.Select(track => CreateRailTrack(track, setActive)).ToList();
        }

        private static RailTrack CreateRailTrack(Track track, bool setActive)
        {
            track.gameObject.SetActive(setActive);
            if (!track.IsSwitch && !track.IsTurntable)
                track.name = track.LogicName;
            var railTrack = track.gameObject.AddComponent<RailTrack>();
            railTrack.generateColliders = !track.IsTurntable;
            railTrack.dontChange = false;
            railTrack.age = (int)track.age;
            railTrack.ApplyRailType();

            return railTrack;
        }

        private static void CreateJunctions()
        {
            CreateCustomSwitches();
            CreateVanillaSwitches();
        }

        private static void CreateCustomSwitches()
        {
            foreach (var customSwitch in Object.FindObjectsOfType<CustomSwitch>())
            {
                CreateCustomSwitch(customSwitch);
            }
        }

        private static void CreateCustomSwitch(CustomSwitch customSwitch)
        {
            // we use SwitchRight because with SwitchLeft the animation would be mirrored
            var vanillaAsset = customSwitch.standSide == CustomSwitch.StandSide.LEFT ? VanillaAsset.SwitchRightOuterSign : VanillaAsset.SwitchRight;

            var prefabClone = AssetCopier.Instantiate(vanillaAsset);
            prefabClone.transform.SetPositionAndRotation(customSwitch.transform.position, customSwitch.transform.rotation);

            //Junction
            var inJunction = prefabClone.GetComponentInChildren<Junction>();
            inJunction.transform.position = customSwitch.GetJointPoint().transform.position;
            inJunction.selectedBranch = customSwitch.defaultBranch;

            DestroyPrefabTracks(prefabClone);
            CreateSwitchTracks(customSwitch, prefabClone, inJunction);
            SetupStalk(prefabClone);

            foreach (var track in customSwitch.GetTracks())
            {
                track.gameObject.SetActive(true);
            }
        }

        private static void DestroyPrefabTracks(GameObject prefabClone)
        {
            // must be destroyed inmediately to prevent:
            // "Junction 'in_junction' doesn't have track '[track diverging]' assigned"
            // from RailManager.TestConnections
            Object.DestroyImmediate(prefabClone.FindChildByName("[track through]"));
            Object.DestroyImmediate(prefabClone.FindChildByName("[track diverging]"));
        }

        private static void CreateSwitchTracks(CustomSwitch customSwitch, GameObject prefabClone, Junction switchJunction)
        {
            var railTracksInSwitch = CreateRailTracks(
                customSwitch.GetTracks(), false
            );

            if (!railTracksInSwitch.Any())
            {
                Mapify.LogError($"{nameof(CreateCustomSwitches)}: {nameof(railTracksInSwitch)} is empty");
                return;
            }

            switchJunction.outBranches = new List<Junction.Branch>();

            foreach (var trackInSwitch in railTracksInSwitch)
            {
                trackInSwitch.transform.SetParent(prefabClone.transform, true);

                trackInSwitch.inBranch = new Junction.Branch();
                trackInSwitch.inBranch.track = null;
                trackInSwitch.inJunction = switchJunction;

                switchJunction.outBranches.Add(new Junction.Branch(trackInSwitch, true));
            }

            //track before the switch
            switchJunction.inBranch = switchJunction.FindClosestBranch(railTracksInSwitch[0].curve[0].transform.position);

            //connect the track before the switch to the switch
            if (switchJunction.inBranch.first)
            {
                switchJunction.inBranch.track.inJunction = switchJunction;
            }
            else
            {
                switchJunction.inBranch.track.outJunction = switchJunction;
            }

            if (switchJunction.inBranch == null)
            {
                Mapify.LogError($"{nameof(CreateSwitchTracks)}: inBranch is null");
            }
        }

        private static void SetupStalk(GameObject prefabClone)
        {
            var graphical = prefabClone.FindChildByName("Graphical");
            string[] toDelete = {"ballast", "anchors", "sleepers", "rails_static", "rails_moving"};

            foreach (var child in graphical.transform.GetChildren())
            {
                if (!toDelete.Contains(child.name)) continue;
                Object.Destroy(child.gameObject);
            }

            var switch_base = graphical.transform.FindChildByName("switch_base");
            if (!switch_base)
            {
                Mapify.LogError("Could not determine switch offset");
                return;
            }

            var offsetZ = switch_base.localPosition.z;
            // Mapify.LogDebug($"offsetZ: {offsetZ}");
            graphical.transform.localPosition -= new Vector3(0f, 0f, offsetZ);
            var switchTrigger = prefabClone.FindChildByName("SwitchTrigger");
            switchTrigger.transform.localPosition -= new Vector3(0f, 0f, offsetZ);
        }

        private static void CreateVanillaSwitches()
        {
            foreach (Switch switch_ in Object.FindObjectsOfType<Switch>())
            {
                Transform swTransform = switch_.transform;
                VanillaAsset vanillaAsset = switch_.GetComponent<VanillaObject>().asset;
                GameObject prefabClone = AssetCopier.Instantiate(vanillaAsset, false);
                Transform prefabCloneTransform = prefabClone.transform;
                Transform junctionTransform = prefabCloneTransform.Find(IN_JUNCTION_NAME);
                Vector3 offset = prefabCloneTransform.position - junctionTransform.position;
                foreach (Transform child in prefabCloneTransform)
                    child.transform.position += offset;
                prefabCloneTransform.SetPositionAndRotation(swTransform.position, swTransform.rotation);

                Junction junction = junctionTransform.GetComponent<Junction>();
                junction.selectedBranch = (byte) (switch_.IsLeft
                    ? switch_.defaultState == Switch.StandSide.THROUGH
                        ? 1
                        : 0
                    : switch_.defaultState == Switch.StandSide.THROUGH
                        ? 0
                        : 1
                    );

                foreach (Junction.Branch branch in junction.outBranches)
                    branch.track.generateColliders = true;

                prefabClone.transform.SetParent(RailTrackRegistry.Instance.TrackRootParent);
                prefabClone.SetActive(true);
            }
        }

        private static void ConnectTracks(IEnumerable<Track> tracks)
        {
            foreach (Track track in tracks)
            {
                //vanilla switches are connected elsewhere
                if(track.IsVanillaSwitch) continue;

                RailTrack railTrack = track.GetComponent<RailTrack>();

                if (track.IsCustomSwitch)
                {
                    railTrack.ConnectOutToClosestBranch();

                    if (railTrack.outBranch != null) continue;
                    Mapify.LogError($"{nameof(ConnectTracks)}: {nameof(railTrack.outBranch)} is null for custom switch track {track.name}");
                }
                else
                {
                    railTrack.ConnectInToClosestJunctionOrBranch();
                    railTrack.ConnectOutToClosestJunctionOrBranch();
                }
            }
        }
    }
}
