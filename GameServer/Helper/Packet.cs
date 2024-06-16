using System;
using System.Text;

public class Packet
{
    public byte[] buffer { get; private set; }
    public int position { get; private set; }
    public int size { get; private set; }

    public const int buffersize = 4096;

    public Packet()
    {
        this.buffer = new byte[buffersize];
    }

    public byte pop_byte()
    {
        byte data = this.buffer[this.position];
        this.position += sizeof(byte);
        return data;
    }

    public Int16 pop_int16()
    {
        Int16 data = BitConverter.ToInt16(this.buffer, this.position);
        this.position += sizeof(Int16);
        return data;
    }

    public Int32 pop_int32()
    {
        Int32 data = BitConverter.ToInt32(this.buffer, this.position);
        this.position += sizeof(Int32);
        return data;
    }

    public string pop_string()
    {
        // 문자열 길이는 최대 2바이트 까지. 0 ~ 32767
        Int16 len = BitConverter.ToInt16(this.buffer, this.position);
        this.position += sizeof(Int16);

        // 인코딩은 utf8로 통일한다.
        string data = System.Text.Encoding.UTF8.GetString(this.buffer, this.position, len);
        this.position += len;

        return data;
    }

    public float pop_float()
    {
        float data = BitConverter.ToSingle(this.buffer, this.position);
        this.position += sizeof(float);
        return data;
    }
    public void push_int16(Int16 data)
    {
        byte[] temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(this.buffer, this.position);
        this.position += temp_buffer.Length;
    }

    public void push(byte data)
    {
        byte[] temp_buffer = BitConverter.GetBytes((short)data);
        temp_buffer.CopyTo(this.buffer, this.position);
        this.position += sizeof(byte);
    }

    public void push(Int16 data)
    {
        byte[] temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(this.buffer, this.position);
        this.position += temp_buffer.Length;
    }

    public void push(Int32 data)
    {
        byte[] temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(this.buffer, this.position);
        this.position += temp_buffer.Length;
    }
    public void push(string data)
    {
        byte[] temp_buffer = Encoding.UTF8.GetBytes(data);
        temp_buffer.CopyTo(this.buffer, this.position);
        this.position += temp_buffer.Length;
    }

    public void push(float data)
    {
        byte[] temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(this.buffer, this.position);
        this.position += temp_buffer.Length;
    }
    public void push(long data)
    {
        byte[] temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(this.buffer, this.position);
        this.position += temp_buffer.Length;
    }
    public void push(byte[] data)
    {
        data.CopyTo(this.buffer, this.position);
        this.position += data.Length;
        this.size += data.Length;
    }
}
