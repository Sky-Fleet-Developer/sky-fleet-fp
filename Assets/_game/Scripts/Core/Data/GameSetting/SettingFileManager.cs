using System.IO;
using System.Linq;
using System.Collections.Generic;
using Paterns.AbstractFactory;

using Core.ContentSerializer;
using UnityEngine;

namespace Core.GameSetting
{

    public static class GameSettingFileManager
    {
        static private void CorrectDirectory()
        {
            string path = PathStorage.GetPathToSettingDirectory();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        static public bool LoadSetting(Setting setting, string path)
        {
            CorrectDirectory();
            if (File.Exists(path))
            {
                using (FileStream file = File.Open(path, FileMode.Open))
                {
                    FactoryOptionLoad factory = new FactoryOptionLoad();
                    ExtensionStream extension = new ExtensionStream();
                    int count = extension.ReadInt(file);
                    for(int i = 0; i < count; i++)
                    {
                        ControlSetting.CategoryInputs category = ReadCategory(file);
                        SetCategoryToControlSetting(setting.Control, category);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        static private void SetCategoryToControlSetting(ControlSetting control, ControlSetting.CategoryInputs category)
        {
            ControlSetting.CategoryInputs originalCategory = control.Categoryes.Where(x => { return x.Name == category.Name; }).FirstOrDefault();
            if(originalCategory != null)
            {
                for(int i = 0; i < category.Elements.Count; i++)
                {
                    int index = -1;
                    for(int i2 = 0; i2 < originalCategory.Elements.Count; i2++)
                    {
                        if(originalCategory.Elements[i2].Name == category.Elements[i].Name)
                        {
                            index = i2;
                            break;
                        }
                    }

                    if(index != -1)
                    {
                        originalCategory.Elements[index] = category.Elements[i];
                    }
                }
            }
        }

        static public bool SaveSetting(Setting setting, string path)
        {
            CorrectDirectory();
            using (FileStream file = File.Open(path, FileMode.OpenOrCreate))
            {
                ControlSetting control = setting.Control;
                ExtensionStream extension = new ExtensionStream();
                extension.WriteInt(control.Categoryes.Count, file);
                for (int i = 0; i < control.Categoryes.Count; i++)
                {
                    WriteCaterogry(file, control.Categoryes[i]);
                }
            }
            return true;
        }

        private static void WriteCaterogry(Stream streamOpen, ControlSetting.CategoryInputs category)
        {
            ExtensionStream extension = new ExtensionStream();
            extension.WriteString(category.Name, streamOpen);
            extension.WriteInt(category.Elements.Count, streamOpen);
            FactoryOptionSave factory = new FactoryOptionSave();
            for (int i = 0; i < category.Elements.Count; i++)
            {
                factory.Generate(new SettingDefine() { Element = category.Elements[i], StreamOpen = streamOpen });
            }
        }

        private static ControlSetting.CategoryInputs ReadCategory(Stream streamOpen)
        {
            ControlSetting.CategoryInputs category = new ControlSetting.CategoryInputs();
            ExtensionStream extension = new ExtensionStream();
            FactoryOptionLoad factory = new FactoryOptionLoad();
            category.Name = extension.ReadString(streamOpen);
            int countInput = extension.ReadInt(streamOpen);
            for (int i = 0; i < countInput; i++)
            {
                TypeSettingElement type = (TypeSettingElement)extension.ReadByte(streamOpen);
                ElementControlSetting element = factory.Generate(new SettingDefine() { TypeElement = type, StreamOpen = streamOpen});
                category.Elements.Add(element);
            }

            return category;
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

        private class FactoryOptionLoad : AbstractFactory<SettingDefine, ElementControlSetting>
        {

            public FactoryOptionLoad()
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