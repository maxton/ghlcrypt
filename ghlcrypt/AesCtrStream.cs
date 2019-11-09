using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace GHLCrypt
{
  public class AesCtrStream : Stream
  {
    Stream s;
    long offset;
    long position;
    AesManaged aes;
    byte[] initialIv;
    byte[] counter;
    byte[] cryptedCounter = new byte[16];
    public AesCtrStream(Stream input, byte[] key, byte[] iv, long offset = 0)
    {
      s = input;
      initialIv = (byte[])iv.Clone();
      counter = (byte[])iv.Clone();
      this.offset = offset;
      aes = new AesManaged()
      {
        Mode = CipherMode.ECB,
        BlockSize = 128,
        KeySize = 128,
        Padding = PaddingMode.None,
        Key = key,
      };
    }

    private void resetCounter()
    {
      Buffer.BlockCopy(initialIv, 0, counter, 0, 16);
      var block = position / 16;
      for(long i = 0; i < block; i++)
      {
        IncrementCounter();
      }
    }

    private void IncrementCounter()
    {
      for (int j = 0; j < 16; j++)
      {
        counter[j]++;
        if (counter[j] != 0)
          break;
      }
    }

    public override bool CanRead => position < Length;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => s.Length - offset;

    public override long Position { get => position; set { position = value; resetCounter(); } }

    public override void Flush()
    {
      throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int bufOffset, int count)
    {
      if (position + count > Length)
      {
        count = (int)(Length - position);
      }

      s.Position = position + offset;
      int bytesRead = s.Read(buffer, bufOffset, count);

      // Create a decrytor to perform the stream transform.
      ICryptoTransform encryptor = aes.CreateEncryptor();
      int counterLoc = (int)(position % 16);
      encryptor.TransformBlock(counter, 0, counter.Length, cryptedCounter, 0);
      for (int i = 0; i < bytesRead; i++)
      {
        if (position != 0 && position % 16 == 0)
        {
          IncrementCounter();
          counterLoc = 0;
          encryptor.TransformBlock(counter, 0, counter.Length, cryptedCounter, 0);
        }
        buffer[bufOffset++] ^= cryptedCounter[counterLoc++]; //decrypt one byte
        position++;
      }

      return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      switch (origin)
      {
        case SeekOrigin.Begin:
          Position = offset;
          break;
        case SeekOrigin.Current:
          Position += offset;
          break;
        case SeekOrigin.End:
          Position = Length + offset;
          break;
        default:
          break;
      }
      return position;
    }

    public override void SetLength(long value)
    {
      throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new NotImplementedException();
    }
  }
}
