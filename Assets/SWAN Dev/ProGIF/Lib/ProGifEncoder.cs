using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

internal class ProGifEncoder
{
    public bool m_IsFirstFrame = true;
    public bool m_IsLastEncoder = false;

    protected int m_Width;
	protected int m_Height;
	protected int m_Repeat = -1;                  // -1: no repeat, 0: infinite, >0: repeat count
	protected int m_FrameDelay = 0;               // Frame delay (milliseconds)
	protected bool m_HasStarted = false;          // Ready to output frames
	
	protected Action<int> m_OnEncodingComplete;
	protected int m_EncoderIndex = 0;
    protected MemoryStream m_MemoryStream;
    public MemoryStream GetMemoryStream()
    {
        return m_MemoryStream;
    }

	protected Frame m_CurrentFrame;
	protected byte[] m_Pixels;                    // RGB byte array from frame
	protected byte[] m_PixelsAlpha;               // Alpha byte array from frame
	protected byte[] m_IndexedPixels;             // Converted frame indexed to palette
	protected int m_ColorDepth;                   // Number of bit planes
	protected byte[] m_ColorTab;                  // RGB palette
	//protected bool[] m_UsedEntry = new bool[256]; // Active palette entries
	protected int m_PaletteSize = 7;              // Color table size (bits-1)
	protected int m_DisposalCode = -1;            // Disposal code (-1 = use default)

	protected bool m_IsSizeSet = false;           // If false, get size from first frame
	protected int m_SampleInterval = 10;          // Default sample interval for quantizer

	/// <summary> Set equal to one of the index in the color table to hide that color. </summary>
	protected int m_TransparentColorIndex = 0;

	/// <summary> The flag indicating if transparency is enabled/disabled. 0(disable) or 1(enable). </summary>
	protected int m_TransparentFlag = 0;

	/// <summary> The transparent color to hide in the GIF. </summary>
	protected Color32 m_TransparentColor;

	/// <summary> If 'TRUE', check if any pixels' alpha value equal zero for supporting GIF transparent. </summary>
	public bool m_AutoTransparent = false;
	protected bool m_HasTransparentPixel = false;

    private bool _transparentByColor = false;
    private byte _transparentColorRange = 0;

    /// <summary>
    /// Default constructor. Repeat will be set to -1 and Quality to 10.
    /// </summary>
    public ProGifEncoder() : this(-1, 10, 0, null) {}

	/// <summary>
	/// Constructor with the number of times the set of GIF frames should be played.
	/// </summary>
	/// <param name="repeat">Default is -1 (no repeat); 0 means play indefinitely</param>
	/// <param name="quality">Sets quality of color quantization (conversion of images to
	/// the maximum 256 colors allowed by the GIF specification). Lower values (minimum = 1)
	/// produce better colors, but slow processing significantly. Higher values will speed
	/// up the quantization pass at the cost of lower image quality (maximum = 100).</param>
	public ProGifEncoder(int repeat, int quality, int encoderIndex, Action<int> OnEncodingComplete)
	{
		if (repeat >= -1) m_Repeat = repeat;
		m_SampleInterval = (int)Mathf.Clamp(quality, 1, 100);
		m_EncoderIndex = encoderIndex;
		m_OnEncodingComplete = OnEncodingComplete;
	}

	/// <summary>
	/// Sets the delay time between each frame, or changes it for subsequent frames (applies
	/// to last frame added).
	/// </summary>
	/// <param name="ms">Delay time in milliseconds</param>
	public void SetDelay(int ms)
	{
		m_FrameDelay = Mathf.RoundToInt(ms / 10f);
	}

	/// <summary>
	/// Sets frame rate in frames per second. Equivalent to <code>SetDelay(1000/fps)</code>.
	/// </summary>
	/// <param name="fps">Frame rate</param>
	public void SetFrameRate(float fps)
	{
		if (fps > 0f)
			m_FrameDelay = Mathf.RoundToInt(100f / fps);
	}

