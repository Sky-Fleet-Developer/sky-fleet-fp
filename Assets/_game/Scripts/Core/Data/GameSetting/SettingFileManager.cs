using System.IO;
using System.Linq;
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
            FactoryOptionLoad factory = new FactoryOptionLoad();
            CorrectDirectory();
            if (File.Exists(path))
            {
                using (FileStream file = File.Open(path, FileMode.Open))
                {
                    Setting res = factory.Generate(new SettingDefine() { Branch = OptionBranch.Control, SettingD = setting, StreamOpen = file });
                    if (res == null)
                    {
                        return false;
                    } 
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        static public bool SaveSetting(Setting setting, string path)
        {
            FactoryOptionSave factory = new FactoryOptionSave();
            CorrectDirectory();
            using (FileStream file = File.Open(path, FileMode.OpenOrCreate))
            {
                Setting res = factory.Generate(new SettingDefine() { Branch = OptionBranch.Control, SettingD = setting, StreamOpen = file });
                if (res == null)
                {
                    return false;
                }
            }
            return true;
        }

        private enum OptionBranch : byte
        {
            Control = 0
        }

        private struct SettingDefine
        {
            public OptionBranch Branch;
            public Setting SettingD;
            public Stream StreamOpen;
        }

        private abstract class GeneratorOption : Generator<SettingDefine, Setting>
        {
            protected OptionBranch typeWork;

            public override bool CheckDefine(SettingDefine define)
            {
                return typeWork == define.Branch;
            }
        }

        private class FactoryOptionSave : AbstractFactory<SettingDefine, Setting>
        {

            public FactoryOptionSave()
            {
                RegisterNewType(new SaveSettingControl());
            }

            protected override Setting GetDefault()
            {
                return null;
            }
        }

        private class FactoryOptionLoad : AbstractFactory<SettingDefine, Setting>
        {

            public FactoryOptionLoad()
            {
                RegisterNewType(new LoadSettingControl());
            }

            protected override Setting GetDefault()
            {
                return null;
            }
        }

        private class SaveSettingControl : GeneratorOption
        {
            ExtensionStream extensionStream = new ExtensionStream();

            public SaveSettingControl()
            {
                typeWork = OptionBranch.Control;
            }

            public override Setting Generate(SettingDefine define)
            {
                ControlSetting control = define.SettingD.Control;
                extensionStream.WriteInt(control.Categoryes.Count, define.StreamOpen);
                for (int i = 0; i < control.Categoryes.Count; i++)
                {
                    ControlSetting.CategoryInputs category = control.Categoryes[i];
                    extensionStream.WriteString(category.Name, define.StreamOpen);
                    extensionStream.WriteInt(category.Inputs.Count, define.StreamOpen);
                    for (int i2 = 0; i2 < category.Inputs.Count; i2++)
                    {
                        WriteInput(define.StreamOpen, category.Inputs[i2]);
                    }
                }
                return define.SettingD;
            }

            private void WriteInput(Stream streamOpen, InputAbstractType input)
            {
                extensionStream.WriteString(input.Name, streamOpen);
                extensionStream.WriteByte((byte)input.GetTypeInput(), streamOpen);
                if (input.GetTypeInput() == TypeInput.InputAxis)
                {
                    InputAxis axis = (InputAxis)(input);                 
                    extensionStream.WriteString(axis.GetAxis().Name, streamOpen);
                }
                else
                {
                    InputButtons buttons = (InputButtons)(input);
                    extensionStream.WriteInt(buttons.Keys.Count, streamOpen);
                    for (int i = 0; i < buttons.Keys.Count; i++)
                    {
                        extensionStream.WriteInt(buttons.Keys[i].KeyCodes.Length, streamOpen);
                        for (int i2 = 0; i2 < buttons.Keys[i].KeyCodes.Length; i2++)
                        {
                            extensionStream.WriteInt((int)buttons.Keys[i].KeyCodes[i2], streamOpen);
                        }
                    }
                }
            }
        }


        private class LoadSettingControl : GeneratorOption
        {
            ExtensionStream extensionStream = new ExtensionStream();

            public LoadSettingControl()
            {
                typeWork = OptionBranch.Control;
            }

            public override Setting Generate(SettingDefine define)
            {
                ControlSetting control = define.SettingD.Control;
                int countCategory = extensionStream.ReadInt(define.StreamOpen);
                for (int i = 0; i < countCategory; i++)
                {
                    string nameCategory = extensionStream.ReadString(define.StreamOpen);
                    ControlSetting.CategoryInputs category = define.SettingD.Control.Categoryes.Where(x => { return (x.Name == nameCategory); }).FirstOrDefault();
                    int countInputs = extensionStream.ReadInt(define.StreamOpen);
                    for (int i2 = 0; i2 < countInputs; i2++)
                    {                     
                        string nameInput = extensionStream.ReadString(define.StreamOpen);
                        if (category != null)
                        {
                            ReadInput(category.Inputs.Where(x => { return (x.Name == nameInput); }).FirstOrDefault(), define.StreamOpen);
                        }
                        else
                        {
                            ReadInput(null, define.StreamOpen);
                        }
                    }
                }
                return define.SettingD;
            }

            private void ReadInput(InputAbstractType input, Stream streamOpen)
            {
                byte t = extensionStream.ReadByte(streamOpen);
                if ((TypeInput)t == TypeInput.InputAxis)
                {
                    InputAxis axis = (InputAxis)input;
                    if(input != null)
                    {
                        axis.SetAxis(new AxisCode(extensionStream.ReadString(streamOpen)));
                    }
                    else
                    {
                        extensionStream.ReadString(streamOpen);
                    }
                }
                else
                {
                    if (input != null)
                    {
                        InputButtons buttons = (InputButtons)input;
                        int countKeysCont = extensionStream.ReadInt(streamOpen);
                        for (int i = 0; i < countKeysCont; i++)
                        {
                            int countKeys = extensionStream.ReadInt(streamOpen);
                            ButtonCodes butts = new ButtonCodes();
                            butts.KeyCodes = new KeyCode[countKeys];
                            for (int i2 = 0; i2 < countKeys; i2++)
                            {
                                butts.KeyCodes[i2] = (KeyCode)extensionStream.ReadInt(streamOpen);
                            }
                            buttons.Keys.Add(butts);
                        }
                    }
                    else
                    {
                        int countKeysCont = extensionStream.ReadInt(streamOpen);
                        for (int i = 0; i < countKeysCont; i++)
                        {
                            int countKeys = extensionStream.ReadInt(streamOpen);

                            for (int i2 = 0; i2 < countKeys; i2++)
                            {
                                extensionStream.ReadInt(streamOpen);
                            }
                        }
                    }
                }
            }


        }
    }

}