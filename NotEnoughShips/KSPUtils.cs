using System.Reflection;
using KSP.Api;
using KSP.Game;
using KSP.Game.Serialization;
using KSP.IO;
using KSP.Messages;
using KSP.Modules;
using KSP.OAB;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.Sim.State;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;

namespace NotEnoughShips;

public static class KSPUtils
{
    public static Vector3 GetUp(Vector3 pos)
    {
        Vector3d horizonUp = Vector3d.up;
        CelestialBodyComponent referenceBody = GameManager.Instance.Game?.UniverseView?.FlightObserver?.ReferenceBody;
        if(referenceBody!=null)
        {
            Position start = new Position((ICoordinateSystem) referenceBody.transform.celestialFrame, Vector3d.zero);
            horizonUp = GameManager.Instance.Game.UniverseView.PhysicsSpace.VectorToPhysics(
                MathDP.Normalize(MathDP.Delta(
                    new Position((ICoordinateSystem) referenceBody.transform.celestialFrame, (Vector3d) pos), start)));
        }
        return (Vector3) horizonUp;
    }

    public static PartState getPartState(PartData a)
    {
        var p = new PartState();
        p.resources = new Dictionary<string, ContainedResourceState>();
        foreach (var res in a.resourceContainers)
        {
            p.resources.Add(res.name, new ContainedResourceState()
            {
                name = res.name, capacityUnits = res.capacityUnits, storedUnits = res.initialUnits,
            });
        }
        p.attachNodeStates = new List<AttachNodeState>();
        return p;
    }
    public static string GetRandomPartName(Random r)
    {
        var p = GameManager.Instance.Game.Parts.AllParts();
        var part = p.ToArray()[r.Next(p.Count)].data;
        return part.partName;
    }

    public static SerializedLocation GetLocation(
        SimulationObjectModel obj)
    {
        var loc = SerializationUtility.SerializeLocation(obj, SerializationUtility.RigidBodySerializationMode.LocalUp);
        if(loc.LocationType!=LocationType.Orbit)
        {
            var rbody = loc.rigidbodyState.Value;
            rbody.localRotation = QuaternionD.identity;
            loc.rigidbodyState = rbody;
        }
        return loc;
    }
    public static SerializedLocation GetLocationWithOffset(
        SimulationObjectModel obj, Vector3 offset)
    {
        var loc = SerializationUtility.SerializeLocation(obj, SerializationUtility.RigidBodySerializationMode.LocalUp);
        var rbody = loc.rigidbodyState.Value;
        rbody.localRotation = QuaternionD.identity;
        loc.rigidbodyState = rbody;
        return loc;
    }
    public static SerializedLocation GetGroundLocation(
        SimulationObjectModel obj)
    {
        SerializedLocation flagLocation = new SerializedLocation();
        RigidbodyComponent rigidbody = obj.Rigidbody;
        if(rigidbody!=null)
        {
            //KerbalComponent kerbal = obj.Kerbal;
            ITransformModel transform = obj.transform;
            Position position = MathDP.Move(transform.Position, new Vector());
            Vector3d localPosition = TransformFrame
                .GetNonInternalCoordinateSystem((ICoordinateSystem) rigidbody.transform.parent).ToLocalPosition(position);
            RigidbodyState state = (RigidbodyState) rigidbody.GetState();

            flagLocation.rigidbodyState = new SerializedRigidbodyState?(new SerializedRigidbodyState()
            {
                referenceFrameType = state.referenceFrameType,
                referenceTransformGuid = state.referenceTransformGuid,
                localPosition = localPosition,
                localRotation = state.localRotation,
                PhysicsMode = PhysicsMode.AtRest
            });
            flagLocation.LocationType = LocationType.SurfaceCoordinates;
        }
        return flagLocation;
    }