    /// <summary>
    /// Adds next GIF frame. The frame is not written immediately, but is actually deferred
    /// until the next frame is received so that timing data can be inserted. Invoking
    /// <code>Finish()</code> flushes all frames.
    /// </summary>
    /// <param name="frame">GifFrame containing frame to write.</param>
    public void AddFrame(Frame frame)
    {
        if ((frame == null))
            throw new ArgumentNullException("Can't add a null frame to the gif.");

        if (!m_HasStarted)
            throw new InvalidOperationException("Call Start() before adding frames to the gif.");

        // Use first frame's size
        if (!m_IsSizeSet) SetSize(frame.Width, frame.Height);

        //		#if UNITY_EDITOR
        //		SetTransparencyColor(new Color32(49, 77, 121, 255)); //Hard code test
        //		m_AutoTransparent = true;
        //		#endif

        m_CurrentFrame = frame;

        GetImagePixels();
        AnalyzePixels();

        if (m_IsFirstFrame)
        {
            WriteLSD();
            WritePalette();

            if (m_Repeat >= 0) 
                WriteNetscapeExt();
        }

        WriteGraphicCtrlExt();
        WriteImageDesc();

        if (!m_IsFirstFrame)
            WritePalette();

        WritePixels();
        m_IsFirstFrame = false;
    }

	/// <summary>
	/// Initiates GIF file creation on the given stream. The stream is not closed automatically.
	/// </summary>
	/// <param name="os">OutputStream on which GIF images are written</param>
	public void Start(MemoryStream os)
	{
		if (os == null) 
			throw new ArgumentNullException("Stream is null.");

		m_MemoryStream = os;

		try
		{
			if(m_EncoderIndex == 0)
				WriteString("GIF89a"); // header
		}
		catch (IOException e)
		{
			throw e;
		}

		m_HasStarted = true;
	}

	/// <summary>
	/// Initiates writing of a GIF file with the specified name. The stream will be handled for you.
	/// </summary>
	/// <param name="file">String containing output file name</param>
	public void Start()
	{
		try
		{
			m_MemoryStream = new MemoryStream();
			Start(m_MemoryStream);
			//m_ShouldCloseStream = true;
		}
		catch (IOException e)
		{
			throw e;
		}
	}

	/// <summary>
	/// Flushes any pending data and closes output file.
	/// If writing to an OutputStream, the stream is not closed.
	/// </summary>
	public void Finish()
	{
        if (!m_HasStarted)
        {
            throw new InvalidOperationException("Can't finish a non-started gif.");
        }
		m_HasStarted = false;

		try
		{
			if(m_IsLastEncoder)
			{
				m_MemoryStream.WriteByte(0x3b); // Gif trailer
			}

			m_MemoryStream.Flush();
		}
		catch (IOException e)
		{
            throw e;
		}

		// Reset for subsequent use
		//m_MemoryStream = null; //Dont reset memorystream.
		m_CurrentFrame = null;
		m_Pixels = null;
		m_PixelsAlpha = null;
		m_IndexedPixels = null;
		m_ColorTab = null;
		m_IsFirstFrame = true;

		if(m_OnEncodingComplete != null) m_OnEncodingComplete(m_EncoderIndex); 
	}

	// Sets the GIF frame size.
	protected void SetSize(int w, int h)
	{
		m_Width = w;
		m_Height = h;
		m_IsSizeSet = true;
	}

