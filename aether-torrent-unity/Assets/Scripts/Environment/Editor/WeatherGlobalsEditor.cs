using UnityEditor;
using UnityEngine.UIElements;

namespace Environment.Editor
{
	[CustomEditor(typeof(WeatherGlobals))]
	public class WeatherGlobalsEditor : UnityEditor.Editor
	{
		public VisualTreeAsset visualTree;
		public override VisualElement CreateInspectorGUI()
		{
			// Create a new VisualElement to be the root of our inspector UI
			VisualElement myInspector = new VisualElement();

			// Add a simple label
			myInspector.Add(new Label("This is a custom inspector"));

			// Load and clone a visual tree from UXML
			visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Weather/Editor/Weather_Globals_UXML.uxml");
			visualTree.CloneTree(myInspector);

			// Return the finished inspector UI
			return myInspector;
		}
	}
}