    public static void GetAssemblies(List<(string, OABPlacedAssembly)> assemblies)
    {
        var stock = ObjectAssemblyBuilderFileIO.GetOABWorkspaceFileNames(IOProvider.DataLocation.OABWorkspacesActiveCampaign);
        NotEnoughShipsMod.ModLogger.Info("Finding workspaces in "+stock);
        if(stock==null)
        {
            NotEnoughShipsMod.ModLogger.Info("Unfortunately there is nothing");
            return;
        }
        assemblies.Clear();
        foreach (var save in stock)
        {
            NotEnoughShipsMod.ModLogger.Info(save);

            OABHistoricalSnapshot outhis;
            string withoutExtension = Path.GetFileNameWithoutExtension(save);
            string entryName = withoutExtension.Trim().Replace("_", " ");

            var oab = IOProvider.FromJsonFile<OABHistoricalSnapshot>(IOProvider.DataLocation.OABWorkspacesActiveCampaign,
                entryName, out outhis)
                ? outhis
                : (OABHistoricalSnapshot) null;
            if(oab!=null && oab.Assemblies.Count > 0)
            {
                for (int i = 0; i < oab.Assemblies.Count; i++)
                {
                    assemblies.Add((
                        oab.Metadata.Name+"/"+(oab.Assemblies[i].isMainAssembly ? " (MAIN) " : "")+
                        oab.Assemblies[i].Assembly.AssemblyDefinition.assemblyName, oab.Assemblies[i]));
                }
            }
        }
        return;
    }
    public static SerializedAssembly GetAssembly(OABPlacedAssembly assembly)
    {
        SerializedAssembly serializedAssembly = assembly.Assembly!=null
            ? assembly.Assembly
            : JsonConvert.DeserializeObject<SerializedAssembly>(assembly.assembly);
        return serializedAssembly;
    }
    public static SerializedAssembly GetAssembly()
    {
        var stock = ObjectAssemblyBuilderFileIO.GetOABWorkspaceFileNames(IOProvider.DataLocation.OABWorkspacesActiveCampaign);
        NotEnoughShipsMod.ModLogger.Info("Finding workspaces in "+stock);
        if(stock==null)
        {
            NotEnoughShipsMod.ModLogger.Info("Unfortunately there is nothing");
            return null;
        }
        foreach (var save in stock)
        {
            NotEnoughShipsMod.ModLogger.Info(save);

            OABHistoricalSnapshot outhis;
            string withoutExtension = Path.GetFileNameWithoutExtension(save);
            string entryName = withoutExtension.Trim().Replace("_", " ");

            var oab = IOProvider.FromJsonFile<OABHistoricalSnapshot>(IOProvider.DataLocation.OABWorkspacesActiveCampaign,
                entryName, out outhis)
                ? outhis
                : (OABHistoricalSnapshot) null;
            if(oab!=null && oab.Assemblies.Count > 0)
            {
                var assembly = oab.Assemblies[0];
                NotEnoughShipsMod.ModLogger.Info("FOUND  "+oab.Metadata.Name);
                SerializedAssembly serializedAssembly = assembly.Assembly!=null
                    ? assembly.Assembly
                    : JsonConvert.DeserializeObject<SerializedAssembly>(assembly.assembly);
                NotEnoughShipsMod.ModLogger.Info("LOADING "+serializedAssembly);
                return serializedAssembly;
            }
        }
        return null;
    }

    public static SerializedAssembly GetSinglePartAssembly(PartData pdata)
    {
        var assm = new SerializedAssembly();
        assm.parts = new List<SerializedPart>();
        assm.parts.Add(GetPart(pdata));
        assm.kerbalState = new KerbalState();
        assm.vesselState = VesselState.GetDefaultStateGround();
        assm.stagingState = new StagingState();
        assm.stagingState.availableStages = new List<StagePartsState>();
        assm.maneuverPlanState = new ManeuverPlanState();
        assm.maneuverPlanState.maneuvers = new List<ManeuverNodeData>();
        assm.partOwnerState = new PartOwnerState();
        assm.partOwnerState.virtualConnections = new List<PartRelationshipData>();
        return assm;
    }

