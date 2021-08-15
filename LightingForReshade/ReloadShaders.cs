using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using OpenTK.Graphics.OpenGL;
using Vintagestory.Client.NoObf;

namespace LightingForReshade
{
    public class ReloadShaders : ModSystem
    {
        //public RenderToNewDepthBuffer DepthBufferRenderer { get; private set; }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () =>
            {
                //DepthBufferRenderer = new RenderToNewDepthBuffer(api);
                api.Shader.ReloadShaders();
            };
        }
    }

    public class ShaderPatch
    {
        public ShaderPatch(string pre, string post, bool replace)
        {
            Pre = pre;
            Post = post;
            Replace = replace;
        }

        public string Pre { get; set; }
        public string Post { get; set; }
        public bool Replace { get; set; }
    }

    public class ShaderPatchFile
    {
        public ShaderPatchFile(string shaderName, Dictionary<string, ShaderPatch> fragment, Dictionary<string, ShaderPatch> vertex)
        {
            Fragment = fragment;
            Vertex = vertex;
            ShaderName = shaderName;
        }

        public string ShaderName { get; set; }
        public Dictionary<string, ShaderPatch> Fragment { get; set; }
        public Dictionary<string, ShaderPatch> Vertex { get; set; }
    }

    //additive shader patching
    public class ShaderPatcher : ModSystem
    {
        public static AssetCategory shaderpatches = new AssetCategory("shaderpatches", false, EnumAppSide.Client);
        
        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () =>
            {
                api.Shader.ReloadShaders();
            };

            api.Event.ReloadShader += () =>
            {

                foreach (var val in api.Assets.GetMany("shaderpatches"))
                {
                    var patchfile = val.ToObject<ShaderPatchFile[]>();
                    foreach (var patch in patchfile)
                    {
                        string shaderName = patch.ShaderName;
                        var shader = api.Shader.GetProgramByName(shaderName) ?? api.Render.GetEngineShader((EnumShaderProgram)Enum.Parse(typeof(EnumShaderProgram), shaderName, true));

                        foreach (var fragpatch in patch.Fragment)
                        {
                            string code = shader.FragmentShader.Code;
                            int functionIndex = code.IndexOf(fragpatch.Key + "()");

                            if (fragpatch.Value.Replace)
                            {
                                while (functionIndex < code.Length && code[functionIndex] != '{') functionIndex++;
                                functionIndex++;

                                int functionLength = 0;

                                while (functionIndex + functionLength < code.Length && code[functionIndex + functionLength] != '}')
                                {
                                    //one deep for now
                                    if (code[functionIndex + functionLength] == '/' && code[functionIndex + functionLength + 1] == '/')
                                    {
                                        while (functionIndex + functionLength < code.Length && code[functionIndex + functionLength] != '\n') functionLength++;
                                    }

                                    if (code[functionIndex + functionLength] == '/' && code[functionIndex + functionLength + 1] == '*')
                                    {
                                        while (functionIndex + functionLength < code.Length && code[functionIndex + functionLength] != '*' && code[functionIndex + functionLength] != '/') functionLength++;
                                    }

                                    if (code[functionIndex + functionLength] == '{')
                                    {
                                        while (functionIndex + functionLength < code.Length && code[functionIndex + functionLength] != '}') functionLength++;
                                    }
                                    functionLength++;
                                }

                                string func = code.Substring(functionIndex, functionLength);
                                code = shader.FragmentShader.Code.Replace(func, fragpatch.Value.Pre + fragpatch.Value.Post);
                            }
                            else
                            {
                                while (functionIndex < code.Length && code[functionIndex] != '{') functionIndex++;
                                shader.FragmentShader.Code = code = code.Insert(functionIndex, fragpatch.Value.Pre);

                                while (functionIndex < code.Length && code[functionIndex] != '}')
                                {
                                    //one deep for now
                                    if (code[functionIndex] == '/' && code[functionIndex + 1] == '/')
                                    {
                                        while (functionIndex < code.Length && code[functionIndex] != '\n') functionIndex++;
                                    }

                                    if (code[functionIndex] == '/' && code[functionIndex + 1] == '*')
                                    {
                                        while (functionIndex < code.Length && code[functionIndex] != '*' && code[functionIndex] != '/') functionIndex++;
                                    }

                                    if (code[functionIndex] == '{')
                                    {
                                        while (functionIndex < code.Length && code[functionIndex] != '}') functionIndex++;
                                    }
                                    functionIndex++;
                                }
                                shader.FragmentShader.Code = code = code.Insert(functionIndex, fragpatch.Value.Post);
                            }
                        }

                        if (shader.Compile())
                        {

                        }
                    }
                }
                return true;
            };
        }
    }
}
