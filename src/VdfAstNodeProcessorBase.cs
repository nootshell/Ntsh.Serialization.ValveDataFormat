using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ntsh.Threading;




namespace Ntsh.Serialization.ValveDataFormat {

	public abstract class VdfAstNodeProcessorBase : IDisposable {

		protected readonly Encoding encoding;
		protected readonly Stream stream;
		protected readonly bool leaveOpen;




		protected VdfAstNodeProcessorBase(Stream stream, Encoding encoding, bool leaveOpen) : base() {
			ArgumentNullException.ThrowIfNull(stream);
			ArgumentNullException.ThrowIfNull(encoding);

			this.encoding = encoding;
			this.stream = stream;
			this.leaveOpen = leaveOpen;
		}

		~VdfAstNodeProcessorBase() {
			this.Dispose(disposing: false);
		}




		protected virtual void DisposeCore(bool disposing) {
			if (disposing && !this.leaveOpen) {
				this.stream.Dispose();
			}
		}


		private bool disposed = false;

		protected void Dispose(bool disposing) {
			if (this.disposed) {
				return;
			}
			this.disposed = true;

			this.DisposeCore(disposing);
		}

		public void Dispose() {
			this.Dispose(disposing: false);
			GC.SuppressFinalize(this);
		}

	}

}
