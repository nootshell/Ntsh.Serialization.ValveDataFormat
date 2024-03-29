using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ntsh.Threading;




namespace Ntsh.Serialization.ValveDataFormat {

	public class VdfAstNodeReader : VdfAstNodeProcessorBase {

		public const int DefaultBufferSize = 512;

		protected readonly Decoder decoder;
		protected readonly StringBuilder builder = new StringBuilder(32);

		protected readonly char[] buffer;
		protected int bufferOffset;
		protected int bufferLength = 0;

		protected enum DecState {
			None,
			String,
			Comment
		}
		DecState state = DecState.None;




		public int CurrentLine { get; protected set; } = 0;

		public int CharacterInLine { get; protected set; } = 0;




		public VdfAstNodeReader(Stream stream, Encoding encoding, int bufferSize = DefaultBufferSize, bool leaveOpen = false) : base(stream, encoding, leaveOpen) {
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

			this.decoder = encoding.GetDecoder();
			this.buffer = new char[bufferSize];
			this.bufferOffset = bufferSize;
		}

		public VdfAstNodeReader(Stream stream, int bufferSize = DefaultBufferSize, bool leaveOpen = false) : this(stream, Encoding.UTF8, bufferSize, leaveOpen) { }




		protected Exception GetUnexpectedEndOfStreamException()
			=> new IOException("Unexpected end of stream.");


		protected Exception GetUnexpectedCharacterException()
			=> new InvalidOperationException("Unexpected character.");




		protected virtual async Task<char?> PollNextCharAsync(CancellationToken token) {
			if (this.bufferOffset >= this.bufferLength) {
				int maxByteCount = this.encoding.GetMaxByteCount(buffer.Length);
				byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
				try {
					int read = await this.stream.ReadAsync(rentedBuffer, 0, maxByteCount, token).Configure();

					this.bufferLength = this.decoder.GetChars(rentedBuffer, 0, read, this.buffer, 0, flush: false);
					if (this.bufferLength <= 0) {
						return null;
					}
				} finally {
					ArrayPool<byte>.Shared.Return(rentedBuffer);
				}

				this.bufferOffset = 0;
			}

			return this.buffer[this.bufferOffset];
		}


		protected virtual void AdvanceOffsets(char responsibleCharacter) {
			++this.bufferOffset;

			if (responsibleCharacter == '\n') {
				++this.CurrentLine;
				this.CharacterInLine = 0;
			} else {
				++this.CharacterInLine;
			}
		}


		protected virtual async Task<char?> NextCharAsync(CancellationToken token) {
			char? maybeNextChar = await this.PollNextCharAsync(token).Configure();

			if (maybeNextChar is char nextChar) {
				this.AdvanceOffsets(nextChar);
			}

			return maybeNextChar;
		}




		protected virtual void ChangeState(DecState state) {
			this.state = state;
		}


		protected virtual TValue ResetAndReturn<TValue>(TValue value, DecState state = DecState.None) where TValue : notnull {
			this.ChangeState(state);
			return value;
		}




		protected virtual VdfAstCommentNode ConstructCommentNode(bool ownLine) {
			/* Trim whitespace from end */
			while (this.builder.Length > 1 && char.IsWhiteSpace(this.builder[this.builder.Length - 1])) {
				--this.builder.Length;
			}
			string value = this.builder.ToString();

			/* Reset builder */
			this.builder.Length = 0;

			return new VdfAstCommentNode(value, ownLine);
		}


		protected virtual async Task<VdfAstCommentNode> ReadCommentNodeAsync(bool ownLine, CancellationToken token) {
			for ( ;;) {
				if (await this.NextCharAsync(token).Configure() is not char c) {
					if (this.state != DecState.Comment) {
						throw this.GetUnexpectedEndOfStreamException();
					}

					return this.ResetAndReturn(this.ConstructCommentNode(ownLine));
				}

				switch (this.state) {
					case DecState.None:
						if (await this.NextCharAsync(token).Configure() is not char nc) {
							throw this.GetUnexpectedEndOfStreamException();
						}

						if (c == '/' && nc == '/') {
							this.ChangeState(DecState.Comment);
						} else {
							throw this.GetUnexpectedCharacterException();
						}
						break;

					case DecState.Comment:
						if (c == '\r' || c == '\n') {
							/* Comments are terminated by EOL */
							return this.ResetAndReturn(this.ConstructCommentNode(ownLine));
						}

						if (this.builder.Length == 0 && char.IsWhiteSpace(c)) {
							/* Skip leading whitespace */
							continue;
						}

						this.builder.Append(c);
						break;

					default:
						throw new InvalidOperationException("Invalid decoder state.");
				}
			}
		}




		protected virtual string ConstructStringBasedNodeValue() {
			string value = this.builder.ToString();
			this.builder.Length = 0;
			return value;
		}


