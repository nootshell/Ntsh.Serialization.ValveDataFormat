using System.Collections.Generic;

using Xunit;

using Ntsh.Serialization.AST;




namespace Ntsh.Serialization.ValveDataFormat.Tests {

	public class VdfAstNodeTests {

		[Fact]
		public void CommentNodeValid() {
			const string value = "This is my comment.";
			VdfAstCommentNode commentNode = new VdfAstCommentNode(value, true);
			Assert.Equal(VdfAstNodeType.Comment, commentNode.NodeType);
			Assert.Equal(value, commentNode.Value);
		}


		[Fact]
		public void KeyNodeValid() {
			const string value = "This is my key.";
			VdfAstKeyNode keyNode = new VdfAstKeyNode(value);
			Assert.Equal(VdfAstNodeType.Key, keyNode.NodeType);
			Assert.Equal(value, keyNode.Value);
		}


		[Fact]
		public void StringNodeValid() {
			const string value = "This is my string.";
			VdfAstStringNode stringNode = new VdfAstStringNode(value);
			Assert.Equal(VdfAstNodeType.String, stringNode.NodeType);
			Assert.Equal(value, stringNode.Value);
		}


		[Fact]
		public void PropertyNodeValid() {
			const string keyNodeValue = "my property key";
			const string valueNodeValue = "my property value";

			VdfAstPropertyNode propertyNode = new VdfAstPropertyNode(
				new VdfAstKeyNode(keyNodeValue),
				new VdfAstStringNode(valueNodeValue)
			);

			Assert.Equal(VdfAstNodeType.Property, propertyNode.NodeType);

			Assert.NotNull(propertyNode.Key);
			Assert.Equal(VdfAstNodeType.Key, propertyNode.Key.NodeType);
			Assert.Equal(keyNodeValue, propertyNode.Key.Value);

			Assert.NotNull(propertyNode.Value);
			Assert.True(propertyNode.Value is IAstStringValueNode);
			Assert.Equal(VdfAstNodeType.String, propertyNode.Value.NodeType);
			Assert.Equal(valueNodeValue, ((IAstStringValueNode)propertyNode.Value).Value);
		}




		[Fact]
		public void ObjectNodeValid() {
			const string keyNode1Value = "my first property key";
			const string valueNode1Value = "my first property value";

			const string keyNode2Value = "my second property key";
			const string valueNode2Value = "my second property value";

			const string commentNodeValue = "my random comment";

			const string keyNode3Value = "my third property key";
			const string keyNode4Value = "my fourth property key";
			const string valueNode4Value = "my fourth property value";

			VdfAstObjectNode objectNode = new VdfAstObjectNode();
			objectNode.Children.Add(new VdfAstPropertyNode(new VdfAstKeyNode(keyNode1Value), new VdfAstStringNode(valueNode1Value)));
			objectNode.Children.Add(new VdfAstPropertyNode(key: new VdfAstKeyNode(keyNode2Value), value: new VdfAstStringNode(valueNode2Value)));
			objectNode.Children.Add(new VdfAstCommentNode(commentNodeValue, false));
			objectNode.Children.Add(new VdfAstPropertyNode(new VdfAstKeyNode(keyNode3Value), new VdfAstObjectNode(new List<VdfAstNode>() {
				new VdfAstPropertyNode(new VdfAstKeyNode(keyNode4Value), new VdfAstStringNode(valueNode4Value))
			})));

			Assert.Equal(VdfAstNodeType.Object, objectNode.NodeType);

			// TODO: swap out VdfAstPropertyNode casts with IAstMappingNode once covariance has been added
			// TODO: swap out VdfAstObjectNode casts with IAstParentNode once covariance has been added
			Assert.Collection(objectNode.Children,
				node1 => {
					Assert.NotNull(node1);
					Assert.True(node1 is VdfAstPropertyNode);
					Assert.Equal(VdfAstNodeType.Property, node1.NodeType);

					VdfAstPropertyNode node1Prop = ((VdfAstPropertyNode)node1);

					Assert.NotNull(node1Prop.Key);
					Assert.Equal(VdfAstNodeType.Key, node1Prop.Key.NodeType);
					Assert.Equal(keyNode1Value, node1Prop.Key.Value);

					Assert.NotNull(node1Prop.Value);
					Assert.True(node1Prop.Value is IAstStringValueNode);
					Assert.Equal(VdfAstNodeType.String, node1Prop.Value.NodeType);
					Assert.Equal(valueNode1Value, ((IAstStringValueNode)node1Prop.Value).Value);
				},
				node2 => {
					Assert.NotNull(node2);
					Assert.True(node2 is VdfAstPropertyNode);
					Assert.Equal(VdfAstNodeType.Property, node2.NodeType);

					VdfAstPropertyNode node2Prop = ((VdfAstPropertyNode)node2);

					Assert.NotNull(node2Prop.Key);
					Assert.Equal(VdfAstNodeType.Key, node2Prop.Key.NodeType);
					Assert.Equal(keyNode2Value, node2Prop.Key.Value);

					Assert.NotNull(node2Prop.Value);
					Assert.True(node2Prop.Value is IAstStringValueNode);
					Assert.Equal(VdfAstNodeType.String, node2Prop.Value.NodeType);
					Assert.Equal(valueNode2Value, ((IAstStringValueNode)node2Prop.Value).Value);
				},
				commentNode => {
					Assert.NotNull(commentNode);
					Assert.True(commentNode is IAstStringValueNode);
					Assert.Equal(VdfAstNodeType.Comment, commentNode.NodeType);
					Assert.Equal(commentNodeValue, ((IAstStringValueNode)commentNode).Value);
				},
				node3 => {
					Assert.NotNull(node3);
					Assert.True(node3 is VdfAstPropertyNode);
					Assert.Equal(VdfAstNodeType.Property, node3.NodeType);

					VdfAstPropertyNode node3Prop = ((VdfAstPropertyNode)node3);

					Assert.NotNull(node3Prop.Key);
					Assert.Equal(VdfAstNodeType.Key, node3Prop.Key.NodeType);
					Assert.Equal(keyNode3Value, node3Prop.Key.Value);

					Assert.NotNull(node3Prop.Value);
					Assert.True(node3Prop.Value is VdfAstObjectNode);
					Assert.Equal(VdfAstNodeType.Object, node3Prop.Value.NodeType);
					Assert.Collection(((VdfAstObjectNode)node3Prop.Value).Children,
						node => {
							Assert.NotNull(node);
							Assert.True(node is VdfAstPropertyNode);
							Assert.Equal(VdfAstNodeType.Property, node.NodeType);

							VdfAstPropertyNode nodeProp = ((VdfAstPropertyNode)node);

							Assert.NotNull(nodeProp.Key);
							Assert.Equal(VdfAstNodeType.Key, nodeProp.Key.NodeType);
							Assert.Equal(keyNode4Value, nodeProp.Key.Value);

							Assert.NotNull(nodeProp.Value);
							Assert.True(nodeProp.Value is IAstStringValueNode);
							Assert.Equal(VdfAstNodeType.String, nodeProp.Value.NodeType);
							Assert.Equal(valueNode4Value, ((IAstStringValueNode)nodeProp.Value).Value);
						}
					);
				}
			);
		}

	}

}
