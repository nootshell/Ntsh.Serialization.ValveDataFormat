using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Ntsh.Serialization.AST;



namespace Ntsh.Serialization.ValveDataFormat {

	public sealed class VdfAstPropertyNode : VdfAstNode, IAstMappingNode<VdfAstKeyNode, VdfAstNode> {

		public VdfAstKeyNode Key { get; }

		public VdfAstNode Value { get; }




		public VdfAstPropertyNode(VdfAstKeyNode key, VdfAstNode value) : base(VdfAstNodeType.Property) {
			this.Key = key;
			this.Value = value;
		}

	}

}
