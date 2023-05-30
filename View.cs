using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL;

namespace tomogram_visualizer
{
    internal class View
    {
        Bitmap textureImage;
        int VBOtexture; //хранит номер текстуры в памяти видеокарты

        public void SetupView(int width, int height) //настраивает окно вывода
        {
            GL.ShadeModel(ShadingModel.Smooth); //интерполирование цветов
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, Bin.X, 0, Bin.Y, -1, 1);
            GL.Viewport(0, 0, width, height);
        }

        /*TF - функция перевода значения плотностей томограммы в цвет.
        Диапазон визуализируемых плотностей называется окном визуализации. В
        нашем случае мы хотим, чтобы TF переводила плотности окна визуализации от
        0 до 2000 линейно в цвет от черного до белого (от 0 до 255).
        */
        Color TransferFunction(short value, int _min, int width)
        {
            int min = _min;
            int max = _min + width;
            int newVal = clamp((value - min) * 255 / (max - min), 0, 255);
            return Color.FromArgb(255, newVal, newVal, newVal);
        }

        public int clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public void DrawQuads(int layerNumber,int min, int width) //layerNumber(номер визуализируемого слоя)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            short value;

            for (int y_coord = 0; y_coord < Bin.Y - 1; ++y_coord)
            {
                GL.Begin(BeginMode.QuadStrip);
                // вершина 1
                value = Bin.array[0 + (y_coord) * Bin.X + layerNumber * Bin.X * Bin.Y];
                GL.Color3(TransferFunction(value, min, width));
                GL.Vertex2(0, y_coord);
                //вершина 2
                value = Bin.array[0 + (y_coord + 1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                GL.Color3(TransferFunction(value, min, width));
                GL.Vertex2(0, y_coord + 1);

                for (int x_coord = 0; x_coord < Bin.X * 2; ++x_coord)
                {
                    GL.Begin(BeginMode.QuadStrip);
                    //вершина 3
                    value = Bin.array[x_coord + 1 + (y_coord + 1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value, min, width));
                    GL.Vertex2(x_coord + 1, y_coord + 1);
                    //вершина 4
                    value = Bin.array[x_coord + 1 + y_coord * Bin.X + layerNumber * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value, min, width));
                    GL.Vertex2(x_coord + 1, y_coord);
                }
            }
            GL.End();
        }//Перечисление вершин четырехугольника происходит против часовой стрелки

        /*Функция GenTextures генерирует уникальный номер текстуры,
        функция BindTexture связывает текстуру, делает ее активной, а также
        указывает ее тип, функция TexImage2D загружает текстуру в память
        видеокарты.*/
        public void Load2DTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, VBOtexture);
            BitmapData data = textureImage.LockBits(
                new System.Drawing.Rectangle(0, 0, textureImage.Width, textureImage.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte, data.Scan0);

            textureImage.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);

            ErrorCode Er = GL.GetError();
            string str = Er.ToString();
        }

        public void generateTextureImage(int layerNumber, int min, int width)
        {
            textureImage = new Bitmap(Bin.X, Bin.Y);
            for (int i = 0; i < Bin.X; i++)
            {
                for (int j = 0; j < Bin.Y; j++)
                {
                    int pixelNumber = i + j * Bin.X + layerNumber * Bin.Y * Bin.X;
                    textureImage.SetPixel(i,j, TransferFunction(Bin.array[pixelNumber], min, width));
                }
            }
        }

        public void DrawTexture()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, VBOtexture);

            GL.Begin(BeginMode.Quads);
            GL.Color3(Color.White);

            GL.TexCoord2(0f, 0f);
            GL.Vertex2(0, 0);

            GL.TexCoord2(0f, 1f);
            GL.Vertex2(0, Bin.Y);

            GL.TexCoord2(1f, 1f);
            GL.Vertex2(Bin.X, Bin.Y);

            GL.TexCoord2(1f, 0f);
            GL.Vertex2(Bin.X, 0);

            GL.End();

            GL.Disable(EnableCap.Texture2D);
        }

    } 
}