		protected virtual async Task<string?> ReadStringBasedNodeValueAsync(bool nullIfEndOfStream, CancellationToken token) {
			bool quoted = false;
			bool escape = false;
			for ( ;;) {
				if (await this.NextCharAsync(token).Configure() is not char c) {
					if (quoted) {
						/* Quoted strings always expect an end quote */
						throw this.GetUnexpectedEndOfStreamException();
					}

					if (nullIfEndOfStream) {
						return null;
					}

					return this.ResetAndReturn(this.ConstructStringBasedNodeValue());
				}

				switch (this.state) {
					case DecState.None:
						if (char.IsWhiteSpace(c)) {
							throw this.GetUnexpectedCharacterException();
						} else if (c == '"') {
							quoted = true;
						} else {
							/* First character is already part of the value */
							this.builder.Append(c);
						}
						this.ChangeState(DecState.String);
						break;

					case DecState.String:
						/* If we're quoted and find an unescaped end quote, or we're not quoted and we find unescaped whitespace: return. */
						if ((quoted && !escape && c == '"') || (!quoted && !escape && char.IsWhiteSpace(c))) {
							return this.ResetAndReturn(this.ConstructStringBasedNodeValue());
						}

						this.builder.Append(c);

						if (c == '\\') {
							escape = !escape;
						} else {
							escape = false;
						}
						break;

					default:
						throw new InvalidOperationException("Invalid decoder state.");
				}
			}
		}


		protected virtual async Task<VdfAstKeyNode> ReadKeyNodeAsync(CancellationToken token) {
			string? value = await this.ReadStringBasedNodeValueAsync(nullIfEndOfStream: true, token).Configure();
			if (value == null) {
				throw this.GetUnexpectedEndOfStreamException();
			}

			return new VdfAstKeyNode(value);
		}


		protected virtual async Task<VdfAstStringNode> ReadStringNodeAsync(CancellationToken token) {
			string? value = await this.ReadStringBasedNodeValueAsync(nullIfEndOfStream: false, token).Configure();
			if (value == null) {
				throw this.GetUnexpectedEndOfStreamException();
			}

			return new VdfAstStringNode(value);
		}




		protected virtual async Task<VdfAstObjectNode> ReadObjectNodeAsync(CancellationToken token) {
			VdfAstObjectNode node = new VdfAstObjectNode();

			bool foundNewLine = false;
			VdfAstNode child;
			for ( ;;) {
				if (await this.PollNextCharAsync(token).Configure() is not char nextChar) {
					throw this.GetUnexpectedEndOfStreamException();
				}

				if (nextChar == '{' || char.IsWhiteSpace(nextChar)) {
					if (nextChar == '\n') {
						foundNewLine = true;
					}
					this.AdvanceOffsets(nextChar);
					continue;
				}

				if (nextChar == '}') {
					this.AdvanceOffsets(nextChar);
					return node;
				}

				if (nextChar == '/') {
					child = await this.ReadCommentNodeAsync(foundNewLine, token).Configure();
				} else {
					child = await this.ReadPropertyNodeAsync(token).Configure();
				}
				node.Children.Add(child);
				foundNewLine = false;
			}
		}




		protected virtual async Task<VdfAstPropertyNode> ReadPropertyNodeAsync(CancellationToken token) {
			VdfAstKeyNode? key = null;
			VdfAstNode? value = null;
			do {
				if (await this.PollNextCharAsync(token).Configure() is not char nextChar) {
					throw this.GetUnexpectedEndOfStreamException();
				}

				/* Skip any whitespace we find between the key and the value */
				if (char.IsWhiteSpace(nextChar)) {
					this.AdvanceOffsets(nextChar);
					continue;
				}

				switch (nextChar) {
					case '{' when (key != null):
						value = await this.ReadObjectNodeAsync(token).Configure();
						break;

					case '"':
					case char when (char.IsLetter(nextChar)):
						if (key == null) {
							key = await this.ReadKeyNodeAsync(token).Configure();
						} else {
							value = await this.ReadStringNodeAsync(token).Configure();
						}
						break;
				}
			} while (key == null || value == null);

			return new VdfAstPropertyNode(key, value);
		}




		public async Task<VdfAstNode?> ReadNodeAsync(CancellationToken token) {
			bool foundNewLine = false;
			for ( ;;) {
				if (this.state != DecState.None) {
					throw new InvalidOperationException("Reader is in an unexpected state.");
				}

				if (await this.PollNextCharAsync(token).Configure() is not char nextChar) {
					return null;
				}

				if (char.IsWhiteSpace(nextChar)) {
					if (nextChar == '\n') {
						foundNewLine = true;
					}
					this.AdvanceOffsets(nextChar);
					continue;
				}

				switch (nextChar) {
					case '/':
						return await this.ReadCommentNodeAsync(foundNewLine, token).Configure();

					case '"':
					case char when (char.IsLetter(nextChar)):
						return await this.ReadPropertyNodeAsync(token).Configure();
				}

				foundNewLine = false;
			}
		}




		protected override void DisposeCore(bool disposing) {
			base.DisposeCore(disposing);

			if (disposing && this.decoder is IDisposable disposableDecoder) {
				disposableDecoder.Dispose();
			}
		}

	}

}
