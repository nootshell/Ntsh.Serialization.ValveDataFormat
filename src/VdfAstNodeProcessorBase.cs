using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ntsh.Threading;




namespace Ntsh.Serialization.ValveDataFormat {

	public abstract class VdfAstNodeProcessorBase : IDisposable {

		protected readonly Stream underlyingStream;
		protected readonly bool ownsUnderlyingStream;




		protected VdfAstNodeProcessorBase(Stream underlyingStream, bool ownsUnderlyingStream) : base() {
			this.underlyingStream = underlyingStream;
			this.ownsUnderlyingStream = ownsUnderlyingStream;
		}

		~VdfAstNodeProcessorBase() {
			this.Dispose(disposing: false);
		}




		protected virtual void DisposeCore(bool disposing) {
			if (disposing && this.ownsUnderlyingStream) {
				this.underlyingStream.Dispose();
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
