using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Ntsh.Serialization.AST;



namespace Ntsh.Serialization.ValveDataFormat {

	public sealed class VdfAstObjectNode : VdfAstNode, IAstParentNode<VdfAstNode> {

		public ICollection<VdfAstNode> Children { get; }




		public VdfAstObjectNode(ICollection<VdfAstNode> children) : base(VdfAstNodeType.Object) {
			this.Children = children;
		}

		public VdfAstObjectNode() : this(new List<VdfAstNode>()) { }

	}

}
