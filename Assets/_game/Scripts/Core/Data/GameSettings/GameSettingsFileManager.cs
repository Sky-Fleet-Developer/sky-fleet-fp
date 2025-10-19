using System.IO;
using System.Linq;
using Core.ContentSerializer;
using Paterns.AbstractFactory;
using UnityEngine;

namespace Core.Data.GameSettings
{
    public static class GameSettingsFileManager
    {
        private static void CorrectDirectory()
        {
            string path = PathStorage.GetPathToSettingDirectory();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void LoadSetting(ControlSettings settings, string path)
        {
            CorrectDirectory();

            using (FileStream file = File.Open(path, FileMode.Open))
            {
                int count = file.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    InputCategory inputCategory = ReadCategory(file);
                    SetCategoryToControlSettings(settings, inputCategory);
                }
            }
        }

        private static void SetCategoryToControlSettings(ControlSettings settings, InputCategory inputCategory)
        {
            InputCategory originalInputCategory = settings.Categories.Where(x => { return x.Name == inputCategory.Name; }).FirstOrDefault();
            if(originalInputCategory != null)
            {
                for(int i = 0; i < inputCategory.Elements.Count; i++)
                {
                    int index = -1;
                    for(int i2 = 0; i2 < originalInputCategory.Elements.Count; i2++)
                    {
                        if(originalInputCategory.Elements[i2].Name == inputCategory.Elements[i].Name)
                        {
                            index = i2;
                            break;
                        }
                    }

                    if(index != -1)
                    {
                        originalInputCategory.Elements[index] = inputCategory.Elements[i];
                    }
                }
            }
        }

        public static void SaveSetting(ControlSettings settings, string path)
        {
            CorrectDirectory();
            using (FileStream file = File.Open(path, FileMode.OpenOrCreate))
            {
                file.WriteInt(settings.Categories.Count);
                for (int i = 0; i < settings.Categories.Count; i++)
                {
                    WriteCategory(file, settings.Categories[i]);
                }
            }
        }

        private static void WriteCategory(Stream streamOpen, InputCategory inputCategory)
        {
            streamOpen.WriteString(inputCategory.Name);
            streamOpen.WriteInt(inputCategory.Elements.Count);
            FactoryOptionSave factory = new FactoryOptionSave();
            for (int i = 0; i < inputCategory.Elements.Count; i++)
            {
                factory.Generate(new SettingDefine() { Element = inputCategory.Elements[i], StreamOpen = streamOpen });
            }
        }

        private static InputCategory ReadCategory(Stream streamOpen)
        {
            InputCategory inputCategory = new InputCategory();
            inputCategory.Name = streamOpen.ReadString();
            int countInput = streamOpen.ReadInt();
            for (int i = 0; i < countInput; i++)
            {
                TypeSettingElement type = (TypeSettingElement)streamOpen.ReadByte();
                ElementControlSetting element = new OptionLoadFactory().Generate(new SettingDefine() { TypeElement = type, StreamOpen = streamOpen});
                inputCategory.Elements.Add(element);
            }

            return inputCategory;
        }

        private enum TypeSettingElement : byte
        {
            InputButtons = 0,
            InputAxis = 1,
            Toogle = 2,
        }

        private struct SettingDefine
        {
            public TypeSettingElement TypeElement;
            public ElementControlSetting Element;
            public Stream StreamOpen;
        }

        private abstract class GeneratorSettingElement : Generator<SettingDefine, ElementControlSetting>
        {
            protected TypeSettingElement typeElement;

            public override bool CheckDefine(SettingDefine define)
            {
                return typeElement == define.TypeElement;
            }
        }

        private class FactoryOptionSave : AbstractFactory<SettingDefine, ElementControlSetting>
        {
            public FactoryOptionSave()
            {
                RegisterNewType(new SaveToggleSetting());
                RegisterNewType(new SaveInputButtonsSetting());
                RegisterNewType(new SaveInputAxisSetting());
            }

            protected override ElementControlSetting GetDefault()
            {
                return null;
            }
        }

        private class OptionLoadFactory : AbstractFactory<SettingDefine, ElementControlSetting>
        {

            public OptionLoadFactory()
            {
                RegisterNewType(new LoadToggleSetting());
                RegisterNewType(new LoadInputButtonsSetting());
                RegisterNewType(new LoadInputAxisSetting());
            }

            protected override ElementControlSetting GetDefault()
            {
                return null;
            }
        }

        //Save
        private class SaveToggleSetting : GeneratorSettingElement
        {
            public override bool CheckDefine(SettingDefine define)
            {
                return define.Element.GetType() == typeof(ToggleSetting);
            }