    // Extracts image pixels into byte array "pixels".
    protected void GetImagePixels()
    {
        int W = m_CurrentFrame.Width, H = m_CurrentFrame.Height;
        m_Pixels = new Byte[3 * W * H];
        Color32[] p = m_CurrentFrame.Data;
        int count = 0;
        int count_a = 0;

        if (m_AutoTransparent)
        {
            m_PixelsAlpha = new Byte[W * H];

            for (int th = H - 1; th >= 0; th--)
            {
                for (int tw = 0; tw < W; tw++)
                {
                    Color32 color = p[th * W + tw];
                    m_Pixels[count++] = color.r;
                    m_Pixels[count++] = color.g;
                    m_Pixels[count++] = color.b;
                    m_PixelsAlpha[count_a++] = color.a;
                    if (!m_HasTransparentPixel && color.a == 0)
                    {
                        m_HasTransparentPixel = true;
                        m_TransparentFlag = 1;
                        m_TransparentColorIndex = 255; // The 256th index of ColorTable reserved for auto detected transparent pixels
                    }
                }
            }
        }
        else
        {
            bool checkTransparentColor = m_TransparentColor.a > 0;
            byte tR = m_TransparentColor.r, tG = m_TransparentColor.g, tB = m_TransparentColor.b;

            if (checkTransparentColor)
            {
                m_PixelsAlpha = new Byte[W * H];

                for (int th = H - 1; th >= 0; th--)
                {
                    for (int tw = 0; tw < W; tw++)
                    {
                        Color32 color = p[th * W + tw];
                        m_Pixels[count++] = color.r;
                        m_Pixels[count++] = color.g;
                        m_Pixels[count++] = color.b;
                        if (Mathf.Abs(tR - color.r) <= _transparentColorRange && Mathf.Abs(tG - color.g) <= _transparentColorRange && Mathf.Abs(tB - color.b) <= _transparentColorRange)
                        {
                            count_a++;
                            if (!_transparentByColor)
                            {
                                _transparentByColor = true;
                                m_HasTransparentPixel = true;
                                m_TransparentFlag = 1;
                                m_TransparentColorIndex = 255; // The 256th index of ColorTable reserved for transparent pixels
                            }
                        }
                        else
                        {
                            m_PixelsAlpha[count_a++] = color.a;
                        }
                    }
                }
            }
            else
            {
                // Texture data is layered down-top, so flip it
                for (int th = H - 1; th >= 0; th--)
                {
                    for (int tw = 0; tw < W; tw++)
                    {
                        Color32 color = p[th * W + tw];
                        m_Pixels[count++] = color.r;
                        m_Pixels[count++] = color.g;
                        m_Pixels[count++] = color.b;
                    }
                }
            }
        }
    }

	// Analyzes image colors and creates color map.
	public void AnalyzePixels()
	{
		int len = m_Pixels.Length;
		int nPix = len / 3;
        m_IndexedPixels = new byte[nPix];
        
        ProGifNeuQuant nq = new ProGifNeuQuant(m_Pixels, len, m_SampleInterval, m_HasTransparentPixel);
        m_ColorTab = nq.Process(); // Create reduced palette

        // Map image pixels to new palette
        int k = 0, k0, k1, k2;
		bool chkTransparent = m_TransparentFlag == 1;
		byte tR = m_TransparentColor.r, tG = m_TransparentColor.g, tB = m_TransparentColor.b;
		byte b, g, r;
		for (int i = 0; i < nPix; i++)
		{
			k0 = k++; k1 = k++; k2 = k++;
			b = m_Pixels[k0]; g = m_Pixels[k1]; r = m_Pixels[k2];
			int index = nq.Map(b & 0xff, g & 0xff, r & 0xff);

            // Find m_TransparentColor in the Picture(m_Pixels), set the same Index as TransparencyColorIndex
            if (chkTransparent)
            {
                if ((m_HasTransparentPixel && m_PixelsAlpha[i] == 0) ||
                    (_transparentByColor && Mathf.Abs(tR - r) <= _transparentColorRange && Mathf.Abs(tG - g) <= _transparentColorRange && Mathf.Abs(tB - b) <= _transparentColorRange))
                {
                    index = m_TransparentColorIndex;
                }
            }

            //m_UsedEntry[index] = true;
            m_IndexedPixels[i] = (byte)index;
		}

		m_Pixels = null;
		m_PixelsAlpha = null;
		m_ColorDepth = 8;
        m_PaletteSize = 7;
	}

    public void SetTransparencyColor(Color32 c32, byte transparentColorRange)
	{
		m_TransparentColor = c32;
		//m_TransparentFlag = 1;
		m_AutoTransparent = false;

        _transparentColorRange = transparentColorRange;
    }

