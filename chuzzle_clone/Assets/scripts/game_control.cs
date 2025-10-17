using UnityEngine;
using System.Collections;

public class game_control : MonoBehaviour {
	public Transform grid_spawn_transform;
	public int grid_width = 5;
	public int grid_height = 7;
	public float grid_spacing = 1f;
	public static float _grid_spacing;
	public GameObject test_ball;
	public static Vector3 mouse_position_when_clicked;
	public static bool dragging_balls_active = false;
	public static Vector3 drag_offset;
	public static Vector3 direction_to_move_balls = Vector3.one;	//MNOZI SE SA DISTANCE OFFSETOM
	public static bool start_dragging_balls = false;
	public static GameObject clicked_ball = null;
	public static string debug_snapping_string;

	public static bool calculating_direction_active = false;
	public enum direction { none, horizontal, vertical }
	public static direction moving_direction = direction.none;

	// 网格数据结构
	public static GameObject[,] grid;
	public static int _grid_width;
	public static int _grid_height;
	public static Transform _grid_spawn_transform;
	public static GameObject balls_container;  // 小球容器
	public static GameObject shadows_container;  // 影子副本容器

	void Start() {
		_grid_spacing = grid_spacing;
		_grid_width = grid_width;
		_grid_height = grid_height;
		_grid_spawn_transform = grid_spawn_transform;
		grid = new GameObject[grid_width, grid_height];
		
		// 创建小球容器文件夹
		balls_container = new GameObject("Balls");
		shadows_container = new GameObject("Shadows");
		
		generate_matrix();
	}

	void generate_matrix() {
		for (int i = 0; i < grid_width; i++) {
			for (int j = 0; j < grid_height; j++) {
				GameObject generic_ball = new GameObject();
				int ball_index = (i * grid_width + j);
				generic_ball.name = "ball_" + ball_index.ToString();
				generic_ball.AddComponent<ball>().create_ball(i, j, ball_index);
				generic_ball.transform.position = new Vector3(i * grid_spacing, j * grid_spacing, 0) + grid_spawn_transform.position;
				
				// 设置父物体为容器
				generic_ball.transform.SetParent(balls_container.transform);
				
				grid[i, j] = generic_ball;  // 存入网格数组
			}
		}
	}

	//GET ALL BALLS ARRAY
	public static GameObject[] all_balls() {
		return GameObject.FindGameObjectsWithTag("ball");
	}

	//RETURN MOUSE WORLD POSITION
	public static Vector3 mouse_position() {
		Vector3 converted_mouse_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		converted_mouse_pos.z = -1;

		return converted_mouse_pos;
	}

	//GET BALL COMPONENT OF GAMEOBJECT
	public static ball get_ball(GameObject g) {
		return g.GetComponent<ball>();
	}

	//SET BALL_IS_MOVABLE PARAMETER
	public static void ball_is_movable(GameObject ball, bool is_movable) {
		game_control.get_ball(ball).is_moving = is_movable;
	}

	//GET BALL_IS_MOVABLE PARAMETER
	public static bool ball_is_movable(GameObject ball) {
		return game_control.get_ball(ball).is_moving;
	}

	//STORE WORLD MOUSE POSITION WHEN MOUSE IS CLICKED
	public static void store_mouse_position_when_clicked() {
		mouse_position_when_clicked = game_control.mouse_position();
	}

	//CALCULATE DIRECTION TO MOVE BALLS
	void calculate_direction() {
		if (drag_offset.x != 0 || drag_offset.y != 0) {
			calculating_direction_active = false;
			if (Mathf.Abs(drag_offset.x) > Mathf.Abs(drag_offset.y)) {	//ako je x vece
				game_control.moving_direction = direction.horizontal;
			}
			else if (Mathf.Abs(drag_offset.x) < Mathf.Abs(drag_offset.y)) { //ako je y vece
				game_control.moving_direction = direction.vertical;
			}
			start_dragging_balls = true;
		}
	}

	void Update() {

		//RESET DRAG OFFSET
		if (Input.GetMouseButtonUp(0)) {
			drag_offset = Vector3.zero;
		}
		//CALCULATE DRAG OFFSET
		if (Input.GetMouseButton(0)) {
			float distance_x = game_control.mouse_position().x - mouse_position_when_clicked.x;
			float distance_y = game_control.mouse_position().y - mouse_position_when_clicked.y;
			//print("X:" + distance_x + " Y:" + distance_y);
			drag_offset = new Vector3(distance_x, distance_y, 0);
		}

		//CALCULATE DIRECTION
		if (calculating_direction_active) {
			calculate_direction();
		}
	}


	//DEBUG SCREEN
	void OnGUI() {
		int w = Screen.width, h = Screen.height;
		GUIStyle style = new GUIStyle();
		Rect rect = new Rect(0, 0, w, h * 2 / 100);
		style.alignment = TextAnchor.UpperLeft;
		style.fontSize = h * 2 / 100;
		style.normal.textColor = Color.green;
		string clicked_ball_name ="";
		if (clicked_ball != null) {
			clicked_ball_name = clicked_ball.name;
		}
		//STUFF
		string text = "Mouse Position:" + mouse_position() +
						"\nMouse Pos WHEN CLICKED:" + mouse_position_when_clicked +
						"\nDrag Offset: \n\tX:" + drag_offset.x + "\n\tY:" + drag_offset.y +
						"\nCalulating_direction_active:" + calculating_direction_active +
						"\nMoving_direction:" + moving_direction.ToString() +
						"\nClicked Ball:" + clicked_ball_name +
						"\nSnap to: " + debug_snapping_string;

		//

		GUI.Label(rect, text, style);
	}


}
