using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VolumetricFogAndMist
{
	[CustomEditor (typeof(FogOfWarHole))]
	public class FogOfWarHoleEditor : Editor
	{
		
		public override void OnInspectorGUI ()
		{
			EditorGUILayout.Separator ();
			EditorGUILayout.HelpBox ("Use the transform to position/scale the hole. Customize global fog of war properties in Volumetric Fog script attached to the camera or the fog script specified below.", MessageType.Info);
			DrawDefaultInspector ();
		}

	}

}
