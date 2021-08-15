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
        public ShaderPatch(string[] pre, string[] post, bool replace)
        {
            Pre = pre;
            Post = post;
            Replace = replace;
        }

        public string[] Pre { get; set; }
        public string[] Post { get; set; }
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
                    int i = 0;
                    foreach (var patch in patchfile)
                    {
                        string shaderName = patch.ShaderName;
                        var shader = api.Shader.GetProgramByName(shaderName) ?? api.Render.GetEngineShader((EnumShaderProgram)Enum.Parse(typeof(EnumShaderProgram), shaderName, true));
                        PatchShader(EnumShaderType.FragmentShader, patch, shader.FragmentShader);
                        PatchShader(EnumShaderType.VertexShader, patch, shader.VertexShader);
                        if (shader.Compile())
                        {
                            api.Logger.Notification(string.Format("Successfully patched shader {0} from shader patch file {1} with the index of {2}.", shaderName, val.Name, i));
                        }
                        else
                        {
                            api.Logger.Notification(string.Format("Failed patching shader {0} from shader patch file {1} with the index of {2}.", shaderName, val.Name, i));
                        }
                        i++;
                    }
                }
                return true;
            };
        }

        public void PatchShader(EnumShaderType shaderType, ShaderPatchFile patch, IShader shader)
        {
            Dictionary<string, ShaderPatch> shaderpatches;
            switch (shaderType)
            {
                case EnumShaderType.FragmentShader:
                    shaderpatches = patch.Fragment;
                    break;
                case EnumShaderType.VertexShader:
                    shaderpatches = patch.Vertex;
                    break;
                default:
                    shaderpatches = patch.Fragment;
                    break;
            }

            foreach (var shaderpatchfile in shaderpatches)
            {
                string code = shader.Code;
                int functionIndex = code.IndexOf(shaderpatchfile.Key + "(");
                string preCode = null;
                string postCode = null;

                if (shaderpatchfile.Value.Pre != null)
                {
                    foreach (var cmd in shaderpatchfile.Value.Pre)
                    {
                        preCode += '\t' + cmd + "\r\n";
                    }
                }

                if (shaderpatchfile.Value.Post != null)
                {
                    foreach (var cmd in shaderpatchfile.Value.Post)
                    {
                        postCode += '\t' + cmd + "\r\n";
                    }
                }
                preCode = preCode ?? "";
                postCode = postCode ?? "";

                if (shaderpatchfile.Value.Replace)
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
                    shader.Code = shader.Code.Replace(func, preCode + postCode);
                }
                else
                {
                    while (functionIndex < code.Length && code[functionIndex] != '{') functionIndex++;
                    shader.Code = code = code.Insert(functionIndex, preCode);

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
                    shader.Code = code.Insert(functionIndex, postCode);
                }
            }
        }
    }
}
