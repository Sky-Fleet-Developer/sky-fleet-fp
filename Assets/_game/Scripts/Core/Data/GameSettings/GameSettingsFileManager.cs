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
                ExtensionStream extension = new ExtensionStream();
                int count = extension.ReadInt(file);
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
                ExtensionStream extension = new ExtensionStream();
                extension.WriteInt(settings.Categories.Count, file);
                for (int i = 0; i < settings.Categories.Count; i++)
                {
                    WriteCategory(file, settings.Categories[i]);
                }
            }
        }

        private static void WriteCategory(Stream streamOpen, InputCategory inputCategory)
        {
            ExtensionStream extension = new ExtensionStream();
            extension.WriteString(inputCategory.Name, streamOpen);
            extension.WriteInt(inputCategory.Elements.Count, streamOpen);
            FactoryOptionSave factory = new FactoryOptionSave();
            for (int i = 0; i < inputCategory.Elements.Count; i++)
            {
                factory.Generate(new SettingDefine() { Element = inputCategory.Elements[i], StreamOpen = streamOpen });
            }
        }

        private static InputCategory ReadCategory(Stream streamOpen)
        {
            InputCategory inputCategory = new InputCategory();
            ExtensionStream extension = new ExtensionStream();
            OptionLoadFactory optionLoadFactory = new OptionLoadFactory();
            inputCategory.Name = extension.ReadString(streamOpen);
            int countInput = extension.ReadInt(streamOpen);
            for (int i = 0; i < countInput; i++)
            {
                TypeSettingElement type = (TypeSettingElement)extension.ReadByte(streamOpen);
                ElementControlSetting element = optionLoadFactory.Generate(new SettingDefine() { TypeElement = type, StreamOpen = streamOpen});
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
                ExtensionStream extension = new ExtensionStream();
                extension.WriteByte((byte)TypeSettingElement.Toogle, define.StreamOpen);
                extension.WriteString(define.Element.Name, define.StreamOpen);
                ToggleSetting toggle = (ToggleSetting)define.Element;
                if (toggle.IsOn)
                {
                    extension.WriteByte(255, define.StreamOpen);
                }
                else
                {
                    extension.WriteByte(0, define.StreamOpen);
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
                ExtensionStream extension = new ExtensionStream();
                extension.WriteByte((byte)TypeSettingElement.InputButtons, define.StreamOpen);
                extension.WriteString(define.Element.Name, define.StreamOpen);
                InputButtons inputButtons = (InputButtons)define.Element;
                extension.WriteByte((byte)inputButtons.Keys.Count, define.StreamOpen);
                for (int i = 0; i < inputButtons.Keys.Count; i++)
                {
                    extension.WriteByte((byte)inputButtons.Keys[i].KeyCodes.Length, define.StreamOpen);
                    for (int i2 = 0; i2 < inputButtons.Keys[i].KeyCodes.Length; i2++)
                    {
                        extension.WriteShort((short)inputButtons.Keys[i].KeyCodes[i2], define.StreamOpen);
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
                ExtensionStream extension = new ExtensionStream();
                extension.WriteByte((byte)TypeSettingElement.InputAxis, define.StreamOpen);
                extension.WriteString(define.Element.Name, define.StreamOpen);
                InputAxis inputAxis = (InputAxis)define.Element;
                extension.WriteString(inputAxis.GetAxis().Name, define.StreamOpen);
                extension.WriteBool(inputAxis.GetAxis().Inverse, define.StreamOpen);
                extension.WriteFloat(inputAxis.GetAxis().Multiply, define.StreamOpen);
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
                ExtensionStream extension = new ExtensionStream();
                toggle.Name = extension.ReadString(define.StreamOpen);
                byte isOn = extension.ReadByte(define.StreamOpen);
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
                ExtensionStream extension = new ExtensionStream();
                inputButtons.Name = extension.ReadString(define.StreamOpen);
                byte countInput = extension.ReadByte(define.StreamOpen);
                for (int i = 0; i < countInput; i++)
                {
                    ButtonCodes buttons = new ButtonCodes();
                    byte countButtons = extension.ReadByte(define.StreamOpen);
                    buttons.KeyCodes = new KeyCode[countButtons];
                    for (int i2 = 0; i2 < countButtons; i2++)
                    {
                        buttons.KeyCodes[i2] = (KeyCode)extension.ReadShort(define.StreamOpen);
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
                ExtensionStream extension = new ExtensionStream();
                inputAxis.Name = extension.ReadString(define.StreamOpen);
                AxisCode code = new AxisCode(extension.ReadString(define.StreamOpen));
                code.Inverse = extension.ReadBool(define.StreamOpen);
                code.Multiply = extension.ReadFloat(define.StreamOpen);
                inputAxis.SetAxis(code);
                
                return inputAxis;
            }
        }
    }
}