	// Writes Graphic Control Extension.
	protected void WriteGraphicCtrlExt()
	{
		m_MemoryStream.WriteByte(0x21); // Extension introducer
		m_MemoryStream.WriteByte(0xf9); // GCE label
		m_MemoryStream.WriteByte(4);    // Data block size

		// Packed fields
		m_MemoryStream.WriteByte(Convert.ToByte(0 |     // 1:3 reserved
			((m_TransparentFlag == 1)? 8:0) |           // 4:6 disposal (8 = 0b00001000)
			0 |                                         // 7   user input - 0 = none
			m_TransparentFlag));                        // 8   transparency flag

		WriteShort(m_FrameDelay);       // Delay x 1/100 sec

		m_MemoryStream.WriteByte(Convert.ToByte((m_TransparentColorIndex != -1) ? m_TransparentColorIndex : 0)); // Transparent color index

		m_MemoryStream.WriteByte(0);    // Block terminator
	}

	// Writes Image Descriptor.
	protected void WriteImageDesc()
	{
		m_MemoryStream.WriteByte(0x2c); // Image separator
		WriteShort(0);                  // Image position x,y = 0,0
		WriteShort(0);
		WriteShort(m_Width);            // image size
		WriteShort(m_Height);

		// Packed fields
		if (m_IsFirstFrame)
		{
			m_MemoryStream.WriteByte(0); // No LCT  - GCT is used for first (or only) frame
		}
		else
		{
			// Specify normal LCT
			m_MemoryStream.WriteByte(Convert.ToByte(0x80 |           // 1 local color table  1=yes
				0 |              // 2 interlace - 0=no
				0 |              // 3 sorted - 0=no
				0 |              // 4-5 reserved
				m_PaletteSize)); // 6-8 size of color table
		}
	}

	// Writes Logical Screen Descriptor.
	protected void WriteLSD()
	{
		// Logical screen size
		WriteShort(m_Width);
		WriteShort(m_Height);

		// Packed fields
		m_MemoryStream.WriteByte(Convert.ToByte(0x80 |           // 1   : global color table flag = 1 (gct used)
			0x70 |           // 2-4 : color resolution = 7
			0x00 |           // 5   : gct sort flag = 0
			m_PaletteSize)); // 6-8 : gct size

		m_MemoryStream.WriteByte(0); // Background color index

		m_MemoryStream.WriteByte(0); // Pixel aspect ratio - assume 1:1
	}

    // Writes Netscape extension to define repeat count.
    protected void WriteNetscapeExt()
    {
        m_MemoryStream.WriteByte(0x21);    // Extension introducer
        m_MemoryStream.WriteByte(0xff);    // App extension label
        m_MemoryStream.WriteByte(11);      // Block size
        WriteString("NETSCAPE" + "2.0");   // App id + auth code
        m_MemoryStream.WriteByte(3);       // Sub-block size
        m_MemoryStream.WriteByte(1);       // Loop sub-block id
        WriteShort(m_Repeat);              // Loop count (extra iterations, 0=repeat forever)
        m_MemoryStream.WriteByte(0);       // Block terminator
    }

    // Write color table.
    protected void WritePalette()
	{
		m_MemoryStream.Write(m_ColorTab, 0, m_ColorTab.Length);
		int n = (3 * 256) - m_ColorTab.Length;

		for (int i = 0; i < n; i++)
			m_MemoryStream.WriteByte(0);
	}

	// Encodes and writes pixel data.
	protected void WritePixels()
	{
		ProGifLzwEncoder encoder = new ProGifLzwEncoder(m_Width, m_Height, m_IndexedPixels, m_ColorDepth);
		encoder.Encode(m_MemoryStream);
	}

	// Write 16-bit value to output stream, LSB first.
	protected void WriteShort(int value)
	{
		m_MemoryStream.WriteByte(Convert.ToByte(value & 0xff));
		m_MemoryStream.WriteByte(Convert.ToByte((value >> 8) & 0xff));
	}

	// Writes string to output stream.
	protected void WriteString(String s)
	{
		char[] chars = s.ToCharArray();

		for (int i = 0; i < chars.Length; i++)
			m_MemoryStream.WriteByte((byte)chars[i]);
	}
}

internal class Frame
{
	public int Width;
	public int Height;
	public Color32[] Data;
}
