using Ntsh.Serialization.AST;




namespace Ntsh.Serialization.ValveDataFormat {

	public abstract class VdfAstNode : AstNode<VdfAstNodeType> {

		protected VdfAstNode(VdfAstNodeType nodeType) : base(nodeType) { }

	}

}