            public override ElementControlSetting Generate(SettingDefine define)
            {
                define.StreamOpen.WriteByte((byte)TypeSettingElement.Toogle);
                define.StreamOpen.WriteString(define.Element.Name);
                ToggleSetting toggle = (ToggleSetting)define.Element;
                if (toggle.IsOn)
                {
                    define.StreamOpen.WriteByte(255);
                }
                else
                {
                    define.StreamOpen.WriteByte(0);
                }
                return define.Element;
            }
        }

        private class SaveInputButtonsSetting : GeneratorSettingElement
        {

            public override bool CheckDefine(SettingDefine define)
            {
                return define.Element.GetType() == typeof(InputButtons);
            }

            public override ElementControlSetting Generate(SettingDefine define)
            {
                define.StreamOpen.WriteByte((byte)TypeSettingElement.InputButtons);
                define.StreamOpen.WriteString(define.Element.Name);
                InputButtons inputButtons = (InputButtons)define.Element;
                define.StreamOpen.WriteByte((byte)inputButtons.Keys.Count);
                for (int i = 0; i < inputButtons.Keys.Count; i++)
                {
                    define.StreamOpen.WriteByte((byte)inputButtons.Keys[i].KeyCodes.Length);
                    for (int i2 = 0; i2 < inputButtons.Keys[i].KeyCodes.Length; i2++)
                    {
                        define.StreamOpen.WriteShort((short)inputButtons.Keys[i].KeyCodes[i2]);
                    }
                }
                return define.Element;
            }
        }

        private class SaveInputAxisSetting : GeneratorSettingElement
        {

            public override bool CheckDefine(SettingDefine define)
            {
                return define.Element.GetType() == typeof(InputAxis);
            }

            public override ElementControlSetting Generate(SettingDefine define)
            {
                define.StreamOpen.WriteByte((byte)TypeSettingElement.InputAxis);
                define.StreamOpen.WriteString(define.Element.Name);
                InputAxis inputAxis = (InputAxis)define.Element;
                define.StreamOpen.WriteString(inputAxis.GetAxis().Name);
                define.StreamOpen.WriteBool(inputAxis.GetAxis().Inverse);
                define.StreamOpen.WriteFloat(inputAxis.GetAxis().Multiply);
                return define.Element;
            }
        }


        //Load
        private class LoadToggleSetting : GeneratorSettingElement
        {
            public LoadToggleSetting()
            {
                typeElement = TypeSettingElement.Toogle;
            }

            public override ElementControlSetting Generate(SettingDefine define)
            {
                ToggleSetting toggle = new ToggleSetting();
                toggle.Name = define.StreamOpen.ReadString();
                int isOn = define.StreamOpen.ReadByte();
                if (isOn > 0)
                {
                    toggle.IsOn = true;
                }
                else
                {
                    toggle.IsOn = false;
                }

                return toggle;
            }
        }

        private class LoadInputButtonsSetting : GeneratorSettingElement
        {
            public LoadInputButtonsSetting()
            {
                typeElement = TypeSettingElement.InputButtons;
            }

            public override ElementControlSetting Generate(SettingDefine define)
            {
                InputButtons inputButtons = new InputButtons();
                inputButtons.Name = define.StreamOpen.ReadString();
                int countInput = define.StreamOpen.ReadByte();
                for (int i = 0; i < countInput; i++)
                {
                    ButtonCodes buttons = new ButtonCodes();
                    int countButtons = define.StreamOpen.ReadByte();
                    buttons.KeyCodes = new KeyCode[countButtons];
                    for (int i2 = 0; i2 < countButtons; i2++)
                    {
                        buttons.KeyCodes[i2] = (KeyCode)define.StreamOpen.ReadShort();
                    }
                    inputButtons.AddKey(buttons);

                }
                return inputButtons;
            }
        }

        private class LoadInputAxisSetting : GeneratorSettingElement
        {
            public LoadInputAxisSetting()
            {
                typeElement = TypeSettingElement.InputAxis;
            }

            public override ElementControlSetting Generate(SettingDefine define)
            {
                InputAxis inputAxis = new InputAxis();
                inputAxis.Name = define.StreamOpen.ReadString();
                AxisCode code = new AxisCode(define.StreamOpen.ReadString());
                code.Inverse = define.StreamOpen.ReadBool();
                code.Multiply = define.StreamOpen.ReadFloat();
                inputAxis.SetAxis(code);
                
                return inputAxis;
            }
        }
    }
}