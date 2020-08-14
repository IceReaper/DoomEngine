namespace DoomEngine.Utils
{
	using System.IO;

	public class SegmentStream : Stream
	{
		private readonly Stream stream;
		private readonly int position;
		private readonly int length;

		public override bool CanRead => this.stream.CanRead;
		public override bool CanSeek => this.stream.CanSeek;
		public override bool CanWrite => this.stream.CanWrite;
		public override long Length => this.length;

		public override long Position { get; set; }

		public SegmentStream(Stream stream, int position, int length)
		{
			this.stream = stream;
			this.position = position;
			this.length = length;
		}

		public override void Flush()
		{
			this.stream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			this.stream.Position = this.position + this.Position;
			this.Position += count;

			return this.stream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return origin switch
			{
				SeekOrigin.Begin => this.Position = offset,
				SeekOrigin.Current => this.Position += offset,
				SeekOrigin.End => this.Position = this.length + offset,
				_ => this.Position
			};
		}

		public override void SetLength(long value)
		{
			throw new System.NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			this.stream.Position = this.position + this.Position;
			this.Position += count;
			this.stream.Write(buffer, offset, count);
		}
	}
}
