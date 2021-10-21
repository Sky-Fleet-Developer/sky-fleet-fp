using System.IO;

using Paterns.AbstractFactory;

using Core.ContentSerializer;

namespace Core.GameSetting
{

    public static class GameSettingFileManager
    {
        static public void LoadSetting(Setting setting, string path)
        {

        }

        static public void SaveSetting(Setting setting, string path)
        {

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
                for(int i = 0; i < control.Categoryes.Count; i++)
                {
                    ControlSetting.CategoryInputs category = control.Categoryes[i];
                    extensionStream.WriteString(category.Name, define.StreamOpen);
                    extensionStream.WriteInt(category.Inputs.Count, define.StreamOpen);
                    for(int i2 = 0; i2 < category.Inputs.Count;i2++)
                    {
                        WriteInput(define.StreamOpen, category.Inputs[i2]);
                    }
                }
                return define.SettingD;
            }

            private void WriteInput(Stream streamOpen, InputAbstractType input)
            {
                extensionStream.WriteByte((byte)input.GetTypeInput(), streamOpen);
                if (input.GetTypeInput() == TypeInput.InputAxis)
                {
                    InputAxis axis = (InputAxis)(input);
                    extensionStream.WriteString(axis.Name, streamOpen);
                }
                else
                {
                    InputButtons buttons = (InputButtons)(input);
                    extensionStream.WriteString(buttons.Name, streamOpen);
                    extensionStream.WriteInt(buttons.Keys.Count, streamOpen);
                    for (int i = 0; i < buttons.Keys.Count; i++)
                    {
                        extensionStream.WriteInt(buttons.Keys[i].Length, streamOpen);
                        for(int i2 = 0; i2 < buttons.Keys[i].Length; i2++)
                        {
                            extensionStream.WriteInt((int)buttons.Keys[i][i2].GetKeyCode(), streamOpen);
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
                /*extensionStream.WriteInt(control.Categoryes.Count, define.StreamOpen);
                for (int i = 0; i < control.Categoryes.Count; i++)
                {
                    ControlSetting.CategoryInputs category = control.Categoryes[i];
                    extensionStream.WriteString(category.Name, define.StreamOpen);
                    for (int i2 = 0; i2 < category.Inputs.Count; i2++)
                    {
                        WriteInput(define.StreamOpen, category.Inputs[i2]);
                    }
                }*/

                int countCategory = extensionStream.ReadInt(define.StreamOpen);
                for(int i = 0; i < countCategory; i++)
                {
                    string nameCategory = extensionStream.ReadString(define.StreamOpen);
                    ControlSetting.CategoryInputs category = define.SettingD.Control.AddCategory(nameCategory);
                    int countInputs = extensionStream.ReadInt(define.StreamOpen);
                }
                return define.SettingD;
            }

            private void ReadInput(Stream streamOpen, InputAbstractType input)
            {
                /*if (input.GetTypeInput() == TypeInput.InputAxis)
                {
                    InputAxis axis = (InputAxis)(input);
                    extensionStream.WriteString(axis.Name, streamOpen);
                }
                else
                {
                    InputButtons buttons = (InputButtons)(input);
                    extensionStream.WriteString(buttons.Name, streamOpen);
                    extensionStream.WriteInt(buttons.Keys.Count, streamOpen);
                    for (int i = 0; i < buttons.Keys.Count; i++)
                    {
                        extensionStream.WriteInt(buttons.Keys[i].Length, streamOpen);
                        for (int i2 = 0; i2 < buttons.Keys[i].Length; i2++)
                        {
                            extensionStream.WriteInt((int)buttons.Keys[i][i2].GetKeyCode(), streamOpen);
                        }
                    }
                }*/
            }


        }
    }

}