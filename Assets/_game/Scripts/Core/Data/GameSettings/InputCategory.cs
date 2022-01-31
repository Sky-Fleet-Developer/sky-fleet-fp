using System.Collections.Generic;
using System.Linq;

namespace Core.Data.GameSettings
{
    public class InputCategory : INameSetting
    {
        public string Name { get; set; }

        public List<ElementControlSetting> Elements { get; private set; }

        public InputCategory()
        {
            Elements = new List<ElementControlSetting>();
        }

        public InputAxis AddAxisInput(string name)
        {
            InputAxis axis = new InputAxis();
            axis.Name = name;
            Elements.Add(axis);
            return axis;
        }

        public InputButtons AddInputButtons(string name)
        {
            InputButtons buttons = new InputButtons();
            buttons.Name = name;
            Elements.Add(buttons);
            return buttons;
        }

        public ToggleSetting AddToggle(string name)
        {
            ToggleSetting toggle = new ToggleSetting();
            toggle.Name = name;
            Elements.Add(toggle);
            return toggle;
        }

        public T FindElement<T>(string name) where T : ElementControlSetting
        {
            return (T)Elements.Where(x => { return x.Name == name && x.GetType() == typeof(T); }).FirstOrDefault();
        }
    }
}