using System;
using System.Collections.Generic;
using System.IO;
using Core.ContentSerializer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UnityEditor
{
    [CreateAssetMenu(menuName = "Tools/Replace Tesselation Data")]
    public class ReplaceTesselationData : ScriptableObject
    {
        [Sirenix.OdinInspector.FilePath(AbsolutePath = false, Extensions = ".shader"), SerializeField] private string shaderPath;
        [Sirenix.OdinInspector.FilePath(AbsolutePath = false), SerializeField] private string outputPath;
        
        private const string subShaderWord = "SubShader";
        private const string shaderWord = "Shader";
        private const string passWord = "Pass";
        private const string openBrace = "{";
        private const string closeBrace = "}";
        private const string nameWord = "Name";
         
        [SerializeField] private Shader main;
        
        [Serializable]
        public abstract class Body
        {
            public string name;
            [SerializeReference] public Body parent;
            [ShowInInspector, SerializeReference] public List<Body> children;
            public int startIndex;
            public int endIndex;

            public Body(Body parent, string name)
            {
                this.parent = parent;
                children = new List<Body>();
                this.name = name;
            }

            public abstract Body OnNewString(string s, string prev, int index);
        }

        [Serializable]
        private class Pass : Body
        {
            private int level = 1;
            public Pass(Body parent) : base(parent, null)
            {
            }

            public override Body OnNewString(string s, string prev, int index)
            {
                if (s.EndsWith(closeBrace) && !s.Contains(openBrace) && --level == 0)
                {
                    endIndex = index;
                    return parent;
                }
                if(s.EndsWith(openBrace))
                {
                    level++;
                }
                
                int nameIndex = s.IndexOf(nameWord, StringComparison.Ordinal);
                if (nameIndex > 0 && name == null)
                {
                    startIndex = index - 1;
                    name = s.Substring(nameIndex + nameWord.Length + 1).Replace("\"", "");
                }
                return this;
            }
        }
        [Serializable]
        private class SubShader : Body
        {
            private int level = 1;
            public SubShader(Body parent, string name) : base(parent, name)
            {
            }

            public override Body OnNewString(string s, string prev, int index)
            {
                if (s.EndsWith(closeBrace) && !s.Contains(openBrace) && --level == 0)
                {
                    endIndex = index;
                    return parent;
                }

                if (s.EndsWith(openBrace))
                {
                    level++;
                    if (prev.EndsWith(passWord))
                    {
                        startIndex = index;
                        var sub = new Pass(this);
                        children.Add(sub);
                        return sub;
                    }
                }
                return this;
            }
        }
        
        [Serializable]
        private class Shader : Body
        {
            private int level = 1;
            public Shader() : base(null, null)
            {
            }

            public override Body OnNewString(string s, string prev, int index)
            {
                if (s.EndsWith(closeBrace) && !s.Contains(openBrace) && --level == 0)
                {
                    endIndex = index;
                    return parent;
                }

                if (s.EndsWith(openBrace))
                {
                    level++;
                    if (prev.StartsWith(shaderWord))
                    {
                        name = prev.Substring(shaderWord.Length + 1).Replace("\"", "");
                        startIndex = index;
                    }
                    if (prev.EndsWith(subShaderWord))
                    {
                        var sub = new SubShader(this, prev.Substring(subShaderWord.Length + 1).Replace("\"", ""));
                        children.Add(sub);
                        return sub;
                    }
                }
                return this;
            }
        }
        
        [Button]
        private void ReadSource()
        {
            main = new Shader();
            Body current = main;
            string prevString = "";
            int i = 1;
            foreach (string readLine in File.ReadLines(shaderPath))
            {
                current = current.OnNewString(readLine, prevString, i++);
                if (current == null)
                {
                    break;
                }
                prevString = readLine;
            }
        }
    }
}