using System.Reflection;
using KSP.Api.CoreTypes;
using KSP.Game;
using KSP.OAB;
using KSP.Rendering.Planets;
using KSP.Sim;
using KSP.Sim.impl;
using KSP.UI;
using KSP.UI.Flight;
using SpaceWarp.API;
using SpaceWarp.API.Logging;
using SpaceWarp.API.Mods;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static NotEnoughShips.NotEnoughShipsMod;
using GUI = UnityEngine.GUI;
using Random = System.Random;

namespace NotEnoughShips
{
    [MainMod]
    public class NotEnoughShipsMod : Mod
    {
        public static BaseModLogger ModLogger;
        public static UIUtils.GUI GUIHelper;

        public override void OnInitialized()
        {
            GUIHelper = new UIUtils.GUI(SpaceWarpManager.Skin);
            ModLogger = Logger;
            Logger.Info("NotEnoughShipsMod is initializing");
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Component.Destroy(g.GetComponent<SphereCollider>());
            Component.Destroy(g.GetComponent<Rigidbody>());
            g.transform.localScale = new Vector3(1, 1, 1);
            g.AddComponent<ModMainBehaviour>();
            Logger.Info("NotEnoughShipsMod has initialized");
        }
    }

    public class ModMainBehaviour : KerbalMonoBehaviour
    {
        Random r = new Random();
        bool displayWindow = true;
        private int windowWidth = 350;
        private int windowHeight = 700;

        UIUtils.ToggleGroup PositionType = new UIUtils.ToggleGroup(new[]
        {
            "Local", "Global", "At flag"
        });

        UIUtils.ToggleGroup CelestialBodySelect = new UIUtils.ToggleGroup(new[]
        {
            "K", "Global"
        });
        
        UIUtils.ToggleGroup FlagSelect = new UIUtils.ToggleGroup(new[]
        {
            "F"
        });

        UIUtils.ToggleGroup VesselType = new UIUtils.ToggleGroup(new[]
        {
            "Vessel", "Part"
        });

        bool populateVessel = false;

        RigidbodyComponent rbc = null;
        VesselBehavior vb;

        List<(string Name, OABPlacedAssembly assembly)> assemblies = new List<(string, OABPlacedAssembly)>();
        (string Name, SerializedAssembly assembly)? selected;

        List<string> bodies = null;
        
        List<(string Name, SimulationObjectModel assembly)> flags = new List<(string, SimulationObjectModel)>();
        (string Name, SimulationObjectModel model)? selectedFlag;

        String error = "";

        void Log(String s)
        {
            NotEnoughShipsMod.ModLogger.Info(s);
        }

        void Start()
        {
            Log("started");
            DontDestroyOnLoad(this.gameObject);

            windowRect = new Rect((Screen.width * 0.85f) - (windowWidth / 2), (Screen.height / 2) - (windowHeight / 2),
                0, 0);
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.J))
                displayWindow = !displayWindow;

            if (Game.ViewController == null)
            {
                return;
            }

            var component = Game.ViewController.GetActiveVehicle(true)?.GetSimVessel();
            if (component == null)
            {
                return;
            }

            vb = this.Game.ViewController.GetBehaviorIfLoaded(component);
            RigidbodyBehavior rb;
            if (vb && (rb = vb.GetComponent<RigidbodyBehavior>()) && (rb.Model != null))
            {
                rbc = rb.Model;
                var coordinateSystem = TransformFrame.GetNonInternalCoordinateSystem(rbc.transform.parent);
                var spawnLoc = coordinateSystem != null
                    ? (Vector3)(coordinateSystem.ToLocalVector(new Vector(coordinateSystem, offsetSpawn)))
                    : offsetSpawn;
                this.transform.position = vb.transform.position + spawnLoc;
            }

