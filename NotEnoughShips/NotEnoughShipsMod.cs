using KSP.Game;
using KSP.OAB;
using KSP.Sim;
using KSP.Sim.impl;
using SpaceWarp.API.Logging;
using SpaceWarp.API.Mods;
using UnityEngine;
using UnityEngine.SceneManagement;
using static NotEnoughShips.NotEnoughShipsMod;
using Random = System.Random;

namespace NotEnoughShips
{
    [MainMod]
    public class NotEnoughShipsMod : Mod
    {
        public static BaseModLogger ModLogger;
        public override void OnInitialized()
        {
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
            "Local", "Global"
        });
        UIUtils.ToggleGroup CelestialBodySelect = new UIUtils.ToggleGroup(new[]
        {
            "K", "Global"
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

        String error = "";

        void Log(String s)
        {
            NotEnoughShipsMod.ModLogger.Info(s);
        }

        void Start()
        {
            Log("started");
            DontDestroyOnLoad(this.gameObject);

            windowRect = new Rect((Screen.width * 0.85f)-(windowWidth / 2), (Screen.height / 2)-(windowHeight / 2), 0, 0);

        }
        void Update()
        {

            if(Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.J))
                displayWindow = !displayWindow;

            if(Game.ViewController==null)
            {
                return;
            }
            var component = Game.ViewController.GetActiveVehicle(true)?.GetSimVessel();
            if(component==null)
            {
                return;
            }
            vb = this.Game.ViewController.GetBehaviorIfLoaded(component);
            RigidbodyBehavior rb;
            if(vb && (rb = vb.GetComponent<RigidbodyBehavior>()) && (rb.Model!=null))
            {
                rbc = rb.Model;
                var coordinateSystem = TransformFrame.GetNonInternalCoordinateSystem(rbc.transform.parent);
                var spawnLoc = coordinateSystem!=null
                    ? (Vector3) (coordinateSystem.ToLocalVector(new Vector(coordinateSystem, offsetSpawn)))
                    : offsetSpawn;
                this.transform.position = vb.transform.position+spawnLoc;
            }
            GetComponent<MeshRenderer>().enabled = (PositionType.Get()=="Local") && displayWindow;
        }
        private Rect windowRect;
        void OnGUI()
        {
            if(displayWindow)
            {
                windowRect = GUILayout.Window(
                    GUIUtility.GetControlID(FocusType.Passive),
                    windowRect,
                    DrawGUI,
                    "Spawn vessel (Alt - J to toggle)",
                    GUILayout.Height(0),
                    GUILayout.Width(350));
            }
        }
        private static GUIStyle boxStyle;
        Vector3 offsetSpawn = new Vector3();
        String ox = "0", oy = "0", oz = "0";
        String SMAxis = "100", Eccen = "0", Inclination = "0";
        String vesselName = "";
        Vector2 scrollPos = new Vector2();
        Vector2 celestialScrollPos = new Vector2();
        String searchString = "";
        private void DrawGUI(int windowID)
        {
            boxStyle = GUI.skin.GetStyle("Box");
            GUILayout.BeginVertical();
            String errorCheck = "";
            if(!this.Game.Parts.IsDataLoaded)
            {
                errorCheck = "Game hasnt loaded its parts yet";
            }
            else if(!this.Game.SessionManager.IsSPCampaign())
            {
                errorCheck = "Session isn't a single player campaign";
            }
            if(errorCheck!="")
            {
                GUILayout.Label(errorCheck);
                GUILayout.EndVertical();
                return;
            }
            if(error!="")
            {
                var redtext = new GUIStyle(GUI.skin.label);
                redtext.normal.textColor = Color.red;
                GUILayout.Label("Error: "+error, redtext);
            }

            if(selected!=null)
            {
                GUILayout.Label($"Selected {selected.Value.Name}");
                populateVessel = GUILayout.Toggle(populateVessel, "Populate vessel: "+populateVessel);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Name (leave empty for auto)");
                vesselName = GUILayout.TextArea(vesselName);
                GUILayout.EndHorizontal();
                if(GUILayout.Button("Spawn"))
                {
                    if(PositionType.Get().Equals("Local"))
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
                    else
                    {
                        SerializedLocation loc = new SerializedLocation();
                        loc.LocationType = LocationType.Orbit;
                        var orbit = new SerializedKeplerOrbit();
                        var body = Game.UniverseModel.FindCelestialBodyByName(CelestialBodySelect.Get());
                        if(body==null)
                        {
                            return;
                        }
                        orbit.referenceBodyGuid = body?.Guid;
                        if(orbit.referenceBodyGuid==null)
                        {
                            return;
                        }
                        orbit.eccentricity = 0;
                        float t;
                        float.TryParse(SMAxis, out t);
                        orbit.semiMajorAxis = body.radius+1000 * t;
                        float.TryParse(Eccen, out t);
                        orbit.eccentricity = t;
                        float.TryParse(Inclination, out t);
                        orbit.inclination = t;

                        loc.serializedOrbit = orbit;
                        try
                        {
                            KSPUtils.CreateVessel(selected.Value.assembly, offsetSpawn, loc, populateVessel,
                                vesselName);
                        }catch (Exception e)
                        {
                            error = e.Message + "\n Contact mod author";
                            Log(e.ToString());
                        }
                    }
                    if(vesselName!="")
                    {
                        vesselName += "(copy)";
                    }
                }
            }
            else
            {
                GUILayout.Label("No part selected");
            }
            if(PositionType.Get().Equals("Local"))
            {
                GUILayout.BeginHorizontal();
                float x, y, z;
                GUILayout.Label("X:");
                ox = GUILayout.TextArea(ox);
                float.TryParse(ox, out x);
                GUILayout.Label("  Y:");
                oy = GUILayout.TextArea(oy);
                float.TryParse(oy, out y);
                GUILayout.Label("  Z:");
                oz = GUILayout.TextArea(oz);
                float.TryParse(oz, out z);
                offsetSpawn = new Vector3(x, y, z);
                GUILayout.EndHorizontal();
            }
            else
            {
                if(bodies==null || bodies.Count==0)
                {
                    bodies = GameManager.Instance.Game.SpaceSimulation.GetBodyNameKeys().ToList();
                    CelestialBodySelect = new UIUtils.ToggleGroup(bodies.ToArray());
                    CelestialBodySelect.Horizontal = false;
                    CelestialBodySelect.ElementsPerStrip = 2;
                }
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label("Body:");
                celestialScrollPos = GUILayout.BeginScrollView(celestialScrollPos, false, true, GUILayout.Height(150),
                GUILayout.Width(windowWidth));
                CelestialBodySelect.Display();
                GUILayout.EndScrollView();
                GUILayout.Label("Average Orbit Height (SM Axis):");
                SMAxis = GUILayout.TextArea(SMAxis);
                GUILayout.Label("Orbit tilt (Inclination):");
                Inclination = GUILayout.TextArea(Inclination);
                GUILayout.Label("Orbit 'Ovalness' (Eccentricity):");
                Eccen = GUILayout.TextArea(Eccen);
                GUILayout.EndVertical();
            }
            PositionType.Display();
            if(VesselType.Display()=="Vessel")
            {
                if(GUILayout.Button("Load crafts"))
                {
                    KSPUtils.GetAssemblies(assemblies);
                }

                GUILayout.BeginVertical(boxStyle);
                scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Height(200),
                    GUILayout.Width(windowWidth));

                foreach (var body in assemblies)
                {
                    if(GUILayout.Button(body.Name))
                    {
                        selected = (body.Name,KSPUtils.GetAssembly(body.assembly));
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Search:");
                searchString = GUILayout.TextArea(searchString);
                GUILayout.EndHorizontal();
                var parts = this.Game.Parts.AllParts();
                scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Height(200),
                    GUILayout.Width(windowWidth));

                string santised = searchString.ToLower().Trim();
                foreach (var part in parts)
                {
                    if(searchString!="" && !part.data.partName.ToLower().Contains(santised))
                    {
                        continue;
                    }
                    if(GUILayout.Button(part.data.partName))
                    {
                        selected = (part.data.partName,KSPUtils.GetSinglePartAssembly(part.data));
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }
    }
}
