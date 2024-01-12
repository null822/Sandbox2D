using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Sandbox2D.Graphics
{
    public class Shader
    {
        public readonly int Handle;

        private readonly Dictionary<string, int> _uniformLocations;

        
        public Shader(string vertPath, string fragPath)
        {
            // create vertex shader
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);

            // bind the vertex shader source
            var vertexSource = File.ReadAllText("assets/shaders/" + vertPath);
            GL.ShaderSource(vertexShader, vertexSource);

            // compile the vertex shader
            CompileShader(vertexShader);

            // create fragment shader
            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            
            // bind the fragment shader source
            var fragmentSource = File.ReadAllText("assets/shaders/" + fragPath);
            GL.ShaderSource(fragmentShader, fragmentSource);
            
            // compile the fragment shader
            CompileShader(fragmentShader);

            // create the shader program
            Handle = GL.CreateProgram();

            // attach the vertex and fragment shaders
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);

            // link them together
            LinkProgram(Handle);

            // detach and delete the old vertex/fragment shaders
            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            
            // get amount of active uniforms
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            // next, allocate the dictionary to hold the locations.
            _uniformLocations = new Dictionary<string, int>();
            
            // for each uniform,
            for (var i = 0; i < numberOfUniforms; i++)
            {
                // get the name
                var key = GL.GetActiveUniform(Handle, i, out _, out _);
                
                // and location
                var location = GL.GetUniformLocation(Handle, key);
                
                // and add them to the dictionary.
                _uniformLocations.Add(key, location);
            }
        }

        private static void CompileShader(int shader)
        {
            // compile the shader
            GL.CompileShader(shader);

            // check for compilation errors
            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                // if there was an error, log it
                var infoLog = GL.GetShaderInfoLog(shader);
                Util.Error($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
            }
        }

        private static void LinkProgram(int program)
        {
            // link the program
            GL.LinkProgram(program);

            // check for linking errors
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                // if there was an error, log it
                Util.Error($"Error occurred whilst linking Program({program})");
            }
        }

        
        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }

        private int GetUniformLocation(string name)
        {
            if (_uniformLocations.TryGetValue(name, out var location))
            {
                return location;
            }
            
            Util.Error($"Uniform \'{name}\' is not present in the shader");
            return -1;
            
        }

        
        /// <summary>
        /// Set a uniform int on this shader.
        /// </summary>
        /// <param name="name">the name of the uniform</param>
        /// <param name="data">the data to set</param>
        public void SetInt(string name, int data)
        {
            var location = GetUniformLocation(name);
            if (location == -1) return;
            
            GL.UseProgram(Handle);
            GL.Uniform1(location, data);
        }

        /// <summary>
        /// Set a uniform float on this shader.
        /// </summary>
        /// <param name="name">the name of the uniform</param>
        /// <param name="data">the data to set</param>
        public void SetFloat(string name, float data)
        {
            var location = GetUniformLocation(name);
            if (location == -1) return;
            
            GL.UseProgram(Handle);
            GL.Uniform1(location, data);
        }

        /// <summary>
        /// Set a uniform Matrix4 on this shader
        /// </summary>
        /// <param name="name">the name of the uniform</param>
        /// <param name="data">the data to set</param>
        /// <remarks>
        /// The matrix is transposed before being sent to the shader.
        /// </remarks>
        public void SetMatrix4(string name, Matrix4 data)
        {
            var location = GetUniformLocation(name);
            if (location == -1) return;
            
            GL.UseProgram(Handle);
            GL.UniformMatrix4(location, true, ref data);
        }

        /// <summary>
        /// Set a uniform Vector2 on this shader.
        /// </summary>
        /// <param name="name">the name of the uniform</param>
        /// <param name="data">the data to set</param>
        public void SetVector2(string name, Vector2 data)
        {
            var location = GetUniformLocation(name);
            if (location == -1) return;
            
            GL.UseProgram(Handle);
            GL.Uniform2(location, data);
        }
        
        /// <summary>
        /// Set a uniform Vector3 on this shader.
        /// </summary>
        /// <param name="name">the name of the uniform</param>
        /// <param name="data">the data to set</param>
        public void SetVector3(string name, Vector3 data)
        {
            var location = GetUniformLocation(name);
            if (location == -1) return;
            
            GL.UseProgram(Handle);
            GL.Uniform3(location, data);
        }
    }
}