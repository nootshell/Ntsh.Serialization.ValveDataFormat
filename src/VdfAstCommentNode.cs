using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Ntsh.Serialization.AST;



namespace Ntsh.Serialization.ValveDataFormat {

	public sealed class VdfAstCommentNode : VdfAstNode, IAstStringValueNode {

		private bool OwnLine { get; } // TODO: add logic to writer

		public string Value { get; }





		public VdfAstCommentNode(string value, bool ownLine) : base(VdfAstNodeType.Comment) {
			this.Value = value;
			this.OwnLine = ownLine;
		}

	}

}