            GetComponent<MeshRenderer>().enabled = (PositionType.Get() == "Local") && displayWindow;
        }

        bool did = false;
        UIFlightHud appbar;
        private Rect windowRect;
        ActionBar ssa;

        void OnGUI()
        {
            if (displayWindow)
            {
                windowRect = GUILayout.Window(
                    GUIUtility.GetControlID(FocusType.Passive),
                    windowRect,
                    DrawGUI,
                    "Spawn thing (Alt+J to toggle)", NotEnoughShipsMod.GUIHelper.GetSkin().window,
                    GUILayout.Height(0),
                    GUILayout.Width(350));
            }
        }

        private static GUIStyle boxStyle;
        Vector3 offsetSpawn = new Vector3();
        float SMAxis = 100, Eccen = 0, Inclination = 0;
        String vesselName = "";
        Vector2 scrollPos = new Vector2();
        Vector2 celestialScrollPos = new Vector2();
        String searchString = "";

        private void DrawGUI(int windowID)
        {
            boxStyle = GUI.skin.GetStyle("Box");
            GUILayout.BeginVertical();
            String errorCheck = "";
            if (!this.Game.Parts.IsDataLoaded)
            {
                errorCheck = "Game hasnt loaded its parts yet";
            }
            else if (!this.Game.SessionManager.IsSPCampaign())
            {
                errorCheck = "Session isn't a single player campaign";
            }

            if (errorCheck != "")
            {
                GUIHelper.Label(errorCheck);
                GUILayout.EndVertical();
                return;
            }

            if (error != "")
            {
                var redtext = new GUIStyle(GUIHelper.GetSkin().label);
                redtext.normal.textColor = Color.red;
                GUILayout.Label("Error: " + error, redtext);
            }

            if (selected != null)
            {
                GUIHelper.Label($"Selected {selected.Value.Name}");
                populateVessel = GUIHelper.Toggle( "Populate vessel: " + populateVessel,"populateVessel");
                GUILayout.BeginHorizontal();
                vesselName = GUIHelper.TextArea("Name (leave empty for auto)", "vesselName", false);
                GUILayout.EndHorizontal();
                if (GUIHelper.Button("Spawn"))
                {
                    if (PositionType.Get().Equals("Local"))
                    {
                        var loc = KSPUtils.GetLocation(vb.Model.SimulationObject);
                        try
                        {
                            KSPUtils.CreateVessel(selected.Value.assembly, offsetSpawn, loc, populateVessel,
                                vesselName);
                        }
                        catch (Exception e)
                        {
                            error = e.Message + "\n Contact mod author";
                            Log(e.ToString());
                        }
                    }
                    else if (PositionType.Get().Equals("At flag"))
                    {
                        if (!selectedFlag.HasValue)
                        {
                            error = ("No flag chosen");
                        }
                        else
                        {
                            var loc = KSPUtils.GetLocation(selectedFlag.Value.model);
                            try
                            {
                                KSPUtils.CreateVessel(selected.Value.assembly, offsetSpawn, loc, populateVessel,
                                    vesselName);
                            }
                            catch (Exception e)
                            {
                                error = e.Message + "\n Contact mod author";
                                Log(e.ToString());
                            }
                        }
                        //SerializationUtility.SerializePlantedFlag(flag1)
                    }
                    else
                    {
                        SerializedLocation loc = new SerializedLocation();
                        loc.LocationType = LocationType.Orbit;
                        var orbit = new SerializedKeplerOrbit();
                        var body = Game.UniverseModel.FindCelestialBodyByName(CelestialBodySelect.Get());
                        if (body == null)
                        {
                            return;
                        }

                        orbit.referenceBodyGuid = body?.Guid;
                        if (orbit.referenceBodyGuid == null)
                        {
                            return;
                        }

                        orbit.eccentricity = 0;
                        orbit.semiMajorAxis = body.radius + 1000 * SMAxis;
                        orbit.eccentricity = Eccen;
                        orbit.inclination = Inclination;

                        loc.serializedOrbit = orbit;
                        try
                        {
                            KSPUtils.CreateVessel(selected.Value.assembly, Vector3.right, loc, populateVessel,
                                vesselName);
                        }
                        catch (Exception e)
                        {
                            error = e.Message + "\n Contact mod author";
                            Log(e.ToString());
                        }
                    }

                    if (vesselName != "")
                    {
                        vesselName += "(copy)";
                    }
                }
            }
            else
            {
                GUIHelper.Label("No part selected");
            }

            if (PositionType.Get().Equals("Local"))
            {
                offsetSpawn = GUIHelper.Vec3TextArea("Offset", "offset");
            }
            else if (PositionType.Get().Equals("At flag"))
            {
                if (selectedFlag.HasValue)
                {
                    GUIHelper.Label($"Selected flag: {selectedFlag.Value.Name}");
                }

                if (GUIHelper.Button("Load flags"))
                {
                    flags.Clear();
                    var playerID = this.Game.LocalPlayer.PlayerId;
                    var _serializedPlantedFlags = new List<SerializedFlag>();
                    foreach (SimulationObjectModel flag1 in (IEnumerable<SimulationObjectModel>) GameManager.Instance.Game.SpaceSimulation.GetAllSimulationObjectsWithComponent<FlagComponent>())
                    {
                        FlagComponent component = flag1.FindComponent<FlagComponent>();
                        bool flag2 = playerID == (byte) 0;
                        SimulationObjectModel simulationObject = component.SimulationObject;
                        bool flag3 = simulationObject != null && simulationObject.IsAuthorizedBy(playerID);
                        if (flag2 | flag3)
                        {
                            flags.Add((flag1.Name,flag1));
                        }
                            //_serializedPlantedFlags.Add();
                    }
                }
                
                GUILayout.BeginVertical(boxStyle);
                scrollPos = GUILayout.BeginScrollView(scrollPos, false, true,GUIHelper.GetSkin().horizontalScrollbar,GUIHelper.GetSkin().verticalScrollbar, GUILayout.Height(200),
                    GUILayout.Width(windowWidth));

                foreach (var flag in flags)
                {
                    if (GUIHelper.Button(flag.Name))
                    {
                        selectedFlag = flag;
                    }
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                
            
            }
            else
            {
                if (bodies == null || bodies.Count == 0)
                {
                    bodies = GameManager.Instance.Game.SpaceSimulation.GetBodyNameKeys().ToList();
                    CelestialBodySelect = new UIUtils.ToggleGroup(bodies.ToArray());
                    CelestialBodySelect.Horizontal = false;
                    CelestialBodySelect.ElementsPerStrip = 2;
                }

                GUILayout.BeginVertical(boxStyle);
                GUIHelper.Label("Body:");
                celestialScrollPos = GUILayout.BeginScrollView(celestialScrollPos, false, true,GUIHelper.GetSkin().horizontalScrollbar,GUIHelper.GetSkin().verticalScrollbar, GUILayout.Height(150),
                    GUILayout.Width(windowWidth));
                CelestialBodySelect.Display(GUIHelper);
                GUILayout.EndScrollView();
                SMAxis = GUIHelper.FloatTextArea("Average Orbit Height (SM Axis):", "SMAxis", false);
                Inclination = GUIHelper.FloatTextArea("Orbit tilt (Inclination):", "Inclination", false);
                Eccen = GUIHelper.FloatTextArea("Orbit 'Ovalness' (Eccentricity):", "Eccen", false);
                GUILayout.EndVertical();
            }

            PositionType.Display(GUIHelper);
            if (VesselType.Display(GUIHelper) == "Vessel")
            {
                if (GUIHelper.Button("Load crafts"))
                {
                    KSPUtils.GetAssemblies(assemblies);
                }

                GUILayout.BeginVertical(boxStyle);
                scrollPos = GUILayout.BeginScrollView(scrollPos, false, true,GUIHelper.GetSkin().horizontalScrollbar,GUIHelper.GetSkin().verticalScrollbar, GUILayout.Height(200),
                    GUILayout.Width(windowWidth));

                foreach (var body in assemblies)
                {
                    if (GUIHelper.Button(body.Name))
                    {
                        selected = (body.Name, KSPUtils.GetAssembly(body.assembly));
                    }
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUIHelper.Label("Search:");
                searchString = GUILayout.TextArea(searchString);
                GUILayout.EndHorizontal();
                var parts = this.Game.Parts.AllParts();
                scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Height(200),
                    GUILayout.Width(windowWidth));

                string santised = searchString.ToLower().Trim();
                foreach (var part in parts)
                {
                    if (searchString != "" && !part.data.partName.ToLower().Contains(santised))
                    {
                        continue;
                    }

                    if (GUIHelper.Button(part.data.partName))
                    {
                        selected = (part.data.partName, KSPUtils.GetSinglePartAssembly(part.data));
                    }
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }
    }
}