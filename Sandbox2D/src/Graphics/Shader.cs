using System;
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
            var vertexSource = File.ReadAllText($"{GlobalValues.AssetDirectory}/shaders/{vertPath}");
            GL.ShaderSource(vertexShader, vertexSource);
            
            // compile the vertex shader
            GL.CompileShader(vertexShader);

            // create fragment shader
            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            
            // bind the fragment shader source
            var fragmentSource = File.ReadAllText($"{GlobalValues.AssetDirectory}/shaders/{fragPath}");
            GL.ShaderSource(fragmentShader, fragmentSource);
            
            // compile the fragment shader
            GL.CompileShader(fragmentShader);

            // create the shader program
            Handle = GL.CreateProgram();

            // attach the vertex and fragment shaders
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);

            // link them together
            GL.LinkProgram(Handle);
            
            // detach and delete the old vertex/fragment shaders
            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);
            
            // get amount of active uniforms
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);
            
            // allocate the dictionary to hold the locations
            _uniformLocations = new Dictionary<string, int>();
            
            // for each uniform,
            for (var i = 0; i < numberOfUniforms; i++)
            {
                // get the name and location
                var name = GL.GetActiveUniform(Handle, i, out _, out _);
                var location = GL.GetUniformLocation(Handle, name);
                
                // and add them to the dictionary
                _uniformLocations.Add(name, location);
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
        /// Set a uniform float on this shader.
        /// </summary>
        /// <param name="name">the name of the uniform</param>
        /// <param name="data">the data to set</param>
        public void SetUInt(string name, uint data)
        {
            var location = GetUniformLocation(name);
            if (location == -1) return;
            
            GL.UseProgram(Handle);
            GL.Uniform1(location, data);
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