    public static VesselBehavior CreateVessel(SerializedAssembly assm, Vector3 posOffset, SerializedLocation origin, bool populateVessel, String name)
    {
        NotEnoughShipsMod.ModLogger.Info("spawning at "+origin.LocationType);
        if(origin.rigidbodyState.HasValue)
        {
            if(origin.LocationType==LocationType.SurfaceCoordinates)
            {
                var r = origin.rigidbodyState.Value;
                NotEnoughShipsMod.ModLogger.Info(r.localPosition.ToString());
                r.localPosition += posOffset;
                origin.rigidbodyState = r;
            }else if(origin.LocationType==LocationType.Orbit)
            {
                var r = origin.rigidbodyState.Value;
                r.localPosition += posOffset;
                origin.rigidbodyState = r;
                NotEnoughShipsMod.ModLogger.Info(r.localPosition.ToString());
            }
        }
        if(origin.surfaceLocation.HasValue)
        {
            NotEnoughShipsMod.ModLogger.Info(origin.surfaceLocation.Value.objectName+","+origin.surfaceLocation.Value.parentGuid);
        }
        assm.location = origin;
        var localPos = assm.location.rigidbodyState?.localPosition;
        var localVel = assm.location.rigidbodyState?.localVelocity;
        var game = GameManager.Instance.Game;
        NotEnoughShipsMod.ModLogger.Info("Making Vessel ");
        assm.Guid = IGGuid.Empty;
        VesselComponent component = game.SpaceSimulation.CreateVesselSimObject(assm,
            GameManager.Instance.Game.LocalPlayer.PlayerGuidString, GameManager.Instance.Game.LocalPlayer.PlayerId,
            GameManager.Instance.Game.LocalPlayer.PlayerId).FindComponent<VesselComponent>();
        if(component!=null)
        {
            component.SimulationObject.Name = (name==null || name =="") ? assm.AssemblyDefinition.assemblyName+UnityEngine.Random.Range(0, 9999999):name;
            if(populateVessel)
            {
                PopulateVessel(component);
            }
        }
        else
        {
            throw new Exception("Vessel creation unsuccessful");
            return null;
        }

        component.SimulationObject.Rigidbody.UpdatePosition( new Position(component.SimulationObject.Rigidbody.coordinateSystem, localPos.Value));
        component.SimulationObject.Rigidbody.UpdateVelocity( new Velocity(component.SimulationObject.Rigidbody.relativeToMotion, localVel.Value));
        NotEnoughShipsMod.ModLogger.Info("D: "+GetLocation(component.SimulationObject).rigidbodyState?.localPosition.ToString());
        component.ApplyFlightCtrlState(new FlightCtrlStateIncremental()
        {
            mainThrottle = new float?(1f)
        });
        component.justLaunched = true;
        component.ParentToInertialReferenceFrame();
        
        NotEnoughShipsMod.ModLogger.Info("DONE Making Vessel ");
        
        VesselCreatedMessage msg2;
        if (game.Messages.TryCreateMessage(out msg2))
        {
            msg2.SerializedVessel = assm;
            msg2.serializedLocation = assm.location;
            msg2.vehicle = null;
            game.Messages.Publish(msg2);
        }
        
        return null;
    }


    public static void PopulateVessel(VesselComponent vc)
    {
        var methods = typeof(VesselComponent).GetMethods(BindingFlags.NonPublic|BindingFlags.Instance);
        foreach(MethodInfo mi in methods)
            if(mi.Name == "AutoPopulateCrewPartIfNeeded")
            {
                mi.Invoke(vc, null);
                break; //leave the loop
            }
    }

    public static SerializedPart GetPart(PartData part)
    {
        var pname = part.partName;
        NotEnoughShipsMod.ModLogger.Info("LOADING "+pname);
        SerializedPart outPart = new SerializedPart();
        outPart.partName = pname;
        outPart.partState = getPartState(part);
        outPart.PartGuid = IGGuid.NewGuid();
        outPart.PartModulesState = new List<SerializedPartModule>();
        foreach (var module in part.serializedPartModules)
        {
            outPart.PartModulesState.Add(module);
        }
        NotEnoughShipsMod.ModLogger.Info("LOADED "+pname);
        return outPart;
    }
    
}
