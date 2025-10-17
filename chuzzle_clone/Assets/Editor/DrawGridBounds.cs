using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(game_control))]
public class DrawGridBounds : Editor {
	void OnSceneGUI() {
		game_control GameControl = target as game_control;

		if (GameControl == null) return;

		else {

			float right_point = GameControl.grid_width * GameControl.grid_spacing;
			float up_point = GameControl.grid_height * GameControl.grid_spacing;

			float spacing_half = GameControl.grid_spacing / 2;

			//    c______d
			//    |      |
			//    |      |
			//    |      |
			//   a|______|b

			Vector3 x = GameControl.grid_spawn_transform.position - new Vector3(spacing_half, spacing_half, 0);
			Vector3 a = x;
			Vector3 b = x + new Vector3(right_point, 0, 0);
			Vector3 c = x + new Vector3(0, up_point, 0);
			Vector3 d = x + new Vector3(right_point, up_point, 0);
			Handles.color = Color.red;
			Handles.DrawLine(a, b);
			Handles.DrawLine(a, c);
			Handles.DrawLine(b, d);
			Handles.DrawLine(c, d);
			Handles.DrawLine(a, d);
			Handles.DrawLine(c, b);
		}
	}

}
