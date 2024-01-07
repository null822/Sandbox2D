using System.IO;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace Sandbox2D.Graphics;

public class Texture(int glHandle)
{
    public readonly int Handle = glHandle;

    /// <summary>
    /// Loads a texture from an image file
    /// </summary>
    /// <param name="path">The path to the image file, relative to `assets/textures`</param>
    public static Texture LoadFromFile(string path)
    {
        // Generate empty texture/handle
        var handle = GL.GenTexture();

        // Bind the handle
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, handle);


        // flip the image vertically on load
        StbImage.stbi_set_flip_vertically_on_load(1);
            
        // open a stream to the image, and load it
        using (Stream stream = File.OpenRead("assets/textures/" + path))
        {
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                
            // create the texture
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
        }
        
        
        // set the min/mag filters
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // set wrapping mode
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        // generate mipmaps
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        return new Texture(handle);
    }

    public void Use(TextureUnit unit)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }
}