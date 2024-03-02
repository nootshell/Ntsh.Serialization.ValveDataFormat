using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Ntsh.Serialization.AST;



namespace Ntsh.Serialization.ValveDataFormat {

	public sealed class VdfAstKeyNode : VdfAstNode, IAstStringValueNode {

		public string Value { get; }




		public VdfAstKeyNode(string value) : base(VdfAstNodeType.Key) {
			this.Value = value;
		}




		public override string ToString()
			=> $"{nameof(VdfAstKeyNode)} [\"{this.Value}\"]";

	}

}
