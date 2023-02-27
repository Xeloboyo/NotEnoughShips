using UnityEngine;

namespace NotEnoughShips;

public static class UIUtils
{
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

        public string Display()
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
                    selection[i] = GUILayout.Toggle(selection[i], options[i]);
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
                        selection[i] = GUILayout.Toggle(selection[i], options[i]);
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
