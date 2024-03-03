using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ntsh.Serialization.AST;

using Xunit;




namespace Ntsh.Serialization.ValveDataFormat.Tests {

	public class VdfAstNodeReaderTests {

		readonly Encoding encoding = Encoding.UTF8;
		readonly int timeout = 150;

		private MemoryStream GetMemoryStreamForLine(string line, Encoding encoding) {
			MemoryStream ms = new MemoryStream();

			byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(encoding.GetMaxByteCount(line.Length));
			try {
				int written = encoding.GetBytes(line, 0, line.Length, rentedBuffer, 0);
				ms.Write(rentedBuffer, 0, written);

				ms.Seek(0, SeekOrigin.Begin);
				return ms;
			} finally {
				ArrayPool<byte>.Shared.Return(rentedBuffer);
			}
		}


		private async Task<VdfAstNode?> GetNodeForLineAsync(string line) {
			using VdfAstNodeReader reader = new VdfAstNodeReader(
				this.GetMemoryStreamForLine(line, this.encoding),
				this.encoding,
				leaveOpen: false
			);

			using CancellationTokenSource cts = new CancellationTokenSource();
			cts.CancelAfter(this.timeout);
			return await reader.ReadNodeAsync(cts.Token);
		}




		[Theory]
		[InlineData("glorious comment", "// glorious comment")]
		[InlineData("// double comment 1", "//// double comment 1")]
		[InlineData("// double comment 2", "// // double comment 2")]
		[InlineData("// double comment 3", "//   // double comment 3")]
		[InlineData("// strange double // comment //whoaoa//ah what's this", "\t\n\t// // strange double // comment //whoaoa//ah what's this\r\n")]
		public async Task ReadsCommentsProperlyAsync(string expected, string given) {
			VdfAstNode? node = await this.GetNodeForLineAsync(given);
			Assert.NotNull(node);
			Assert.Equal(VdfAstNodeType.Comment, node.NodeType);
			Assert.True(node is IAstStringValueNode);
			Assert.Equal(expected, ((IAstStringValueNode)node).Value);
		}


		[Theory]
		[InlineData("my key", "my value", "\"my key\"  \"my value\"")]
		[InlineData("myKey", "my value!", "myKey\n\t\"my value!\"")]
		public async Task ReadsStringPropertyNodesProperlyAsync(string expectedKey, string expectedValue, string given) {
			VdfAstNode? node = await this.GetNodeForLineAsync(given);
			Assert.NotNull(node);
			Assert.Equal(VdfAstNodeType.Property, node.NodeType);
			Assert.True(node is VdfAstPropertyNode);

			VdfAstPropertyNode propertyNode = ((VdfAstPropertyNode)node);

			Assert.NotNull(propertyNode.Key);
			Assert.Equal(VdfAstNodeType.Key, propertyNode.Key.NodeType);
			Assert.Equal(expectedKey, propertyNode.Key.Value);

			Assert.NotNull(propertyNode.Value);
			Assert.Equal(VdfAstNodeType.String, propertyNode.Value.NodeType);
			Assert.True(propertyNode.Value is IAstStringValueNode);
			Assert.Equal(expectedValue, ((IAstStringValueNode)propertyNode.Value).Value);
		}

	}

}
