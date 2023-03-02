using UnityEngine;

namespace NotEnoughShips;

public static class UIUtils
{
    public class GUI
    {
        private Dictionary<String, object> values = new Dictionary<string, object>();
        private GUISkin skin;
        private GUIStyle errorLabel;

        public GUI(GUISkin skin)
        {
            this.skin = skin;
            errorLabel = new GUIStyle(this.skin.label);
            
            Texture2D t = new Texture2D(16, 16, TextureFormat.ARGB32, false);
            var fillColorArray =  t.GetPixels();
            Color fillColor = new Color(1.0f, 0.4f, 0.3f, 0.8f);
            for(var i = 0; i < fillColorArray.Length; ++i)
            {
                fillColorArray[i] = fillColor;
            }
  
            t.SetPixels( fillColorArray );
            t.Apply();
            
            errorLabel.normal.background = t;
            errorLabel.normal.textColor = new Color(0.08f, 0.08f, 0.1f, 1);

        }

        public void Label(string label)
        {
            GUILayout.Label(label,skin.label);
        }
        public void Label(object label)
        {
            GUILayout.Label(label.ToString(),skin.label);
        }
        
        public void ErrorLabel(object label)
        {
            GUILayout.BeginHorizontal(errorLabel);
            GUILayout.Label(label.ToString(),errorLabel);
            GUILayout.EndHorizontal();
        }
        public bool Button(object label)
        {
            return GUILayout.Button(label.ToString(),skin.button);
        }

        public void CheckMakeField(string f, string def = "")
        {
            if (!values.ContainsKey(f))
            {
                values.Add(f,def);
            }
        }
        public void CheckMakeToggleField(string f, bool def = false)
        {
            if (!values.ContainsKey(f))
            {
                values.Add(f,def);
            }
        }
        public bool Toggle(string label, string fieldName)
        {
            CheckMakeToggleField(fieldName);
            return (bool)(values[fieldName] = GUILayout.Toggle((bool)values[fieldName],label, skin.toggle));
        }
        public string TextArea(string label, string fieldName, bool horz = true)
        {
            CheckMakeField(fieldName);
            if(horz) {GUILayout.BeginHorizontal();}else {GUILayout.BeginVertical();}
            Label(label);
            var f = (values[fieldName] = GUILayout.TextArea(values[fieldName].ToString(),skin.textArea)).ToString();
            if(horz) {GUILayout.EndHorizontal();}else {GUILayout.EndVertical();}
            return f;
        }
        
        public float FloatTextArea(string label, string fieldName, bool horz = true)
        {
            CheckMakeField(fieldName,"0");
            GUILayout.BeginVertical();
            if (!horz)
            {
                GUILayout.Label(label);
            }

            float f;
            if (!float.TryParse(values[fieldName].ToString(), out f))
            {
                ErrorLabel($"'{values[fieldName].ToString()}' is not a number");
            }

            if (horz)
            {
                TextArea(label, fieldName);
            }
            else
            {
                (values[fieldName] = GUILayout.TextArea(values[fieldName].ToString(),skin.textArea)).ToString();
            }

            GUILayout.EndVertical();
            return f;
        }
        
        public Vector3 Vec3TextArea(string label, string fieldName)
        {
            CheckMakeField(fieldName+"x","0");
            CheckMakeField(fieldName+"y","0");
            CheckMakeField(fieldName+"z","0");
            Label(label);
            GUILayout.BeginVertical();
            float f;
            if (!float.TryParse(values[fieldName+"x"].ToString(), out f))
            {
                ErrorLabel($"'{values[fieldName+"x"].ToString()}' is not a number");
            }
            if (!float.TryParse(values[fieldName+"y"].ToString(), out f))
            {
                ErrorLabel($"'{values[fieldName+"y"].ToString()}' is not a number");
            }
            if (!float.TryParse(values[fieldName+"z"].ToString(), out f))
            {
                ErrorLabel($"'{values[fieldName+"z"].ToString()}' is not a number");
            }
            GUILayout.BeginHorizontal();
            TextArea("x:", fieldName+"x");
            TextArea("y:", fieldName+"y");
            TextArea("z:", fieldName+"z");
            float x, y, z;
            float.TryParse(values[fieldName + "x"].ToString(), out x);
            float.TryParse(values[fieldName + "y"].ToString(), out y);
            float.TryParse(values[fieldName + "z"].ToString(), out z);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return new Vector3(x,y,z);
        }

        public GUISkin GetSkin()
        {
            return skin;
        }
    }

    public class ToggleGroup
    {
        bool[] selection;
        String[] options;
        int selectedIndex = 0;
        public int ElementsPerStrip = 1;
        public bool Horizontal = true;

        public ToggleGroup(string[] options)
        {
            this.options = (string[])options.Clone();
            selection = new bool[options.Length];
            selection[0] = true;
            selectedIndex = 0;
        }

        public string Display(GUI gui)
        {
            if(Horizontal)
            {
                GUILayout.BeginHorizontal();
            }
            else
            {
                GUILayout.BeginVertical();
            }

            if(ElementsPerStrip==1)
            {
                for (int i = 0;i < options.Length;i++)
                {
                    selection[i] = GUILayout.Toggle(selection[i], options[i],gui.GetSkin().toggle);
                    if(selection[i])
                    {
                        selection[selectedIndex] = false;
                        selectedIndex = i;
                        selection[i] = true;
                    }
                }
            }
            else
            {
                int i = 0;
                while (i < options.Length)
                {
                    if(!Horizontal)
                    {
                        GUILayout.BeginHorizontal();
                    }
                    else
                    {
                        GUILayout.BeginVertical();
                    }
                    for (int z = 0; z < ElementsPerStrip; z++, i++)
                    {
                        if(i>=options.Length)
                        {
                            break;
                        }
                        selection[i] = GUILayout.Toggle(selection[i], options[i],gui.GetSkin().toggle);
                        if(selection[i])
                        {
                            selection[selectedIndex] = false;
                            selectedIndex = i;
                            selection[i] = true;
                        }
                    }
                    if(!Horizontal)
                    {
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        GUILayout.EndVertical();
                    }
                }
            }

            if(Horizontal)
            {
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.EndVertical();
            }
            return options[selectedIndex];
        }

        public String Get()
        {
            return options[selectedIndex];
        }

    }
}
