using System.IO;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace Sandbox2D.Graphics;

public class Texture
{
    public readonly int Handle;
    
    /// <summary>
    /// Loads a texture from a file.
    /// </summary>
    /// <param name="path">the image to load</param>
    /// <param name="options">[optional] the options for the new texture</param>
    public Texture(string path, TextureOptions? options = null)
    {
        var imageStream = File.OpenRead(path);
        Handle = CreateTexture(imageStream, options ?? new TextureOptions());
        imageStream.Dispose();
    }
    
    /// <summary>
    /// Loads a texture from an image stream.
    /// </summary>
    /// <param name="imageStream">a stream containing the image data</param>
    /// <param name="options">[optional] the options for the new texture</param>
    public Texture(Stream imageStream, TextureOptions? options = null)
    {
        Handle = CreateTexture(imageStream, options ?? new TextureOptions());
    }
    
    private static int CreateTexture(Stream imageStream, TextureOptions options)
    {
        // generate the texture
        var handle = GL.GenTexture();
        
        // bind the handle
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, handle);
        
        // flip the image vertically on load
        StbImage.stbi_set_flip_vertically_on_load(1);
        
        // load the image        
        var image = ImageResult.FromStream(imageStream, ColorComponents.RedGreenBlueAlpha);
        
        // create the texture
        GL.TexImage2D(TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba,
            image.Width, image.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            image.Data);
        
        options.Apply(handle);
        
        // generate mipmaps
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        
        GL.BindTexture(TextureTarget.Texture2D, 0);
        
        return handle;
    }

    public void Use(TextureUnit unit)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }
}

/// <summary>
/// Represents a set of options that can be applied to a texture
/// </summary>
public readonly struct TextureOptions
{
    private readonly TextureWrapMode _wrapModeX = TextureWrapMode.ClampToBorder;
    private readonly TextureWrapMode _wrapModeY = TextureWrapMode.ClampToBorder;
    private readonly float[] _borderColor = [1.0f, 1.0f, 0.0f, 0.0f];
    
    private readonly TextureMinFilter _minFilter = TextureMinFilter.Nearest;
    private readonly TextureMagFilter _magFilter = TextureMagFilter.Nearest;
    
    public TextureOptions() { }
    
    public TextureOptions(TextureWrapMode wrapModeX, TextureWrapMode wrapModeY, float[] borderColor,
        TextureMinFilter minFilter, TextureMagFilter magFilter)
    {
        _wrapModeX = wrapModeX;
        _wrapModeY = wrapModeY;
        _borderColor = borderColor;
        _minFilter = minFilter;
        _magFilter = magFilter;
    }
    
    public void Apply(Texture texture)
    {
        Apply(texture.Handle);
    }
    
    public void Apply(int handle)
    {
        // bind the texture
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, handle);
        
        // set wrapping mode
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)_wrapModeX);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)_wrapModeY);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, _borderColor);
        
        // set the min/mag filters
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)_minFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)_magFilter);
    }
}